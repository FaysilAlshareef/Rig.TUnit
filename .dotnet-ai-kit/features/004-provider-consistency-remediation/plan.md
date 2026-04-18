# Implementation Plan: Provider Consistency Remediation

**Feature ID**: 004-provider-consistency-remediation
**Generated**: 2026-04-18
**Mode**: Generic (single-repo .NET 10 class-library ecosystem)
**Complexity**: Complex (7 entities, external services via Testcontainers, 26 FRs, 9 user stories, 6 phases)
**Source spec**: [spec.md](spec.md) — 3 clarifications resolved (C-001..C-003), 0 markers remaining

---

## Constitution Check

`.dotnet-ai-kit/memory/constitution.md` — **NOT PRESENT**. Gate skipped with warning. Run `/dotnet-ai-kit:learn` to generate one. In the meantime, `.claude/rules/*.md` plus the 003 plan are the operative rulebook.

**Implied invariants (from `.claude/rules/` + 003):**

- **Detect-first**: build on existing `IRigConnectionSource`, `RigBuilder` (Core), family-specific `{Family}RigBuilder<TSelf>` CRTP bases, `{Family}FixtureBase` classes — all verified present in `src/` on 2026-04-18.
- **Pattern fidelity**: Canonical provider shape is proven by `Rig.TUnit.Databases.Sql.SqlServer` (full: Fixture + Options + Builder + Extensions + Helpers + README). Every remediation target copies that shape.
- **Architecture-agnostic**: This is a class-library ecosystem — no Clean Arch / VSA / microservice constraints apply inside the library itself.
- **TDD non-negotiable**: Every production class ships in the same commit as its failing test (FR-024).

---

## Executive Summary

Close the provider-surface-area gap left by 003: bring every `Rig.TUnit.{Family}.{Provider}` package to the canonical shape (Fixture + Options + Builder + Extensions + Helpers + README); ship 4 never-delivered packages (MySql, Oracle, Cosmos, AppInsights); complete the partial Docker package; extract inline test-setup infrastructure into per-project `TestInfrastructure/` folders; enforce the uniformity via three new architecture tests.

**Delivery discipline** — strictly test-first (RED → GREEN → REFACTOR), **4-test categories per provider (Unit + Integration + Contract + Benchmark — FR-030..FR-034) plus 2 coverage-lifting unit tests (FixtureOptions + RigBuilder exerciser — FR-035, added post-Mongo measurement)**, per-package merge gate identical to 003 (≥ 90 % line / ≥ 85 % branch / 100 % contract suite / parallel-isolation smoke), zero regressions on the 219-test baseline. The `## TDD Gate` section in `tasks.md` is the normative source for per-task cadence — reviewers verify commit order (`test(004): TNNN — RED` must precede `feat(004): TNNN — GREEN`) before approving any PR hunk that touches `src/`. `TestCompletenessTests` (landed in Phase 6 T157a) makes the 4-category requirement machine-visible alongside the existing three architecture rules. Coverage is collected via TUnit/MTP-native `dotnet run -- --coverage --coverage-output-format cobertura` (see FR-036 + research §R16) — the `coverlet.msbuild` path does not work under Microsoft.Testing.Platform.

**Phase order is a hard dependency chain.** No phase starts until the previous is green.

---

## Target Architecture

### Package topology (post-004)

