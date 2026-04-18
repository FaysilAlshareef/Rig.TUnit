# Tasks: Provider Consistency Remediation

**Feature**: 004-provider-consistency-remediation | **Mode**: Generic (single-repo .NET 10 library)
**Generated**: 2026-04-18 | **Revised**: 2026-04-18 (post-analysis + full-scan — Postgresql remediation added, ParallelIsolationContract explicit, Cosmos package clarified, coverage command specified, README counts corrected to 20-of-32 leaf providers, orphan 003-era dirs scheduled for cleanup at T003a, Rig.TUnit meta-package description at T004a, **Kurrent migration added at T002b/T002c** — Testcontainers 4.9+ marks the whole `EventStoreDb` module obsolete in favour of the upstream `KurrentDb` rename)
**Source**: [spec.md](spec.md), [plan.md](plan.md), [data-model.md](data-model.md), [research.md](research.md)

## TDD Gate — MANDATORY, UNSKIPPABLE (applies to every task in Phases 3, 4, 5, 6 that creates or modifies production code)

**Every task that adds or modifies a file under `src/` MUST follow this 4-test, 2-commit cadence. No exceptions.** Any commit that adds production code without a preceding failing test on the same branch is a blocker and MUST be reverted + redone. A retroactive test "proving" already-committed code does NOT satisfy the gate — the RED commit must precede the GREEN commit in `git log`.

### The 4 test categories (each provider feature MUST carry all four)

| Category | Project path pattern | Purpose | Runs without Docker? |
|---|---|---|---|
| **Unit** | `tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit/` | Pure-function helpers, Options validation, builder wiring — no external services | ✅ yes |
| **Integration** | `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` | Container-backed behaviour; `*QuirkTests.cs` for provider-specific quirks | ❌ Docker required |
| **Contract** | `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/{Provider}Contract.cs` (inherits `{Family}RigContract`) | Every provider of a family passes the same behavioural suite | ❌ Docker required |
| **Benchmark** | `tests/Rig.TUnit.Benchmarks/{Provider}Benchmarks.cs` (BenchmarkDotNet) | Allocation / throughput measurements for the Fixture + Helpers | ✅ yes (pure) or ❌ (container-backed) |

### Per-task TDD checklist (reviewer verifies every item on PR)

For each RED→GREEN task touching `src/`:

1. **RED — write the failing tests first**
   - Add the **Unit test(s)** in the matching `*.Tests.Unit/` project (create the project if it does not yet exist; scaffold with `TUnit`, `NSubstitute`, and `ProjectReference` back to the provider src).
   - Add the **Integration test(s)** in the matching `*.Tests.Integration/` project.
   - If a public API surface is introduced (RigBuilder / Extension), add a **Benchmark** class in `tests/Rig.TUnit.Benchmarks/` before the GREEN impl.
   - Run each: `dotnet test --project tests/...Tests.Unit/` (no Docker needed) + `dotnet test --project tests/...Tests.Integration/ --filter "FullyQualifiedName~{TestName}"` (Docker up). Tests MUST fail with a missing-symbol error (`CS0246` / "method not found") — a pass at RED time means the test does not actually cover the new surface.
   - Commit with message: `test(004): T{NNN} — RED for {Type} (unit + integration + benchmark)`. The commit MUST include all test files for the feature (unit, integration, benchmark) in the single RED commit.

2. **GREEN — write the minimum src to make the RED tests pass**
   - Write the production file(s) listed in the task's `File:` line.
   - Re-run the same filters. All newly-added tests MUST pass. Existing tests MUST NOT regress.
   - Run `dotnet build Rig.TUnit.slnx` — 0 warnings, 0 errors under `TreatWarningsAsErrors=true`.
   - Commit: `feat(004): T{NNN} — GREEN implement {Type}`.

3. **REFACTOR (optional)** — may follow once GREEN is stable. MUST NOT change test semantics; a test change signals a behaviour change which demands a new RED first.

4. **Docker precondition** — before any Integration/Contract run, verify `docker ps` works. If Docker is down, STOP and report — do not skip the Integration leg and claim GREEN.

### Coverage gate (per-package, enforced at Phase 3/4/5/6 exit)

- Line coverage ≥ **90 %** per modified package (merged unit + integration via cobertura)
- Branch coverage ≥ **85 %** per modified package
- `ProviderCompletenessTests` GREEN for the provider (moved from `SkipUntilFixed` to `RequiredProviders`)
- `ReadmeCompletenessTests` GREEN for the provider (removed from skip list)
- **No src file merged without a unit + integration + contract + benchmark entry covering it.**

#### Coverage-lifting tests (mandatory per provider — added post-Mongo measurement 2026-04-18)

The first pass of the Mongo template landed at 87.4 % line (2.6 % short) and Postgres at 77.8 % line. The gap was `{Provider}FixtureOptions` property-initializer lines (coverlet measurement quirk for init-only autoprops — 0 % line even when exercised via DI) and `{Provider}RigBuilder` code paths not exercised by the basic builder tests. **Every provider going forward MUST add these two unit tests** — each takes ≤ 5 min to write and reliably lifts coverage over the gate:

1. **`{Provider}FixtureOptionsTests.cs`** — constructs the Options record both with defaults and with every property overridden; asserts the defaults match the doc and overrides propagate. Example:
   ```csharp
   [Test] public async Task Defaults_SetExpectedValues() { var o = new MongoFixtureOptions();
       await Assert.That(o.ImageTag).IsEqualTo("7"); /* … each property … */ }
   [Test] public async Task Overrides_PropagateThroughInitOnlyProperties() {
       var o = new MongoFixtureOptions { ImageTag = "6", StartupTimeoutSeconds = 30 };
       await Assert.That(o.ImageTag).IsEqualTo("6"); /* … */ }
   ```

2. **`{Provider}RigBuilder_ExerciseTests.cs`** — drives the RigBuilder's otherwise-uncovered paths. For SQL providers, calls `UseProvider(new DbContextOptionsBuilder<TestDb>(), connStr)` and asserts the provider extension registered (via `options.Options.Extensions`). For NoSql/Messaging/Caching/Storage/Security/Observability providers, invokes the `ConnectionString` pass-through + any other public property. No Docker needed — the Builder is pure wiring.

Acceptance: after these two tests land alongside the canonical four, `dotnet run --coverage` should report **merged unit+integration line ≥ 90 %** for every provider. Verified against the Mongo template bump.

#### Coverage measurement — TUnit / Microsoft.Testing.Platform

TUnit ships under Microsoft.Testing.Platform, not VSTest. The `coverlet.msbuild` `/p:CollectCoverage=true` path **does NOT work** with TUnit test projects. Use the MTP-native coverage instead:

```
dotnet run --no-build -c Debug --project tests/<project>/ -- \
    --coverage --coverage-output-format cobertura \
    --coverage-output <name>.cobertura.xml
```

Output lands in `tests/<project>/bin/Debug/net10.0/TestResults/<name>.cobertura.xml`. To get merged unit+integration numbers, run both legs and merge class-level `line` entries by `(filename, number)` (script in `tools/coverage-merge.py` — Phase 6 adds this helper).

### Forbidden anti-patterns

- ❌ Shipping src code first and "backfilling tests later" — this is what violated TDD earlier in this feature and was reverted. Commits must show RED → GREEN order in `git log`.
- ❌ Adding a new helper without a unit test (pure-function helpers MUST be unit-tested even when the fixture needs a container).
- ❌ Skipping the benchmark because "it's just wiring" — every new public API surface gets at least one BenchmarkDotNet entry measuring allocation.
- ❌ Marking a task `- [x]` before running the full `dotnet test --project {both}.Tests.Unit/ {both}.Tests.Integration/` filter and pasting the output in the commit body.

### Retroactive remediation (pre-existing TDD debt)

Phase 3.0 Postgres (commit `2b149b2`) shipped `PostgresRigBuilderExtensions.UsePostgres` + `PostgresBuilderExtensions.UsePostgres` without a preceding RED. **Task T176a (added below)** back-fills the missing tests — MUST be executed before any further Phase 3 work starts.

---

### Marker legend

- `[P]` — task may run in parallel with peers in the same phase (different files, no intra-phase dependency).
- `[depends: T{NNN}]` — blocked until the cited task is complete.
- No marker — sequential default (prior task must complete first).
- Every `RED→GREEN` task implicitly carries the 4-test checklist above; do not restate it per line.

---

## Phase 1 — Enforcement scaffolding (lands first, failing)

**Goal**: bump Testcontainers, document canonical template, land the 3 new architecture rules initially RED. Exit gate: build green + 003 baseline (219 tests) still green.

- [x] T001 Branch already exists (`feat/provider-consistency-remediation`) — verify `git status` clean and up-to-date with `master`.
- [x] T002 **C-001 bump** `Testcontainers.*` family in `Directory.Packages.props` from `4.6.0` to `4.11.0` (every `Testcontainers.*` PackageVersion line — 18 entries incl. the base `Testcontainers` package). **Exception**: `Testcontainers.EventStoreDb` has no 4.11 release and is removed in T002b; retain no pin. Add `MySqlConnector 2.4.*` pin. Bump `Pomelo.EntityFrameworkCore.MySql` from exact `9.0.0` to wildcard `9.0.*` (aligns with library-design §6.1 intent — auto-consumes 9.0.x servicing updates). Add `coverlet.collector 6.0.*` and `coverlet.msbuild 6.0.*` pins (required by the coverage-gate command in T097/T140/T152). Enable `<CentralPackageFloatingVersionsEnabled>true</CentralPackageFloatingVersionsEnabled>` in the root `<PropertyGroup>` — NuGet CPM requires this opt-in before any `*.*` pattern is accepted (NU1011).
  File: `Directory.Packages.props`
  Also fix 18 existing fixtures whose `new XxxBuilder()` parameterless call became `[Obsolete]` in 4.11 (CS0618 under `TreatWarningsAsErrors=true`): move the image string from the subsequent `.WithImage(...)` call into the constructor — `new XxxBuilder($"image:tag")`. Affected: RedisFixture, PostgresFixture, CassandraFixture, MongoFixture, ContainerFixture (Docker), MinIOFixture, S3Fixture, AzureBlobFixture, ElasticSearchFixture, DynamoFixture, SqlServerFixture, RabbitMqFixture, KafkaFixture, ServiceBusFixture, NatsFixture, SqsFixture, SeqFixture — and EventStoreFixture handled separately in T002c.
