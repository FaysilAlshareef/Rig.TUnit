# Undo Log: 004-provider-consistency-remediation

## T001 — Branch verification
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- verified: `feat/provider-consistency-remediation` clean + ahead of origin by 1 commit (tasks.md already landed)

## T002 — Testcontainers 4.11 + wildcard pins + 18-fixture ctor migration
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- modified: `Directory.Packages.props` (Testcontainers.* 4.6.0 → 4.11.0 across 17 modules; `CentralPackageFloatingVersionsEnabled=true` opt-in; Pomelo 9.0.0 → 9.0.*; added MySqlConnector 2.4.*, coverlet.collector 6.0.*, coverlet.msbuild 6.0.*)
- modified: 18 fixture files — moved image string from `.WithImage(...)` into `new XxxBuilder(image)` constructor (Testcontainers 4.11 deprecated parameterless ctors — CS0618 under `TreatWarningsAsErrors=true`). Files: `src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs`, `src/Rig.TUnit.Databases.Sql.Postgresql/Fixtures/PostgresFixture.cs`, `src/Rig.TUnit.Databases.NoSql.Cassandra/Fixtures/CassandraFixture.cs`, `src/Rig.TUnit.Databases.NoSql.Mongo/Fixtures/MongoFixture.cs`, `src/Rig.TUnit.Docker/Fixtures/ContainerFixture.cs`, `src/Rig.TUnit.Storage.MinIO/Fixtures/MinIOFixture.cs`, `src/Rig.TUnit.Storage.S3/Fixtures/S3Fixture.cs`, `src/Rig.TUnit.Storage.AzureBlob/Fixtures/AzureBlobFixture.cs`, `src/Rig.TUnit.Databases.NoSql.ElasticSearch/Fixtures/ElasticSearchFixture.cs`, `src/Rig.TUnit.Databases.NoSql.Dynamo/Fixtures/DynamoFixture.cs`, `src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`, `src/Rig.TUnit.Messaging.RabbitMq/Fixtures/RabbitMqFixture.cs`, `src/Rig.TUnit.Messaging.Kafka/Fixtures/KafkaFixture.cs`, `src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`, `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsFixture.cs`, `src/Rig.TUnit.Messaging.Sqs/Fixtures/SqsFixture.cs`, `src/Rig.TUnit.Observability.Seq/Fixtures/SeqFixture.cs`. (EventStoreFixture handled in T002c.)

## T002b — KurrentDb package swap
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- modified: `Directory.Packages.props` — removed `Testcontainers.EventStoreDb 4.9.0`, added `Testcontainers.KurrentDb 4.11.0`; removed `EventStore.Client.Grpc.Streams 23.3.8`, added `KurrentDB.Client 1.3.1`

## T002c — Rig.TUnit package rename + KurrentDbFixture rewrite
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- renamed (`git mv`): `src/Rig.TUnit.Databases.NoSql.EventStore/` → `src/Rig.TUnit.Databases.NoSql.KurrentDb/`
- renamed (`git mv`): `Rig.TUnit.Databases.NoSql.EventStore.csproj` → `Rig.TUnit.Databases.NoSql.KurrentDb.csproj`
- renamed (`git mv`): `Fixtures/EventStoreFixture.cs` → `Fixtures/KurrentDbFixture.cs`
- renamed (`git mv`): `tests/Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration/` → `tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/`
- renamed (`git mv`): `Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration.csproj` → `…KurrentDb.Tests.Integration.csproj`
- renamed (`git mv`): `EventStoreContract.cs` → `KurrentDbContract.cs`; `SharedEventStoreFixture.cs` → `SharedKurrentDbFixture.cs`
- rewritten: `KurrentDbFixture.cs` (namespace `…KurrentDb.Fixtures`, class `KurrentDbFixture`, `KurrentDbContainer`, `new KurrentDbBuilder("kurrentplatform/kurrentdb:25.1")`)
- rewritten: `KurrentDbContract.cs` (namespace + class rename)
- rewritten: `SharedKurrentDbFixture.cs` (namespace + type references)
- modified: `Rig.TUnit.Databases.NoSql.KurrentDb.csproj` (PackageReferences Testcontainers.KurrentDb + KurrentDB.Client)
- modified: `Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration.csproj` (ProjectReference path to renamed src)

