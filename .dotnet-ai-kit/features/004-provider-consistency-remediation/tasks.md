# Tasks: Provider Consistency Remediation

**Feature**: 004-provider-consistency-remediation | **Mode**: Generic (single-repo .NET 10 library)
**Generated**: 2026-04-18 | **Revised**: 2026-04-18 (post-analysis + full-scan — Postgresql remediation added, ParallelIsolationContract explicit, Cosmos package clarified, coverage command specified, README counts corrected to 20-of-32 leaf providers, orphan 003-era dirs scheduled for cleanup at T003a, Rig.TUnit meta-package description at T004a, **Kurrent migration added at T002b/T002c** — Testcontainers 4.9+ marks the whole `EventStoreDb` module obsolete in favour of the upstream `KurrentDb` rename)
**Source**: [spec.md](spec.md), [plan.md](plan.md), [data-model.md](data-model.md), [research.md](research.md)

## TDD cadence (applies to every task that creates or modifies production code)

Each task with `RED→GREEN` means: write the failing test FIRST (commit `test(004): T{NNN} — RED for {Type}`), then write the minimum implementation (commit `feat(004): T{NNN} — GREEN implement {Type}`). Optional refactor commit follows. Reviewers verify commit order on PR.

Marker legend:
- `[P]` — task may run in parallel with peers in the same phase (different files, no intra-phase dependency)
- `[depends: T{NNN}]` — blocked until the cited task is complete
- No marker — sequential default (prior task must complete first)

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

- [ ] T016 [P] Sweep every `*Contract.cs` under `tests/**/`. Inventory inline helper types. Extract to `TestInfrastructure/ContractHelpers/` per owning Tests.Contract project.
  Affected projects: `Rig.TUnit.Caching.Tests.Contract`, `Rig.TUnit.Databases.NoSql.Tests.Contract`, `Rig.TUnit.Databases.Sql.Tests.Contract`, `Rig.TUnit.Databases.Tests.Contract`, `Rig.TUnit.Messaging.Tests.Contract`, `Rig.TUnit.Observability.Tests.Contract`, `Rig.TUnit.Parallelism.Tests.Contract`, `Rig.TUnit.Storage.Tests.Contract`

### 2c Quirk-file sweep

- [ ] T017 [P] Sweep every `*QuirkTests.cs` under `tests/**/`. Extract inline test entities + fake handlers + shared fixtures to `TestInfrastructure/` per owning Tests.Integration project.
- [ ] T018 [P] Sweep remaining `*Tests.cs` files declared >1 top-level class. Extract per same pattern.

### 2d Gate flip