```
Rig.TUnit.Core                              [UNCHANGED — RigBuilder + IsolationKey + IRigConnectionSource]
Rig.TUnit.Mediator / Grpc / WebAPI          [UNCHANGED]

# Base + Provider (CRTP chain, all bases already in place)
Rig.TUnit.Databases
├─ Rig.TUnit.Databases.Sql                  [UNCHANGED base]
│  ├─ SqlServer / Sqlite / Postgresql       [MODIFIED — Postgresql gets BuilderExtensions]
│  ├─ Rig.TUnit.Databases.Sql.MySql         [NEW PACKAGE — Phase 4]
│  └─ Rig.TUnit.Databases.Sql.Oracle        [NEW PACKAGE — Phase 4]
└─ Rig.TUnit.Databases.NoSql                [UNCHANGED base]
   ├─ Redis (KV, reuses Caching.Redis)      [UNCHANGED]
   ├─ Mongo / Cassandra / Dynamo / ElasticSearch  [MODIFIED — Phase 3]
   ├─ KurrentDb (was EventStore)            [RENAMED — Phase 1 (T002b–T002d) + MODIFIED in Phase 3a.v]
   └─ Rig.TUnit.Databases.NoSql.Cosmos      [NEW PACKAGE — Phase 4]

Rig.TUnit.Messaging                         [UNCHANGED base]
├─ ServiceBus                               [UNCHANGED]
└─ Kafka / RabbitMq / Nats / Sqs            [MODIFIED — Phase 3]

Rig.TUnit.Caching                           [UNCHANGED base]
├─ Memory / Redis                           [MODIFIED — Memory gets UseMemoryCache extension]
├─ Hybrid                                   [MODIFIED — Phase 3]
└─ Fusion                                   [MODIFIED — Phase 3]

Rig.TUnit.Storage                           [UNCHANGED base]
└─ AzureBlob / S3 / MinIO / FileSystem      [MODIFIED — Phase 3]

Rig.TUnit.Security                          [UNCHANGED — base types already present]
├─ Jwt / OAuth / Mtls / Policies            [MODIFIED — wire RigBuilders to SecurityRigBuilder<TSelf>]
(Mtls also gains MtlsFixture)

Rig.TUnit.Observability                     [UNCHANGED base]
├─ Logging / Tracing / Seq                  [UNCHANGED]
├─ Metrics                                  [MODIFIED — gains MetricsFixture, Options, Builder]
└─ Rig.TUnit.Observability.AppInsights      [NEW PACKAGE — Phase 4]

Rig.TUnit.Microservices
├─ Outbox / Inbox / Snapshots               [UNCHANGED]
├─ EventSourcing                            [MODIFIED — AggregateAssert + catalogue verifier + schema helper]
├─ Saga                                     [MODIFIED — SagaAssert.Compensated + timeout helper]
└─ Contracts                                [MODIFIED — ProviderVerificationFixture + PactBrokerClientStub]

Rig.TUnit.Docker                            [MODIFIED — was fixture-only; complete the template]

Rig.TUnit.All                               [MODIFIED — add refs to 4 new packages + completed Docker]

# Architecture tests
tests/Rig.TUnit.Architecture.Tests/Rules/
├─ ProviderCompletenessTests.cs             [NEW]
├─ TestFileOrganizationTests.cs             [NEW]
└─ ReadmeCompletenessTests.cs               [NEW]
```

### Provider file layout (canonical)

Proven by `src/Rig.TUnit.Databases.Sql.SqlServer/`. Every provider ends in this shape:

```
src/Rig.TUnit.{Family}.{Provider}/
├── {Provider}.csproj
├── README.md                              ( > 100 chars, 30-sec quick-start )
├── Fixtures/{Provider}Fixture.cs           : {Family}FixtureBase
├── Options/{Provider}FixtureOptions.cs     // public const string SectionName + [Required] + defaults
├── Builder/{Provider}RigBuilder.cs         : {Family}RigBuilder<{Provider}RigBuilder>   // sealed
├── Builder/{Provider}RigBuilderExtensions.cs  // public static RigBuilder Use{Provider}(this RigBuilder, ...)
├── Extensions/ (SQL only — EF provider wire-up)
└── Helpers/    (family-specific: Listener/Sender, SasBuilder, StreamAssert, KeyspacePerTest, RuChargeCapture, etc.)
```

---

## Phase Plan

Numbered 1–6 to match the library design doc's phased delivery. Each phase has a hard entry gate (prior phase green) and an exit gate (architecture tests + contract tests + coverage).