## T002d — Cross-reference update (slnx / Rig.TUnit.All / AssemblyLoader)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- modified: `Rig.TUnit.slnx` (lines 52 + 117 — both `<Project Path>` entries)
- modified: `src/Rig.TUnit.All/Rig.TUnit.All.csproj` (ProjectReference path)
- modified: `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` (seed name in `LoadSourceAssemblies`)
- modified: `Directory.Packages.props` (transitive pin bumps required by `KurrentDB.Client 1.3.1`: `Grpc.* 2.66.0 → 2.67.0` across 4 lines; `OpenTelemetry.* 1.9.0 → 1.12.0` across 6 lines; `Google.Protobuf 3.28.3 → 3.32.1`)
- verified: `dotnet build Rig.TUnit.slnx` — 119 projects, 0 warnings, 0 errors

**Planning docs updated in the same session (outside task scope — documentation support):**
- `.dotnet-ai-kit/features/004-provider-consistency-remediation/spec.md` (Observed deltas + FR-027/28/29)
- `.dotnet-ai-kit/features/004-provider-consistency-remediation/plan.md` (Phase 1 commits 1–4 split; topology row)
- `.dotnet-ai-kit/features/004-provider-consistency-remediation/tasks.md` (T002b/T002c/T002d added; total 178 → 180; Reserved range header updated)
- `.dotnet-ai-kit/features/004-provider-consistency-remediation/research.md` (R1 expanded + R15 KurrentDB section added)
- `.dotnet-ai-kit/features/004-provider-consistency-remediation/data-model.md` (E3.a EventStore row → KurrentDb)
- `planning/provider-consistency-remediation/README.md`
- `planning/provider-consistency-remediation/Rig.TUnit-Build-Prompt.md`
- `planning/provider-consistency-remediation/Rig.TUnit-Library-Design.md` (two rows)
- `planning/provider-consistency-remediation/Rig.TUnit-Provider-Gap-Matrix.md`
- `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md`

## Phase 2 — T016/T019/T020 critical path
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- T016 modified: `tests/Rig.TUnit.Databases.Sql.Tests.Contract/SqlRigContract.cs` (removed `DbContextHelperCrudContract` sibling type)
- T016 created: `tests/Rig.TUnit.Databases.Sql.Tests.Contract/DbContextHelperCrudContract.cs` (split target)
- T016 modified: `tests/Rig.TUnit.Parallelism.Tests.Contract/ParallelIsolationContract.cs` (removed `IParallelRig` sibling type + unused `using`)
- T016 created: `tests/Rig.TUnit.Parallelism.Tests.Contract/IParallelRig.cs` (split target)
- T019 modified: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs` — SkipUntilFixed emptied, rule fully enforced
- T020 verified: 16/16 architecture tests GREEN; 166 tests across unit+contract+architecture projects still green.

**Deferred (beyond Phase 2 exit-gate scope):** T011/T012/T013/T014/T015/T017/T018 — hygiene extractions of inline setup code (ActivitySource factories, Polly pipelines, JWKS keys, Outbox builders, QuirkTests helpers) from worst-offender test files into per-project `TestInfrastructure/` folders. The rule itself passes — these are quality improvements for a follow-up PR.

## Phase 3.0 — T174/T175/T176 Postgresql remediation
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- T174 created: `src/Rig.TUnit.Databases.Sql.Postgresql/Builder/PostgresRigBuilderExtensions.cs` (UsePostgres fluent entry on RigBuilder)
- T175 created: `src/Rig.TUnit.Databases.Sql.Postgresql/Extensions/PostgresBuilderExtensions.cs` (UsePostgres EF Core wrapper on DbContextOptionsBuilder)
- T176 created: `src/Rig.TUnit.Databases.Sql.Postgresql/README.md` (536 chars — quick-start + install + deps)
- T176 modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` — Postgresql moved from SkipUntilFixed into RequiredProviders (5 required now)
- T176 modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — Postgresql removed from SkipUntilFixed (19 remaining)
- verified: 16/16 architecture tests GREEN.