- [ ] T019 [depends: T011-T018] Remove all `[Category("SkipUntilFixed")]` markers from `TestFileOrganizationTests`. Rule fully enforced.
  File: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs`
- [ ] T020 [depends: T019] Run full `dotnet test`. Confirm `TestFileOrganizationTests` GREEN + no regression on 219 baseline.
- [ ] T021 [P] [depends: T020] Commit Phase 2: `refactor(004): Phase 2 — test-file hygiene (TestFileOrganizationTests enforced)`.

---

## Phase 3 — Close gaps in existing providers

**Goal**: every existing provider exposes the canonical shape. Exit gate: `ProviderCompletenessTests` flipped GREEN for every Phase-3 provider + each family's contract suite 100% GREEN + coverage gate met.

Pattern per provider: Options (if missing) → Fixture adjustments (if missing) → RigBuilder → Use extension → helpers → README → add provider to family contract harness → flip skip marker.

### 3.0 Databases.Sql — Postgresql remediation (added post-analysis 2026-04-18)

Postgresql already has `PostgresFixture + PostgresFixtureOptions + PostgresRigBuilder`. Library design §4.1 requires adding `PostgresRigBuilderExtensions` (fluent entry) and `PostgresBuilderExtensions` (EF quickstart) plus README. `SqlRigContract` already exists and runs against `PostgresFixture`.

- [ ] T174 [P] [depends: T020] RED→GREEN `PostgresRigBuilderExtensions.UsePostgres(this RigBuilder, IRigConnectionSource, Action<PostgresRigBuilder>)`.
  File: `src/Rig.TUnit.Databases.Sql.Postgresql/Builder/PostgresRigBuilderExtensions.cs`
- [ ] T175 [depends: T174] RED→GREEN `PostgresBuilderExtensions` — `UsePostgresInMemory`-style EF quickstart shortcut (mirrors `SqlServerBuilderExtensions` / `SqliteBuilderExtensions` shape for developer IntelliSense parity).
  File: `src/Rig.TUnit.Databases.Sql.Postgresql/Extensions/PostgresBuilderExtensions.cs`
- [ ] T176 [depends: T175] Add `README.md` (> 100 chars, 30-sec quick-start using `UsePostgres`). Verify `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/` continues to pass `SqlRigContract` + `ParallelIsolationContract`. Remove Postgresql from `ProviderCompletenessTests` skip list (T005); confirm GREEN.
  Files: `src/Rig.TUnit.Databases.Sql.Postgresql/README.md`, `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`

### 3a Databases.NoSql

Contract suite: `NoSqlRigContract` — runs 13+ tests per provider (per 003 baseline pattern).

#### 3a.i Mongo
- [ ] T022 [P] RED→GREEN `MongoRigBuilder` + `UseMongo` extension.
  Files: `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilder.cs`, `MongoRigBuilderExtensions.cs`
- [ ] T023 [depends: T022] RED→GREEN `CollectionPerTestHelper` + `BsonDiff`.
  Files: `src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/CollectionPerTestHelper.cs`, `BsonDiff.cs`
- [ ] T024 [depends: T023] Add `README.md` and wire `MongoContractTests : NoSqlRigContract<MongoFixture>` in `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration/`.
  Files: `src/Rig.TUnit.Databases.NoSql.Mongo/README.md`, `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration/MongoContractTests.cs`
- [ ] T025 [depends: T024] Remove Mongo from `ProviderCompletenessTests` skip list. Confirm GREEN.

#### 3a.ii Cassandra
- [ ] T026 [P] RED→GREEN `CassandraFixtureOptions`.
  File: `src/Rig.TUnit.Databases.NoSql.Cassandra/Options/CassandraFixtureOptions.cs`
- [ ] T027 [depends: T026] RED→GREEN `CassandraRigBuilder` + `UseCassandra` extension.
  Files: `src/Rig.TUnit.Databases.NoSql.Cassandra/Builder/CassandraRigBuilder.cs`, `CassandraRigBuilderExtensions.cs`
- [ ] T028 [depends: T027] RED→GREEN `KeyspacePerTestHelper`.
  File: `src/Rig.TUnit.Databases.NoSql.Cassandra/Helpers/KeyspacePerTestHelper.cs`
- [ ] T029 [depends: T028] Add README + `CassandraContractTests`. Remove from skip list. Confirm GREEN.
  Files: `src/Rig.TUnit.Databases.NoSql.Cassandra/README.md`, `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration/CassandraContractTests.cs`

#### 3a.iii Dynamo
- [ ] T030 [P] RED→GREEN `DynamoFixtureOptions`.
  File: `src/Rig.TUnit.Databases.NoSql.Dynamo/Options/DynamoFixtureOptions.cs`
- [ ] T031 [depends: T030] RED→GREEN `DynamoRigBuilder` + `UseDynamo` extension.
  Files: `src/Rig.TUnit.Databases.NoSql.Dynamo/Builder/DynamoRigBuilder.cs`, `DynamoRigBuilderExtensions.cs`
- [ ] T032 [depends: T031] RED→GREEN `GsiVerifier` using LocalStack.
  File: `src/Rig.TUnit.Databases.NoSql.Dynamo/Helpers/GsiVerifier.cs`
- [ ] T033 [depends: T032] Add README + `DynamoContractTests`. Remove from skip list.
  Files: `src/Rig.TUnit.Databases.NoSql.Dynamo/README.md`, `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration/DynamoContractTests.cs`

#### 3a.iv ElasticSearch
- [ ] T034 [P] RED→GREEN `ElasticSearchFixtureOptions`.
  File: `src/Rig.TUnit.Databases.NoSql.ElasticSearch/Options/ElasticSearchFixtureOptions.cs`
- [ ] T035 [depends: T034] RED→GREEN `ElasticSearchRigBuilder` + `UseElasticSearch` extension.
  Files: `src/Rig.TUnit.Databases.NoSql.ElasticSearch/Builder/ElasticSearchRigBuilder.cs`, `ElasticSearchRigBuilderExtensions.cs`
- [ ] T036 [depends: T035] RED→GREEN `IndexRefreshHelper` + `DslAssert`.
  Files: `src/Rig.TUnit.Databases.NoSql.ElasticSearch/Helpers/IndexRefreshHelper.cs`, `Assertions/DslAssert.cs`
- [ ] T037 [depends: T036] Add README + `ElasticSearchContractTests`. Remove from skip list.

#### 3a.v KurrentDb (was EventStore — package renamed in Phase 1 T002c)
- [ ] T038 [P] RED→GREEN `KurrentDbFixtureOptions`.
  File: `src/Rig.TUnit.Databases.NoSql.KurrentDb/Options/KurrentDbFixtureOptions.cs`
- [ ] T039 [depends: T038] RED→GREEN `KurrentDbRigBuilder` + `UseKurrentDb` extension.
  Files: `src/Rig.TUnit.Databases.NoSql.KurrentDb/Builder/KurrentDbRigBuilder.cs`, `KurrentDbRigBuilderExtensions.cs`
- [ ] T040 [depends: T039] RED→GREEN `StreamAssert` + `ProjectionAssert` — built against `KurrentDB.Client 1.3.x`.
  Files: `src/Rig.TUnit.Databases.NoSql.KurrentDb/Assertions/StreamAssert.cs`, `ProjectionAssert.cs`
- [ ] T041 [depends: T040] Add README (cite upstream rebrand + image `kurrentplatform/kurrentdb:25.1`) + `KurrentDbContractTests`. Remove `Rig.TUnit.Databases.NoSql.KurrentDb` from `ProviderCompletenessTests` + `ReadmeCompletenessTests` skip lists.

### 3b Messaging

Contract suite: `MessagingRigContract`.

#### 3b.i Kafka
- [ ] T042 [P] RED→GREEN `KafkaRigBuilder` + `UseKafka` extension.
  Files: `src/Rig.TUnit.Messaging.Kafka/Builder/KafkaRigBuilder.cs`, `KafkaRigBuilderExtensions.cs`
- [ ] T043 [depends: T042] RED→GREEN `KafkaListener : ListenerBase` + `KafkaEventSender : EventSenderBase`.
  Files: `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs`, `KafkaEventSender.cs`
- [ ] T044 [depends: T043] Add README + `KafkaContractTests : MessagingRigContract<KafkaFixture>`. Remove from skip list.

#### 3b.ii RabbitMq
- [ ] T045 [P] RED→GREEN `RabbitMqRigBuilder` + `UseRabbitMq` extension.
  Files: `src/Rig.TUnit.Messaging.RabbitMq/Builder/RabbitMqRigBuilder.cs`, `RabbitMqRigBuilderExtensions.cs`
- [ ] T046 [depends: T045] RED→GREEN `RabbitMqListener` + `RabbitMqEventSender`.
- [ ] T047 [depends: T046] Add README + `RabbitMqContractTests`. Remove from skip list.

#### 3b.iii Nats
- [ ] T048 [P] RED→GREEN `NatsFixtureOptions`.
  File: `src/Rig.TUnit.Messaging.Nats/Options/NatsFixtureOptions.cs`
- [ ] T049 [depends: T048] RED→GREEN `NatsRigBuilder` + `UseNats` extension.
- [ ] T050 [depends: T049] RED→GREEN `NatsListener` + `NatsEventSender`.
- [ ] T051 [depends: T050] Add README + `NatsContractTests`. Remove from skip list.

#### 3b.iv Sqs
- [ ] T052 [P] RED→GREEN `SqsFixtureOptions`.
  File: `src/Rig.TUnit.Messaging.Sqs/Options/SqsFixtureOptions.cs`
- [ ] T053 [depends: T052] RED→GREEN `SqsRigBuilder` + `UseSqs` extension (LocalStack-backed).
- [ ] T054 [depends: T053] RED→GREEN `SqsListener` + `SqsEventSender`.
- [ ] T055 [depends: T054] Add README + `SqsContractTests`. Remove from skip list.

### 3c Caching

Contract suite: `CacheRigContract`.

#### 3c.i Memory
- [ ] T056 [P] RED→GREEN `UseMemoryCache` extension (no options; parameterless).
  File: `src/Rig.TUnit.Caching.Memory/Builder/MemoryCacheRigBuilderExtensions.cs`
- [ ] T057 [depends: T056] Add README + verify `MemoryCacheContractTests` passes.

#### 3c.ii Hybrid
- [ ] T058 [P] RED→GREEN `HybridCacheFixtureOptions`.
  File: `src/Rig.TUnit.Caching.Hybrid/Options/HybridCacheFixtureOptions.cs`
- [ ] T059 [depends: T058] RED→GREEN `HybridCacheRigBuilder` + `UseHybridCache` extension.
  Files: `src/Rig.TUnit.Caching.Hybrid/Builder/HybridCacheRigBuilder.cs`, `HybridCacheRigBuilderExtensions.cs`
- [ ] T060 [depends: T059] Add README + `HybridCacheContractTests`. Remove from skip list.

#### 3c.iii Fusion
- [ ] T061 [P] RED→GREEN `FusionCacheFixtureOptions`.
  File: `src/Rig.TUnit.Caching.Fusion/Options/FusionCacheFixtureOptions.cs`
- [ ] T062 [depends: T061] RED→GREEN `FusionCacheRigBuilder` + `UseFusionCache` extension.
- [ ] T063 [depends: T062] RED→GREEN fail-safe helper + eager-refresh helper per 003 §4.6.
  Files: `src/Rig.TUnit.Caching.Fusion/Helpers/FailSafeHelper.cs`, `EagerRefreshHelper.cs`
- [ ] T064 [depends: T063] Add README + `FusionCacheContractTests`. Remove from skip list.

### 3d Storage

Contract suite: `StorageRigContract`.

#### 3d.i AzureBlob
- [ ] T065 [P] RED→GREEN `AzureBlobRigBuilder` + `UseAzureBlob` extension.
  Files: `src/Rig.TUnit.Storage.AzureBlob/Builder/AzureBlobRigBuilder.cs`, `AzureBlobRigBuilderExtensions.cs`
- [ ] T066 [depends: T065] RED→GREEN `AzureBlobSasBuilder`.
  File: `src/Rig.TUnit.Storage.AzureBlob/Helpers/AzureBlobSasBuilder.cs`
- [ ] T067 [depends: T066] Add README + `AzureBlobContractTests : StorageRigContract<AzureBlobFixture>`.

#### 3d.ii S3
- [ ] T068 [P] RED→GREEN `S3RigBuilder` + `UseS3` extension.
- [ ] T069 [depends: T068] RED→GREEN `S3SasBuilder`.
- [ ] T070 [depends: T069] Add README + `S3ContractTests`.

#### 3d.iii MinIO
- [ ] T071 [P] RED→GREEN `MinIOFixtureOptions`.
  File: `src/Rig.TUnit.Storage.MinIO/Options/MinIOFixtureOptions.cs`
- [ ] T072 [depends: T071] RED→GREEN `MinIORigBuilder` + `UseMinIO` extension.
- [ ] T073 [depends: T072] RED→GREEN `MinIOSasBuilder`.
- [ ] T074 [depends: T073] Add README + `MinIOContractTests`.

#### 3d.iv FileSystem
- [ ] T075 [P] RED→GREEN `FileSystemFixtureOptions`.
- [ ] T076 [depends: T075] RED→GREEN `FileSystemRigBuilder` + `UseFileSystem` extension.
- [ ] T077 [depends: T076] RED→GREEN `PathSandboxHelper` (N/A for SAS — sandboxed temp-dir isolation).
  File: `src/Rig.TUnit.Storage.FileSystem/Helpers/PathSandboxHelper.cs`
- [ ] T078 [depends: T077] Add README + `FileSystemContractTests`.

### 3e Security

Wires Jwt / OAuth / Mtls / Policies to the existing `SecurityRigBuilder<TSelf>` base.

#### 3e.i Jwt
- [ ] T079 [P] RED→GREEN `JwtRigBuilder : SecurityRigBuilder<JwtRigBuilder>` + `UseJwt` extension. Do NOT rename existing `JwtBuilder` (token builder).
  Files: `src/Rig.TUnit.Security.Jwt/Builder/JwtRigBuilder.cs`, `JwtRigBuilderExtensions.cs`
- [ ] T080 [depends: T079] Add README; remove Jwt from skip list.

#### 3e.ii OAuth
- [ ] T081 [P] RED→GREEN `OAuthRigBuilder : SecurityRigBuilder<OAuthRigBuilder>` + `UseOAuthServer` extension (wraps existing `MockOAuthServer`).
- [ ] T082 [depends: T081] Add README; remove OAuth from skip list.

#### 3e.iii Mtls
- [ ] T083 [P] RED→GREEN `MtlsFixtureOptions`.
  File: `src/Rig.TUnit.Security.Mtls/Options/MtlsFixtureOptions.cs`
- [ ] T084 [depends: T083] RED→GREEN `MtlsFixture : SecurityFixtureBase` (generates CA + leaf cert on initialize).
  File: `src/Rig.TUnit.Security.Mtls/Fixtures/MtlsFixture.cs`
- [ ] T085 [depends: T084] RED→GREEN `MtlsRigBuilder` + `UseMtls` extension. Keep existing `MtlsCertificateBuilder` as helper.
- [ ] T086 [depends: T085] Add README; remove Mtls from skip list.

#### 3e.iv Policies
- [ ] T087 [P] RED→GREEN `PolicyFixtureOptions`.
- [ ] T088 [depends: T087] RED→GREEN `PolicyFixture : SecurityFixtureBase` (registers in-memory `IAuthorizationService`).
  File: `src/Rig.TUnit.Security.Policies/Fixtures/PolicyFixture.cs`
- [ ] T089 [depends: T088] RED→GREEN `PolicyRigBuilder` + `UsePolicies` extension. Keep `PolicyAssert` untouched.
- [ ] T090 [depends: T089] Add README; remove Policies from skip list.

### 3f Observability.Metrics

- [ ] T091 [P] RED→GREEN `MetricsFixtureOptions`.
  File: `src/Rig.TUnit.Observability.Metrics/Options/MetricsFixtureOptions.cs`
- [ ] T092 [depends: T091] RED→GREEN `MetricsFixture : TelemetryFixtureBase` wrapping `System.Diagnostics.Metrics.MeterListener`.
  File: `src/Rig.TUnit.Observability.Metrics/Fixtures/MetricsFixture.cs`
- [ ] T093 [depends: T092] RED→GREEN `MetricsRigBuilder : TelemetryRigBuilder<MetricsRigBuilder>` + `UseMetricsCapture` extension.
  Files: `src/Rig.TUnit.Observability.Metrics/Builder/MetricsRigBuilder.cs`, `MetricsRigBuilderExtensions.cs`
- [ ] T094 [depends: T093] RED→GREEN `TagCardinalityGuard` helper (default N=100).
  File: `src/Rig.TUnit.Observability.Metrics/Helpers/TagCardinalityGuard.cs`
- [ ] T095 [depends: T094] Add README; remove Metrics from skip list.

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

### 4a `Rig.TUnit.Databases.Sql.MySql`

- [ ] T100 [P] Scaffold `src/Rig.TUnit.Databases.Sql.MySql/Rig.TUnit.Databases.Sql.MySql.csproj` with ProjectReference to `Rig.TUnit.Databases.Sql`. Register in `Rig.TUnit.slnx`.
- [ ] T101 [depends: T100] RED→GREEN `MySqlFixtureOptions`.
- [ ] T102 [depends: T101] RED→GREEN `MySqlFixture : SqlFixtureBase` using `Testcontainers.MySql` 4.11.
  File: `src/Rig.TUnit.Databases.Sql.MySql/Fixtures/MySqlFixture.cs`
- [ ] T103 [depends: T102] RED→GREEN `MySqlRigBuilder : SqlRigBuilder<MySqlRigBuilder>` overriding `UseProvider` to call `options.UseMySql(connectionString, ServerVersion.AutoDetect(...))` via Pomelo 9.
- [ ] T104 [depends: T103] RED→GREEN `MySqlRigBuilderExtensions.UseMySql(...)`.
- [ ] T105 [depends: T104] RED→GREEN `MySqlBuilderExtensions` — EF Core wrapper convenience (cites Pomelo PR #2019 in a class-level comment).
- [ ] T106 [depends: T105] Create `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/` with `MySqlContractTests : SqlRigContract<MySqlFixture>` + `MySqlParallelIsolationTests : ParallelIsolationContract<MySqlFixture>` + `MySqlQuirkTests` (AUTO_INCREMENT, timestamp behaviour).
- [ ] T107 [depends: T106] Add README; remove MySql from `ProviderCompletenessTests` skip list; confirm GREEN.

### 4b `Rig.TUnit.Databases.Sql.Oracle`

- [ ] T108 [P] Scaffold `src/Rig.TUnit.Databases.Sql.Oracle/Rig.TUnit.Databases.Sql.Oracle.csproj`. Register in slnx.
- [ ] T109 [depends: T108] RED→GREEN `OracleFixtureOptions`.
- [ ] T110 [depends: T109] RED→GREEN `OracleFixture : SqlFixtureBase` — image `gvenzl/oracle-free:23.5-slim-faststart`, `Wait.ForListeningPorts()`, 5-min startup timeout (aspire#12036).
- [ ] T111 [depends: T110] RED→GREEN `OracleRigBuilder : SqlRigBuilder<OracleRigBuilder>` overriding `UseProvider` to call `options.UseOracle(connectionString)`.
- [ ] T112 [depends: T111] RED→GREEN `OracleRigBuilderExtensions.UseOracle(...)`.
- [ ] T113 [depends: T112] RED→GREEN `OracleBuilderExtensions` — EF Core wrapper.
- [ ] T114 [depends: T113] Create `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/` with contract + parallel-isolation + `OracleQuirkTests` (PL/SQL specifics).
- [ ] T115 [depends: T114] Add README; remove from skip list; confirm GREEN.

### 4c `Rig.TUnit.Databases.NoSql.Cosmos`

- [ ] T116 [P] Scaffold `src/Rig.TUnit.Databases.NoSql.Cosmos/Rig.TUnit.Databases.NoSql.Cosmos.csproj`. Register in slnx.
- [ ] T117 [depends: T116] RED→GREEN `CosmosFixtureOptions`.
- [ ] T118 [depends: T117] RED→GREEN `CosmosFixture : DocumentFixtureBase` using **`Testcontainers.GenericContainer` (from the base `Testcontainers` package) — NOT the `Testcontainers.CosmosDb` module**, which targets the legacy Windows emulator and hard-codes an incompatible image path. Image: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`. Wait strategy: custom `IWaitUntil` that HTTP-GETs `https://localhost:{port}/_explorer/emulator.pem` with `ServerCertificateCustomValidationCallback` trust-all (testcontainers-dotnet#1306 workaround). After T140, remove `Testcontainers.CosmosDb 4.6.0` from `Directory.Packages.props` if no production code references it — it becomes dead weight in the transitive graph.
- [ ] T119 [depends: T118] RED→GREEN `CosmosRigBuilder : NoSqlRigBuilder<CosmosRigBuilder>` + `UseCosmos` extension.
- [ ] T120 [depends: T119] RED→GREEN `RuChargeCapture` helper.
  File: `src/Rig.TUnit.Databases.NoSql.Cosmos/Helpers/RuChargeCapture.cs`
- [ ] T121 [depends: T120] RED→GREEN `PartitionKeyDistributionChecker` helper.
- [ ] T122 [depends: T121] Create `tests/Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration/` with `CosmosContractTests : NoSqlRigContract<CosmosFixture>` + `CosmosParallelIsolationTests : ParallelIsolationContract<CosmosFixture>` + `CosmosQuirkTests` (RU-charge via `RuChargeCapture`, partition-distribution via `PartitionKeyDistributionChecker`). Gate Windows runners with `[Category("cosmos")]` + runtime `OperatingSystem.IsWindows()` skip with clear reason (Linux-only emulator).
- [ ] T123 [depends: T122] Add README; remove from skip list; confirm GREEN.

### 4d `Rig.TUnit.Observability.AppInsights`

- [ ] T124 [P] Scaffold `src/Rig.TUnit.Observability.AppInsights/Rig.TUnit.Observability.AppInsights.csproj`. Register in slnx.
- [ ] T125 [depends: T124] RED→GREEN `AppInsightsFixtureOptions`.
- [ ] T126 [depends: T125] RED→GREEN `CapturingTelemetryChannel : ITelemetryChannel` (internal, thread-safe `ConcurrentQueue<ITelemetry>` capture).
  File: `src/Rig.TUnit.Observability.AppInsights/Fixtures/CapturingTelemetryChannel.cs`
- [ ] T127 [depends: T126] RED→GREEN `AppInsightsFixture : TelemetryFixtureBase` — no container, in-process TelemetryClient with custom channel.
- [ ] T128 [depends: T127] RED→GREEN `AppInsightsRigBuilder : TelemetryRigBuilder<AppInsightsRigBuilder>` + `UseAppInsights` extension.
- [ ] T129 [depends: T128] RED→GREEN `AppInsightsAssert` mirroring `TraceAssert` / `MetricAssert` surface.
  File: `src/Rig.TUnit.Observability.AppInsights/Assertions/AppInsightsAssert.cs`
- [ ] T130 [depends: T129] Create `tests/Rig.TUnit.Observability.AppInsights.Tests.Integration/` with `AppInsightsContractTests : TelemetryRigContract<AppInsightsFixture>` + `AppInsightsParallelIsolationTests : ParallelIsolationContract<AppInsightsFixture>` (20 parallel in-process fixtures with zero captured-telemetry cross-talk).
- [ ] T131 [depends: T130] Add README; remove from skip list; confirm GREEN.

### 4e `Rig.TUnit.Docker` (complete template)

- [ ] T132 [P] [depends: T099] Verify existing `src/Rig.TUnit.Docker/Fixtures/ContainerFixture.cs` compiles cleanly under Testcontainers 4.11.
- [ ] T133 [depends: T132] RED→GREEN `DockerFixtureOptions` (image-pull cache reuse, per-test networks, healthcheck ready-detection toggles).
  File: `src/Rig.TUnit.Docker/Options/DockerFixtureOptions.cs`
- [ ] T134 [depends: T133] RED→GREEN `DockerRigBuilder` + `UseDocker` extension (no family base — ships its own fluent surface).
  Files: `src/Rig.TUnit.Docker/Builder/DockerRigBuilder.cs`, `DockerRigBuilderExtensions.cs`
- [ ] T135 [depends: T134] RED→GREEN `DockerComposeFixture` — primary `Testcontainers` native compose; fallback to `Ductus.FluentDocker` only if regressed (documented activation criteria in README).
  File: `src/Rig.TUnit.Docker/Fixtures/DockerComposeFixture.cs`
- [ ] T136 [depends: T135] Create `tests/Rig.TUnit.Docker.Tests.Integration/` with basic `alpine:3` echo container test + 2-container compose test + `DockerParallelIsolationTests : ParallelIsolationContract<ContainerFixture>` (20 parallel `alpine:3` containers, distinct `IsolationKey`, zero cross-talk on per-test networks). Register in slnx.
- [ ] T137 [depends: T136] Add README (document compose-backend activation criteria); confirm `ProviderCompletenessTests` GREEN for Docker.

### Phase 4 gate

- [ ] T138 [depends: T107, T115, T123, T131, T137] Update `Rig.TUnit.All/Rig.TUnit.All.csproj` — add `<ProjectReference>` for MySql, Oracle, Cosmos, AppInsights, Docker (if not transitive already).
- [ ] T139 [depends: T138] Run full `dotnet test` including new Integration projects. Confirm zero regression + new tests GREEN.
- [ ] T140 [depends: T139] Verify coverage gate per new package (MySql, Oracle, Cosmos, AppInsights, Docker): line ≥ 90% / branch ≥ 85% using the same `coverlet.msbuild` commands documented in T097. Record per-package numbers in the PR description.
- [ ] T141 [P] [depends: T140] Commit Phase 4: `feat(004): Phase 4 — 4 new packages + Docker template complete`.

---

## Phase 5 — Microservices depth

**Goal**: EventSourcing, Saga, Contracts packages gain the richer surface per 003 §4.11. Exit gate: new types tested + integration tests GREEN.

### 5a EventSourcing

- [ ] T142 [P] RED→GREEN `AggregateAssert` fluent: `AggregateAssert.For(aggregate).Raised<TEvent>().WithData(predicate)`.
  File: `src/Rig.TUnit.Microservices.EventSourcing/Assertions/AggregateAssert.cs`
- [ ] T143 [depends: T142] RED→GREEN `EventCatalogueVerifier` — walks every event type in the catalogue and confirms each is producible through its factory.
  File: `src/Rig.TUnit.Microservices.EventSourcing/Helpers/EventCatalogueVerifier.cs`
- [ ] T144 [depends: T143] RED→GREEN `SchemaEvolutionHelper` — loads legacy-JSON payload, asserts current type deserializes without data loss.
- [ ] T145 [depends: T144] Extend `tests/Rig.TUnit.Microservices.EventSourcing.Tests.Integration/` with coverage for all three new surfaces. Add README.

### 5b Saga

- [ ] T146 [P] RED→GREEN `SagaAssert` fluent: `SagaAssert.For(history).Step(name).Compensated()`.
  File: `src/Rig.TUnit.Microservices.Saga/Assertions/SagaAssert.cs`
- [ ] T147 [depends: T146] RED→GREEN `SagaTimeoutHelper` — advances injected `TimeProvider` until saga timeout fires, asserts correct compensation.
  File: `src/Rig.TUnit.Microservices.Saga/Helpers/SagaTimeoutHelper.cs`
- [ ] T148 [depends: T147] Extend `tests/Rig.TUnit.Microservices.Saga.Tests.Integration/`. Add README.

### 5c Contracts

- [ ] T149 [P] RED→GREEN `PactBrokerClientStub` — file-based (reads `TestInfrastructure/Pacts/*.json` per C-002). No HTTP, no HAL emulation.
  File: `src/Rig.TUnit.Microservices.Contracts/Helpers/PactBrokerClientStub.cs`
- [ ] T150 [depends: T149] RED→GREEN `ProviderVerificationFixture` — loads Pact, stands up producer endpoints, verifies every interaction.
  File: `src/Rig.TUnit.Microservices.Contracts/Fixtures/ProviderVerificationFixture.cs`
- [ ] T151 [depends: T150] Extend `tests/Rig.TUnit.Microservices.Contracts.Tests.Integration/`. Add README. Seed `TestInfrastructure/Pacts/sample.json` fixture.

### Phase 5 gate

- [ ] T152 [depends: T145, T148, T151] Run full `dotnet test`. Confirm coverage gate met per Microservices package (EventSourcing, Saga, Contracts) using the `coverlet.msbuild` commands from T097.
- [ ] T153 [P] [depends: T152] Commit Phase 5: `feat(004): Phase 5 — microservices depth (EventSourcing + Saga + Contracts)`.

---

## Phase 6 — Polish & CI

**Goal**: every provider ships README; `ReadmeCompletenessTests` fully enforced; `Rig.TUnit.All` transitively covers everything; CI matrix extended. Exit gate: all SC-001..SC-011 met.

### 6a README coverage

- [ ] T154 [P] Audit every `src/Rig.TUnit.{Family}.{Provider}/` directory for README > 100 chars. Cross-reference with `ReadmeCompletenessTests` skip list.
- [ ] T155 [P] Write README for every provider directory missing one or too-short. Expected backlog at Phase 6 entry: **~0 provider READMEs** (the 20 missing + 4 new packages = 24 were all landed in Phases 3-5 alongside their Builder/Fixture commits). Phase 6 T155 catches residuals only: any leaf provider missed during Phases 3-5, plus base-package READMEs if `ReadmeCompletenessTests` is extended to cover them (out of scope per FR-003, which scopes to `src/Rig.TUnit.{Family}.{Provider}/`). Run `ReadmeCompletenessTests` to get the precise residual list.
  Files: `src/Rig.TUnit.{various}/README.md`
- [ ] T156 [depends: T155] Remove all `[Category("SkipUntilFixed")]` markers from `ReadmeCompletenessTests`.
- [ ] T157 [depends: T156] Run full `dotnet test`. Confirm `ReadmeCompletenessTests` fully GREEN.

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
- [ ] T169 [depends: T168] Run `dotnet build` — zero new warnings.
- [ ] T170 [depends: T169] Run full `dotnet test` — all tests green, coverage gates met.
- [ ] T171 [depends: T170] Verify commit log shows RED → GREEN → REFACTOR order across all production changes (`git log --oneline --grep='— RED'` and `— GREEN` counts match within each phase).
- [ ] T172 [depends: T171] Open PR against `master`. Title: `feat(004): Provider Consistency Remediation — uniform provider shape + 4 new packages + architecture-test enforcement`.
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