### Phase 1 — Enforcement scaffolding (lands first, failing)

**Goal:** Make the gaps machine-visible before any provider is touched. All three rules land RED for every gap; they turn GREEN as each provider is completed.

**Commit 1 (prep):** bump every `Testcontainers.*` pin in `Directory.Packages.props` from `4.6.0` → `4.11.0` (C-001), enable `CentralPackageFloatingVersionsEnabled` (required for the new wildcard pins — NU1011 fails restore otherwise), add `MySqlConnector 2.4.*` + `coverlet.collector 6.0.*` + `coverlet.msbuild 6.0.*`, wildcard `Pomelo.EntityFrameworkCore.MySql 9.0.*`. `Testcontainers.EventStoreDb` has no 4.11 release (latest is 4.9.0) and its Builder type is marked `[Obsolete]` per the KurrentDb rebrand — it is REMOVED in the same commit (see Commit 2). The 18 existing fixtures using `new XxxBuilder()` must be updated to pass the image into the constructor (`new XxxBuilder("image:tag")`) — the parameterless ctor is obsolete in 4.11 and breaks the `TreatWarningsAsErrors=true` build with CS0618. Run `dotnet test` on unit + contract + architecture projects only (integration tests require Docker daemons and belong to `/dotnet-ai-kit:verify`). If any 003-era test regresses, root-cause and fix before proceeding.