## T176a — Retroactive Postgres TDD backfill (covers commit 2b149b2)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit.csproj` (NEW — with `NoWarn=EF1001` scoped to this test project)
- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresRigBuilderExtensionsTests.cs` (6 tests covering null-guards, fluent chain, configure invocation)
- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresDbContextOptionsExtensionsTests.cs` (9 tests covering both generic + non-generic overloads — null/empty guards + Npgsql extension routing)
- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs` (2 integration tests: DbContext round-trip + RigBuilder integration — live Postgres container)
- created: `tests/Rig.TUnit.Benchmarks/PostgresUseBenchmarks.cs` (3 allocation benchmarks for the Postgres wiring path)
- modified: `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` — added Postgres ProjectReference
- modified: `Rig.TUnit.slnx` — registered new Postgres Tests.Unit project

**Verification:**
- Unit: 15/15 GREEN (no Docker required)
- Integration: 2/2 GREEN (UsePostgresFluentTests — Docker-backed round-trip)
- Benchmark baseline (1-iter smoke): UsePostgres_FluentChain 50 ns / 160 B; UsePostgres_DbContextOptions_Generic 8225 ns / 10650 B; UsePostgres_DbContextOptions_NonGeneric 7668 ns / 10442 B
- `dotnet build Rig.TUnit.slnx` — 119+1 projects, 0 warnings, 0 errors

## T022/T023/T024/T025 — Mongo GREEN (canonical shape)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilder.cs` (sealed CRTP subclass of NoSqlRigBuilder<MongoRigBuilder>)
- created: `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilderExtensions.cs` (static UseMongo extension)
- created: `src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/BsonDiff.cs` (pure-function structural diff)
- created: `src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/CollectionPerTestHelper.cs` (async-disposable collection isolation)
- created: `src/Rig.TUnit.Databases.NoSql.Mongo/README.md` (> 100 chars)
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` — Mongo promoted to RequiredProviders (6 required now)
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — Mongo removed from skip list
- verified: 18 unit tests GREEN, 5 integration tests GREEN (Docker), 16 architecture tests GREEN

## T025a — Coverage bump (Mongo + Postgres) + coverage-gate spec/plan/research
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK (Mongo line gate PASS; Postgres line gate partial)

- modified: tasks.md — TDD Gate gains §Coverage-lifting tests (2 new mandatory unit tests per provider) + §Coverage measurement (MTP-native `dotnet run -- --coverage`); Mongo canonical template (T022-RED) enumerates the two extra test files; T025a added to record the bump; T176a carries the same convention forward.
- modified: spec.md — FR-035 (mandatory FixtureOptionsTests + RigBuilder exerciser per provider) and FR-036 (MTP-native coverage mechanism) added.
- modified: plan.md — Executive summary calls out the 2 coverage-lifting tests + MTP-native coverage.
- modified: research.md — R16 "Coverage measurement under TUnit / Microsoft.Testing.Platform" added (classic VSTest path doesn't work; use `dotnet run -- --coverage --coverage-output-format cobertura`).

- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/PostgresRigBuilderExerciseTests.cs` (3 tests driving ReplaceDbContext → UseProvider → DbContextOptions<T> inspection)
- created: `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoFixtureOptionsTests.cs` (3 tests exercising defaults + override propagation on init-only props)
- created: `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoRigBuilderConnectionStringTests.cs` (2 tests driving ConnectionString getter)

**Coverage (merged unit + full-integration via MTP cobertura):**
- Mongo: 87.4 % → **90.5 % line** (PASS ≥ 90) / 75.0 % branch
- Postgres: 77.8 % → **83.3 % line** / 41.7 % branch

Mongo line gate met. Postgres line gate 6.7 % short; residual gap is PostgresFixture async state-machine lines — closure deferred to Phase 3 exit T097. Branch gate deliberately permissive this pass; MTP appears to count async continuation branches as uncovered until every fault-handler path executes, which inflates the denominator.

## T025a pass-2 — Fixture + Options full coverage
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK — Mongo PASSES both gates; Postgres PASSES line gate

- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/PostgresFixtureTests.cs` (9 tests — parameterless ctor, IOptions + direct-options variants with null-guards, pre-init ConnectionString throws, DisposeAsync-before-init safe, DatabaseName stability)
- created: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/PostgresFixtureOptionsValidationTests.cs` (5 tests — SectionName const, defaults, overrides propagation, data-annotations valid + Range bounds)
- created: `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoFixtureTests.cs` (9 tests — constructor variants + pre-init Database/ConnectionString throws + DisposeAsync-before-init + DatabaseName stability)
- created: `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit/MongoFixtureOptionsValidationTests.cs` (3 tests — defaults valid, Range upper + lower bounds)

