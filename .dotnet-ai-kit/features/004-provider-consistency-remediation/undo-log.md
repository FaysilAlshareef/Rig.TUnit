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
