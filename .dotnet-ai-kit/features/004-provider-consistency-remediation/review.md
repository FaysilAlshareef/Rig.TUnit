# Review Report: 004-provider-consistency-remediation

**Date**: 2026-04-19 | **Mode**: Generic (single-repo .NET 10 test-infrastructure library)
**Diff range**: `7a64b66..3b936df` (merged via PR #3, merge commit `9d3369f`)
**Scope**: 885 files changed · +43,653 / -1,399 · ~186 production `.cs` files · ~250 test `.cs` files
**Reviewer**: dotnet-ai-kit /review (standards-only; CodeRabbit CLI not installed)

---

## Rig.TUnit (PASS with advisories)

### Check 1 — Naming Conventions: PASS
- Solution/project naming follows `Rig.TUnit.{Family}.{Provider}` uniformly.
- Fixture / Options / RigBuilder / Use{Provider} quartet is machine-verified by `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` against 26 providers.
- Test method naming: 1128 of 1264 `[Test]` methods (~89%) follow `Method_Scenario_Expected`. Architecture test `TestFileOrganizationTests` enforces one-class-per-file.

### Check 2 — Architecture Boundary Violations: PASS
- `DependencyDirectionTests.cs` enforces the layer direction at build time.
- No cross-layer shortcuts found in the diff; all providers depend through `Core` → `{Family}` → `{Family}.{Provider}`.
- `Rig.TUnit.slnx` assembles 84 projects with no circular references.

### Check 3 — Localization: N/A
- Test library; no `.resx` / `IStringLocalizer` usage. Localization rule explicitly skips this case.

### Check 4 — Error Handling: PASS
- `async void`: 0 occurrences.
- Swallowed `catch (Exception) { }`: 0 occurrences.
- `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` in src: 0 occurrences.
- `CancellationToken` propagated across 189 call sites in 65 files (every `*Fixture.InitializeAsync`, every `Helper`/`Listener`/`Sender`, every `Assertions.*Await*`).
- `Task.Delay` usage (8 sites) all carry `CancellationToken` and are semantically legitimate (poll intervals in `WaitHelper`, simulated response latency in `HttpMockDelegatingHandler`, short backoff in `OutboxAssert`/`SeqAssert`/`MessageAssert`).

### Check 5 — Testing: PASS
- 250 test files, 1264 `[Test]` methods, co-located Unit / Integration / Contract / Benchmark projects per provider.
- 8 self-enforcing architecture rules live in `tests/Rig.TUnit.Architecture.Tests/Rules/`:
  `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`, `CodeOrganizationTests`, `ForbiddenApiTests`, `DependencyDirectionTests`, `CoverageRuleTests`, `TestCompletenessTests`.
- CI matrix at [.github/workflows/ci.yml](../../../.github/workflows/ci.yml) segments build/unit/arch, contract, integration, benchmarks.

### Check 6 — Security: PASS (1 LOW advisory)
- **No hardcoded secrets / tokens / API keys** in src (scan patterns: `password=`, `secret=`, `token="…"`, etc.).
- All provider credentials flow through `{Provider}FixtureOptions` (`WithUsername(_options.Username)` / `WithPassword(_options.Password)` in MySql, Mongo, Postgres, Oracle, RabbitMq, MinIO).
- `Regex` in [src/Rig.TUnit.Microservices.Snapshots/Scrubbers/MicroserviceScrubbers.cs:25](src/Rig.TUnit.Microservices.Snapshots/Scrubbers/MicroserviceScrubbers.cs:25) defines protective patterns for redacting secrets from snapshots — not a secret itself.
- Parameterized queries only; `\bstring\.Concat\b` + `SELECT` / interpolated `SqlCommand($"…")`: 0 occurrences.
- **LOW — default SA password literal**: [src/Rig.TUnit.Databases.Sql.SqlServer/Options/SqlServerFixtureOptions.cs:21](src/Rig.TUnit.Databases.Sql.SqlServer/Options/SqlServerFixtureOptions.cs:21) — `public string SaPassword { get; init; } = "Your_password123!";`. Standard Testcontainers dev-bootstrap password; `init`-only and overridable via `IOptions<T>` binding. Acceptable for a dev-scope test fixture, but callout as advisory: XML doc doesn't currently warn consumers that the default ships in source — consider stating explicitly "default is public knowledge; override in production-like environments" in the XML doc comment.

### Check 7 — Event Structure: N/A (generic, single-repo library, not microservice)

### Check 8 — Performance: PASS
- `.ToList()` before `.Where()`: 0 occurrences.
- `UseLazyLoadingProxies` / lazy-loading proxies: 0 occurrences.
- `DateTime.Now` / `DateTime.UtcNow` in src: 0 occurrences — clocks are injected (`ResilienceClock`, `ClockControl`) or use `DateTimeOffset.UtcNow` deliberately (e.g. `SeqFixture.CaptureDashboardSnapshotAsync`).
- `Console.Write*` in runtime src: 0 occurrences. (The `Observability.Logging.Analyzers` RTU002 rule *forbids* it in user code — intentional mention.)
- `N+1` patterns not applicable (test library — no bulk entity loads).

### Check 9 — Brief Compliance: N/A (generic mode, no secondary-repo briefs)

---

## Advisories (not blocking — style / hardening)

### MEDIUM — 3 non-sealed concrete fixtures
Rule `architecture-profile.md`: "MUST use `sealed` on classes not designed for inheritance." Three concrete provider fixtures are declared `public class` without `sealed`, and the codebase has no subclasses of them:

- [src/Rig.TUnit.Observability.Seq/Fixtures/SeqFixture.cs:17](src/Rig.TUnit.Observability.Seq/Fixtures/SeqFixture.cs:17)
- [src/Rig.TUnit.Observability.Logging/Fixtures/LoggingFixture.cs:14](src/Rig.TUnit.Observability.Logging/Fixtures/LoggingFixture.cs:14)
- [src/Rig.TUnit.Observability.Tracing/Fixtures/TracingFixture.cs:17](src/Rig.TUnit.Observability.Tracing/Fixtures/TracingFixture.cs:17)

Surrounding compliance: 240 `public sealed class/record` declarations across src — 98.8% rate.
Fix: add `sealed` to each declaration (no API break).

### LOW — 13 non-sealed test classes
Same rule literally applies to test classes. TUnit / xUnit convention typically allows non-sealed for testing frameworks that may scan inheritance, so this is advisory only. Affected: 9 classes under `tests/Rig.TUnit.Core.Tests.Unit/**` and 4 under `tests/Rig.TUnit.Grpc.Tests.Unit/**`. Leave as-is unless you want strict rule conformance.

### LOW — Test method naming outliers (~11%)
~136 test methods deviate from the `Method_Scenario_Expected` convention. Mostly integration/quirk tests that use descriptive sentence-style names. Not worth mass-refactor; recommend enforcing on new tests only via a soft lint.

---

## CodeRabbit

CodeRabbit CLI not found on PATH. To enable: install from https://coderabbit.ai/cli and re-run `/dotnet-ai-kit:review`.

---

## Summary

- **Total findings**: 17 (3 MEDIUM, 14 LOW)
- **CRITICAL**: 0 | **HIGH**: 0 | **MEDIUM**: 3 | **LOW**: 14
- **Auto-fixed**: 0 (ran in report-only mode)
- **Remaining**: 17
- **Verdict**: **PASS** — all HIGH/CRITICAL checks clean; advisories are style/hardening only.

The feature is production-grade and already merged. Architecture tests enforce ongoing compliance, so any regression will fail CI.

### Follow-up suggestions (post-handover candidates)
1. Seal the 3 Observability fixtures (1-line change each).
2. Strengthen XML doc on `SqlServerFixtureOptions.SaPassword` describing the public default.
3. If strict sealed-on-all rule matters, seal the 13 test classes or add an arch-test exemption.

### CI / Post-merge notes
- 3 commits after PR merge landed directly on the feature branch to fix CI:
  - `55b63a2` — fix `dotnet test` positional arg (use `--solution`/`--project`)
  - `b024346` — unit tests + PG/MySQL/Oracle contract failures + full CI matrix
  - `3b936df` — exclude `Tests.Contract` projects from unit run
- Branch is at `3b936df`; `origin/master` is at `9d3369f` (merge commit whose feature parent is `3b936df`). Branch is fully integrated.

Next: `/dotnet-ai-kit:wrap-up` to produce handover notes.