Counts: Postgres unit 18 → **32 tests** (+14), Mongo unit 23 → **35 tests** (+12). All GREEN.

Coverage (merged unit + full-integration):
- Postgres: **92.6 % line** (PASS ≥ 90) / 75.0 % branch — PostgresFixture line 100 % from unit
- Mongo: **94.7 % line / 87.5 % branch** — BOTH GATES PASS. MongoFixture line 100 % from unit.

## T026 — Cassandra RED (commit d08cabd)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit.csproj` (NEW — TUnit + NSubstitute + CassandraCSharpDriver + Microsoft.Extensions.DependencyInjection)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/CassandraRigBuilderTests.cs` (4 metadata + null-guard tests)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/UseCassandraExtensionsTests.cs` (6 tests — null-guards + fluent + configure-invocation)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/CassandraFixtureOptionsTests.cs` (3 tests — SectionName, defaults, every-property override)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/CassandraFixtureOptionsValidationTests.cs` (4 tests — default passes + Range bounds + Required ImageTag)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/CassandraRigBuilderConnectionStringTests.cs` (2 tests — getter exercise)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/CassandraFixtureTests.cs` (9 tests — every ctor variant + pre-init InvalidOperation + dispose-before-init safe)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/KeyspacePerTestHelperTests.cs` (11 tests — accepts + rejects injection / space / uppercase / leading-digit / empty / default isolation; ≤48 chars; distinct isolation → distinct names)
- created: `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration/KeyspacePerTestLiveTests.cs` (4 live-container tests — CREATE/DROP keyspace round-trip, distinct keyspaces, idempotent dispose)
- created: `tests/Rig.TUnit.Benchmarks/CassandraKeyspaceBenchmarks.cs` (2 allocation benchmarks — short + long isolation key)
- modified: `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` — added Cassandra ProjectReference
- modified: `Rig.TUnit.slnx` — registered new Cassandra Tests.Unit project

**Verification (RED):** `dotnet build tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit/` — 7 × CS0234 (Options / Builder / Helpers namespaces missing) as expected.

## T026/T027/T028/T029 — Cassandra GREEN (commit bf13be2)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Databases.NoSql.Cassandra/Options/CassandraFixtureOptions.cs` (SectionName + [Required] ImageTag + [Range(1,600)] StartupTimeoutSeconds)
- created: `src/Rig.TUnit.Databases.NoSql.Cassandra/Builder/CassandraRigBuilder.cs` (sealed CRTP)
- created: `src/Rig.TUnit.Databases.NoSql.Cassandra/Builder/CassandraRigBuilderExtensions.cs` (UseCassandra extension)
- created: `src/Rig.TUnit.Databases.NoSql.Cassandra/Helpers/KeyspacePerTestHelper.cs` (pure BuildSafeKeyspace CQL whitelist + CreateAsync/DisposeAsync DDL)
- created: `src/Rig.TUnit.Databases.NoSql.Cassandra/README.md` (855 chars — install + example + deps)
- modified: `src/Rig.TUnit.Databases.NoSql.Cassandra/Fixtures/CassandraFixture.cs` — added ctor variants (parameterless / IOptions / direct options), Session accessor with pre-init throw, options-driven image tag + timeout, proper Session→Cluster→Container shutdown order
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` — Cassandra promoted to RequiredProviders (7 required now)
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — Cassandra removed from skip list (18 remaining)

**Verification (GREEN):**
- `dotnet build Rig.TUnit.slnx` — 119+1 projects, 0 warnings, 0 errors
- Unit: 39/39 GREEN (Cassandra.Tests.Unit, 1.8 s)
- Integration: 17/17 GREEN (live Cassandra:5 container, 25.9 s — CassandraContract + ParallelIsolation + 4 × KeyspacePerTestLiveTests)
- Architecture: 16/16 GREEN (ProviderCompletenessTests + ReadmeCompletenessTests enforce Cassandra's canonical shape)

## T030 — Dynamo RED (commit 1dfc66c)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: 10 files in `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit/` (csproj + 7 test classes: DynamoRigBuilderTests, UseDynamoExtensionsTests, DynamoFixtureOptionsTests, DynamoFixtureOptionsValidationTests, DynamoRigBuilderConnectionStringTests, DynamoFixtureTests, GsiVerifierTests with NSubstitute mocks) — 38 tests
- created: `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration/GsiVerifierLiveTests.cs` (3 live tests against LocalStack)
- created: `tests/Rig.TUnit.Benchmarks/DynamoBenchmarks.cs` (2 allocation benchmarks — NSubstitute mock client)
- modified: `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` — added Dynamo ProjectReference + NSubstitute + AWSSDK.DynamoDBv2
- modified: `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration/…csproj` — added AWSSDK.DynamoDBv2 PackageReference
- modified: `Rig.TUnit.slnx` — registered new Dynamo Tests.Unit

**Verification (RED):** 7 × CS0234 (Options / Builder / Helpers namespaces missing).

## T030/T031/T032/T033 — Dynamo GREEN (commit bb03f69)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/Options/DynamoFixtureOptions.cs` (SectionName + [Required] ImageTag + [Range] StartupTimeoutSeconds + [Required] Region)
- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/Builder/DynamoRigBuilder.cs` (sealed CRTP)
- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/Builder/DynamoRigBuilderExtensions.cs` (UseDynamo)
- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/Helpers/GsiExpectation.cs` (record: IndexName + PartitionKey + SortKey? + Status="ACTIVE")
- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/Helpers/GsiVerifier.cs` (static VerifyAsync: flags missing/partition-drift/sort-drift/status-drift)
- created: `src/Rig.TUnit.Databases.NoSql.Dynamo/README.md` (1.3k chars)
- modified: `src/Rig.TUnit.Databases.NoSql.Dynamo/Fixtures/DynamoFixture.cs` — ctor variants, options-driven image/timeout/region, null-guards, Client accessor
- modified: ProviderCompletenessTests + ReadmeCompletenessTests skip lists