**Commit 2 (KurrentDb rename):** Align with the upstream rebrand (https://www.kurrent.io/blog/kurrent-re-brand-faq). This is a deliberate breaking change — the feature is labelled Provider **Consistency** Remediation and keeping a stale `EventStore` name while every dependency reads "Kurrent" violates that intent.

- `Directory.Packages.props`: `Testcontainers.EventStoreDb` → `Testcontainers.KurrentDb 4.11.0`; `EventStore.Client.Grpc.Streams` → `KurrentDB.Client 1.3.1`.
- Rename `src/Rig.TUnit.Databases.NoSql.EventStore/` → `src/Rig.TUnit.Databases.NoSql.KurrentDb/` (preserve history with `git mv`).
- Rename the test project, the fixture class (`EventStoreFixture` → `KurrentDbFixture`), and the contract + shared-fixture test files.
- Update `Rig.TUnit.slnx`, `src/Rig.TUnit.All/Rig.TUnit.All.csproj`, and `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` seed list.
- Container image: `eventstore/eventstore:24.10.0-bookworm-slim` → `kurrentplatform/kurrentdb:25.1`.
- Connection string scheme becomes `kurrentdb://…?tls=false` — consumed directly by `KurrentDB.Client`.
- Release notes MUST call out the rename under a "Provider rename (breaking)" heading.

**Commit 3 (docs):** add `src/Rig.TUnit/Contributing-ProviderTemplate.md` — canonical copy-paste template for a hypothetical `Rig.TUnit.{Family}.Example` provider.

**Commit 4 (architecture tests land RED):**
- `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`
- `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs` (applies uniformly, including `*Contract.cs` per C-003)
- `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`

Rules use `NetArchTest.Rules` for assembly introspection + direct filesystem walks for README/test-organization. Providers known to be in-flight get `[Category("SkipUntilFixed")]` markers with a tracking issue reference in a comment.

**Exit gate:** Build succeeds; 003 baseline (219 tests) still green; the three new rules compile and execute (some `[Skip]` expected).

### Phase 2 — Test-file hygiene sweep

**Goal:** Every test `.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` contains exactly one top-level class.

**Worst offenders first** (full list in Phase 2 of Session-Handoff):

1. `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/` → `TestInfrastructure/TracingTestHarness.cs`
2. `tests/Rig.TUnit.Http.Tests.Unit/` → `TestInfrastructure/HttpMockTestHarness.cs`
3. `tests/Rig.TUnit.Resilience.Tests.Integration/` → `TestInfrastructure/ResiliencePipelines.cs`
4. `tests/Rig.TUnit.Security.OAuth.Tests.Integration/` → `TestInfrastructure/OAuthTestHarness.cs`
5. `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/` → `TestInfrastructure/OutboxTestData.cs`
6. Every `*QuirkTests.cs`, `*Contract.cs` file in `tests/` — scan for inline types, extract.
7. `*Contract.cs` helper types → `TestInfrastructure/ContractHelpers/` (C-003).

**TDD discipline on refactors:** extraction must not change test behavior. If tests fail after extraction, the extraction was unsafe — fix in place, don't rewrite the test.

**Exit gate:** `TestFileOrganizationTests` is fully enforced (no `[SkipUntilFixed]`). `dotnet test` green.

### Phase 3 — Close provider gaps

Providers grouped by family. **Within each family, a provider is "done" when**: (a) the four required files exist, (b) `Use{Provider}` is on `RigBuilder`, (c) the provider's integration test project inherits the family's contract suite and passes, (d) `ProviderCompletenessTests` is flipped GREEN for that provider, (e) coverage gate met.

| Order | Family | Providers | Per-provider adds |
|---|---|---|---|
| 3a | Databases.Sql | Postgresql | `PostgresRigBuilderExtensions` (`UsePostgres` fluent) + `PostgresBuilderExtensions` (`UsePostgresInMemory` EF quickstart per design §4.1) + README. `PostgresRigBuilder` already present. |
| 3b | Databases.NoSql | Mongo, Cassandra, Dynamo, ElasticSearch, KurrentDb (renamed from EventStore in Phase 1 T002c) | Builder + Extensions + Options (where missing) + family-specific helper per design §4.4 |
| 3c | Messaging | Kafka, RabbitMq, Nats, Sqs | Builder + Extensions + Options (Nats/Sqs) + `{Provider}Listener : ListenerBase` + `{Provider}EventSender : EventSenderBase` |
| 3d | Caching | Memory, Hybrid, Fusion | Memory: `UseMemoryCache` ext only. Hybrid/Fusion: Options + Builder + Extensions + fail-safe/eager-refresh helpers |
| 3e | Storage | AzureBlob, S3, MinIO, FileSystem | Options (MinIO/FileSystem) + Builder + Extensions + `{Provider}SasBuilder` (FS gets `PathSandboxHelper` instead) |
| 3f | Security | Jwt, OAuth, Mtls, Policies | `{Provider}RigBuilder : SecurityRigBuilder<TSelf>` + `Use{Provider}` extension. Mtls adds `MtlsFixture`. Policies adds `PolicyFixture`. Existing `JwtBuilder` / `MtlsCertificateBuilder` / `PolicyAssert` / `MockOAuthServer` kept as helpers |
| 3g | Observability.Metrics | (single) | `MetricsFixture` (wraps `MeterListener`) + `MetricsFixtureOptions` + `MetricsRigBuilder : TelemetryRigBuilder<…>` + `UseMetricsCapture` + `TagCardinalityGuard` |

**Exit gate:** `ProviderCompletenessTests` flipped GREEN for every provider touched in Phase 3. Every family's contract suite passes 100%. Coverage gate met.

### Phase 4 — Create the 4 missing packages + complete Docker

Each new package lands with the full canonical layout plus an `*.Tests.Integration` project inheriting the family contract + `ParallelIsolationContract` + provider-specific quirk tests. Each ships its README in the same commit.

| Order | Package | Key .NET 10 strategy |
|---|---|---|
| 4a | Rig.TUnit.Databases.Sql.MySql | `Testcontainers.MySql 4.11.x` + `Pomelo.EntityFrameworkCore.MySql 9.0.0` (already pinned — add comment citing PR #2019) + `MySqlConnector 2.4.*` (add pin) |
| 4b | Rig.TUnit.Databases.Sql.Oracle | `Testcontainers.Oracle 4.11.x` + image `gvenzl/oracle-free:23.5-slim-faststart` + `Wait.ForListeningPorts()` + 5-min timeout (aspire#12036) + `Oracle.EntityFrameworkCore 10.0.0` (already pinned) |
| 4c | Rig.TUnit.Databases.NoSql.Cosmos | `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` + custom wait probe hitting `/_explorer/emulator.pem` over HTTPS with self-signed trust (dotnet/testcontainers-dotnet#1306) + `Microsoft.Azure.Cosmos 3.44.0` (already pinned) + `RuChargeCapture` + `PartitionKeyDistributionChecker` helpers |
| 4d | Rig.TUnit.Observability.AppInsights | In-process `ITelemetryChannel` capture (no container) + `Microsoft.ApplicationInsights 2.23.0` (already pinned) + `AppInsightsAssert` mirroring `TraceAssert` surface |
| 4e | Rig.TUnit.Docker | Complete the template: add `DockerFixtureOptions` + `DockerRigBuilder` + `UseDocker` extension + `DockerComposeFixture`. Primary compose backend: `Testcontainers` 4.11 native; fallback to `Ductus.FluentDocker` only if regressed (decided at implementation) |

**Slnx + meta-package updates:**
- `Rig.TUnit.slnx` — register 4 new src projects + 5 test projects (4 new + 1 Docker).
- `Rig.TUnit.All/Rig.TUnit.All.csproj` — add ProjectReferences to the 5.
- `Directory.Packages.props` — ensure every pin cited above is present.

**Exit gate:** All 5 packages present in slnx; `ProviderCompletenessTests` GREEN for all 5; every quirk test passes; coverage gate met.

### Phase 5 — Microservices depth

| Package | New surface |
|---|---|
| Rig.TUnit.Microservices.EventSourcing | `AggregateAssert.Raised<TEvent>().WithData(predicate)` + `EventCatalogueVerifier` + `SchemaEvolutionHelper` |
| Rig.TUnit.Microservices.Saga | `SagaAssert.Step(name).Compensated()` + `SagaTimeoutHelper` (advances injected `TimeProvider`) |
| Rig.TUnit.Microservices.Contracts | `ProviderVerificationFixture` (loads Pact from `TestInfrastructure/Pacts/*.json`) + `PactBrokerClientStub` (file-based per C-002) |

**Exit gate:** All three packages have the new surface tested; integration tests pass.

### Phase 6 — Polish

1. Add `README.md` to every provider lacking one — **20 existing leaf providers lack READMEs today** (verified 2026-04-18) + 4 new packages created in Phase 4 = ~24 READMEs to write. Phases 3-5 land most of them in-commit with each provider; Phase 6 catches residuals. (Planning docs' "57 of 59" figure is stale — superseded.)
2. Flip `ReadmeCompletenessTests` fully enforced.
3. Verify `Rig.TUnit.All` transitively pulls every provider.
4. `.github/workflows/ci.yml` — add MySql / Oracle / Cosmos matrix rows OR `[Category("containers")]` dedicated job.
5. Document Windows-runner skip logic for Cosmos (Linux emulator requires Linux containers).

**Exit gate:** All success criteria SC-001..SC-011 in the spec are met. Every checkbox in `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` is ticked.

---

## TDD Workflow (carry-forward from 003 R1)

For every production class in every phase:

1. **RED commit.** Write the failing test. Commit message: `test({phase-id}): T{NNN} — RED for {type}`.
2. **GREEN commit.** Write the minimum production code. Commit message: `feat({phase-id}): T{NNN} — GREEN implement {type}`.
3. **REFACTOR commit (optional).** Tighten names, extract helpers. Tests stay green; if a test changes, it was behavior change, not refactor — write a new RED test first.

**Per-package merge gate** (identical to 003):

| Gate | Target |
|------|--------|
| Line coverage | ≥ 90% |
| Branch coverage | ≥ 85% |
| Contract suite | 100% green |
| Parallel-isolation smoke | green (20 parallel fixtures, zero cross-talk) |
| XML docs on public API | present |

**Architecture tests** (the three this feature adds) run on every PR by default from Phase 1.

---

## Dependency & Sequencing Rules

- **Phase 1 blocks everything.** Architecture tests must land first, failing, so the scope is visible.
- **Phase 2 can start after Phase 1 exit gate.** Tests reorganized before production changes ensures no test regresses silently during refactor.
- **Phase 3 providers parallelize within a family.** Different families (Databases.NoSql, Messaging, Storage, Security, Observability.Metrics) can be worked in parallel, but within a family providers serialize — contract suite changes affect all providers in that family.
- **Phase 4 starts after Phase 3.** New packages borrow helpers/Extensions/Listener/Sender patterns first established by Phase 3 completion.
- **Phase 5 and Phase 6 can overlap.** Microservices depth (Phase 5) doesn't depend on new packages; README polish (Phase 6) wraps over everything.

Within each phase, subtasks ordered: architecture test stub → contract test stub → Options → Fixture → RigBuilder → Use extension → provider helpers → README → flip `[SkipUntilFixed]` to enforced.

---

## Risk Ledger

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Testcontainers 4.6 → 4.11 bump breaks existing 003 tests | Low | High | Bump in Phase 1 commit 1; full `dotnet test` before any provider work |
| Pomelo 9 on .NET 10 TFM hits an obscure runtime bug | Low | Medium | `MySqlQuirkTests` covers AUTO_INCREMENT + timestamp; fallback to Microting fork documented but not used unless blocked |
| Oracle container init hangs (aspire#12036) | Medium | Medium | `Wait.ForListeningPorts()` + 5-min startup timeout + retry-once on init failure |
| Cosmos emulator wait probe needs self-signed cert trust | Medium | Medium | Custom wait strategy does `ServerCertificateCustomValidationCallback` trust-all against `/_explorer/emulator.pem` — documented in `CosmosFixture` |
| `Testcontainers` native compose support regresses on .NET 10 for Docker package | Low | Medium | Spec allows `Ductus.FluentDocker` fallback; decision point at Phase 4e implementation |
| `TestFileOrganizationTests` enforced on `*Contract.cs` (C-003) breaks base classes that declare helper types | Medium | Low | Phase 2 scans all contract files first, extracts helpers to `TestInfrastructure/ContractHelpers/`; rule only flips enforced after Phase 2 exit |
| CI matrix additions (MySql/Oracle/Cosmos) blow CI time budget | Low | Low | Use `[Category("containers")]` job separation + image pull cache; Cosmos Linux-only → Windows runners skip |
| Coverage slips under 90% on any new package | Medium | Medium | Per-package gate enforced at PR time; new quirk tests + contract suite + parallel-isolation smoke typically clear 90% without effort |

---

## Open Decisions (deferred to implementation)

Tracked from spec edge cases + clarifications for the implementation team:

- **Pomelo 10 release status at merge time.** If PR #2019 (Pomelo's .NET 10 / EF Core 10 release) merges before this feature's PR, bump `Directory.Packages.props` in a late Phase 4 commit. Otherwise stay at 9.0.*.
- **Docker compose backend choice.** Default to `Testcontainers` native. Only pivot to `Ductus.FluentDocker` if compose regresses on .NET 10 — document activation criteria in Docker README.
- **Dynamo test backend.** LocalStack preferred over DynamoDB Local for GSI fidelity. Document the tradeoff in the Dynamo README.

---

## Related Artifacts

- [research.md](research.md) — driver/library research + decisions with cited issues/PRs.
- [data-model.md](data-model.md) — key entity schemas, required-types-per-provider inventory.
- [quickstart.md](quickstart.md) — contributor handbook: "how to add a new provider in under an hour".

---

## Next

```
/dotnet-ai-kit:tasks      # break this plan into ordered test-first tasks
/dotnet-ai-kit:analyze    # optional: cross-check spec ↔ plan consistency
```