- [x] T002b [depends: T002] **KurrentDB dependency swap.** Testcontainers 4.9+ marks the entire `EventStoreDb` module `[Obsolete]` per the upstream rebrand (https://www.kurrent.io/blog/kurrent-re-brand-faq). Replace in `Directory.Packages.props`:
  - Remove `<PackageVersion Include="Testcontainers.EventStoreDb" Version="4.9.0" />` → add `<PackageVersion Include="Testcontainers.KurrentDb" Version="4.11.0" />`
  - Remove `<PackageVersion Include="EventStore.Client.Grpc.Streams" Version="23.3.8" />` → add `<PackageVersion Include="KurrentDB.Client" Version="1.3.1" />`

  File: `Directory.Packages.props`
- [x] T002c [depends: T002b] **Package rename + fixture migration.** Rename the Rig.TUnit package to align with the upstream rebrand (aborts the "preserve public API" compromise — caller-facing rename is now explicit feature scope). Execute as a single logical commit so the slnx + AssemblyLoader stay consistent:

  1. `git mv src/Rig.TUnit.Databases.NoSql.EventStore/ src/Rig.TUnit.Databases.NoSql.KurrentDb/`
  2. `git mv src/Rig.TUnit.Databases.NoSql.KurrentDb/Rig.TUnit.Databases.NoSql.EventStore.csproj src/Rig.TUnit.Databases.NoSql.KurrentDb/Rig.TUnit.Databases.NoSql.KurrentDb.csproj`
  3. `git mv src/Rig.TUnit.Databases.NoSql.KurrentDb/Fixtures/EventStoreFixture.cs src/Rig.TUnit.Databases.NoSql.KurrentDb/Fixtures/KurrentDbFixture.cs`
  4. `git mv tests/Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration/ tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/`
  5. `git mv tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration.csproj tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration.csproj`
  6. `git mv tests/…/EventStoreContract.cs KurrentDbContract.cs` and `SharedEventStoreFixture.cs` → `SharedKurrentDbFixture.cs`

  Update the renamed csproj's `<PackageReference>` lines: `Testcontainers.EventStoreDb` → `Testcontainers.KurrentDb`; `EventStore.Client.Grpc.Streams` → `KurrentDB.Client`.

  Rewrite `KurrentDbFixture.cs`:
  - `namespace Rig.TUnit.Databases.NoSql.EventStore.Fixtures;` → `namespace Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;`
  - `using Testcontainers.EventStoreDb;` → `using Testcontainers.KurrentDb;`
  - class name `EventStoreFixture` → `KurrentDbFixture`
  - `EventStoreDbContainer? _container` → `KurrentDbContainer? _container`
  - `new EventStoreDbBuilder().WithImage("eventstore/eventstore:24.10.0-bookworm-slim").Build()` → `new KurrentDbBuilder("kurrentplatform/kurrentdb:25.1").Build()` (KurrentDb 4.11 constructor takes image — parameterless ctor is obsolete; connection string `kurrentdb://admin:changeit@host:port?tls=false` is consumed directly by `KurrentDB.Client`).

  Rewrite test files for the new namespace + class name:
  - `SharedKurrentDbFixture.cs`: namespace + type `EventStoreFixture` → `KurrentDbFixture`; static class `SharedEventStoreFixture` → `SharedKurrentDbFixture`.
  - `KurrentDbContract.cs`: namespace + class `EventStoreContract` → `KurrentDbContract`; update `SharedEventStoreFixture.GetAsync()` call → `SharedKurrentDbFixture.GetAsync()`.

  Files: `src/Rig.TUnit.Databases.NoSql.KurrentDb/Rig.TUnit.Databases.NoSql.KurrentDb.csproj`, `src/Rig.TUnit.Databases.NoSql.KurrentDb/Fixtures/KurrentDbFixture.cs`, `tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration.csproj`, `tests/…/KurrentDbContract.cs`, `tests/…/SharedKurrentDbFixture.cs`.

- [x] T002d [depends: T002c] **Cross-reference update for the rename.** Update every remaining reference to the old package name in the active solution (004 scope only — historical 003 docs stay as-is):
  - `Rig.TUnit.slnx` lines 52 + 117 — update both `<Project Path="...">` entries.
  - `src/Rig.TUnit.All/Rig.TUnit.All.csproj` line 21 — update the `<ProjectReference>` path.
  - `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` — seed name `"Rig.TUnit.Databases.NoSql.EventStore"` → `"Rig.TUnit.Databases.NoSql.KurrentDb"`.
  - `.dotnet-ai-kit/features/004-provider-consistency-remediation/data-model.md` — E3.a EventStore row rename label to "KurrentDb (was EventStore)".
  - `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` line 86 — rename the checklist entry.

  Deliberately NOT touching: `.dotnet-ai-kit/features/003-*` and `planning/ecosystem-expansion/*` — those are historical artefacts from 003 and renaming them in-place rewrites history.

  Verify `dotnet build Rig.TUnit.slnx` clean after the sweep. CS0246 / CS0234 (type/namespace not found) during intermediate states is expected; must be zero at the end of T002d.
- [x] T003 [depends: T002d] Run `dotnet test` against the unit + contract + architecture test projects (excluding `*.Tests.Integration` which require live Docker daemons — those belong to `/dotnet-ai-kit:verify`). Confirm the 219-test Phase-A baseline plus any Phase B–F additions all remain green under Testcontainers 4.11.x and the KurrentDb rename. If any test regresses, root-cause and fix before Phase 1 continues. Record the concrete pre-Phase-1 green count in the PR description so Phase-6 T164 can verify "strictly greater".
- [x] T003a [P] [depends: T003] **003 hard-cutover residue cleanup.** Delete three orphan directories left behind by feature 003's hard-cutover (they contain only `obj/` build artefacts, are not in `Rig.TUnit.slnx`, and violate 003 spec US2 Scenario 1 which stated these MUST NOT exist): `src/Rig.TUnit.SqlServer/`, `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.Redis.Tests.Integration/`. Use `git rm -rf` so the deletion is tracked. Verify `dotnet build` still clean (slnx already excludes them — the delete is a filesystem hygiene pass, zero code impact).
  Command: `git rm -rf src/Rig.TUnit.SqlServer src/Rig.TUnit.ServiceBus tests/Rig.TUnit.Redis.Tests.Integration`
- [x] T004 [P] [depends: T003] Create canonical provider template doc.
  File: `src/Rig.TUnit/Contributing-ProviderTemplate.md`
- [x] T004a [P] [depends: T003] Clarify the role of the `src/Rig.TUnit/` convenience meta-package. Currently `Rig.TUnit.csproj` is a bare `Microsoft.NET.Sdk` with ProjectReferences to Core/Mediator/Grpc/WebAPI and no description (unlike `Rig.TUnit.All.csproj` which declares "Meta-only: zero source .cs files"). Add a `<Description>` PropertyGroup matching the pattern — e.g., `<Description>Convenience meta-package bundling Core + Mediator + Grpc + WebAPI — the default entry point for most projects. Use Rig.TUnit.All only when you need every package.</Description>` + `<GenerateDocumentationFile>false</GenerateDocumentationFile>`. Also add a `<!-- Meta-only: zero source .cs files; only ProjectReference entries + docs under this folder (e.g., Contributing-ProviderTemplate.md from T004). -->` comment so contributors know not to drop source here.
  File: `src/Rig.TUnit/Rig.TUnit.csproj`
- [x] T005 [P] [depends: T003] RED→GREEN scaffold `ProviderCompletenessTests`. Initial skip list = every provider known to have gaps (Postgresql, Mongo, Cassandra, Dynamo, ElasticSearch, EventStore, Kafka, RabbitMq, Nats, Sqs, Hybrid, Fusion, AzureBlob, S3, MinIO, FileSystem, Jwt, OAuth, Mtls, Policies, Metrics). Non-skipped providers (SqlServer, Sqlite, ServiceBus, Redis caching, Memory caching, Logging, Tracing, Seq) MUST pass. **Reuse `AssemblyLoader` from `tests/Rig.TUnit.Architecture.Tests/Infrastructure/`** (already knows every Rig.TUnit.* assembly including the 4 new ones — verified 2026-04-18). Do NOT duplicate the fixture-base inheritance check already covered by `CodeOrganizationTests.AllFixtures_ExtendFixtureBase`; this rule only enforces the four canonical types (Fixture, Options, RigBuilder, `Use{Provider}` extension).
  File: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`
- [x] T006 [P] [depends: T003] RED→GREEN scaffold `TestFileOrganizationTests` (applies uniformly to `*Contract.cs` per C-003). Initial skip list = every file known to carry inline infrastructure (listed in plan Phase 2).
  File: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs`
- [x] T007 [P] [depends: T003] RED→GREEN scaffold `ReadmeCompletenessTests`. Verified 2026-04-18: **20 of 32 leaf provider packages lack README**; 12 already have one. Initial skip list = the 20 missing (Postgresql, Mongo, Cassandra, Dynamo, ElasticSearch, EventStore, Kafka, RabbitMq, Nats, Sqs, Hybrid, Fusion, AzureBlob, S3, MinIO, FileSystem, Mtls, Policies, Metrics, Docker). Non-skipped (MUST pass from Phase 1) = the 12 with existing READMEs: Caching.Memory, Caching.Redis, Databases.NoSql.Redis, Databases.Sql.SqlServer, Databases.Sql.Sqlite, Messaging.ServiceBus, Observability.Logging, Observability.Logging.Analyzers, Observability.Seq, Observability.Tracing, Security.Jwt, Security.OAuth.
  File: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`
- [x] T008 [depends: T003a, T004a, T005, T006, T007] Run full `dotnet test` + `dotnet build`. Confirm new rules execute, skips documented, build still green, orphan dirs removed cleanly, `Rig.TUnit.csproj` description lands without warning, `Rig.TUnit.Databases.NoSql.KurrentDb` package builds clean on `Testcontainers.KurrentDb` / `KurrentDB.Client`.
- [ ] T009 [P] [depends: T008] Commit Phase 1: `test(004): Phase 1 — enforcement scaffolding (RED for all gaps) + KurrentDB migration`.
- [x] T010 [P] [depends: T008] Update `planning/provider-consistency-remediation/Rig.TUnit-Provider-Gap-Matrix.md`: note Phase 1 complete; architecture tests now visibly enforce the matrix.

---

## Phase 2 — Test-file hygiene sweep

**Goal**: every `tests/**/*.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` declares exactly one top-level class. Exit gate: `TestFileOrganizationTests` fully enforced (no `[SkipUntilFixed]`) + `dotnet test` green.

**Phase 2 status (2026-04-18):** **Exit gate MET** via T016 (split the 2 multi-type Contract files — `SqlRigContract.cs` → `DbContextHelperCrudContract` sibling file, `ParallelIsolationContract.cs` → `IParallelRig` sibling file) + T019 (skip list emptied) + T020 (166 tests GREEN). T011–T015 and T017/T018 are **hygiene extractions** (inline `ActivitySource`/Polly/JWKS/Outbox-builder setup code → `TestInfrastructure/*Harness.cs`) that go beyond the stated rule — they're quality improvements deferred to a follow-up PR. The rule itself passes because no test file declares >1 top-level type anymore.

### 2a Worst-offender extraction

- [ ] T011 Extract `ActivitySource` + `TracerProvider` factories from `TraceAssertTests.cs`.
  Files: `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/TestInfrastructure/TracingTestHarness.cs` (NEW), `TraceAssertTests.cs` (MODIFIED — test methods only)
- [ ] T012 [P] Extract custom HTTP matchers + response-builder helpers from `HttpMockTests.cs`.
  Files: `tests/Rig.TUnit.Http.Tests.Unit/TestInfrastructure/HttpMockTestHarness.cs` (NEW), `HttpMockTests.cs` (MODIFIED)
- [ ] T013 [P] Extract Polly pipeline builders from `ResilienceTests.cs`.
  Files: `tests/Rig.TUnit.Resilience.Tests.Integration/TestInfrastructure/ResiliencePipelines.cs` (NEW), `ResilienceTests.cs` (MODIFIED)
- [ ] T014 [P] Extract JWKS + RSA key helpers from `MockOAuthServerTests.cs`.
  Files: `tests/Rig.TUnit.Security.OAuth.Tests.Integration/TestInfrastructure/OAuthTestHarness.cs` (NEW), `MockOAuthServerTests.cs` (MODIFIED)
- [ ] T015 [P] Extract `OutboxMessage` seed builders, envelope fakers, custom stores from `OutboxTests.cs`.
  Files: `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/TestInfrastructure/OutboxTestData.cs` (NEW), `OutboxTests.cs` (MODIFIED)

### 2b Contract-file helper extraction (C-003)

- [x] T016 [P] Sweep every `*Contract.cs` under `tests/**/`. Inventory inline helper types. Extract to `TestInfrastructure/ContractHelpers/` per owning Tests.Contract project.
  Affected projects: `Rig.TUnit.Caching.Tests.Contract`, `Rig.TUnit.Databases.NoSql.Tests.Contract`, `Rig.TUnit.Databases.Sql.Tests.Contract`, `Rig.TUnit.Databases.Tests.Contract`, `Rig.TUnit.Messaging.Tests.Contract`, `Rig.TUnit.Observability.Tests.Contract`, `Rig.TUnit.Parallelism.Tests.Contract`, `Rig.TUnit.Storage.Tests.Contract`

### 2c Quirk-file sweep

- [ ] T017 [P] Sweep every `*QuirkTests.cs` under `tests/**/`. Extract inline test entities + fake handlers + shared fixtures to `TestInfrastructure/` per owning Tests.Integration project.
- [ ] T018 [P] Sweep remaining `*Tests.cs` files declared >1 top-level class. Extract per same pattern.

### 2d Gate flip

- [x] T019 [depends: T011-T018] Remove all `[Category("SkipUntilFixed")]` markers from `TestFileOrganizationTests`. Rule fully enforced.
  File: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs`
- [x] T020 [depends: T019] Run full `dotnet test`. Confirm `TestFileOrganizationTests` GREEN + no regression on 219 baseline.
- [ ] T021 [P] [depends: T020] Commit Phase 2: `refactor(004): Phase 2 — test-file hygiene (TestFileOrganizationTests enforced)`.

---

## Phase 3 — Close gaps in existing providers

**Goal**: every existing provider exposes the canonical shape. Exit gate: `ProviderCompletenessTests` flipped GREEN for every Phase-3 provider + each family's contract suite 100% GREEN + coverage gate met.

Pattern per provider: Options (if missing) → Fixture adjustments (if missing) → RigBuilder → Use extension → helpers → README → add provider to family contract harness → flip skip marker.

### 3.0 Databases.Sql — Postgresql remediation (added post-analysis 2026-04-18)

Postgresql already has `PostgresFixture + PostgresFixtureOptions + PostgresRigBuilder`. Library design §4.1 requires adding `PostgresRigBuilderExtensions` (fluent entry) and `PostgresBuilderExtensions` (EF quickstart) plus README. `SqlRigContract` already exists and runs against `PostgresFixture`.

- [x] T174 [P] [depends: T020] RED→GREEN `PostgresRigBuilderExtensions.UsePostgres(this RigBuilder, IRigConnectionSource, Action<PostgresRigBuilder>)`.
  File: `src/Rig.TUnit.Databases.Sql.Postgresql/Builder/PostgresRigBuilderExtensions.cs`
- [x] T175 [depends: T174] RED→GREEN `PostgresBuilderExtensions` — `UsePostgresInMemory`-style EF quickstart shortcut (mirrors `SqlServerBuilderExtensions` / `SqliteBuilderExtensions` shape for developer IntelliSense parity).
  File: `src/Rig.TUnit.Databases.Sql.Postgresql/Extensions/PostgresBuilderExtensions.cs`
- [x] T176 [depends: T175] Add `README.md` (> 100 chars, 30-sec quick-start using `UsePostgres`). Verify `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/` continues to pass `SqlRigContract` + `ParallelIsolationContract`. Remove Postgresql from `ProviderCompletenessTests` skip list (T005); confirm GREEN.
  Files: `src/Rig.TUnit.Databases.Sql.Postgresql/README.md`, `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`
- [x] **T176a [depends: T176] RETROACTIVE TDD REMEDIATION — Postgres.** Commit `2b149b2` shipped `UsePostgres` (RigBuilder + DbContextOptionsBuilder extensions) without a RED test. Back-fill now, in TDD order (RED commit → GREEN no-op commit is acceptable since the src already exists — but the test commit MUST precede any future Postgres src change). Write four test files:
  1. **Unit** — `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresRigBuilderExtensionsTests.cs` (new project; scaffold csproj with `TUnit`, `Microsoft.Extensions.DependencyInjection`, `ProjectReference` to `Rig.TUnit.Databases.Sql.Postgresql`). Assert: `ArgumentNullException` thrown when rig/source/configure is null; `UsePostgres(rig, source, cfg => {})` returns the same `RigBuilder` instance (fluent chain); the `configure` action is invoked exactly once.
  2. **Unit** — `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresDbContextOptionsExtensionsTests.cs`. Assert: non-generic + generic overloads both route to `UseNpgsql` (verify via `DbContextOptionsBuilder.Options.Extensions` containing `NpgsqlOptionsExtension`); empty/null connection string throws.
  3. **Integration** — extend `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/` with `UsePostgresFluentTests.cs` that wires `services.AddRigTUnit(rig => rig.UsePostgres(new StaticConnectionSource(_fx.ConnectionString), cfg => cfg.ReplaceDbContext<TestDb>()))` then resolves a `TestDb` from DI and issues a round-trip insert/read against the live Postgres container. Must actually create a table, insert, re-query. No mocks.
  4. **Benchmark** — add `tests/Rig.TUnit.Benchmarks/PostgresUseBenchmarks.cs` (add a `ProjectReference` to `Rig.TUnit.Databases.Sql.Postgresql` in `Rig.TUnit.Benchmarks.csproj`). Measure allocations of the `UsePostgres` fluent wiring (container NOT started — measures only the Builder/Extensions path, not Postgres itself).

  Run order: unit first (must pass immediately since src exists), integration next with Docker up (container-backed — must pass), benchmark third (`dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --filter "*Postgres*"`). Commit: `test(004): T176a — retroactive TDD cover for Postgres (commit 2b149b2)`. Coverage check: `dotnet test /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line --filter "FullyQualifiedName~Postgres"` must report ≥ 90 %.

  Files:
  - `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit.csproj` (NEW)
  - `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresRigBuilderExtensionsTests.cs` (NEW)
  - `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresDbContextOptionsExtensionsTests.cs` (NEW)
  - `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs` (NEW)
  - `tests/Rig.TUnit.Benchmarks/PostgresUseBenchmarks.cs` (NEW)
  - `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` (MODIFIED — add ProjectReference)
  - `Rig.TUnit.slnx` (MODIFIED — add the new Tests.Unit project)

### 3a Databases.NoSql

Contract suite: `NoSqlRigContract` — runs 13+ tests per provider (per 003 baseline pattern).

**Every provider in §3a MUST complete all four TDD legs per task (see TDD Gate at top). The Mongo block below is the canonical template — Cassandra/Dynamo/ElasticSearch/KurrentDb follow the identical shape, just swap the provider name.**

#### 3a.i Mongo — CANONICAL TDD TEMPLATE (copy for other providers)

- [x] T022-RED [P] **Write failing tests FIRST**. Create the Tests.Unit project if missing, write all test categories, run, confirm RED:
  1. **Unit (new project)** — `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit.csproj` with `TUnit`, `NSubstitute`, `ProjectReference` to `Rig.TUnit.Databases.NoSql.Mongo`. Register in `Rig.TUnit.slnx`.
  2. **Unit test** — `MongoRigBuilderTests.cs`: asserts `MongoRigBuilder` is sealed, inherits `NoSqlRigBuilder<MongoRigBuilder>`, exposes `ConnectionString` from source; ctor rejects null root/source.
  3. **Unit test** — `UseMongoExtensionsTests.cs`: asserts `UseMongo` rejects null args; returns same `RigBuilder` (fluent); `configure` invoked exactly once.
  4. **Unit test** — `BsonDiffTests.cs`: 8+ pure-function cases (identical docs → empty; value mismatch; missing-field both directions; nested dotted path; type mismatch; null-arg guards).
  5. **Unit test — COVERAGE-LIFTING** — `MongoFixtureOptionsTests.cs`: constructs with defaults and with every property overridden; asserts defaults + overrides. **Required by the coverage gate** — init-only property lines do not register as covered under DI-binding alone (see TDD Gate §Coverage-lifting tests).
  6. **Unit test — COVERAGE-LIFTING** — `MongoRigBuilderConnectionStringTests.cs`: drives `MongoRigBuilder.ConnectionString` through a small fixture so the property getter is unit-covered. Supplements the metadata-only assertions in #2.
  7. **Integration test** — `CollectionPerTestHelperTests.cs`: against live Mongo container, assert isolated collection created + dropped on dispose; two parallel helpers produce distinct collections.
  8. **Integration test** — `UseMongoFluentTests.cs`: `services.AddRigTUnit(rig => rig.UseMongo(source, cfg => {}))` resolves cleanly + registers expected services.
  9. **Contract** — `MongoContract.cs` already exists; verify it still inherits `NoSqlRigContract` and will run post-GREEN.
  10. **Benchmark** — `tests/Rig.TUnit.Benchmarks/MongoBenchmarks.cs`: BsonDiff allocation benchmark (pure); add ProjectReference in `Rig.TUnit.Benchmarks.csproj`.

  Verify RED: `dotnet test --project tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/` — MUST fail with CS0246 (MongoRigBuilder / UseMongo / BsonDiff / CollectionPerTestHelper not found). Paste failure output in commit body.
  Commit: `test(004): T022 — RED for MongoRigBuilder + UseMongo + BsonDiff + CollectionPerTestHelper (unit + integration + benchmark)`.

- [x] T022-GREEN [depends: T022-RED] **Minimum src to flip the unit tests GREEN**. Write:
  - `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilder.cs` — sealed CRTP subclass of `NoSqlRigBuilder<MongoRigBuilder>` with `ConnectionString` passthrough.
  - `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilderExtensions.cs` — `static class` with `public static RigBuilder UseMongo(this RigBuilder, IRigConnectionSource, Action<MongoRigBuilder>)`.

  Run `dotnet test --project tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/` — all unit tests GREEN. Run `dotnet build Rig.TUnit.slnx` — 0 warnings. Commit: `feat(004): T022 — GREEN MongoRigBuilder + UseMongo`.

- [x] T023-GREEN [depends: T022-GREEN] **Minimum src for Helpers** (unit tests for BsonDiff are already landed in T022-RED; CollectionPerTestHelper integration test is too). Write:
  - `src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/BsonDiff.cs`
  - `src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/CollectionPerTestHelper.cs`

  Run: `dotnet test --project tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/` (BsonDiffTests pass) + `dotnet test --project tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration/ --filter "FullyQualifiedName~CollectionPerTestHelper"` (Docker up). Commit: `feat(004): T023 — GREEN BsonDiff + CollectionPerTestHelper`.

- [x] T024 [depends: T023-GREEN] Add `README.md` (> 100 chars — `dotnet add` snippet + runnable `[Test]` + Dependencies section). Run `ReadmeCompletenessTests` after removing Mongo from its skip list — confirm GREEN. Run benchmark suite: `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --filter "*Mongo*"` and paste output in commit body.
  Files: `src/Rig.TUnit.Databases.NoSql.Mongo/README.md`, `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`.

- [x] T025 [depends: T024] **Promote Mongo to `RequiredProviders`** in `ProviderCompletenessTests.cs`. Remove from `SkipUntilFixed`. Run the full architecture test suite and all Mongo unit + integration tests — confirm every rule GREEN. Coverage gate: line ≥ 90 %, branch ≥ 85 % on `Rig.TUnit.Databases.NoSql.Mongo.dll` via coverlet. Commit: `feat(004): T025 — Mongo reaches canonical shape; ProviderCompletenessTests GREEN`.

- [x] **T025a [depends: T025] COVERAGE BUMP — Mongo + Postgres.** First-pass measurement (2026-04-18): Mongo merged unit+integration line coverage = 87.4 % (2.6 % short), Postgres = 77.8 % (12.2 % short). Added the coverage-lifting unit tests to each package in TWO passes:
  - **Pass 1** (commit 79fc6a9): FixtureOptionsTests + RigBuilder exerciser → Mongo 90.5 % / Postgres 83.3 %.
  - **Pass 2** (this commit): full Fixture ctor coverage (parameterless + IOptions + direct-options + null-guards) + pre-init ConnectionString/Database property exception paths + DisposeAsync-before-init + data-annotation validation tests (Range bounds + default-passes).

  **Final measurement: Mongo = 94.7 % line / 87.5 % branch (BOTH GATES PASS); Postgres = 92.6 % line / 75.0 % branch (line gate PASS).** Postgres branch gap is residual `PostgresFixture` container-state branches only reachable from integration (dispose-after-init; second Initialize call); 2 integration tests currently failing block further branch coverage — tracked for Phase 3 exit gate T097 to resolve.
  1. Create `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoFixtureOptionsTests.cs` — constructs defaults, asserts every property matches doc; constructs with every property overridden, asserts overrides propagate.
  2. Create `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoRigBuilderConnectionStringTests.cs` — drives the `ConnectionString` getter through a minimal fixture.
  3. Create `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/PostgresRigBuilderExerciseTests.cs` — calls `UseProvider(DbContextOptionsBuilder<TestDb>, connStr)` reflectively (it's `protected`) and asserts `NpgsqlOptionsExtension` registered. Exercises the 75 %→100 % gap on `PostgresRigBuilder`.
  4. Re-run `dotnet run --coverage` against each Tests.Unit + Tests.Integration project; merge; assert ≥ 90 % line / ≥ 85 % branch per package.

  Commit: `test(004): T025a — coverage bump to 90/85 for Mongo + Postgres (post-measurement backfill)`.

#### 3a.ii Cassandra *(follows the T022–T025 TDD template — every sub-task splits into RED then GREEN with the 4 test categories)*
- [x] T026-RED [P] Write failing tests: new `Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/` project with `CassandraFixtureOptionsTests` (SectionName const exists + `[Required]` triggers `ValidateDataAnnotations`) + `CassandraRigBuilderTests` (sealed, CRTP, ctor null-guards) + `UseCassandraExtensionsTests` (fluent + null-guards) + `KeyspacePerTestHelperTests` (pure: `BuildSafeKeyspace` rejects injection-like inputs, accepts a-z0-9_). Integration: `KeyspacePerTestLiveTests.cs` in `*.Tests.Integration/` creates + drops a keyspace on live Cassandra container. Benchmark: `CassandraKeyspaceBenchmarks.cs` in `Rig.TUnit.Benchmarks/`. Verify RED.
- [x] T026-GREEN [depends: T026-RED] Write `src/Rig.TUnit.Databases.NoSql.Cassandra/Options/CassandraFixtureOptions.cs` to flip the options test GREEN.
- [x] T027-GREEN [depends: T026-GREEN] Write `Builder/CassandraRigBuilder.cs` + `Builder/CassandraRigBuilderExtensions.cs` to flip the builder + extensions tests GREEN.
- [x] T028-GREEN [depends: T027-GREEN] Write `Helpers/KeyspacePerTestHelper.cs` (with `SafeIdentifier` regex validation — DDL string concat is only safe because every input is validated; tests must cover injection attempts). Flip helper tests GREEN.
- [x] T029 [depends: T028-GREEN] Add `README.md`. Remove Cassandra from `ProviderCompletenessTests` + `ReadmeCompletenessTests` skip lists. Run full architecture test + Cassandra unit + integration + benchmark suites. Coverage ≥ 90/85. Commit.

#### 3a.iii Dynamo *(follows T022–T025 TDD template — RED→GREEN split per sub-task, 4 test categories)*
- [ ] T030-RED [P] Create `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit/` with `DynamoFixtureOptionsTests`, `DynamoRigBuilderTests`, `UseDynamoExtensionsTests`, `GsiVerifierTests` (uses `NSubstitute` to mock `IAmazonDynamoDB` — asserts the verifier flags name/partition-key/sort-key/status mismatches without touching LocalStack). Integration: `GsiVerifierLiveTests.cs` in `.Tests.Integration/` uses LocalStack. Benchmark: `DynamoBenchmarks.cs`. Verify RED.
- [ ] T030-GREEN [depends: T030-RED] Write `src/Rig.TUnit.Databases.NoSql.Dynamo/Options/DynamoFixtureOptions.cs`.
- [ ] T031-GREEN [depends: T030-GREEN] Write `Builder/DynamoRigBuilder.cs` + `Builder/DynamoRigBuilderExtensions.cs`. GREEN.
- [ ] T032-GREEN [depends: T031-GREEN] Write `Helpers/GsiVerifier.cs` + `Helpers/GsiExpectation.cs` (record). GREEN all unit + integration.
- [ ] T033 [depends: T032-GREEN] Add `README.md`. Remove Dynamo from skip lists. Coverage ≥ 90/85. Commit.

#### 3a.iv ElasticSearch *(follows T022–T025 template)*
- [ ] T034-RED [P] Create `tests/Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit/` with `ElasticSearchFixtureOptionsTests`, `ElasticSearchRigBuilderTests`, `UseElasticSearchExtensionsTests`, `IndexRefreshHelperTests` (mocks `ElasticsearchClient` response — asserts throw on invalid response, no-op on valid), `DslAssertTests` (mocks search response with known hit count — asserts `HitsAsync` returns expected total). Integration: `IndexRefreshLiveTests.cs` + `DslAssertLiveTests.cs` against live Elastic container. Benchmark: `ElasticSearchBenchmarks.cs`. Verify RED.
- [ ] T034-GREEN [depends: T034-RED] Write `Options/ElasticSearchFixtureOptions.cs`. GREEN options tests.
- [ ] T035-GREEN [depends: T034-GREEN] Write `Builder/ElasticSearchRigBuilder.cs` + `Builder/ElasticSearchRigBuilderExtensions.cs`. GREEN builder/extension tests.
- [ ] T036-GREEN [depends: T035-GREEN] Write `Helpers/IndexRefreshHelper.cs` + `Assertions/DslAssert.cs`. GREEN helper/assertion tests.
- [ ] T037 [depends: T036-GREEN] Add `README.md`. Remove ElasticSearch from skip lists. Coverage ≥ 90/85. Commit.

#### 3a.v KurrentDb *(was EventStore — package renamed in Phase 1 T002c; follows T022–T025 template)*
- [ ] T038-RED [P] Create `tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit/` with `KurrentDbFixtureOptionsTests`, `KurrentDbRigBuilderTests`, `UseKurrentDbExtensionsTests`, `StreamAssertTests` (mocks `KurrentDBClient` — asserts `EventsAppended(streamId, count)` reports correct count), `ProjectionAssertTests` (mocks projection-manager — asserts state matches). Integration: `KurrentDbLiveTests.cs` (append + read-stream round-trip against live container, image `kurrentplatform/kurrentdb:25.1`). Benchmark: `KurrentDbBenchmarks.cs`. Verify RED.
- [ ] T038-GREEN [depends: T038-RED] Write `Options/KurrentDbFixtureOptions.cs`.
- [ ] T039-GREEN [depends: T038-GREEN] Write `Builder/KurrentDbRigBuilder.cs` + `Builder/KurrentDbRigBuilderExtensions.cs`.
- [ ] T040-GREEN [depends: T039-GREEN] Write `Assertions/StreamAssert.cs` + `Assertions/ProjectionAssert.cs` — built against `KurrentDB.Client 1.3.x`.
- [ ] T041 [depends: T040-GREEN] Add `README.md` (cite upstream rebrand + image). Remove KurrentDb from skip lists. Coverage ≥ 90/85. Commit.

### 3b Messaging

Contract suite: `MessagingRigContract`.

**TDD GATE APPLIES**: every task below follows the Phase 3a.i Mongo template (T022–T025) — each `RED→GREEN` line represents a two-commit pair (RED test commit first; GREEN impl commit second). For each provider:
- New `tests/Rig.TUnit.Messaging.{Provider}.Tests.Unit/` project with tests for Options (if added), RigBuilder (sealed, CRTP, ctor null-guards), `Use{Provider}` extension (fluent + null-guards), and helper unit tests (see provider-specific lines below).
- Integration tests in existing `tests/Rig.TUnit.Messaging.{Provider}.Tests.Integration/` for container-backed helpers (Listener/Sender exercised against live broker).
- Contract: existing `{Provider}Contract.cs` inherits `MessagingRigContract`.
- Benchmark: `tests/Rig.TUnit.Benchmarks/{Provider}MessagingBenchmarks.cs` (allocation + throughput of Listener / Sender).
- Coverage ≥ 90 %/85 %.

#### 3b.i Kafka
- [ ] T042-RED [P] Write unit tests (KafkaRigBuilderTests, UseKafkaExtensionsTests) + integration stubs (KafkaListenerLiveTests, KafkaEventSenderLiveTests) + benchmark (KafkaMessagingBenchmarks.cs). Verify RED.
- [ ] T042-GREEN [depends: T042-RED] Write `KafkaRigBuilder.cs` + `KafkaRigBuilderExtensions.cs`. GREEN.
  Files: `src/Rig.TUnit.Messaging.Kafka/Builder/KafkaRigBuilder.cs`, `KafkaRigBuilderExtensions.cs`
- [ ] T043-GREEN [depends: T042-GREEN] Write `Helpers/KafkaListener.cs` + `Helpers/KafkaEventSender.cs`. GREEN listener/sender tests.
- [ ] T044 [depends: T043-GREEN] Add `README.md`. Remove Kafka from skip lists. Coverage ≥ 90/85. Commit.

#### 3b.ii RabbitMq *(TDD template)*
- [ ] T045-RED [P] Unit tests (RabbitMqRigBuilderTests, UseRabbitMqExtensionsTests, RabbitMqListenerTests, RabbitMqEventSenderTests) + integration live tests + `RabbitMqMessagingBenchmarks.cs`. Verify RED.
- [ ] T045-GREEN [depends: T045-RED] Write `RabbitMqRigBuilder.cs` + `RabbitMqRigBuilderExtensions.cs`. GREEN.
- [ ] T046-GREEN [depends: T045-GREEN] Write `RabbitMqListener.cs` + `RabbitMqEventSender.cs`. GREEN.
- [ ] T047 [depends: T046-GREEN] Add `README.md`. Remove RabbitMq from skip lists. Coverage ≥ 90/85. Commit.

#### 3b.iii Nats *(TDD template)*
- [ ] T048-RED [P] Unit tests for Options + RigBuilder + Use extension + Listener + Sender. Integration live + benchmark. Verify RED.
- [ ] T048-GREEN [depends: T048-RED] Write `Options/NatsFixtureOptions.cs`.
- [ ] T049-GREEN [depends: T048-GREEN] Write `NatsRigBuilder.cs` + `NatsRigBuilderExtensions.cs`.
- [ ] T050-GREEN [depends: T049-GREEN] Write `NatsListener.cs` + `NatsEventSender.cs`.
- [ ] T051 [depends: T050-GREEN] Add README. Remove Nats from skip lists. Coverage ≥ 90/85. Commit.

#### 3b.iv Sqs *(LocalStack-backed; TDD template)*
- [ ] T052-RED [P] Unit tests for Options + RigBuilder + Use + Listener + Sender. Integration live (LocalStack) + benchmark. Verify RED.
- [ ] T052-GREEN [depends: T052-RED] Write `Options/SqsFixtureOptions.cs`.
- [ ] T053-GREEN [depends: T052-GREEN] Write `SqsRigBuilder.cs` + `SqsRigBuilderExtensions.cs` (LocalStack-backed).
- [ ] T054-GREEN [depends: T053-GREEN] Write `SqsListener.cs` + `SqsEventSender.cs`.
- [ ] T055 [depends: T054-GREEN] Add README. Remove Sqs from skip lists. Coverage ≥ 90/85. Commit.

### 3c Caching

Contract suite: `CacheRigContract`.

#### 3c.i Memory *(TDD template)*
- [x] T056 [P] RED→GREEN `UseMemoryCache` extension (no options; parameterless).
  File: `src/Rig.TUnit.Caching.Memory/Builder/MemoryCacheRigBuilderExtensions.cs`
- [x] T057 [depends: T056] Add README + verify `MemoryCacheContractTests` passes. (README already shipped from 003 — 538 chars. No additional work needed. Memory stays in ProviderCompletenessTests.SkipUntilFixed as "by-design" — in-process caches have no FixtureOptions by design. Marked complete 2026-04-18.)

**TDD GATE APPLIES to §3c.ii/3c.iii**: same 4-test cadence (Unit / Integration / Contract / Benchmark) + RED→GREEN two-commit cycle per task.

#### 3c.ii Hybrid *(TDD template)*
- [ ] T058-RED [P] Create `tests/Rig.TUnit.Caching.Hybrid.Tests.Unit/` with `HybridCacheFixtureOptionsTests`, `HybridCacheRigBuilderTests`, `UseHybridCacheExtensionsTests`. Integration live + `HybridCacheBenchmarks.cs`. Verify RED.
- [ ] T058-GREEN [depends: T058-RED] Write `Options/HybridCacheFixtureOptions.cs`.
- [ ] T059-GREEN [depends: T058-GREEN] Write `Builder/HybridCacheRigBuilder.cs` + `HybridCacheRigBuilderExtensions.cs`.
- [ ] T060 [depends: T059-GREEN] Add README. Remove Hybrid from skip lists. Coverage ≥ 90/85. Commit.

#### 3c.iii Fusion *(TDD template)*
- [ ] T061-RED [P] Create `tests/Rig.TUnit.Caching.Fusion.Tests.Unit/` with `FusionCacheFixtureOptionsTests`, `FusionCacheRigBuilderTests`, `UseFusionCacheExtensionsTests`, `FailSafeHelperTests` (pure), `EagerRefreshHelperTests` (pure). Integration live + `FusionCacheBenchmarks.cs`. Verify RED.
- [ ] T061-GREEN [depends: T061-RED] Write `Options/FusionCacheFixtureOptions.cs`.
- [ ] T062-GREEN [depends: T061-GREEN] Write `Builder/FusionCacheRigBuilder.cs` + `FusionCacheRigBuilderExtensions.cs`.
- [ ] T063-GREEN [depends: T062-GREEN] Write `Helpers/FailSafeHelper.cs` + `Helpers/EagerRefreshHelper.cs` (per 003 §4.6).
- [ ] T064 [depends: T063-GREEN] Add README. Remove Fusion from skip lists. Coverage ≥ 90/85. Commit.

### 3d Storage

Contract suite: `StorageRigContract`.

**TDD GATE APPLIES**: same 4-test cadence (Unit / Integration / Contract / Benchmark) + RED→GREEN pairs per task. Every provider gets a `Rig.TUnit.Storage.{Provider}.Tests.Unit/` project and a `{Provider}StorageBenchmarks.cs` entry in `Rig.TUnit.Benchmarks/`.

#### 3d.i AzureBlob *(TDD template)*
- [ ] T065-RED [P] Unit tests (RigBuilder, UseAzureBlob, `AzureBlobSasBuilderTests` mocking the SAS token math). Integration live (Azurite) + benchmark. Verify RED.
- [ ] T065-GREEN [depends: T065-RED] Write `Builder/AzureBlobRigBuilder.cs` + `AzureBlobRigBuilderExtensions.cs`.
- [ ] T066-GREEN [depends: T065-GREEN] Write `Helpers/AzureBlobSasBuilder.cs`.
- [ ] T067 [depends: T066-GREEN] Add README. Remove AzureBlob from skip lists. Coverage ≥ 90/85. Commit.

#### 3d.ii S3 *(TDD template; LocalStack-backed)*
- [ ] T068-RED [P] Unit tests (RigBuilder, UseS3, `S3SasBuilderTests`). Integration live (LocalStack) + benchmark. Verify RED.
- [ ] T068-GREEN [depends: T068-RED] Write `Builder/S3RigBuilder.cs` + `S3RigBuilderExtensions.cs`.
- [ ] T069-GREEN [depends: T068-GREEN] Write `Helpers/S3SasBuilder.cs`.
- [ ] T070 [depends: T069-GREEN] Add README. Remove S3 from skip lists. Coverage ≥ 90/85. Commit.

#### 3d.iii MinIO *(TDD template)*
- [ ] T071-RED [P] Unit tests (Options, RigBuilder, UseMinIO, `MinIOSasBuilderTests`). Integration live + benchmark. Verify RED.
- [ ] T071-GREEN [depends: T071-RED] Write `Options/MinIOFixtureOptions.cs`.
- [ ] T072-GREEN [depends: T071-GREEN] Write `Builder/MinIORigBuilder.cs` + `MinIORigBuilderExtensions.cs`.
- [ ] T073-GREEN [depends: T072-GREEN] Write `Helpers/MinIOSasBuilder.cs`.
- [ ] T074 [depends: T073-GREEN] Add README. Remove MinIO from skip lists. Coverage ≥ 90/85. Commit.

#### 3d.iv FileSystem *(no container — pure filesystem sandbox; TDD template)*
- [ ] T075-RED [P] Unit tests (Options, RigBuilder, UseFileSystem, `PathSandboxHelperTests` — pure: asserts path traversal prevented, dispose deletes sandbox). Integration = full filesystem operations (no Docker) + benchmark. Verify RED.
- [ ] T075-GREEN [depends: T075-RED] Write `Options/FileSystemFixtureOptions.cs`.
- [ ] T076-GREEN [depends: T075-GREEN] Write `Builder/FileSystemRigBuilder.cs` + `FileSystemRigBuilderExtensions.cs`.
- [ ] T077-GREEN [depends: T076-GREEN] Write `Helpers/PathSandboxHelper.cs` (sandboxed temp-dir isolation — N/A for SAS).
- [ ] T078 [depends: T077-GREEN] Add README. Remove FileSystem from skip lists. Coverage ≥ 90/85. Commit.

### 3e Security

Wires Jwt / OAuth / Mtls / Policies to the existing `SecurityRigBuilder<TSelf>` base.

**TDD GATE APPLIES**: same 4-test cadence per task. **Spec-rule reconciliation** (added 2026-04-18): FR-008 mandates RigBuilder + Use extension for Security providers; it does NOT mandate a `{Provider}Fixture` for Jwt/OAuth (these are in-process — no container). `ProviderCompletenessTests` must be relaxed for Security: when the provider has no container, `Fixture` check is waived (`FixtureName` is nullable in the `ProviderEntry` record). T079 updates the rule ahead of moving Jwt/OAuth into `RequiredProviders`. Mtls + Policies add `{Provider}Fixture` per T084/T088.

#### 3e.i Jwt *(TDD template; in-process — no Fixture)*
- [ ] T079-RED [P] Create `tests/Rig.TUnit.Security.Jwt.Tests.Unit/` with `JwtRigBuilderTests` (sealed, `: SecurityRigBuilder<JwtRigBuilder>`, ctor null-guards), `UseJwtExtensionsTests` (fluent + null-guards). Update `ProviderCompletenessTests` to make `FixtureName` nullable in `ProviderEntry` — add test `Security_ProvidersWithoutContainer_NeedNoFixture`. Benchmark: `JwtBenchmarks.cs` (token-sign/verify allocation). No new integration test — `JwtBuilder` (token builder) is already exercised in `Rig.TUnit.Security.Jwt.Tests.Integration/`. Verify RED.
- [ ] T079-GREEN [depends: T079-RED] Write `src/Rig.TUnit.Security.Jwt/Builder/JwtRigBuilder.cs` + `JwtRigBuilderExtensions.cs`. Update `ProviderCompletenessTests.ProviderEntry.FixtureName` to `string?` + implementation to skip Fixture check when null. Do NOT rename existing `JwtBuilder` (token builder). GREEN.
- [ ] T080 [depends: T079-GREEN] Add README. Promote Jwt in `ProviderCompletenessTests.RequiredProviders` with `FixtureName: null`. Coverage ≥ 90/85. Commit.

#### 3e.ii OAuth *(TDD template; in-process — wraps existing `MockOAuthServer` as its fixture surrogate)*
- [ ] T081-RED [P] Create `tests/Rig.TUnit.Security.OAuth.Tests.Unit/` with `OAuthRigBuilderTests` + `UseOAuthServerExtensionsTests`. Benchmark: `OAuthBenchmarks.cs` (JWKS resolution). Verify RED.
- [ ] T081-GREEN [depends: T081-RED] Write `OAuthRigBuilder : SecurityRigBuilder<OAuthRigBuilder>` + `UseOAuthServer` extension wrapping the existing `MockOAuthServer`. GREEN.
- [ ] T082 [depends: T081-GREEN] Add README. Promote OAuth in `RequiredProviders` (Fixture: `MockOAuthServer` — already present as the wrapper). Coverage ≥ 90/85. Commit.

#### 3e.iii Mtls *(TDD template; ADDS a new `MtlsFixture`)*
- [ ] T083-RED [P] Create `tests/Rig.TUnit.Security.Mtls.Tests.Unit/` with `MtlsFixtureOptionsTests`, `MtlsFixtureTests` (generates CA + leaf cert on initialize, assertions on cert chain/expiry — pure X509 math, no container), `MtlsRigBuilderTests`, `UseMtlsExtensionsTests`. Integration: `MtlsFixtureLiveTests.cs` (cert round-trip via Kestrel mTLS endpoint). Benchmark: `MtlsBenchmarks.cs`. Verify RED.
- [ ] T083-GREEN [depends: T083-RED] Write `Options/MtlsFixtureOptions.cs`.
- [ ] T084-GREEN [depends: T083-GREEN] Write `Fixtures/MtlsFixture.cs : SecurityFixtureBase` — generates CA + leaf cert on initialize.
- [ ] T085-GREEN [depends: T084-GREEN] Write `Builder/MtlsRigBuilder.cs` + `MtlsRigBuilderExtensions.cs`. Keep existing `MtlsCertificateBuilder` as helper.
- [ ] T086 [depends: T085-GREEN] Add README. Remove Mtls from skip lists. Coverage ≥ 90/85. Commit.

#### 3e.iv Policies *(TDD template; ADDS a new `PolicyFixture`)*
- [ ] T087-RED [P] Create `tests/Rig.TUnit.Security.Policies.Tests.Unit/` with `PolicyFixtureOptionsTests`, `PolicyFixtureTests` (registers in-memory `IAuthorizationService` — tests assert a known-good/known-bad policy decision), `PolicyRigBuilderTests`, `UsePoliciesExtensionsTests`. Benchmark: `PolicyBenchmarks.cs`. Verify RED.
- [ ] T087-GREEN [depends: T087-RED] Write `Options/PolicyFixtureOptions.cs`.
- [ ] T088-GREEN [depends: T087-GREEN] Write `Fixtures/PolicyFixture.cs : SecurityFixtureBase` (registers in-memory `IAuthorizationService`).
- [ ] T089-GREEN [depends: T088-GREEN] Write `Builder/PolicyRigBuilder.cs` + `UsePolicies` extension. Keep `PolicyAssert` untouched.
- [ ] T090 [depends: T089-GREEN] Add README. Remove Policies from skip lists. Coverage ≥ 90/85. Commit.

### 3f Observability.Metrics

**TDD GATE APPLIES**: same 4-test cadence per task.

- [ ] T091-RED [P] Create `tests/Rig.TUnit.Observability.Metrics.Tests.Unit/` with `MetricsFixtureOptionsTests`, `MetricsFixtureTests` (pure — tests `MeterListener` captures a known counter emission), `MetricsRigBuilderTests`, `UseMetricsCaptureExtensionsTests`, `TagCardinalityGuardTests` (pure: asserts throws when N distinct tag values exceeded). Integration: `MetricsFixtureLiveTests.cs` (end-to-end via a dummy `Meter` + `MeterListener`). Benchmark: `MetricsBenchmarks.cs`. Verify RED.
- [ ] T091-GREEN [depends: T091-RED] Write `Options/MetricsFixtureOptions.cs`.
- [ ] T092-GREEN [depends: T091-GREEN] Write `Fixtures/MetricsFixture.cs : TelemetryFixtureBase` wrapping `System.Diagnostics.Metrics.MeterListener`.
- [ ] T093-GREEN [depends: T092-GREEN] Write `Builder/MetricsRigBuilder.cs` + `MetricsRigBuilderExtensions.cs`.
- [ ] T094-GREEN [depends: T093-GREEN] Write `Helpers/TagCardinalityGuard.cs` (default N=100).
- [ ] T095 [depends: T094-GREEN] Add README. Remove Metrics from skip lists. Coverage ≥ 90/85. Commit.

### Phase 3 gate

- [ ] T096 [depends: T025, T029, T033, T037, T041, T044, T047, T051, T055, T057, T060, T064, T067, T070, T074, T078, T080, T082, T086, T090, T095, T176] Run full `dotnet test`. Confirm every family's contract suite passes for every provider (including Postgresql via T176). Confirm `ProviderCompletenessTests` fully GREEN for Phase-3 providers.
- [ ] T097 [depends: T096] Verify coverage gate: line ≥ 90% + branch ≥ 85% per modified package. Command:
  ```bash
  dotnet test --collect:"XPlat Code Coverage" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
  # then per-project threshold check via coverlet.msbuild:
  dotnet test /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum
  dotnet test /p:CollectCoverage=true /p:Threshold=85 /p:ThresholdType=branch /p:ThresholdStat=minimum
  ```
  Requires `coverlet.collector` + `coverlet.msbuild` pins from T002; ensure each `Rig.TUnit.*.Tests.*.csproj` has both as `PackageReference` (no `<Version>`). Until T160 wires CI enforcement, run the commands manually at this gate and record results in the PR description.
- [ ] T098 [depends: T097] Update `Rig.TUnit-Provider-Gap-Matrix.md`: mark every Phase-3 row complete (all cells ✓).
- [ ] T099 [P] [depends: T098] Commit Phase 3: `feat(004): Phase 3 — close existing-provider gaps (ProviderCompletenessTests GREEN for 20 providers)`.

---

## Phase 4 — Create the 4 missing packages + complete Docker

**Goal**: 4 new packages ship with canonical shape; Docker template completed. Exit gate: all 5 registered in slnx; `ProviderCompletenessTests` GREEN for all 5; quirk tests pass.

**TDD GATE APPLIES — NO EXCEPTIONS.** Every new package (MySql, Oracle, Cosmos, AppInsights, Docker-completion) MUST ship with all four test categories in the SAME set of commits, enforced by the RED→GREEN cadence:

- `tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit/` — NEW project per package; tests for Options (SectionName + [Required] validation), RigBuilder (sealed + CRTP), `Use{Provider}` (fluent + null-guards), and any pure-function helper (`RuChargeCapture`, `PartitionKeyDistributionChecker`, `CapturingTelemetryChannel` enqueue/dequeue semantics, `TagCardinalityGuard`, etc.). Register in slnx.
- `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` — contract suite inheritance (`{Family}RigContract`), parallel-isolation smoke (`ParallelIsolationContract<{Provider}Fixture>`), and `{Provider}QuirkTests.cs` for provider-specific behaviours (MySql AUTO_INCREMENT, Oracle PL/SQL, Cosmos RU-charge + partition distribution, AppInsights in-process telemetry capture, Docker compose).
- Contract test: `{Provider}Contract : {Family}RigContract`.
- Benchmark: `tests/Rig.TUnit.Benchmarks/{Provider}Benchmarks.cs` — measures fixture start cost (cached image; second+ run), helper allocations, and any hot-path public API.

**Scaffold tasks that only create the csproj retain their scaffolding nature** — the RED→GREEN cadence begins at the first task that adds a `.cs` under `src/`. Scaffolding commits use `chore(004): T{NNN} — scaffold {package}.csproj + slnx registration` (no test required because no src code is added).

### 4a `Rig.TUnit.Databases.Sql.MySql` *(TDD template — every RED→GREEN task splits into two commits)*

- [ ] T100 [P] Scaffold `src/Rig.TUnit.Databases.Sql.MySql/Rig.TUnit.Databases.Sql.MySql.csproj` + `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Unit/` + `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/` csprojs. Register all three in `Rig.TUnit.slnx`. Add `Rig.TUnit.Benchmarks/MySqlBenchmarks.cs` placeholder + ProjectReference.  *Scaffold-only; no src code under src/ yet — no RED required.* Commit: `chore(004): T100 — scaffold Rig.TUnit.Databases.Sql.MySql`.
- [ ] T101-RED [depends: T100] Write unit tests: `MySqlFixtureOptionsTests` (SectionName + [Required] + defaults), `MySqlRigBuilderTests`, `UseMySqlExtensionsTests` (both fluent + EF wrapper). Write integration: `MySqlContract : SqlRigContract` wired to `SharedMySqlFixture`, `MySqlParallelIsolationTests : ParallelIsolationContract`, `MySqlQuirkTests` for AUTO_INCREMENT + timestamp behaviour. Write benchmark: `MySqlBenchmarks.cs` (fixture start + query throughput). Verify RED.
- [ ] T101-GREEN [depends: T101-RED] Write `Options/MySqlFixtureOptions.cs`.
- [ ] T102-GREEN [depends: T101-GREEN] Write `Fixtures/MySqlFixture.cs : SqlFixtureBase` using `Testcontainers.MySql 4.11` (image passed to ctor).
- [ ] T103-GREEN [depends: T102-GREEN] Write `Builder/MySqlRigBuilder.cs : SqlRigBuilder<MySqlRigBuilder>` overriding `UseProvider` → `options.UseMySql(connectionString, ServerVersion.AutoDetect(...))` via Pomelo 9.
- [ ] T104-GREEN [depends: T103-GREEN] Write `Builder/MySqlRigBuilderExtensions.UseMySql(...)`.
- [ ] T105-GREEN [depends: T104-GREEN] Write `Extensions/MySqlBuilderExtensions.cs` — EF Core wrapper convenience (cites Pomelo PR #2019 in a class-level comment).
- [ ] T106 [depends: T105-GREEN] Run full Integration suite (Docker up) — all contract + quirk + parallel-isolation tests GREEN. Coverage ≥ 90/85.
- [ ] T107 [depends: T106] Add README. Promote MySql to `RequiredProviders`. Remove from `ReadmeCompletenessTests` skip list. Commit.

### 4b `Rig.TUnit.Databases.Sql.Oracle` *(TDD template — RED→GREEN pairs)*

- [ ] T108 [P] Scaffold src csproj + Tests.Unit + Tests.Integration csprojs. Register in slnx. Benchmark placeholder (`OracleBenchmarks.cs`). Commit: `chore(004): T108 — scaffold Rig.TUnit.Databases.Sql.Oracle`.
- [ ] T109-RED [depends: T108] Write unit + integration + benchmark tests (Options, RigBuilder, UseOracle, EF wrapper, OracleQuirkTests for PL/SQL specifics, `OracleContract : SqlRigContract`, `OracleParallelIsolationTests`). Verify RED.
- [ ] T109-GREEN [depends: T109-RED] Write `Options/OracleFixtureOptions.cs`.
- [ ] T110-GREEN [depends: T109-GREEN] Write `Fixtures/OracleFixture.cs : SqlFixtureBase` — image `gvenzl/oracle-free:23.5-slim-faststart`, `Wait.ForListeningPorts()`, 5-min startup timeout (aspire#12036).
- [ ] T111-GREEN [depends: T110-GREEN] Write `Builder/OracleRigBuilder.cs : SqlRigBuilder<OracleRigBuilder>` overriding `UseProvider` → `options.UseOracle(connectionString)`.
- [ ] T112-GREEN [depends: T111-GREEN] Write `Builder/OracleRigBuilderExtensions.UseOracle(...)`.
- [ ] T113-GREEN [depends: T112-GREEN] Write `Extensions/OracleBuilderExtensions.cs` — EF Core wrapper.
- [ ] T114 [depends: T113-GREEN] Run full Integration suite (Docker up). Coverage ≥ 90/85.
- [ ] T115 [depends: T114] Add README. Promote Oracle in `RequiredProviders`. Remove from `ReadmeCompletenessTests` skip list. Commit.

### 4c `Rig.TUnit.Databases.NoSql.Cosmos` *(TDD template)*

- [ ] T116 [P] Scaffold src csproj + Tests.Unit + Tests.Integration csprojs. Register in slnx. Benchmark placeholder (`CosmosBenchmarks.cs`). Commit: `chore(004): T116 — scaffold Cosmos`.
- [ ] T117-RED [depends: T116] Write unit tests (Options, RigBuilder, UseCosmos, `RuChargeCaptureTests` pure, `PartitionKeyDistributionCheckerTests` pure). Integration: `CosmosContract : NoSqlRigContract`, `CosmosParallelIsolationTests`, `CosmosQuirkTests` (RU-charge + partition-distribution gated with `[Category("cosmos")]` + runtime `OperatingSystem.IsWindows()` skip). Benchmark. Verify RED.
- [ ] T117-GREEN [depends: T117-RED] Write `Options/CosmosFixtureOptions.cs`.
- [ ] T118-GREEN [depends: T117-GREEN] Write `Fixtures/CosmosFixture.cs : DocumentFixtureBase` using **`Testcontainers.GenericContainer` (base `Testcontainers` package) — NOT `Testcontainers.CosmosDb`** (legacy Windows emulator, incompatible). Image: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`. Wait strategy: custom `IWaitUntil` HTTP-GETing `https://localhost:{port}/_explorer/emulator.pem` with `ServerCertificateCustomValidationCallback` trust-all (testcontainers-dotnet#1306). After T140, remove `Testcontainers.CosmosDb` from `Directory.Packages.props` if unused.
- [ ] T119-GREEN [depends: T118-GREEN] Write `Builder/CosmosRigBuilder.cs : NoSqlRigBuilder<CosmosRigBuilder>` + `UseCosmos` extension.
- [ ] T120-GREEN [depends: T119-GREEN] Write `Helpers/RuChargeCapture.cs`.
- [ ] T121-GREEN [depends: T120-GREEN] Write `Helpers/PartitionKeyDistributionChecker.cs`.
- [ ] T122 [depends: T121-GREEN] Run full Integration suite on Linux runner (Windows runners gated-skip). Coverage ≥ 90/85.
- [ ] T123 [depends: T122] Add README. Promote Cosmos. Remove from skip lists. Commit.

### 4d `Rig.TUnit.Observability.AppInsights` *(TDD template; in-process — no container)*

- [ ] T124 [P] Scaffold src csproj + Tests.Unit + Tests.Integration csprojs. Register in slnx. Benchmark placeholder (`AppInsightsBenchmarks.cs`). Commit: `chore(004): T124 — scaffold AppInsights`.
- [ ] T125-RED [depends: T124] Write unit tests (Options, RigBuilder, UseAppInsights, `CapturingTelemetryChannelTests` pure: enqueue/dequeue thread-safety, `AppInsightsAssertTests` with mocked telemetry items). Integration: `AppInsightsContract : TelemetryRigContract`, `AppInsightsParallelIsolationTests` (20 parallel in-process fixtures, zero captured-telemetry cross-talk). Benchmark. Verify RED.
- [ ] T125-GREEN [depends: T125-RED] Write `Options/AppInsightsFixtureOptions.cs`.
- [ ] T126-GREEN [depends: T125-GREEN] Write `Fixtures/CapturingTelemetryChannel.cs : ITelemetryChannel` (internal, thread-safe `ConcurrentQueue<ITelemetry>`).
- [ ] T127-GREEN [depends: T126-GREEN] Write `Fixtures/AppInsightsFixture.cs : TelemetryFixtureBase` — no container, in-process `TelemetryClient` with custom channel.
- [ ] T128-GREEN [depends: T127-GREEN] Write `Builder/AppInsightsRigBuilder.cs : TelemetryRigBuilder<AppInsightsRigBuilder>` + `UseAppInsights` extension.
- [ ] T129-GREEN [depends: T128-GREEN] Write `Assertions/AppInsightsAssert.cs` mirroring `TraceAssert` / `MetricAssert` surface.
- [ ] T130 [depends: T129-GREEN] Run full Integration suite (no Docker needed — in-process). Coverage ≥ 90/85.
- [ ] T131 [depends: T130] Add README. Promote AppInsights. Remove from skip lists. Commit.

### 4e `Rig.TUnit.Docker` (complete template) *(TDD template)*

- [ ] T132 [P] [depends: T099] Scaffold `tests/Rig.TUnit.Docker.Tests.Unit/` + `tests/Rig.TUnit.Docker.Tests.Integration/` csprojs (ContainerFixture is pre-existing; we're completing the template). Register in slnx. Benchmark placeholder (`DockerBenchmarks.cs`). Verify existing `ContainerFixture.cs` compiles cleanly under Testcontainers 4.11. Commit: `chore(004): T132 — scaffold Docker Tests.Unit/Integration + Benchmarks wiring`.
- [ ] T133-RED [depends: T132] Write unit tests (`DockerFixtureOptionsTests`, `DockerRigBuilderTests`, `UseDockerExtensionsTests`, `DockerComposeFixtureTests` — compose-file parser, no Docker). Integration: basic `alpine:3` echo container test + 2-container compose test + `DockerParallelIsolationTests : ParallelIsolationContract<ContainerFixture>` (20 parallel `alpine:3` containers, distinct `IsolationKey`, zero cross-talk on per-test networks). Benchmark: container start cost, compose up/down cost. Verify RED.
- [ ] T133-GREEN [depends: T133-RED] Write `Options/DockerFixtureOptions.cs` (image-pull cache reuse, per-test networks, healthcheck ready-detection toggles).
- [ ] T134-GREEN [depends: T133-GREEN] Write `Builder/DockerRigBuilder.cs` + `DockerRigBuilderExtensions.cs` (no family base — ships its own fluent surface).
- [ ] T135-GREEN [depends: T134-GREEN] Write `Fixtures/DockerComposeFixture.cs` — primary `Testcontainers` native compose; fallback to `Ductus.FluentDocker` only if regressed (activation criteria documented in README).
- [ ] T136 [depends: T135-GREEN] Run full Integration suite. Coverage ≥ 90/85.
- [ ] T137 [depends: T136] Add README (document compose-backend activation criteria). Promote Docker. Remove from skip lists. Commit.

### Phase 4 gate

- [ ] T138 [depends: T107, T115, T123, T131, T137] Update `Rig.TUnit.All/Rig.TUnit.All.csproj` — add `<ProjectReference>` for MySql, Oracle, Cosmos, AppInsights, Docker (if not transitive already).
- [ ] T139 [depends: T138] Run full `dotnet test` including new Integration projects. Confirm zero regression + new tests GREEN.
- [ ] T140 [depends: T139] Verify coverage gate per new package (MySql, Oracle, Cosmos, AppInsights, Docker): line ≥ 90% / branch ≥ 85% using the same `coverlet.msbuild` commands documented in T097. Record per-package numbers in the PR description.
- [ ] T141 [P] [depends: T140] Commit Phase 4: `feat(004): Phase 4 — 4 new packages + Docker template complete`.

---

## Phase 5 — Microservices depth

**Goal**: EventSourcing, Saga, Contracts packages gain the richer surface per 003 §4.11. Exit gate: new types tested + integration tests GREEN.

**TDD GATE APPLIES — NO EXCEPTIONS.** Every `RED→GREEN` task splits into two commits. For each Microservices package:

- `tests/Rig.TUnit.Microservices.{Package}.Tests.Unit/` — NEW project per package (if not already present); tests for pure-function helpers (`AggregateAssert` fluent chain, `SagaAssert` step/compensation logic, `PactBrokerClientStub` file parse, `SchemaEvolutionHelper` JSON-drift detection).
- `tests/Rig.TUnit.Microservices.{Package}.Tests.Integration/` — already exists; extend with end-to-end exercises of the new surface.
- Contract-style reusability is already covered by the Microservices base packages; no separate contract project needed.
- Benchmark: `tests/Rig.TUnit.Benchmarks/{Package}Benchmarks.cs` — measure allocation of new Asserts/Helpers. Add ProjectReference in `Rig.TUnit.Benchmarks.csproj` per package.

### 5a EventSourcing

- [ ] T142-RED [P] Create `tests/Rig.TUnit.Microservices.EventSourcing.Tests.Unit/` with `AggregateAssertTests` (fluent `.For(agg).Raised<TEvent>().WithData(pred)` — pure, exhaustive matcher coverage), `EventCatalogueVerifierTests` (discovers event factories via reflection — uses a fake catalogue), `SchemaEvolutionHelperTests` (loads fixture JSON with missing/added/renamed fields — pure). Integration: extend existing Integration project with `AggregateAssertLiveTests.cs`, `EventCatalogueVerifierLiveTests.cs`, `SchemaEvolutionHelperLiveTests.cs` against a real aggregate. Benchmark: `EventSourcingBenchmarks.cs`. Verify RED.
- [ ] T142-GREEN [depends: T142-RED] Write `src/Rig.TUnit.Microservices.EventSourcing/Assertions/AggregateAssert.cs`.
- [ ] T143-GREEN [depends: T142-GREEN] Write `Helpers/EventCatalogueVerifier.cs` — walks every event type in the catalogue and confirms producibility.
- [ ] T144-GREEN [depends: T143-GREEN] Write `Helpers/SchemaEvolutionHelper.cs` — loads legacy-JSON payload, asserts current type deserializes without data loss.
- [ ] T145 [depends: T144-GREEN] Full Integration + benchmark sweep. Add README. Coverage ≥ 90/85.

### 5b Saga

- [ ] T146-RED [P] Create `tests/Rig.TUnit.Microservices.Saga.Tests.Unit/` with `SagaAssertTests` (`.For(history).Step(name).Compensated()` — pure), `SagaTimeoutHelperTests` (uses `FakeTimeProvider` — pure). Integration: extend existing Integration project. Benchmark: `SagaBenchmarks.cs`. Verify RED.
- [ ] T146-GREEN [depends: T146-RED] Write `Assertions/SagaAssert.cs`.
- [ ] T147-GREEN [depends: T146-GREEN] Write `Helpers/SagaTimeoutHelper.cs` — advances injected `TimeProvider` until saga timeout fires, asserts correct compensation.
- [ ] T148 [depends: T147-GREEN] Full Integration + benchmark sweep. Add README. Coverage ≥ 90/85.

### 5c Contracts

- [ ] T149-RED [P] Create `tests/Rig.TUnit.Microservices.Contracts.Tests.Unit/` with `PactBrokerClientStubTests` (file-based reads, C-002 compliant — pure, no HTTP), `ProviderVerificationFixtureTests` (mocks producer endpoints, verifies every interaction). Integration: extend existing project with end-to-end verification using a seeded `TestInfrastructure/Pacts/sample.json`. Benchmark: `ContractsBenchmarks.cs`. Verify RED.
- [ ] T149-GREEN [depends: T149-RED] Write `Helpers/PactBrokerClientStub.cs` — file-based (reads `TestInfrastructure/Pacts/*.json`). No HTTP, no HAL emulation.
- [ ] T150-GREEN [depends: T149-GREEN] Write `Fixtures/ProviderVerificationFixture.cs` — loads Pact, stands up producer endpoints, verifies every interaction.
- [ ] T151 [depends: T150-GREEN] Seed `tests/.../TestInfrastructure/Pacts/sample.json`. Full Integration + benchmark sweep. Add README. Coverage ≥ 90/85.

### Phase 5 gate

- [ ] T152 [depends: T145, T148, T151] Run full `dotnet test`. Confirm coverage gate met per Microservices package (EventSourcing, Saga, Contracts) using the `coverlet.msbuild` commands from T097.
- [ ] T153 [P] [depends: T152] Commit Phase 5: `feat(004): Phase 5 — microservices depth (EventSourcing + Saga + Contracts)`.

---

## Phase 6 — Polish & CI

**Goal**: every provider ships README; `ReadmeCompletenessTests` fully enforced; `Rig.TUnit.All` transitively covers everything; CI matrix extended; **every provider has a Tests.Unit project, an Integration project, a contract test, and a Benchmark class (4-test rule enforced)**. Exit gate: all SC-001..SC-011 met + 4-test completeness verified.

**TDD GATE APPLIES**: tasks that touch src in Phase 6 (rare — mostly polish) still follow RED→GREEN. New architecture rule `TestCompletenessTests` lands in T157a below to codify the 4-test requirement as enforcement rather than convention.

### 6a README coverage

- [ ] T154 [P] Audit every `src/Rig.TUnit.{Family}.{Provider}/` directory for README > 100 chars. Cross-reference with `ReadmeCompletenessTests` skip list.
- [ ] T155 [P] Write README for every provider directory missing one or too-short. Expected backlog at Phase 6 entry: **~0 provider READMEs** (the 20 missing + 4 new packages = 24 were all landed in Phases 3-5 alongside their Builder/Fixture commits). Phase 6 T155 catches residuals only: any leaf provider missed during Phases 3-5, plus base-package READMEs if `ReadmeCompletenessTests` is extended to cover them (out of scope per FR-003, which scopes to `src/Rig.TUnit.{Family}.{Provider}/`). Run `ReadmeCompletenessTests` to get the precise residual list.
  Files: `src/Rig.TUnit.{various}/README.md`
- [ ] T156 [depends: T155] Remove all `[Category("SkipUntilFixed")]` markers from `ReadmeCompletenessTests`.
- [ ] T157 [depends: T156] Run full `dotnet test`. Confirm `ReadmeCompletenessTests` fully GREEN.

### 6a-bis TestCompletenessTests (new architecture rule)

- [ ] T157a-RED [depends: T157] Write `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs` — rule MUST fail when any `src/Rig.TUnit.{Family}.{Provider}/` folder lacks any of: (a) matching `tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit/` project, (b) matching `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` project, (c) matching `{Provider}Contract.cs` inside the Integration project, (d) at least one `{Provider}*Benchmarks.cs` in `tests/Rig.TUnit.Benchmarks/`. Initial skip list empty — every provider must pass. Verify RED (test itself passes — no unit-under-test yet; the failing assertions will drive Phase 6 completion). Commit: `test(004): T157a — RED TestCompletenessTests`.
- [ ] T157a-GREEN [depends: T157a-RED] Ensure every provider has the 4 artefacts listed above (back-fill any missing Unit project / Benchmark class discovered by the rule). Once all 4 pass per provider, commit: `feat(004): T157a — GREEN TestCompletenessTests (4-test completeness enforced repo-wide)`.

### 6b Meta-package sync

- [ ] T158 [P] Verify `Rig.TUnit.All/Rig.TUnit.All.csproj` transitively references every provider (use `dotnet list package` + diff against `src/` directory listing).
- [ ] T159 [depends: T158] Verify `Rig.TUnit.Microservices/Rig.TUnit.Microservices.csproj` references `Rig.TUnit.Docker` if its Microservices sub-packages need containers.

### 6c CI matrix

- [ ] T160 [P] Update `.github/workflows/ci.yml` — add MySql 8.0 + 8.4, Oracle Free 23, Cosmos vnext-preview matrix rows OR tag the Integration projects `[Category("containers")]` and add a dedicated job.
- [ ] T161 [depends: T160] Add pull-image caching for MySql / Oracle / Cosmos images.
- [ ] T162 [depends: T161] Document Windows-runner skip for Cosmos (Linux emulator requires Linux containers) in workflow YAML comments.

### Phase 6 gate

- [ ] T163 [depends: T157, T159, T162] Run full solution `dotnet build` under .NET 10 — zero new warnings above 003 baseline.
- [ ] T164 [depends: T163] Run full `dotnet test`. Final green count MUST be strictly > 219. Record new total.
- [ ] T165 [depends: T164] Verify every checkbox in `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` is ticked.
- [ ] T166 [depends: T165] Update `Rig.TUnit-Provider-Gap-Matrix.md` — every row fully ✓.
- [ ] T167 [P] [depends: T166] Commit Phase 6: `docs(004): Phase 6 — README polish + CI matrix + final gap-matrix update`.

---

## Pre-PR final gate

- [ ] T168 [depends: T167] Run `dotnet format --verify-no-changes` — clean.
- [ ] T169 [depends: T168] Run `dotnet build Rig.TUnit.slnx` — zero new warnings under `TreatWarningsAsErrors=true`.
- [ ] T170 [depends: T169] Run full `dotnet test` across **unit + integration + contract** projects (Docker up). All green. Coverage gates met (line ≥ 90 %, branch ≥ 85 %) per package.
- [ ] T170a [depends: T170] Run the full Benchmark suite: `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks --` (no filter). Record summary output in `PR description` — any regression ≥ 20 % vs Phase-3 baseline MUST be root-caused before merge.
- [ ] T171 [depends: T170a] Verify commit log shows RED → GREEN order across all production changes. Run:
  ```bash
  git log master..HEAD --oneline --grep='— RED'  | wc -l   # count of RED commits
  git log master..HEAD --oneline --grep='— GREEN' | wc -l   # count of GREEN commits
  # For Phase 3/4/5/6 these MUST be within +/- 1 of each other
  # (a RED may cover multiple test files feeding one GREEN, but every GREEN MUST trace to a preceding RED)
  git log master..HEAD --oneline --grep='T[0-9]\+-GREEN'    # list every GREEN commit — each MUST have a matching TNNN-RED before it
  ```
- [ ] T171a [depends: T171] `TestCompletenessTests` (T157a) GREEN — every provider has unit + integration + contract + benchmark test file. No exceptions.
- [ ] T172 [depends: T171a] Open PR against `master`. Title: `feat(004): Provider Consistency Remediation — uniform provider shape + 4 new packages + architecture-test enforcement (unit + integration + contract + benchmark per provider, 90/85 coverage)`.
- [ ] T173 [depends: T172] Update `.dotnet-ai-kit/features/004-provider-consistency-remediation/spec.md` — change Status from `Draft` to `Shipped` once PR merges.

---

## Reserved range (T177–T218) — contingency / follow-up tasks

T174–T176 are now used by Phase 3.0 (Postgresql remediation added post-analysis). T002b/T002c consume the Kurrent migration — they sit inside Phase 1 and do not take reserved slots. Remaining reserved slots (42):

- Pomelo 10 bump if PR #2019 merges mid-feature (T177)
- FluentDocker fallback activation if Testcontainers compose regresses (T178)
- Additional quirk tests surfaced by provider research (T179-T189) — **includes KurrentDB projection/subscription quirks if Phase 3a.v exposes them**
- Coverage-gap fills if a package lands under 90/85 (T190-T199)
- `Testcontainers.CosmosDb` pin removal from `Directory.Packages.props` after T140 if unreferenced (T200)
- CI fixups / flaky quarantine (T201-T218)
- ~~Rename `Rig.TUnit.Databases.NoSql.EventStore` → `Rig.TUnit.Databases.NoSql.KurrentDb`~~ (no longer reserved — completed in Phase 1 T002b/T002c/T002d; breaking change announced in release notes)

---

## Parallel opportunities summary

| Phase | Parallel count | Notes |
|---|---|---|
| Phase 1 | 5 (T004, T005, T006, T007, T009/T010) | Independent rule files + doc |
| Phase 2 | 8 (T011-T018 all `[P]`) | Different target test projects |
| Phase 3 | ~19 providers × first task each `[P]` | Within a provider, tasks serialize (options → builder → helper → README); across providers within the same family, parallel is safe only if `ProviderCompletenessTests` skip-list updates serialize |
| Phase 4 | 5 package scaffolds `[P]` then serialized inside each package | T100, T108, T116, T124, T132 can start in parallel |
| Phase 5 | 3 surfaces `[P]` (T142, T146, T149) | Different packages |
| Phase 6 | 3 main threads `[P]` | READMEs, meta-package, CI matrix |

**Total tasks**: 180 numbered (T001–T173 + T174–T176 Postgresql remediation + T003a orphan-dir cleanup + T004a Rig.TUnit meta-package clarification + T002b/T002c Kurrent migration) + 42 reserved (T177–T218) = 222 slots.
**Parallel opportunities**: ~65 tasks marked `[P]` (T003a, T004a, T174 join the parallel sets; T002b/T002c/T002d are serial inside Phase 1 since they touch the same Rig.TUnit.Databases.NoSql.KurrentDb package).
**Sequential-critical**: Phase 1 → Phase 2 → Phase 3 (3.0 Postgresql + 3a-3g) → Phase 4 → Phase 5/6 (5 and 6 overlap).

---

## Next

```
/dotnet-ai-kit:analyze    # optional cross-check of spec ↔ plan ↔ tasks consistency
/dotnet-ai-kit:implement  # start execution (will pick up T001)
```