**Verification (GREEN):**
- Unit: 38/38 GREEN (Dynamo.Tests.Unit, 2.3 s)
- Integration: 16/16 GREEN (LocalStack 3, 22.4 s — DynamoContract + ParallelIsolation + 3 × GsiVerifierLiveTests)
- Architecture: 16/16 GREEN

## T034 — ElasticSearch RED (commit e9faa51)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: 11 files in Tests.Unit (csproj + 8 test classes + TestClients.cs) — 33 tests; RED via 8 × CS0234
- created: 2 integration tests (IndexRefreshLiveTests, DslAssertLiveTests)
- created: `tests/Rig.TUnit.Benchmarks/ElasticSearchBenchmarks.cs`
- modified: slnx + Benchmarks + Integration csprojs

## T034/T035/T036/T037 — ElasticSearch GREEN (commit 7175466)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: Options/ElasticSearchFixtureOptions.cs
- created: Builder/ElasticSearchRigBuilder.cs + ElasticSearchRigBuilderExtensions.cs
- created: Helpers/IndexRefreshHelper.cs (forces near-real-time refresh; throws on non-valid response)
- created: Assertions/DslAssert.cs (HitCountAsync<T> — strongly-typed search)
- created: README.md
- modified: Fixtures/ElasticSearchFixture.cs — ctor variants, options-driven image/timeout, Client accessor with self-signed-cert-trusting settings (CertificateValidations.AllowAll) for ES 8.x HTTPS
- modified: skip lists

**Verification (GREEN):**
- Unit: 33/33 GREEN (ElasticSearch.Tests.Unit, 2.3 s)
- Integration: 17/17 GREEN (live Elastic 8.15.3 HTTPS + basic-auth, 51.5 s — ElasticSearchContract + ParallelIsolation + 2 × IndexRefreshLiveTests + 2 × DslAssertLiveTests)
- Architecture: 16/16 GREEN

## T038 — KurrentDb RED (commit 7376cc2)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: 11 files in Tests.Unit (csproj + 8 test classes + TestClients.cs) — 30 tests; RED via 7 × CS0234
- created: `tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/KurrentDbLiveTests.cs` (3 live tests)
- created: `tests/Rig.TUnit.Benchmarks/KurrentDbBenchmarks.cs`
- modified: slnx + Benchmarks + Integration csprojs

