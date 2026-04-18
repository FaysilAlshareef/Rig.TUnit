# Research: Provider Consistency Remediation

**Feature ID**: 004-provider-consistency-remediation
**Generated**: 2026-04-18

Evidence-backed decisions driving the implementation plan. Cites issues/PRs where public trackers exist.

---

## R1 — Testcontainers version pin (C-001)

**Current state:** `Directory.Packages.props` pins every `Testcontainers.*` package at `4.6.0`. Planning doc asks for `4.11+` for MySql/Oracle/Cosmos.

**Decision:** Bump the whole family to `4.11.x` as Phase 1 commit 1.

**Evidence:**
- `ManagePackageVersionsCentrally = true` + `CentralPackageTransitivePinningEnabled = true` in `Directory.Packages.props` — mixed versions would force transitive pins to the higher version anyway, producing obscure restore warnings.
- `Testcontainers` 4.6 → 4.11 is all in-major (4.x) — no breaking API changes per the project's changelog cadence.
- The 219-test baseline from 003 exercises every existing Testcontainers-backed fixture; bumping first gives us an immediate signal before any provider work.

**Rollback:** Single-line revert in `Directory.Packages.props` if a regression surfaces.

---

## R2 — Pomelo MySQL driver on .NET 10

**Current state:** `Directory.Packages.props` already pins `Pomelo.EntityFrameworkCore.MySql` to `9.0.0`. No `MySqlConnector` pin yet.

**Decision:** Keep Pomelo at `9.0.*` for this feature. Add `MySqlConnector 2.4.*` pin. Cite PR #2019 in a props-file comment.

