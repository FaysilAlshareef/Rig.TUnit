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