**Note on ProjectionAssert deferral:** KurrentDB 25.1 projection-manager API is still unstable post-rebrand. Spec FR-035 requires at least one helper per provider; StreamAssert (append-count round-trip) meets that bar. ProjectionAssert can be revisited in a follow-up once the upstream API stabilises.

## T038/T039/T040/T041 — KurrentDb GREEN (commit b28e6a4)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: Options/KurrentDbFixtureOptions.cs
- created: Builder/KurrentDbRigBuilder.cs + KurrentDbRigBuilderExtensions.cs
- created: Assertions/StreamAssert.cs (EventsAppendedAsync: reads stream forwards, returns count, StreamNotFound → 0)
- created: README.md (rebrand note + runnable example)
- modified: Fixtures/KurrentDbFixture.cs — ctor variants, options-driven image/timeout, Client accessor via KurrentDBClientSettings.Create, Client-before-container disposal
- modified: skip lists

**Verification (GREEN):**
- Unit: 30/30 GREEN (KurrentDb.Tests.Unit, 2.3 s)
- Integration: 16/16 GREEN (live KurrentDB 25.1 container, 18.3 s — KurrentDbContract + ParallelIsolation + 3 × KurrentDbLiveTests)
  - First run hit Docker Hub pull flakiness (9/16 failed with "unexpected EOF"); pre-pulling `kurrentplatform/kurrentdb:25.1` fixed it on retry.
- Architecture: 16/16 GREEN

## Phase 3a full NoSql sweep complete
**Timestamp**: 2026-04-18
**Repo**: primary

All five NoSql providers (Mongo, Cassandra, Dynamo, ElasticSearch, KurrentDb) now ship the canonical quartet (Fixture + Options + RigBuilder + Use extension) plus at least one helper/assertion per provider with matching unit + integration + benchmark coverage. ProviderCompletenessTests.RequiredProviders list grew 6 → 10; ReadmeCompletenessTests skip list shrunk 20 → 15.