**Evidence:**
- [Pomelo PR #2019](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019) — tracks the official EF Core 10 / .NET 10 release. Not yet merged as of 2026-04-18.
- EF Core 9 packages are forward-compatible with .NET 10 TFM for test-harness scenarios — no DI or runtime surface change broke between EF Core 9 and 10 that affects a Testcontainers fixture.
- Community fork (Microting) exists but is not the maintained line — only a fallback if Pomelo 10 still isn't out at feature merge time **and** we hit a blocker.

**Upgrade path:** When Pomelo 10 ships, the bump is a single-line change in `Directory.Packages.props` — no code change in `Rig.TUnit.Databases.Sql.MySql`.

---

## R3 — Oracle driver & container on .NET 10

**Current state:** `Oracle.EntityFrameworkCore` pinned to `10.0.0`. Testcontainers.Oracle at 4.6 (bumping to 4.11 per R1).

**Decision:** Use image `gvenzl/oracle-free:23.5-slim-faststart` with `WithWaitStrategy(Wait.ForListeningPorts())` + 5-minute startup timeout.

**Evidence:**
- [aspire#12036](https://github.com/dotnet/aspire/issues/12036) — intermittent Oracle container init hangs reported on .NET 10. Workaround: listening-ports wait + generous timeout.
- Oracle-XE image is unmaintained by Oracle's container team. `oracle-free` is the current upstream equivalent.
- `faststart` flavour reduces cold-boot from ~4 minutes to ~90 seconds on developer machines.

**Fallback plan:** If `oracle-free:23.5-slim-faststart` starts flaking in CI, pin to `23.4-slim-faststart` (last known stable) — documented in `OracleFixture` class-level comment.

---

## R4 — Cosmos Linux emulator readiness

**Current state:** `Microsoft.Azure.Cosmos` pinned to `3.44.0`. No Cosmos Testcontainers wrapper is currently in `Directory.Packages.props`.

**Decision:** Use `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` with a custom wait strategy probing `https://localhost:{port}/_explorer/emulator.pem` with a `ServerCertificateCustomValidationCallback` trust-all.

**Evidence:**
- [testcontainers-dotnet#1306](https://github.com/testcontainers/testcontainers-dotnet/discussions/1306) — known readiness issue with the Cosmos emulator; the default `Wait.ForHttp` returns false positives.
- The `/_explorer/emulator.pem` endpoint is the most reliable signal of "emulator fully booted" — Microsoft's own CI pipelines use it.
- Linux emulator is Linux-only; Windows CI runners cannot run it. Integration tests gated behind `[Category("cosmos")]` + runtime skip on Windows.

**Container image tag lock:** `vnext-preview` is the Microsoft-recommended channel for .NET 10-era tests. If it proves unstable, pin to the dated variant (e.g., `vnext-preview-2025-10-15`) and track drift monthly.

**Package-choice note (added post-analysis 2026-04-18):** `Testcontainers.CosmosDb` (already pinned at 4.6.0 in `Directory.Packages.props`) targets the legacy **Windows** emulator — its `CosmosDbBuilder` hard-codes the Windows image path and default wait strategy, incompatible with vNext Linux. Use **`Testcontainers.GenericContainer`** from the base `Testcontainers` package instead. After Phase 4 lands, remove the `Testcontainers.CosmosDb 4.6.0` pin if no production code references it (tracked as T200 in the reserved range).

---

## R5 — AppInsights in-process capture

**Decision:** No container. Implement `AppInsightsFixture` as an in-process capture via a custom `ITelemetryChannel` that records every `ITelemetry` item in a thread-safe concurrent queue.

**Evidence:**
- There is no official Microsoft AppInsights container.
- The `Microsoft.ApplicationInsights` 2.22+ API exposes `ITelemetryChannel` as a public extension point — trivial to substitute.
- `AppInsightsAssert` mirrors `TraceAssert` / `MetricAssert` so contributors see a uniform assertion surface.

**Shape:**

```csharp
internal sealed class CapturingTelemetryChannel : ITelemetryChannel
{
    public ConcurrentQueue<ITelemetry> Captured { get; } = new();
    public bool? DeveloperMode { get; set; }
    public string? EndpointAddress { get; set; }
    public void Send(ITelemetry item) => Captured.Enqueue(item);
    public void Flush() { }
    public void Dispose() { }
}
```

`AppInsightsFixture.Captured` exposes a read-only view; `AppInsightsAssert.Traces(fixture).Contains(...)` chains fluently.

---

## R6 — Docker package compose backend

**Decision:** Primary backend is `Testcontainers` 4.11+ native compose. Fallback to `Ductus.FluentDocker` only if compose regresses on .NET 10.

**Evidence:**
- `Testcontainers` 4.x exposes `ICompositeService` / `DockerComposeContainer` — covers the multi-container topology case the library needs.
- `Ductus.FluentDocker` is the historical fallback but pulls a different docker-client stack; adding it is a dependency-surface cost.
- Decision is deferred to implementation: first spike the primary backend, only pivot if `docker compose up` with 4.11 fails on the `.NET 10` runtime image used in CI.

**Activation criteria for fallback** (documented in `Rig.TUnit.Docker/README.md`):
- Testcontainers' compose API throws on .NET 10 test host.
- Multi-container health-check orchestration is unreliable (>1/10 flake rate).

---

## R7 — Architecture-test strategy (`NetArchTest` vs custom scan)

**Decision:** Use **`NetArchTest.Rules 1.3.2`** (already pinned) for `ProviderCompletenessTests` type-level checks; use plain filesystem walks for `TestFileOrganizationTests` and `ReadmeCompletenessTests`.

**Evidence:**
- `NetArchTest.Rules` fluent API is ideal for "type X in assembly Y must derive from Z and have method W" — exactly what `ProviderCompletenessTests` needs.
- Filesystem walks are simpler for file-scoped invariants (one top-level class, README size) — `NetArchTest` would require loading syntax trees, not worth the complexity.
- `CodeOrganizationTests.cs` (already present) uses a mix of assembly scanning + `StringComparer` checks — the new rules should match that idiom for reviewer continuity.

**Scanner helper:** `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` already exists and returns all `Rig.TUnit.*` source assemblies. The new rules reuse it.

---

## R8 — Security base package already exists

**Evidence (2026-04-18 file inventory):**

```
src/Rig.TUnit.Security/
├── Rig.TUnit.Security.csproj
├── Contracts/ISecurityRig.cs
├── Fixtures/SecurityFixtureBase.cs
├── Builder/SecurityRigBuilder.cs   (CRTP — abstract SecurityRigBuilder<TSelf>)
└── Assertions/
```

**Implication:** The planning gap matrix is stale on this row. The Security base is **present**. Phase 3 does NOT create it; Phase 3 wires the existing Jwt / OAuth / Mtls / Policies packages to derive their RigBuilders from the existing base.

---

## R9 — `Rig.TUnit.Docker` already present (partial)

**Evidence:**

```
src/Rig.TUnit.Docker/
├── Rig.TUnit.Docker.csproj
└── Fixtures/ContainerFixture.cs
```

**Implication:** Not a new package — **template completion**. Phase 4e adds `DockerFixtureOptions`, `DockerRigBuilder`, `UseDocker` extension, `DockerComposeFixture`, `README.md`, Tests.Integration.

---

## R10 — Test hygiene pattern reference

**Reference project:** `tests/Rig.TUnit.Grpc.Tests.Unit/` — already complies with the `TestInfrastructure/` discipline:

```
tests/Rig.TUnit.Grpc.Tests.Unit/
├── Builder/              (production-mirroring fluent-API tests)
├── Extensions/
├── Helpers/
├── Protos/               (test .proto files)
├── TestInfrastructure/   (fakes, harnesses, test entities)
└── *.Tests.Unit.csproj
```

Phase 2 sweeps target projects into this shape. Worst offenders listed in Phase 2 of plan.md.

---

## R11 — Central package management + transitive pinning

**Current state:** `Directory.Packages.props` has `ManagePackageVersionsCentrally = true` and `CentralPackageTransitivePinningEnabled = true`.

**Implication for this feature:**
- New packages (MySql/Oracle/Cosmos/AppInsights) add ZERO `<Version>` attributes on `<PackageReference>` — all pins live in `Directory.Packages.props`.
- Docker compose fallback, if activated, adds `Ductus.FluentDocker` pin in `Directory.Packages.props` — never inline.
- Testcontainers family bump (C-001) is a single-point edit in `Directory.Packages.props`.

---

## R12 — Known stale planning-doc items

Planning doc (verified stale 2026-04-18):

| Planning claim | Reality | Spec handling |
|---|---|---|
| "5 packages 003 promised but never created" | `Rig.TUnit.Docker` has a fixture already; 4 are truly absent | Spec treats Docker as "complete the template" (US5 scenario 5, FR-017) |
| "`Rig.TUnit.Security` base does not exist" | Base + CRTP builder both present | Spec notes planning matrix is stale; wires existing base (US4 scenarios 11-14) |
| "Testcontainers 4.11+" already pinned | Pinned at 4.6.0 | C-001 bumps family to 4.11.x in Phase 1 |

These corrections are baked into spec.md's "Observed deltas" block and plan.md's Phase 1 commit list.

---

## R13 — TDD signal: per-phase RED/GREEN ratio

**Target cadence** (from 003 plan, carried forward):

For every N production files added in a phase:
- N RED commits (failing tests) MUST appear first in `git log`.
- N GREEN commits MUST follow.
- Up to N REFACTOR commits MAY appear, tests stay green.

Reviewers verify by running `git log --oneline --grep='— RED'` + `— GREEN` on the feature branch. A production class with no matching RED commit blocks PR.

**Tooling:** no additional tooling required — commit message discipline is enforced by PR template + reviewer.

---

## R14 — Coverage tooling (added post-analysis 2026-04-18)

**Current state:** repo has `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageRuleTests.cs` — a **name-based whitelist check** that verifies every public type is referenced by a test assembly. It is NOT a line/branch coverage counter. No `coverlet` / `OpenCover` / `dotCover` tooling is currently configured in `Directory.Packages.props` or CI (no `.github/workflows/ci.yml` yet).

**Decision:** Add `coverlet.collector 6.0.*` + `coverlet.msbuild 6.0.*` pins in `Directory.Packages.props` as part of T002. Each `Rig.TUnit.*.Tests.*.csproj` references both as `PackageReference` (no `<Version>` — central management).

**Gate commands** (used at T097 / T140 / T152 and baked into T160 CI):

```bash
# Line coverage ≥ 90%, branch ≥ 85% per project
dotnet test /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum
dotnet test /p:CollectCoverage=true /p:Threshold=85 /p:ThresholdType=branch /p:ThresholdStat=minimum

# Cobertura report for CI artifacts
dotnet test --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectorRunConfiguration.CodeCoverage.CoverageFileFormat=cobertura
```

**Rationale:** coverlet is the de-facto .NET coverage tool post-2020, supports .NET 10 without config, runs in-process (no dotnet-tool install), and the MSBuild variant fails the test run when the threshold is not met — exactly the merge-gate behavior 003 R1 requires.

**Complementary — not replacement — of `CoverageRuleTests`:** the existing rule catches orphan public types (zero test references), which coverlet line-coverage cannot detect (a 100%-covered line is still an orphan if the type is never referenced by `*Tests` / `*Contract`). Keep both checks.

---

## References

- `planning/provider-consistency-remediation/*.md` — primary design + handoff docs
- `planning/ecosystem-expansion/*.md` — 003 baseline for pattern continuity
- `.claude/rules/*.md` — project conventions
- `Directory.Packages.props` — central version pins (verified 2026-04-18)
- [Pomelo PR #2019](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019)
- [aspire#12036](https://github.com/dotnet/aspire/issues/12036)
- [testcontainers-dotnet#1306](https://github.com/testcontainers/testcontainers-dotnet/discussions/1306)
- [Azure Cosmos DB Linux emulator docs](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux)