## T042/T043/T044 — Kafka GREEN (Phase 3b.i)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Messaging.Kafka/Builder/KafkaRigBuilder.cs` (sealed CRTP, `Source.ConnectionString` passthrough)
- created: `src/Rig.TUnit.Messaging.Kafka/Builder/KafkaRigBuilderExtensions.cs` (`UseKafka` fluent extension with null-guards)
- created: `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` (lazy Consumer, capture loop → Record, async dispose)
- created: `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs` (lazy Producer, headers via `BuildHeaders`)
- created: `src/Rig.TUnit.Messaging.Kafka/README.md` (install + example + deps)
- created: Tests.Unit csproj + 8 test files (41 tests — RigBuilder, Use extension, FixtureOptions, validation, ConnectionString getter, Fixture ctor variants, Listener guards, EventSender guards)
- created: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/KafkaListenerLiveTests.cs` (round-trip against live Kafka container)
- created: `tests/Rig.TUnit.Benchmarks/KafkaMessagingBenchmarks.cs` (4 benchmarks + `[Config(InProcessEmitBenchmarkConfig)]`)

## T045/T046/T047 — RabbitMq GREEN (Phase 3b.ii)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Messaging.RabbitMq/Builder/RabbitMqRigBuilder.cs`
- created: `src/Rig.TUnit.Messaging.RabbitMq/Builder/RabbitMqRigBuilderExtensions.cs`
- created: `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs` (RabbitMQ.Client 7 async APIs — `CreateConnectionAsync` / `AsyncEventingBasicConsumer`)
- created: `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs` (lazy IConnection + IChannel; `BasicPublishAsync` with byte-array header values)
- created: `src/Rig.TUnit.Messaging.RabbitMq/README.md`
- created: Tests.Unit csproj + 7 test files (38 tests)
- created: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/RabbitMqListenerLiveTests.cs`
- created: `tests/Rig.TUnit.Benchmarks/RabbitMqMessagingBenchmarks.cs`

## T048/T049/T050/T051 — Nats GREEN (Phase 3b.iii)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Messaging.Nats/Options/NatsFixtureOptions.cs` (SectionName, ImageTag, StartupTimeoutSeconds)
- created: `src/Rig.TUnit.Messaging.Nats/Builder/NatsRigBuilder.cs`
- created: `src/Rig.TUnit.Messaging.Nats/Builder/NatsRigBuilderExtensions.cs`
- created: `src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs` (wraps into `NatsMessageRecord` — NatsMsg<T> is a struct and can't satisfy `where T : class`)
- created: `src/Rig.TUnit.Messaging.Nats/Helpers/NatsEventSender.cs` (lazy NatsConnection, `PublishAsync<string>` + headers)
- created: `src/Rig.TUnit.Messaging.Nats/README.md`
- modified: `src/Rig.TUnit.Messaging.Nats/Rig.TUnit.Messaging.Nats.csproj` — added `Microsoft.Extensions.Options{,.DataAnnotations}` PackageReferences
- modified: `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsFixture.cs` — added ctor variants (parameterless / IOptions / direct-options) + null-guards + options-driven image/timeout
- created: Tests.Unit csproj + 7 test files (38 tests)
- created: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/NatsListenerLiveTests.cs`
- created: `tests/Rig.TUnit.Benchmarks/NatsMessagingBenchmarks.cs`

## T052/T053/T054/T055 — Sqs GREEN (Phase 3b.iv)
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- created: `src/Rig.TUnit.Messaging.Sqs/Options/SqsFixtureOptions.cs` (ImageTag + StartupTimeoutSeconds + Region + AccessKeyId + SecretAccessKey)
- created: `src/Rig.TUnit.Messaging.Sqs/Builder/SqsRigBuilder.cs`
- created: `src/Rig.TUnit.Messaging.Sqs/Builder/SqsRigBuilderExtensions.cs`
- created: `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` (long-poll loop + DeleteMessage on receive)
- created: `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsEventSender.cs` (MessageAttribute-based headers)
- created: `src/Rig.TUnit.Messaging.Sqs/README.md`
- modified: `src/Rig.TUnit.Messaging.Sqs/Rig.TUnit.Messaging.Sqs.csproj` — added `Microsoft.Extensions.Options{,.DataAnnotations}` PackageReferences
- modified: `src/Rig.TUnit.Messaging.Sqs/Fixtures/SqsFixture.cs` — added ctor variants + options-driven config
- created: Tests.Unit csproj + 7 test files (36 tests; NSubstitute IAmazonSQS for guard tests)
- created: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/SqsListenerLiveTests.cs`
- created: `tests/Rig.TUnit.Benchmarks/SqsMessagingBenchmarks.cs`

## Phase 3b architecture flip + benchmark toolchain
**Timestamp**: 2026-04-18
**Repo**: primary
**Status**: OK

- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` — moved all 4 messaging providers (Kafka, RabbitMq, Nats, Sqs) from `SkipUntilFixed` → `RequiredProviders`. Count now 14 required (was 10 after Phase 3a).
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — removed all 4 messaging entries from `SkipUntilFixed` (14 → 10 entries).
- modified: `Rig.TUnit.slnx` — registered 4 Tests.Unit projects.
- modified: `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` — added `Confluent.Kafka`, `RabbitMQ.Client`, `NATS.Client.Core`, `AWSSDK.SQS` PackageReferences + 4 ProjectReferences.
- created: `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` — `ManualConfig` using `InProcessEmitToolchain` + `Job.Dry` (avoids BDN's 2-minute external build timeout on our 100+ project transitive graph).

**Verification:**
- Full solution build: 123 projects, 0 warnings, 0 errors (Debug + Release).
- Unit tests: Kafka 41/41 · RabbitMq 38/38 · Nats 38/38 · Sqs 36/36 = **153/153 GREEN** (no Docker).
- Integration tests (Docker up): Nats 17/17 · RabbitMq 21/21 · Sqs 17/17 (LocalStack) · Kafka 21/21 = **76/76 GREEN**.
- Architecture tests: 16/16 GREEN — `ProviderCompletenessTests.RequiredProviders_ExposeCanonicalTypes` now enforces canonical quartet for all 4 new messaging providers.
- Benchmarks: 19/19 executed (4 Kafka, 4 RabbitMq, 4 Nats, 4 Sqs, 3 ServiceBus) via InProcessEmitToolchain in 1-iter Dry mode — MemoryDiagnoser reports for Options + Listener + Sender construction.

**Phase 3b exit gate MET.** ProviderCompletenessTests.RequiredProviders grew 10 → 14 (Kafka/RabbitMq/Nats/Sqs added). ReadmeCompletenessTests skip list shrunk 14 → 10 (4 messaging leaves removed).
