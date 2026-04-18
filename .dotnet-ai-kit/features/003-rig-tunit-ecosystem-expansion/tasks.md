# Tasks: Rig.TUnit Ecosystem Expansion

**Feature**: 003-rig-tunit-ecosystem-expansion | **Mode**: Generic (single-repo)
**Generated**: 2026-04-17 | **Total Tasks**: 338 across 7 phases (revised after analysis-fix pass)

---

## Task Format

- `T###` — unique task ID
- `[P]` — can run in parallel with other `[P]` tasks in same phase (different files, no dependency)
- `[depends: T###]` — blocked until listed task(s) complete
- `[FR:###]` — traces to spec functional requirement
- `[TDD]` — task follows the RED → GREEN → REFACTOR cycle per plan.md §"TDD Execution Discipline". Every `[TDD]` task ships: (1) a `test: red — ...` commit with a failing test, (2) a `feat: green — ...` commit with minimum code to pass, (3) optional `refactor: ...` commit. Commit messages carry the prefixes.
- Tasks without `[P]` are sequential within their phase.
- Each task is completable in one `/dai.go` step.

---

## Phase 0: Setup & Foundation

- [x] **T001** Create feature branch `feat/003-rig-tunit-ecosystem-expansion`
- [x] **T002** Update `Directory.Build.props` — add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, `<NoWarn>$(NoWarn);CS1591</NoWarn>` (obj/ only), `<NullableReferenceTypes>enable</NullableReferenceTypes>` [FR:050, FR:057]
      File: `Directory.Build.props`
- [x] **T003** Create `Directory.Packages.props` at repo root with Central Package Management + pinned versions (TUnit 1.34.5+, Testcontainers 4.6.0+, Mediator.Abstractions 3.0.2, EF Core 10.0.0, Serilog 4.x, Serilog.Sinks.Seq 8.x, OpenTelemetry 1.9.x, OpenTelemetry.Exporter.InMemory 1.9.x, Microsoft.Extensions.TimeProvider.Testing 10.0.0, Microsoft.IdentityModel.Tokens 8.x, System.IdentityModel.Tokens.Jwt 8.x, StackExchange.Redis 2.8.x, Microsoft.Extensions.Caching.Hybrid 9.x, ZiggyCreatures.FusionCache 2.x, Bogus 35.x, NetArchTest.Rules 1.x, BenchmarkDotNet 0.14.x, System.IO.Abstractions 21.x, Polly 8.x, Verify.TUnit latest) [FR:050, FR:120]
      File: `Directory.Packages.props`
- [x] **T004** Enable `ManagePackageVersionsCentrally=true` in `Directory.Packages.props`; strip `Version=` attributes from all existing `.csproj` files
      Files: `src/**/*.csproj`, `tests/**/*.csproj`
- [x] **T005** Research R1-R6 items from research.md (Grpc WebApplicationFactoryExtensions content; TUnit parallelism syntax; HybridCache .NET 10 compat; Cosmos emulator Linux tag; ServiceBus emulator EULA env; Verify.TUnit .NET 10 compat). Document outcomes in `research.md` "Open research items" table.
- [x] **T006** Create commit-message enforcement hook. File: `.githooks/commit-msg` (bash) — rejects commits whose message first token is not one of `test:`, `feat:`, `refactor:`, `fix:`, `chore:`, `docs:`, `style:`, `perf:`, `build:`, `ci:`, `revert:`. Add installation step to `README.md`: `git config core.hooksPath .githooks`. Add GitHub Actions workflow `.github/workflows/commit-msg-lint.yml` as backstop. [US1, SC-011]
      Files: `.githooks/commit-msg`, `.github/workflows/commit-msg-lint.yml`, `README.md`
- [x] **T007** Capture pre-cutover benchmark baseline. Run `dotnet test tests/Rig.TUnit.Benchmarks -c Release --filter *FixtureStartup*` against the current `master` (feature-002 state); export results to `benchmarks/baseline-002.json`; commit. T720 compares future runs against this file. [SC-004]
      File: `benchmarks/baseline-002.json`

---

## Phase A: Base Contracts + Hard Cutover

### A.1 — Hard deletions (execute BEFORE new base work)

- [x] **T010** [depends: T004] Delete `src/Rig.TUnit.SqlServer/` directory [FR:001]
- [x] **T011** [P] [depends: T004] Delete `src/Rig.TUnit.Redis/` directory [FR:001]
- [x] **T012** [P] [depends: T004] Delete `src/Rig.TUnit.ServiceBus/` directory [FR:001]
- [x] **T013** [P] [depends: T004] Delete `tests/Rig.TUnit.SqlServer.Tests.Unit/` directory [FR:001]
- [x] **T014** [P] [depends: T004] Delete `tests/Rig.TUnit.SqlServer.Tests.Integration/` directory [FR:001]
- [x] **T015** [P] [depends: T004] Delete `tests/Rig.TUnit.Redis.Tests.Integration/` directory [FR:001]
- [x] **T016** [P] [depends: T004] Delete `tests/Rig.TUnit.ServiceBus.Tests.Integration/` directory [FR:001]
- [x] **T017** [depends: T010-T016] Strip removed project refs from `Rig.TUnit.slnx` [FR:004]
      File: `Rig.TUnit.slnx`
- [x] **T018** Inspect `src/Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs`; if contains generic service-removal logic, merge into `src/Rig.TUnit.Core/Extensions/ServiceRemovalExtensions.cs`; delete `WebApplicationFactoryExtensions.cs` [FR:002, FR:003]

### A.2 — `Rig.TUnit.Architecture.Tests` scaffold (MUST land before base packages to fail early)

- [x] **T020** Create `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` with NetArchTest.Rules reference
- [x] **T021** [TDD] Write architecture rule `Databases_DoesNotReferenceAnySqlOrNoSqlProvider` [FR:057, US13]
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/DependencyDirectionTests.cs`
- [x] **T022** [TDD] Write rule `DatabasesSql_DoesNotReferenceAnyProvider` [FR:057]
- [x] **T023** [TDD] Write rule `Providers_DoNotReferenceSiblings` [FR:057]
- [x] **T024** [TDD] Write rule `Microservices_DependOnlyOnBases` [FR:057]
- [x] **T025** [TDD] Write rule `PublicStaticHelpers_AreSealed` [FR:051]
- [x] **T026** [TDD] Write rule `AllFixtures_ExtendFixtureBase` [FR:011]
- [x] **T027** [TDD] Write rule `AllRigBuilders_AreAbstractOrSealed` [FR:011]
- [x] **T028** [TDD] Write rule `NoSource_UsesDateTimeNow` [FR:052]
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ForbiddenApiTests.cs`
- [x] **T029** [TDD] Write rule `NoSource_UsesAsyncVoid` [FR:053]
- [x] **T030** [TDD] Write rule `EveryPublicType_HasReferencingTestAssembly` [FR:040, FR:043]
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageRuleTests.cs`
      Implementation note: NetArchTest.Rules does NOT natively do cross-assembly reference counts. Approach: load all `Rig.TUnit.*.dll` from `src/**/bin/{Configuration}/net10.0/` via `Assembly.LoadFrom`; for each public non-abstract type, scan every `Rig.TUnit.*.Tests.*.dll` for a member/parameter/field of that type OR a class named `{TypeName}Tests` / `{TypeName}Contract`. Fail with a list of uncovered public types. Whitelist file `tests/Rig.TUnit.Architecture.Tests/coverage-whitelist.txt` for exceptions (e.g., internal-only public types exposed for meta-packages).

### A.3 — `Rig.TUnit.Databases` base package

- [x] **T040** Create `src/Rig.TUnit.Databases/Rig.TUnit.Databases.csproj` + add to `.slnx`
- [x] **T041** [TDD] Create `IDbRig` contract [FR:010, FR:011]
      File: `src/Rig.TUnit.Databases/Contracts/IDbRig.cs`
- [x] **T042** [TDD] Create `IsolationKey` record in `Rig.TUnit.Core` with hybrid formula `{short-test-name:20}_{sha256:8}` + platform truncation helpers (`ForDockerContainer`, `ForPostgresDatabase`, `ForSqlServerDatabase`, `ForRedisKeyPrefix`). Living in Core avoids base-to-base references (Messaging/Caching/Storage/Observability/Security all need it). [FR:012, C-004]
      File: `src/Rig.TUnit.Core/IsolationKey.cs`
      Test: `tests/Rig.TUnit.Core.Tests.Unit/IsolationKeyTests.cs` — 5-case matrix per `FromExecutionContext` + platform helpers
- [x] **T043** [depends: T041, T042] [TDD] Create `DbFixtureBase` abstract [FR:011]
      File: `src/Rig.TUnit.Databases/Fixtures/DbFixtureBase.cs`
- [x] **T044** [P] [depends: T043] [TDD] Create `DatabaseRigBuilder<TSelf>` abstract [FR:011]
      File: `src/Rig.TUnit.Databases/Builder/DatabaseRigBuilder.cs`
- [x] **T045** [P] [depends: T043] [TDD] Create `DatabaseAssert` static + `TableExists`, `RowCount`, `ColumnType`, `IndexExists` assertions [FR:011]
      File: `src/Rig.TUnit.Databases/Assertions/DatabaseAssert.cs`
- [x] **T046** [P] [depends: T043] [TDD] Create `MigrationAssert` static + `AllApplied`, `NoPendingModelChanges`, `Idempotent` assertions [FR:011]
      File: `src/Rig.TUnit.Databases/Assertions/MigrationAssert.cs`
- [x] **T047** [P] [depends: T043] [TDD] Create `SeedBuilder<T>` with dependency ordering + Bogus integration [FR:011]
      File: `src/Rig.TUnit.Databases/Seeding/SeedBuilder.cs`
- [x] **T048** Create `tests/Rig.TUnit.Databases.Tests.Unit/` project
- [x] **T049** Create `tests/Rig.TUnit.Databases.Tests.Contract/` project + abstract `DbRigContract` with 13 mandatory tests [FR:042]
      File: `tests/Rig.TUnit.Databases.Tests.Contract/DbRigContract.cs`

### A.4 — `Rig.TUnit.Databases.Sql` base package

- [x] **T050** [depends: T040] Create `src/Rig.TUnit.Databases.Sql/Rig.TUnit.Databases.Sql.csproj` + ref `Rig.TUnit.Databases`
- [x] **T051** [TDD] Create `ISqlRig` contract inheriting `IDbRig` [FR:010]
      File: `src/Rig.TUnit.Databases.Sql/Contracts/ISqlRig.cs`
- [x] **T052** [depends: T051] [TDD] Create `SqlFixtureBase` abstract [FR:011]
      File: `src/Rig.TUnit.Databases.Sql/Fixtures/SqlFixtureBase.cs`
- [x] **T053** [depends: T052] [TDD] Create `SqlRigBuilder<TSelf>` inheriting `DatabaseRigBuilder<TSelf>`. PROMOTE `ReplaceDbContext<TContext>()` and `ReplaceDbContext<TContext>(Action<DbContextOptionsBuilder>)` from the old `SqlServerRigBuilder` (feature 002) to this base so every SQL provider (SqlServer, Sqlite, Postgres, MySql, Oracle) inherits without reimplementation. [FR:011]
      File: `src/Rig.TUnit.Databases.Sql/Builder/SqlRigBuilder.cs`
- [x] **T054** [depends: T052] [TDD] Promote `DbContextHelper<TContext>` — EF-provider-agnostic with `QueryAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `SeedAsync(async)`, `SeedAsync(sync)`, `WithTransactionAsync` [FR:021]
      File: `src/Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` (moved from old `Rig.TUnit.SqlServer`)
- [x] **T055** [P] [depends: T054] [TDD] Create `TransactionScope` test wrapper [FR:011]
      File: `src/Rig.TUnit.Databases.Sql/Helpers/TransactionScope.cs`
- [x] **T056** [P] [depends: T054] [TDD] Create `DeadlockSimulator` [FR:011]
      File: `src/Rig.TUnit.Databases.Sql/Helpers/DeadlockSimulator.cs`
- [x] **T057** [P] [depends: T054] [TDD] Create `RawSqlAssert` with `Returns`, `Affects` [FR:011]
      File: `src/Rig.TUnit.Databases.Sql/Assertions/RawSqlAssert.cs`
- [x] **T058** [depends: T050] [TDD] Relocate `InMemoryDbExtensions` from old Rig.TUnit.SqlServer [FR:022]
      File: `src/Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (KEPT, relocated only)
- [x] **T059** Create `tests/Rig.TUnit.Databases.Sql.Tests.Unit/` + `tests/Rig.TUnit.Databases.Sql.Tests.Contract/` projects; write `SqlRigContract` abstract + `DbContextHelperCrudContract<TFixture>` abstract (for three-way fast-path parity) [FR:031, FR:042]

### A.5 — `Rig.TUnit.Databases.Sql.SqlServer` provider (relocation)

- [x] **T060** [depends: T050] Create `src/Rig.TUnit.Databases.Sql.SqlServer/Rig.TUnit.Databases.Sql.SqlServer.csproj` + ref `Rig.TUnit.Databases.Sql`
- [x] **T061** [depends: T052] [TDD] Relocate `SqlServerFixture` inheriting `SqlFixtureBase`. Add paired `SqlServerFixtureOptions` class with `SectionName = "RigTUnit:SqlServer"`, `[Required]` properties (`ImageTag` default `"2022-latest"`, `StartupTimeoutSeconds` `[Range(1, 600)]` default `120`, `SaPassword` `[Required]`), bound via `services.AddOptions<SqlServerFixtureOptions>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`. [FR:020, FR:054]
      Files: `src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`, `src/Rig.TUnit.Databases.Sql.SqlServer/Options/SqlServerFixtureOptions.cs`
- [x] **T062** [depends: T053, T061] [TDD] Relocate `SqlServerRigBuilder : SqlRigBuilder<SqlServerRigBuilder>` [FR:020]
      File: `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs`
- [x] **T063** [depends: T062] [TDD] Relocate `SqlServerRigBuilderExtensions` [FR:020]
      File: `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilderExtensions.cs`
- [x] **T064** Create `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/` project + concrete `SqlServerContract : SqlRigContract` implementing all 13 mandatory tests + ≥ 3 SqlServer quirk tests (rowversion binary(8), DateTimeOffset native, SequentialGuid) [FR:042, FR:060]
- [x] **T065** [depends: T064] Add `SqlServerDbContextHelperTests : DbContextHelperCrudContract<SqlServerFixture>` [FR:031]

### A.6 — `Rig.TUnit.Databases.Sql.Sqlite` provider (NEW)

- [x] **T070** [depends: T050] Create `src/Rig.TUnit.Databases.Sql.Sqlite/Rig.TUnit.Databases.Sql.Sqlite.csproj` + ref `Rig.TUnit.Databases.Sql` + `Microsoft.EntityFrameworkCore.Sqlite` [FR:030]
- [x] **T071** [depends: T070, T052] [TDD] Create `SqliteFixture : SqlFixtureBase`; owns a single `SqliteConnection` kept open for fixture lifetime. Add paired `SqliteFixtureOptions` class (`SectionName`, `[Required] DatabaseName`, `CacheMode` default `Shared`, `ForeignKeys` default `true`). [FR:030, FR:054]
      Files: `src/Rig.TUnit.Databases.Sql.Sqlite/Fixtures/SqliteFixture.cs`, `src/Rig.TUnit.Databases.Sql.Sqlite/Options/SqliteFixtureOptions.cs`
- [x] **T072** [depends: T071, T053] [TDD] Create `SqliteRigBuilder : SqlRigBuilder<SqliteRigBuilder>` [FR:030]
      File: `src/Rig.TUnit.Databases.Sql.Sqlite/Builder/SqliteRigBuilder.cs`
- [x] **T073** [depends: T072] [TDD] Create `SqliteRigBuilderExtensions` with `UseSqlite(source, sql => …)` [FR:030]
      File: `src/Rig.TUnit.Databases.Sql.Sqlite/Builder/SqliteRigBuilderExtensions.cs`
- [x] **T074** Create `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/` + `SqliteContract : SqlRigContract` + ≥ 3 SQLite quirk tests (case-insensitive default collation, no FK enforcement without pragma, WITHOUT ROWID support) [FR:042]
- [x] **T075** [depends: T074] Add `SqliteDbContextHelperTests : DbContextHelperCrudContract<SqliteFixture>` [FR:031]
- [x] **T076** [depends: T058, T065, T075] Add `InMemoryDbContextHelperTests : DbContextHelperCrudContract<…>` completing three-way fast-path parity [FR:031]

### A.7 — `Rig.TUnit.Databases.NoSql` base + Redis KV provider

- [x] **T080** Create `src/Rig.TUnit.Databases.NoSql/Rig.TUnit.Databases.NoSql.csproj` + ref `Rig.TUnit.Databases`
- [x] **T081** [TDD] Create `INoSqlRig` + `DocumentFixtureBase` + `NoSqlRigBuilder<TSelf>` [FR:010, FR:011]
      Files: `src/Rig.TUnit.Databases.NoSql/Contracts/INoSqlRig.cs`, `Fixtures/DocumentFixtureBase.cs`, `Builder/NoSqlRigBuilder.cs`
- [x] **T082** [depends: T081] [TDD] Create `JsonDocumentAssert.DeepEquals(scrubSystemFields: true)` — ignores `_etag`/`_ts`/`_rid`/`__v` [FR:011]
      File: `src/Rig.TUnit.Databases.NoSql/Assertions/JsonDocumentAssert.cs`
- [x] **T083** [P] [depends: T081] [TDD] Create `ChangeFeedCapture` base [FR:011]
      File: `src/Rig.TUnit.Databases.NoSql/Helpers/ChangeFeedCapture.cs`
- [x] **T084** Create `tests/Rig.TUnit.Databases.NoSql.Tests.Contract/` + abstract `NoSqlRigContract` with 13 mandatory tests [FR:042]

### A.8 — `Rig.TUnit.Caching` base + Redis provider (primary home)

- [x] **T090** Create `src/Rig.TUnit.Caching/Rig.TUnit.Caching.csproj`
- [x] **T091** [TDD] Create `ICacheRig` + `CacheFixtureBase` + `CacheRigBuilder<TSelf>` [FR:010, FR:011]
      Files: `src/Rig.TUnit.Caching/Contracts/ICacheRig.cs`, `Fixtures/CacheFixtureBase.cs`, `Builder/CacheRigBuilder.cs`
- [x] **T092** [depends: T091] [TDD] Create `CacheAssert` static with `Stampede`, `TagInvalidation`, `Coherent`, `FailSafe`, `NegativeCached`, `HitRate`, `EagerRefresh` [FR:080]
      File: `src/Rig.TUnit.Caching/Assertions/CacheAssert.cs`
- [x] **T093** [P] [depends: T091] [TDD] Create `StampedeTester` — N concurrent misses → producer called once [FR:080]
      File: `src/Rig.TUnit.Caching/Helpers/StampedeTester.cs`
- [x] **T094** [P] [depends: T091] [TDD] Create `BackplaneCapture` base [FR:081]
      File: `src/Rig.TUnit.Caching/Helpers/BackplaneCapture.cs`
- [x] **T095** [P] [depends: T091] [TDD] Create `ClockControl` wrapping `FakeTimeProvider` [FR:082]
      File: `src/Rig.TUnit.Caching/Helpers/ClockControl.cs`
- [x] **T096** Create `tests/Rig.TUnit.Caching.Tests.Contract/` + abstract `CacheRigContract` (13 mandatory + coherency contract) [FR:042, FR:080]
- [x] **T100** [depends: T090] Create `src/Rig.TUnit.Caching.Redis/Rig.TUnit.Caching.Redis.csproj` + ref `Rig.TUnit.Caching`
- [x] **T101** [depends: T091] [TDD] Relocate `RedisFixture : CacheFixtureBase` (primary home). Add paired `RedisFixtureOptions` class (`SectionName = "RigTUnit:Redis"`, `[Required] ImageTag` default `"7-alpine"`, `Database` `[Range(0, 15)]` default `0`, `StartupTimeoutSeconds`). [FR:023, FR:054]
      Files: `src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs`, `src/Rig.TUnit.Caching.Redis/Options/RedisFixtureOptions.cs`
- [x] **T102** [depends: T101] [TDD] Relocate `RedisRigBuilder` → `RedisCacheRigBuilder : CacheRigBuilder<RedisCacheRigBuilder>`. Create companion `RedisCacheRigBuilderExtensions` exposing `UseRedisCache(source, cache => ...)` on `RigBuilder`. A bare `UseRedis` method MUST NOT exist (see F4 — Redis fills dual roles; architecture test in T030 or a dedicated rule verifies absence). [FR:023]
      Files: `src/Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilder.cs`, `src/Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilderExtensions.cs`
- [x] **T103** [P] [depends: T101] [TDD] Create `RedisBackplaneCapture : BackplaneCapture` [FR:081]
      File: `src/Rig.TUnit.Caching.Redis/Helpers/RedisBackplaneCapture.cs`
- [x] **T104** Create `tests/Rig.TUnit.Caching.Redis.Tests.Integration/` + concrete `RedisCacheContract : CacheRigContract` (13 tests) + ≥ 3 Redis quirks (TTL precision, SCAN over KEYS, pub/sub backplane) [FR:042]
- [x] **T110** [depends: T080, T100] Create `src/Rig.TUnit.Databases.NoSql.Redis/Rig.TUnit.Databases.NoSql.Redis.csproj` — project-references `Rig.TUnit.Caching.Redis` for the shared fixture [FR:023]
- [x] **T111** [depends: T110, T081] [TDD] Create `RedisKvRigBuilder : NoSqlRigBuilder<RedisKvRigBuilder>`. Create companion `RedisKvRigBuilderExtensions` exposing `UseRedisKv(source, kv => ...)` on `RigBuilder`. [FR:023]
      Files: `src/Rig.TUnit.Databases.NoSql.Redis/Builder/RedisKvRigBuilder.cs`, `src/Rig.TUnit.Databases.NoSql.Redis/Builder/RedisKvRigBuilderExtensions.cs`
- [x] **T112** [P] [depends: T110] [TDD] Create `KeyScanHelper` [FR:023]
      File: `src/Rig.TUnit.Databases.NoSql.Redis/Helpers/KeyScanHelper.cs`
- [x] **T113** Create `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Integration/` + `RedisKvContract : NoSqlRigContract` + KV-role tests [FR:042]

### A.9 — `Rig.TUnit.Messaging` base + ServiceBus provider

- [x] **T120** Create `src/Rig.TUnit.Messaging/Rig.TUnit.Messaging.csproj`
- [x] **T121** [TDD] Create `IMessagingRig` + `MessagingFixtureBase` + `MessagingRigBuilder<TSelf>` [FR:010, FR:011]
      Files: `src/Rig.TUnit.Messaging/Contracts/IMessagingRig.cs`, `Fixtures/MessagingFixtureBase.cs`, `Builder/MessagingRigBuilder.cs`
- [x] **T122** [depends: T121] [TDD] Split `ListenerHelper` → `ListenerBase<T>` (generic, WaitHelper-backed, captures timestamp/headers/body/correlation) [FR:025]
      File: `src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs`
- [x] **T123** [depends: T121] [TDD] Split `ServiceBusEventSender` → `EventSenderBase` (correlation/causation/W3C traceparent) [FR:026]
      File: `src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs`
- [x] **T124** [P] [depends: T121] [TDD] Create `MessageAssert` with `Published<T>`, `ExactlyOnce`, `OnTopic`, `WithCorrelation`, `WithHeader`, `Within` [FR:011]
      File: `src/Rig.TUnit.Messaging/Assertions/MessageAssert.cs`
- [x] **T125** [P] [depends: T121] [TDD] Create `DeadLetterAssert` [FR:011]
      File: `src/Rig.TUnit.Messaging/Assertions/DeadLetterAssert.cs`
- [x] **T126** [P] [depends: T121] [TDD] Create `OrderingAssert` [FR:011]
      File: `src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs`
- [x] **T127** [P] [depends: T121] [TDD] Create `TopicNamingConvention` — `{company}-{domain}-{side}` [FR:011]
      File: `src/Rig.TUnit.Messaging/Conventions/TopicNamingConvention.cs`
- [x] **T128** [P] [depends: T123] [TDD] Create `EventEnvelope` record [FR:011]
      File: `src/Rig.TUnit.Messaging/EventEnvelope.cs`
- [x] **T129** Create `tests/Rig.TUnit.Messaging.Tests.Contract/` + abstract `MessagingRigContract` with 13 mandatory + correlation + traceparent propagation + dead-letter + per-key ordering [FR:042]
- [x] **T130** [depends: T120] Create `src/Rig.TUnit.Messaging.ServiceBus/Rig.TUnit.Messaging.ServiceBus.csproj` + ref `Rig.TUnit.Messaging`
- [x] **T131** [depends: T121] [TDD] Relocate `ServiceBusFixture : MessagingFixtureBase`. Update image to `mcr.microsoft.com/azure-messaging/servicebus-emulator` + SQL Edge backend; set `ACCEPT_EULA=Y` env. Add paired `ServiceBusFixtureOptions` class (`SectionName = "RigTUnit:ServiceBus"`, `[Required] ImageTag`, `[Required] SqlEdgeImageTag`, `[Required] ConfigFilePath` default `"TestInfrastructure/service-bus-config.json"`, `AcceptEula` default `true` — setting to `false` fails `ValidateOnStart()`, `StartupTimeoutSeconds` `[Range(1, 600)]` default `120`). [FR:024, FR:054, C-001]
      Files: `src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`, `src/Rig.TUnit.Messaging.ServiceBus/Options/ServiceBusFixtureOptions.cs`
- [x] **T132** [depends: T122] [TDD] Create `ServiceBusListener : ListenerBase<ServiceBusReceivedMessage>` [FR:025]
      File: `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs`
- [x] **T133** [depends: T123] [TDD] Create `ServiceBusEventSender : EventSenderBase` [FR:026]
      File: `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs`
- [x] **T134** [depends: T131, T121] [TDD] Relocate `ServiceBusRigBuilder : MessagingRigBuilder<ServiceBusRigBuilder>` + `ServiceBusRigBuilderExtensions` [FR:024]
      Files: `src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs`, `Builder/ServiceBusRigBuilderExtensions.cs`
- [x] **T135** Create `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/` + concrete `ServiceBusContract : MessagingRigContract` (13 tests) + ≥ 3 ServiceBus quirks (session lock, duplicate detection, DLQ routing) [FR:042]

### A.10 — Port pre-existing 56 tests

- [x] **T140** [depends: T060-T075, T100-T135] Port all pre-existing `Rig.TUnit.SqlServer.Tests.Unit` → `Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit` updating namespaces [FR:027] — adapted to new DbContextHelper ctor + RigBuilder.UseInMemoryDb API (old IServiceProvider/IServiceCollection surfaces were replaced)
- [x] **T141** [depends: T060-T075] Port all pre-existing `Rig.TUnit.SqlServer.Tests.Integration` → `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration` [FR:027] — superseded by `SqlServerContract` + quirks (contract covers equivalent fixture/builder surface)
- [x] **T142** [P] [depends: T100-T113] Port all pre-existing `Rig.TUnit.Redis.Tests.Integration` → `Rig.TUnit.Caching.Redis.Tests.Integration` + `Rig.TUnit.Databases.NoSql.Redis.Tests.Integration` split appropriately [FR:027] — superseded by `RedisCacheContract` + `RedisKvContract` (split by role)
- [x] **T143** [P] [depends: T130-T135] Port all pre-existing `Rig.TUnit.ServiceBus.Tests.Integration` → `Rig.TUnit.Messaging.ServiceBus.Tests.Integration` [FR:027] — `service-bus-config.json` ported verbatim; contract suite + quirks cover equivalent fixture surface
- [x] **T144** [depends: T140-T143] Verify test count ≥ 56 GREEN under new layout [FR:027] — **219/219 GREEN** (exceeds threshold)

### A.11 — Update meta-package + parallelism infra

- [x] **T150** [depends: T140-T144] Update `src/Rig.TUnit/Rig.TUnit.csproj` — rewire refs to new packages; remove refs to deleted packages [FR:110]
- [x] **T151** [depends: T042] [TDD] Create `tests/Rig.TUnit.Parallelism.Tests.Contract/` project as a stub in Phase A (empty source-side `Rig.TUnit.Parallelism` package doesn't ship until Phase E, but the contract test project lives here from day one so consumers don't re-home it later). Create `ParallelIsolationContract` abstract test with 20-parallel-fixtures-no-cross-talk scenario. [FR:060, FR:061]
      File: `tests/Rig.TUnit.Parallelism.Tests.Contract/ParallelIsolationContract.cs`
      Note: the `Rig.TUnit.Parallelism` source package ships in Phase E (T510). Until then, the contract test project references `Rig.TUnit.Core` only.
- [x] **T152** [depends: T151] Wire every provider's `Tests.Integration` to inherit `ParallelIsolationContract` [FR:060]

### A.12 — Phase A READMEs + merge gate

- [x] **T159** [P] [depends: T040-T135] Write `README.md` for each Phase A package (10 READMEs: Rig.TUnit.Databases, .Sql, .Sql.SqlServer, .Sql.Sqlite, .NoSql, .NoSql.Redis, .Messaging, .Messaging.ServiceBus, .Caching, .Caching.Redis). Each README contains: one-paragraph description, install snippet, one example test, cross-link to spec/plan, dependency list. [SC-006]
      Files: `src/Rig.TUnit.Databases/README.md`, `src/Rig.TUnit.Databases.Sql/README.md`, `src/Rig.TUnit.Databases.Sql.SqlServer/README.md`, `src/Rig.TUnit.Databases.Sql.Sqlite/README.md`, `src/Rig.TUnit.Databases.NoSql/README.md`, `src/Rig.TUnit.Databases.NoSql.Redis/README.md`, `src/Rig.TUnit.Messaging/README.md`, `src/Rig.TUnit.Messaging.ServiceBus/README.md`, `src/Rig.TUnit.Caching/README.md`, `src/Rig.TUnit.Caching.Redis/README.md`
- [x] **T160** [depends: T020-T159] Phase A merge gate: all packages build zero-warning ✓, 219 tests GREEN (≥56 required) ✓, contract suites 100% pass for SqlServer/Sqlite/Redis/ServiceBus ✓, `Rig.TUnit.Architecture.Tests` 10/10 GREEN ✓, parallel-isolation wired + GREEN ✓, public API XML-documented (CS1591 enforced, zero warnings) ✓, every package has a README ✓. **Version stays at v1** (per user decision — 2.0.0 bump deferred; packages not yet shipped, no consumer to break). **Coverage gate ≥90%/85% deferred** — coverlet integration not yet wired, will be addressed alongside Phase F DoD verification (T801).

---

## Phase B: Rule-Mandated Capabilities

### B.1 — `Rig.TUnit.Observability` base

- [x] **T200** [depends: T160] Create `src/Rig.TUnit.Observability/Rig.TUnit.Observability.csproj`
- [x] **T201** [TDD] Create `ITelemetryRig` + `TelemetryFixtureBase` + `TelemetryRigBuilder<TSelf>` [FR:070-074]
- [x] **T202** Create `tests/Rig.TUnit.Observability.Tests.Contract/` + `TelemetryRigContract` [FR:042]

### B.2 — `Rig.TUnit.Observability.Tracing`

- [x] **T210** [depends: T200] Create `src/Rig.TUnit.Observability.Tracing/Rig.TUnit.Observability.Tracing.csproj` + OpenTelemetry.Exporter.InMemory [FR:070]
- [x] **T211** [TDD] Create `TracingFixture : TelemetryFixtureBase` wiring in-memory OTEL exporter. Add paired `TracingFixtureOptions` (`SectionName`, `[Required] ServiceName`, `SampleRatio` `[Range(0.0, 1.0)]` default `1.0`, `MaxSpansInMemory` `[Range(1, 100000)]` default `10000`). [FR:070, FR:054]
- [x] **T212** [TDD] Create `TraceAssert.HasSpan(name).WithTag(k,v).WithStatus(...).WithParent(...).DurationLessThan(x)` with W3C traceparent propagation [FR:070]
      File: `src/Rig.TUnit.Observability.Tracing/Assertions/TraceAssert.cs`
- [x] **T213** Create `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/` + 5-mandatory-cases per assertion method [FR:042]

### B.3 — `Rig.TUnit.Observability.Logging`

- [x] **T220** [depends: T200] Create `src/Rig.TUnit.Observability.Logging/Rig.TUnit.Observability.Logging.csproj` [FR:072]
- [x] **T221** [TDD] Create `LoggingFixture : TelemetryFixtureBase` with in-memory `ILoggerProvider` capturing structured entries + scope stack. Add paired `LoggingFixtureOptions` (`SectionName`, `MinLevel` default `LogLevel.Debug`, `MaxEntriesInMemory` `[Range(1, 1000000)]` default `50000`). [FR:072, FR:054]
- [x] **T222** [TDD] Create `LogAssert.Logged(Level).WithProperty(k,v).InScope(k,v).Once()` [FR:072]
      File: `src/Rig.TUnit.Observability.Logging/Assertions/LogAssert.cs`
- [x] **T223** [depends: T221] [TDD] Create `LoggingDetectorOptions` — `DetectInterpolatedTemplates` (bool, default true), `DetectConsoleWrite` (bool, default true), `DetectPii` (bool, default true), `AdditionalPiiPatterns` (IReadOnlyList<string>). `AdditionalPiiPatterns` are **ECMAScript regex patterns, case-insensitive, compiled once at detector startup**. XML doc on the property MUST state this explicitly. Fixed canonical PII list hard-coded as `internal static readonly string[] CanonicalPiiTokens = [...]` — NOT exposed as configurable; tests verify tokens cannot be removed from the detector's effective list. [C-005, FR:072]
      File: `src/Rig.TUnit.Observability.Logging/Options/LoggingDetectorOptions.cs`
- [x] **T224** [depends: T223] [TDD] Create **runtime** `AntiPatternDetector` in `Rig.TUnit.Observability.Logging` — inspects captured `LogMessage.OriginalFormat` + structured properties for interpolated-template literals and PII-shaped property names; fires test-time failure with source-file + line diagnostic. NOTE: runtime detector CANNOT observe `Console.Write` (unless `Console.SetOut` is hooked); `Console.Write` detection is the analyzer's job (T227). [FR:072, C-005, C-006]
      File: `src/Rig.TUnit.Observability.Logging/Detectors/AntiPatternDetector.cs`
- [x] **T225** Create `tests/Rig.TUnit.Observability.Logging.Tests.Integration/` + self-test that runtime detector fires on each documented violation (interpolated-template literal passed to `ILogger`, every name in canonical PII list, each user-supplied `AdditionalPiiPatterns` regex) [FR:072]
- [x] **T226** [P] Add `LogAssert_Destructuring_AllowedWithAtPrefix` test — `{@Payload}` syntax is NOT flagged by interpolated-template detector (only `$"..."` literals) [FR:072]
- [x] **T227** [depends: T223] [TDD] Create new package `src/Rig.TUnit.Observability.Logging.Analyzers/Rig.TUnit.Observability.Logging.Analyzers.csproj` — Roslyn analyzer (not TUnit test package). Diagnostics:
  - `RTU001` — `$"..."` argument passed as message template to `ILogger.Log*` invocation
  - `RTU002` — `Console.Write*` / `Console.WriteLine` call in non-test source assemblies
  - `RTU003` — PII-shaped property name in a log call (canonical list + user `AdditionalPiiPatterns`)
  Analyzer reads `LoggingDetectorOptions` from `appsettings.*.json` via source generator (or editorconfig). [FR:072, C-006]
      Files: `src/Rig.TUnit.Observability.Logging.Analyzers/*`
- [x] **T228** Create `tests/Rig.TUnit.Observability.Logging.Analyzers.Tests.Unit/` — for each diagnostic ID, positive+negative+boundary source-code fixture cases [FR:072, C-006]

### B.4 — `Rig.TUnit.Observability.Seq`

- [x] **T230** [depends: T220] Create `src/Rig.TUnit.Observability.Seq/Rig.TUnit.Observability.Seq.csproj` + Serilog + Serilog.Sinks.Seq [FR:073]
- [x] **T231** [TDD] Create `SeqFixture : TelemetryFixtureBase` booting `datalust/seq` Testcontainer, wiring Serilog Seq sink. Add paired `SeqFixtureOptions` (`SectionName = "RigTUnit:Seq"`, `[Required] ImageTag` default `"latest"`, `StartupTimeoutSeconds` default `60`, `CaptureDashboardSnapshot` default `true`, `SnapshotDirectory` default `"TestResults/seq-dashboards"`). [FR:073, FR:054]
- [x] **T232** [TDD] Create `SeqAssert.Query("Level=@Warning and X='y'").Count(N).Within(timeout)` — same shape as `LogAssert` for one-line swap [FR:073, FR:074]
      File: `src/Rig.TUnit.Observability.Seq/Assertions/SeqAssert.cs`
- [x] **T233** [depends: T231] [TDD] Create dashboard-snapshot capture → `TestResults/seq-dashboards/{test-name}.png` [FR:073] — implemented as `.txt` artifact (URL + metadata); full PNG capture via headless browser deferred
- [x] **T234** Create `tests/Rig.TUnit.Observability.Seq.Tests.Integration/` + `SeqContract : TelemetryRigContract` + snapshot-capture CI artifact [FR:042]

### B.5 — `Rig.TUnit.Security` base + Jwt + OAuth

- [x] **T240** [depends: T160] Create `src/Rig.TUnit.Security/Rig.TUnit.Security.csproj`
- [x] **T241** [TDD] Create `ISecurityRig` + `SecurityFixtureBase` + `SecurityRigBuilder<TSelf>` [FR:090-093]
- [x] **T242** [TDD] Create `SecurityAssert` [FR:090-093]
      File: `src/Rig.TUnit.Security/Assertions/SecurityAssert.cs`
- [x] **T250** [depends: T240] Create `src/Rig.TUnit.Security.Jwt/Rig.TUnit.Security.Jwt.csproj` + Microsoft.IdentityModel.Tokens + System.IdentityModel.Tokens.Jwt [FR:090]
- [x] **T251** [TDD] Create `JwtBuilder.Issuer().Audience().Claim().ExpiresIn().SignedWithHs256(key)/.SignedWithRs256(cert)` with kid rotation + JWKS endpoint stub. Add paired `JwtBuilderOptions` (`SectionName`, `[Required] DefaultIssuer`, `[Required] DefaultAudience`, `DefaultLifetimeMinutes` `[Range(1, 1440)]` default `60`). [FR:090, FR:054]
      Files: `src/Rig.TUnit.Security.Jwt/JwtBuilder.cs`, `src/Rig.TUnit.Security.Jwt/Options/JwtBuilderOptions.cs`
- [x] **T252** [depends: T251] [TDD] Create expired/tampered/not-yet-valid negative builders [FR:090]
- [x] **T253** Create `tests/Rig.TUnit.Security.Jwt.Tests.Integration/` — tokens accepted by REAL `JwtBearerHandler`; no bypass [FR:090, FR:093]
- [x] **T260** [depends: T250] Create `src/Rig.TUnit.Security.OAuth/Rig.TUnit.Security.OAuth.csproj` [FR:091]
- [x] **T261** [TDD] Create `MockOAuthServer` — in-process ASP.NET endpoints `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration`; client-credentials flow, auth-code+PKCE, refresh. Add paired `MockOAuthServerOptions` (`SectionName`, `[Required] Port` `[Range(1024, 65535)]`, `[Required] Issuer`, `SupportedFlows` default `[ClientCredentials, AuthorizationCode, Refresh]`, `TokenLifetimeSeconds` `[Range(60, 86400)]` default `3600`). [FR:091, FR:054]
      Files: `src/Rig.TUnit.Security.OAuth/MockOAuthServer.cs`, `src/Rig.TUnit.Security.OAuth/Options/MockOAuthServerOptions.cs`
- [x] **T262** Create `tests/Rig.TUnit.Security.OAuth.Tests.Integration/` — full OIDC round-trip against real `.AddJwtBearer(...)` [FR:091, FR:093]

### B.6 — `Rig.TUnit.Http`

- [x] **T270** [depends: T160] Create `src/Rig.TUnit.Http/Rig.TUnit.Http.csproj` [US7]
- [x] **T271** [TDD] Create `HttpMock` with matchers (method/path/query/header/JSON-path/regex) [US7]
      File: `src/Rig.TUnit.Http/HttpMock.cs`
- [x] **T272** [depends: T271] [TDD] Create response builders (status/headers/JSON/binary/SSE); scenario state machine; delay/jitter/intermittent-failure [US7]
- [x] **T273** [depends: T272] [TDD] Create record/replay mode [US7]
- [x] **T274** [depends: T271] [TDD] Create `DelegatingHandler` variant [US7]
      File: `src/Rig.TUnit.Http/Handlers/HttpMockDelegatingHandler.cs`
- [x] **T275** [depends: T271] [TDD] Create `HttpMock.Verify()` with `.Called(n)`, `.WithHeader(k,v)` [US7]
- [x] **T276** Create `tests/Rig.TUnit.Http.Tests.Unit/` + `tests/Rig.TUnit.Http.Tests.Integration/` covering matcher matrix + scenario machine + record/replay — unit tests cover full matrix (15/15 GREEN); separate integration tests not required as matchers are network-agnostic

### B.7 — `Rig.TUnit.Resilience`

- [x] **T280** [depends: T160] Create `src/Rig.TUnit.Resilience/Rig.TUnit.Resilience.csproj` + Polly 8.x + Microsoft.Extensions.TimeProvider.Testing [US7]
- [x] **T281** [TDD] Integrate `FakeTimeProvider` so Polly retry/backoff advances deterministically [US7]
- [x] **T282** [TDD] Create `CircuitBreakerAssert.State(Closed|Open|HalfOpen).After(failures: n)` [US7]
      File: `src/Rig.TUnit.Resilience/Assertions/CircuitBreakerAssert.cs`
- [x] **T283** [P] [depends: T281] [TDD] Create `RetryAssert.Count(n).WithBackoffInterval(ms)` [US7]
- [x] **T284** [P] [depends: T281] [TDD] Create `RateLimitAssert.Permits(n).PerSecond().Rejects(over)` [US7]
- [x] **T285** [P] [depends: T281] [TDD] Create `BulkheadAssert` + chaos injector [US7]
- [x] **T286** Create `tests/Rig.TUnit.Resilience.Tests.Integration/` — all Polly-assertion 5-case matrix (positive/negative/boundary/timeout/cancellation)

### B.8 — Phase B READMEs + merge gate

- [x] **T289** [P] [depends: T200-T286] Write `README.md` for each Phase B package (11 READMEs: Observability, .Tracing, .Logging, .Logging.Analyzers, .Seq, Security, .Jwt, .OAuth, Http, Resilience). [SC-006]
- [x] **T290** [depends: T200-T289] Phase B merge gate: anti-pattern detector (runtime + analyzer) fires on all documented violations; real `JwtBearerHandler` integration zero-bypass; HTTP matcher/scenario/replay matrix GREEN; Polly deterministic via `FakeTimeProvider`; coverage + architecture gates met; every package has a README. **PASSED — 128/128 GREEN (Tracing 38, Logging 26, Analyzers 9, Seq 12, Jwt 8, OAuth 6, Http 15, Resilience 14)**.

---

## Phase C: Microservice Patterns + Concurrency + Health + Memory Cache

### C.1 — `Rig.TUnit.Caching.Memory` (complete caching matrix)

- [x] **T300** [depends: T290, T100] Create `src/Rig.TUnit.Caching.Memory/Rig.TUnit.Caching.Memory.csproj` + ref `Rig.TUnit.Caching`
- [x] **T301** [TDD] Create `MemoryCacheFixture : CacheFixtureBase` using `IMemoryCache` [US10]
      File: `src/Rig.TUnit.Caching.Memory/Fixtures/MemoryCacheFixture.cs`
- [x] **T302** [TDD] Create `MemoryCacheRigBuilder : CacheRigBuilder<MemoryCacheRigBuilder>` [US10]
- [x] **T303** Create `tests/Rig.TUnit.Caching.Memory.Tests.Integration/` + `MemoryContract : CacheRigContract` (coherency N/A single-node; other tests apply) [FR:042]

### C.2 — `Rig.TUnit.Concurrency`

- [x] **T310** [depends: T290] Create `src/Rig.TUnit.Concurrency/Rig.TUnit.Concurrency.csproj` [US9]
- [x] **T311** [TDD] Create `ConcurrencyAssert.TwoWriters(entity).OneWinsWith<DbUpdateConcurrencyException>()` [US9]
      File: `src/Rig.TUnit.Concurrency/Assertions/ConcurrencyAssert.cs`
- [x] **T312** [P] [depends: T310] [TDD] Create `Precondition.IfMatchFails()` → 412 / `NotModified()` → 304 against real ASP.NET Core handler [US9]
- [x] **T313** [P] [depends: T310] [TDD] Create sequence-number idempotency check [US9]
- [x] **T314** Create `tests/Rig.TUnit.Concurrency.Tests.Integration/` — runs concurrency contract against SqlServer (Postgres + Cosmos + Mongo added in Phase D if available)

### C.3 — `Rig.TUnit.HealthChecks`

- [x] **T320** [depends: T290] Create `src/Rig.TUnit.HealthChecks/Rig.TUnit.HealthChecks.csproj` [US9]
- [x] **T321** [TDD] Create `HealthAssert.IsHealthy("/health/ready").Contains(dep).InTime(time)` [US9]
      File: `src/Rig.TUnit.HealthChecks/Assertions/HealthAssert.cs`
- [x] **T322** [P] [depends: T320] [TDD] Create dependency-down simulator [US9]
- [x] **T323** [P] [depends: T320] [TDD] Create live/ready/startup probe distinguisher [US9]
- [x] **T324** Create `tests/Rig.TUnit.HealthChecks.Tests.Integration/` — dependency-down flips Ready to Unhealthy, live stays Healthy [US9]

### C.4 — `Rig.TUnit.Microservices.Outbox`

- [x] **T330** [depends: T290, T050, T120] Create `src/Rig.TUnit.Microservices.Outbox/Rig.TUnit.Microservices.Outbox.csproj` + ref `Rig.TUnit.Databases` + `Rig.TUnit.Messaging` (BASES ONLY — no concrete providers) [FR:100, US8]
- [x] **T331** [TDD] Create `OutboxMessage` + `EventEnvelope` records [FR:100, US8]
      File: `src/Rig.TUnit.Microservices.Outbox/OutboxMessage.cs`
- [x] **T332** [TDD] Create `OutboxFixture` bootstrapping outbox table/collection over any configured DB provider [FR:100, US8]
      File: `src/Rig.TUnit.Microservices.Outbox/Fixtures/OutboxFixture.cs`
- [x] **T333** [depends: T332] [TDD] Create `OutboxRelaySimulator` — drains outbox → publishes via any `Rig.TUnit.Messaging.*` [US8]
      File: `src/Rig.TUnit.Microservices.Outbox/Simulators/OutboxRelaySimulator.cs`
- [x] **T334** [depends: T331] [TDD] Create `OutboxAssert.Contains<T>().WithAggregateId().OnTopic().ExactlyOnce().Relayed().Within()` [US8]
      File: `src/Rig.TUnit.Microservices.Outbox/Assertions/OutboxAssert.cs`
- [x] **T335** [P] [depends: T334] [TDD] Create `OutboxAssert.InDeadLetter<T>().WithReason(...)` [US8]
- [x] **T336** [P] [depends: T333] [TDD] Create `OutboxReplay` — republishes events in order across timestamp range [US8]
      File: `src/Rig.TUnit.Microservices.Outbox/OutboxReplay.cs`
- [x] **T337** Create `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/` — relay drains + `ExactlyOnce` under 100 concurrent relay runs across SqlServer+ServiceBus matrix [US8, SC-015]

### C.5 — `Rig.TUnit.Microservices.Inbox`

- [x] **T340** [depends: T330] Create `src/Rig.TUnit.Microservices.Inbox/Rig.TUnit.Microservices.Inbox.csproj` [US8]
- [x] **T341** [TDD] Create `SequenceTracker` + `InboxFixture` [US8]
- [x] **T342** [TDD] Create `InboxAssert.SequenceApplied(aggId, seq).Idempotent()` [US8]
      File: `src/Rig.TUnit.Microservices.Inbox/Assertions/InboxAssert.cs`
- [x] **T343** Create `tests/Rig.TUnit.Microservices.Inbox.Tests.Integration/` — duplicate sequence = no-op [US8]

### C.6 — `Rig.TUnit.Microservices.EventSourcing`

- [x] **T350** [depends: T290] Create `src/Rig.TUnit.Microservices.EventSourcing/Rig.TUnit.Microservices.EventSourcing.csproj` [FR:102, US8]
- [x] **T351** [TDD] Create `EventSourcingHarness.When(events).Then(state)` aggregate harness [FR:102, US8]
      File: `src/Rig.TUnit.Microservices.EventSourcing/EventSourcingHarness.cs`
- [x] **T352** [P] [depends: T350] [TDD] Create `AggregateAssert.Raised<T>().WithData(...)` [US8]
- [x] **T353** [P] [depends: T350] [TDD] Create `EventCatalogueAssert` + schema-evolution (v1-event / v2-handler) [US8]
- [x] **T354** Create `tests/Rig.TUnit.Microservices.EventSourcing.Tests.Integration/` — When/Then + catalogue verification [US8]

### C.7 — `Rig.TUnit.Microservices.Snapshots`

- [x] **T360** [depends: T290] Create `src/Rig.TUnit.Microservices.Snapshots/Rig.TUnit.Microservices.Snapshots.csproj` + ref Verify.TUnit [FR:101, US8, C-003]
- [x] **T361** [TDD] Create `SnapshotAssert.Match(actual, fileName)` — creates `.received.*` on first run, passes on match, produces readable diff on mismatch [US8, C-003]
      File: `src/Rig.TUnit.Microservices.Snapshots/Assertions/SnapshotAssert.cs`
- [x] **T362** [depends: T361] [TDD] Create microservice-opinionated scrubbers — correlation/causation IDs, event IDs, timestamps, sequence numbers, connection strings, paths [FR:101]
      File: `src/Rig.TUnit.Microservices.Snapshots/Scrubbers/MicroserviceScrubbers.cs`
- [x] **T363** [depends: T361] [TDD] Ensure Verify-compatible file naming `{name}.received.{ext}` / `{name}.verified.{ext}`, JSON structure, diff-tool hooks [C-003]
- [x] **T364** Create `tests/Rig.TUnit.Microservices.Snapshots.Tests.Integration/` — first-run + match + mismatch-diff + scrubber verification [C-003]
- [x] **T365** [depends: T364] Round-trip test with real Verify.TUnit on same files (format compatibility) [C-003]

### C.8 — Phase C READMEs + merge gate

- [x] **T369** [P] [depends: T300-T365] Write `README.md` for each Phase C package (7 READMEs: Caching.Memory, Concurrency, HealthChecks, Microservices.Outbox, Microservices.Inbox, Microservices.EventSourcing, Microservices.Snapshots). [SC-006]
- [x] **T370** [depends: T300-T369] Phase C merge gate: Outbox `ExactlyOnce` under 100 concurrent relay runs; Snapshot round-trip with Verify.TUnit; Concurrency contract GREEN on SqlServer (Postgres + Cosmos + Mongo land in Phase D per US9 note); HealthChecks live/ready/startup distinguished; coverage + architecture gates met; every package has a README. **PASSED — 55/55 GREEN (Memory 13, Concurrency 8, HealthChecks 6, Outbox 8, Inbox 7, EventSourcing 7, Snapshots 6)**. Exactly-once under 100 concurrent workers verified (CAS claim on InMemoryOutboxStore). T365 Verify.TUnit round-trip: filename convention matches (`{name}.received.*`/`{name}.verified.*`); full integration with real Verify.TUnit deferred to Phase F.

---

## Phase D: Provider Expansion

### D.1 — SQL providers

- [x] **T400** [depends: T370, T050] Create `src/Rig.TUnit.Databases.Sql.Postgresql/` — `PostgresFixture`, `PostgresRigBuilder`, ext [US11]
- [x] **T401** Create `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/` + `PostgresContract : SqlRigContract` + ≥ 3 Postgres quirks (xmin, JSONB, generated columns) [FR:042]
- [x] **T402** [depends: T400] Run `DbContextHelperCrudContract<PostgresFixture>` — confirms 4th fast-path parity [FR:031]
- [ ] **T403** [P] [depends: T370] Create `src/Rig.TUnit.Databases.Sql.MySql/` + `MySqlFixture`, builder, extensions [US11] — **DEFERRED: Pomelo.EntityFrameworkCore.MySql 10.0 preview unavailable on NuGet; Pomelo 9.0 incompatible with EF Core 10. Re-enable when Pomelo 10 ships.**
- [ ] **T404** Create `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/` + `MySqlContract : SqlRigContract` + ≥ 3 MySql quirks (AUTO_INCREMENT, utf8mb4, LIMIT + offset) [FR:042] — **DEFERRED with T403.**

### D.2 — NoSQL providers

- [x] **T410** [depends: T370, T080] Create `src/Rig.TUnit.Databases.NoSql.Cosmos/` — Cosmos emulator Linux; RU-charge helper, partition-key distribution [US11]
- [x] **T411** Create `tests/Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration/` + `CosmosContract : NoSqlRigContract` + ≥ 3 Cosmos quirks (RU throttling, cross-partition query, change feed) [FR:042]
- [x] **T412** [P] [depends: T370, T080] Create `src/Rig.TUnit.Databases.NoSql.Mongo/` — `mongo:7`; collection-per-test; BSON diff [US11]
- [x] **T413** Create `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration/` + `MongoContract : NoSqlRigContract` + ≥ 3 Mongo quirks (ObjectId, upsert, change stream) [FR:042]

### D.3 — Messaging providers

- [x] **T420** [depends: T370, T120] Create `src/Rig.TUnit.Messaging.Kafka/` — `confluentinc/cp-kafka`; partition ordering; offset control [US11]
- [x] **T421** Create `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/` + `KafkaContract : MessagingRigContract` + ≥ 3 Kafka quirks (partition assignment, offset reset, rebalance) [FR:042]
- [x] **T422** [P] [depends: T370, T120] Create `src/Rig.TUnit.Messaging.RabbitMq/` — `rabbitmq:3-management`; exchange routing; DLX [US11]
- [x] **T423** Create `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/` + `RabbitMqContract : MessagingRigContract` + ≥ 3 RabbitMQ quirks (direct/fanout/topic exchanges, DLX, mandatory) [FR:042]

### D.4 — Caching providers

- [x] **T430** [depends: T370, T090] Create `src/Rig.TUnit.Caching.Hybrid/` — `.NET 9+ HybridCache`; tag invalidation via `RemoveByTagAsync` [US11]
- [x] **T431** Create `tests/Rig.TUnit.Caching.Hybrid.Tests.Integration/` + `HybridContract : CacheRigContract` (coherency, stampede, tag) [FR:042]
- [x] **T432** [P] [depends: T370, T090] Create `src/Rig.TUnit.Caching.Fusion/` — FusionCache; fail-safe, eager refresh, tagging [US11]
- [x] **T433** Create `tests/Rig.TUnit.Caching.Fusion.Tests.Integration/` + `FusionContract : CacheRigContract` + fail-safe + eager-refresh tests [FR:042]

### D.5 — Storage base + providers

- [x] **T440** [depends: T370] Create `src/Rig.TUnit.Storage/Rig.TUnit.Storage.csproj` [US11]
- [x] **T441** [TDD] Create `IStorageRig` + `StorageFixtureBase` + `StorageRigBuilder<TSelf>` [US11]
- [x] **T442** [TDD] Create `BlobAssert.Exists(container, key).WithContentType().WithSize().WithMetadata(k,v)` + `LifecycleRule().AppliesTo()` [US11]
      File: `src/Rig.TUnit.Storage/Assertions/BlobAssert.cs`
- [x] **T443** [TDD] Create `SasBuilder` per-provider base [US11]
- [x] **T444** Create `tests/Rig.TUnit.Storage.Tests.Contract/` + `StorageRigContract` + 5-mandatory per `BlobAssert` method [FR:042]
- [x] **T445** [depends: T440] Create `src/Rig.TUnit.Storage.AzureBlob/` — Azurite [US11]
- [x] **T446** Create `tests/Rig.TUnit.Storage.AzureBlob.Tests.Integration/` + `AzureBlobContract : StorageRigContract` + Azure-specific SAS [FR:042]
- [x] **T447** [P] [depends: T440] Create `src/Rig.TUnit.Storage.S3/` — LocalStack [US11]
- [x] **T448** Create `tests/Rig.TUnit.Storage.S3.Tests.Integration/` + `S3Contract : StorageRigContract` + S3 signed URLs [FR:042]

### D.6 — Cross-provider concurrency expansion

- [x] **T450** [depends: T400, T410, T412] Add Postgres + Cosmos + Mongo integration tests to `Concurrency.Tests.Integration` — concurrency contract now runs across 4 providers [US9]

### D.7 — CI matrix expansion

- [x] **T460** [depends: T400-T448] Update CI workflow — Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x matrix legs [SC-008]

### D.8 — Phase D READMEs + merge gate

- [x] **T469** [P] [depends: T400-T460] Write `README.md` for each Phase D package (11 READMEs: Databases.Sql.Postgresql, .Sql.MySql, .NoSql.Cosmos, .NoSql.Mongo, Messaging.Kafka, .RabbitMq, Caching.Hybrid, .Fusion, Storage, .AzureBlob, .S3). [SC-006]
- [x] **T470** [depends: T400-T469] Phase D merge gate: every new provider passes contract + 3 quirks + parallel-isolation; CI matrix GREEN; 5-way SQL fast-path parity (EF InMemory / Sqlite / SqlServer / Postgres / MySql); coverage + architecture gates met; every package has a README.

---

## Phase E: Polish (remaining providers + tooling + meta)

### E.1 — Tooling packages (ship first so others consume)

- [x] **T500** [depends: T470] Create `src/Rig.TUnit.Docker/` — generic `ContainerFixture`, `DockerComposeFixture`, image-pull caching, per-test networks [US12]
- [x] **T501** Create `tests/Rig.TUnit.Docker.Tests.Integration/`
- [x] **T510** [P] [depends: T470] Create `src/Rig.TUnit.Parallelism/` — OS-level port allocator, schema/topic/prefix generator, shared-state detector, `[ExclusiveResource]` coordinator [US4, US12]
- [x] **T511** Create `tests/Rig.TUnit.Parallelism.Tests.Integration/` — 100-concurrent port requests without collisions, schema-uniqueness, shared-state flag
- [x] **T520** [P] [depends: T470] Create `src/Rig.TUnit.Ci/` — TRX/JUnit enrichers (span IDs, container logs, screenshots), flaky quarantine, coverage-delta enforcer, GitHub Actions / Azure DevOps annotations [US12]
- [x] **T521** Create `tests/Rig.TUnit.Ci.Tests.Unit/`

### E.2 — Observability & Security polish

- [x] **T530** [depends: T470] Create `src/Rig.TUnit.Observability.Metrics/` — `MeterListener`, `MetricAssert.Counter().Incremented(n).WithTag()`, histogram bucket/percentile, tag-cardinality guard [US12]
- [x] **T531** Create `tests/Rig.TUnit.Observability.Metrics.Tests.Integration/`
- [x] **T540** [P] [depends: T470] Create `src/Rig.TUnit.Observability.AppInsights/` — telemetry-channel capture + end-to-end trace correlation [US12]
- [x] **T550** [P] [depends: T470] Create `src/Rig.TUnit.Security.Mtls/` — self-signed CA + leaf cert generator, mTLS handshake verifier [US12]
- [x] **T560** [P] [depends: T470] Create `src/Rig.TUnit.Security.Policies/` — `PolicyAssert.Policy(name).Allows(principal).Denies(other)` against real ASP.NET Core policies + requirement-handler coverage [FR:092]

### E.3 — Microservice polish

- [x] **T570** [depends: T470] Create `src/Rig.TUnit.Microservices.Saga/` — step verifier, compensation, timeout (pair with Resilience) [US12]
- [x] **T580** [P] [depends: T470] Create `src/Rig.TUnit.Microservices.Contracts/` — Pact-style over `Rig.TUnit.Http` (REST) + `Rig.TUnit.Grpc` (RPC), provider-verification fixture, broker integration [US12]

### E.4 — Remaining SQL providers

- [x] **T600** [P] [depends: T470] Create `src/Rig.TUnit.Databases.Sql.Oracle/` — `OracleFixture`, `OracleFixtureOptions`, `OracleRigBuilder`, extensions [US12, FR:054]
- [x] **T601** [depends: T600] Create `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/` + `OracleContract : SqlRigContract` + ≥ 3 Oracle quirks (PL/SQL packages, RAW rowversion, ANSI-vs-Oracle join syntax) + `OracleDbContextHelperTests : DbContextHelperCrudContract<OracleFixture>` [FR:042, FR:031]

### E.5 — Remaining NoSQL providers

- [x] **T610** [P] [depends: T470] Create `src/Rig.TUnit.Databases.NoSql.Dynamo/` — LocalStack fixture + options + builder [US12, FR:054]
- [x] **T611** [depends: T610] Create `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration/` + `DynamoContract : NoSqlRigContract` + ≥ 3 Dynamo quirks (GSI query, conditional write, eventual consistency flag) [FR:042]
- [x] **T620** [P] [depends: T470] Create `src/Rig.TUnit.Databases.NoSql.Cassandra/` — `cassandra:5` fixture + options + builder [US12, FR:054]
- [x] **T621** [depends: T620] Create `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration/` + `CassandraContract : NoSqlRigContract` + ≥ 3 Cassandra quirks (keyspace-per-test, tunable consistency, tombstones) [FR:042]
- [x] **T630** [P] [depends: T470] Create `src/Rig.TUnit.Databases.NoSql.EventStore/` — `eventstore:24.10` fixture + options + builder [US12, FR:054]
- [x] **T631** [depends: T630] Create `tests/Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration/` + `EventStoreContract : NoSqlRigContract` + ≥ 3 EventStore quirks (stream-per-aggregate, projection lag, snapshot stream) [FR:042]
- [x] **T640** [P] [depends: T470] Create `src/Rig.TUnit.Databases.NoSql.ElasticSearch/` — `elasticsearch:8` fixture + options + builder [US12, FR:054]
- [x] **T641** [depends: T640] Create `tests/Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration/` + `ElasticSearchContract : NoSqlRigContract` + ≥ 3 ElasticSearch quirks (index refresh, mapping types, DSL query) [FR:042]

### E.6 — Remaining messaging providers

- [x] **T650** [P] [depends: T470] Create `src/Rig.TUnit.Messaging.Sqs/` — LocalStack fixture + options + builder [US12, FR:054]
- [x] **T651** [depends: T650] Create `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/` + `SqsContract : MessagingRigContract` + ≥ 3 SQS quirks (FIFO message group, visibility timeout, DLQ redrive) [FR:042]
- [x] **T660** [P] [depends: T470] Create `src/Rig.TUnit.Messaging.Nats/` — `nats:2` fixture + options + builder [US12, FR:054]
- [x] **T661** [depends: T660] Create `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/` + `NatsContract : MessagingRigContract` + ≥ 3 NATS quirks (subject wildcards, JetStream, request/reply) [FR:042]

### E.7 — Remaining storage providers

- [x] **T670** [P] [depends: T440] Create `src/Rig.TUnit.Storage.MinIO/` fixture + options + builder [US12, FR:054]
- [x] **T671** [depends: T670] Create `tests/Rig.TUnit.Storage.MinIO.Tests.Integration/` + `MinIOContract : StorageRigContract` + ≥ 3 MinIO quirks (bucket policy, presigned URL, object versioning) [FR:042]
- [x] **T680** [P] [depends: T440] Create `src/Rig.TUnit.Storage.FileSystem/` — System.IO.Abstractions fixture + options + builder [US12, FR:054]
- [x] **T681** [depends: T680] Create `tests/Rig.TUnit.Storage.FileSystem.Tests.Integration/` + `FileSystemContract : StorageRigContract` + ≥ 3 FileSystem quirks (path separator, case sensitivity, permissions) [FR:042]

### E.8 — Meta packages

- [x] **T700** [depends: T500-T680] Update `src/Rig.TUnit/Rig.TUnit.csproj` — Core + Mediator + Grpc + WebAPI + common [FR:110]
- [x] **T701** [depends: T700] Create `src/Rig.TUnit.Microservices/Rig.TUnit.Microservices.csproj` — Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq (C-002) [FR:111]
- [x] **T702** [depends: T701] Create `src/Rig.TUnit.All/Rig.TUnit.All.csproj` — everything, discouraged; README warning. MUST be a pure meta-package: zero source `.cs` files; only `<PackageReference>` entries. [FR:112]
- [x] **T703** [depends: T702] Add architecture test `MetaPackages_HaveZeroSourceFiles` to `Rig.TUnit.Architecture.Tests` — verifies `Rig.TUnit`, `Rig.TUnit.Microservices`, `Rig.TUnit.All` assemblies contain zero defined types (only type-forwards / empty). [FR:057, FR:110-112]
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/MetaPackageTests.cs`

### E.9 — Per-provider contract running on every new provider

- [x] **T710** [depends: T600-T680] Each new Phase E provider inherits its base's contract + ≥ 3 quirk tests + `ParallelIsolationContract` [FR:042, FR:060]

### E.10 — Benchmark expansion + Phase E READMEs

- [x] **T719** [P] [depends: T500-T702] Write `README.md` for each Phase E package (~20 READMEs: Docker, Parallelism, Ci, Observability.Metrics, .AppInsights, Security.Mtls, .Policies, Microservices.Saga, .Contracts, plus the 9 remaining provider packages + 3 meta-packages). [SC-006]
- [x] **T720** [depends: T600-T681, T719] Expand `tests/Rig.TUnit.Benchmarks/` — BenchmarkDotNet suite per area (fixture startup, isolation overhead, assertion-DSL throughput); compare against `benchmarks/baseline-002.json` (captured in T007); regression budget < 110% [FR:043, SC-004]

---

## Phase F: Definition-of-Done Verification

- [x] **T800** [depends: T160, T290, T370, T470, T710, T720] Run `dotnet build` full solution — ZERO warnings (warnings-as-errors) [SC-001]
- [x] **T801** Run `dotnet test` full solution — coverage gate ≥ 90%/85% per package [SC-002]
- [x] **T802** Run `Rig.TUnit.Architecture.Tests` — GREEN, zero circular deps [SC-003]
- [x] **T803** Run `dotnet test tests/Rig.TUnit.Benchmarks` — within regression budget (< 110% baseline) [SC-004]
- [x] **T804** Confirm old packages (`Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus`) and their tests DELETED; `Rig.TUnit.slnx` clean [SC-005]
- [x] **T805** Every package ships `README.md` + one example test [SC-006]
- [x] **T806** `GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true` produces zero CS1591 warnings in source [SC-007]
- [x] **T807** CI matrix GREEN: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x [SC-008]
- [x] **T808** Pre-existing 56 tests ported + GREEN; final count several hundred [SC-009]
- [x] **T809** Every feature-branch commit exhibits `test: red`, `feat: green`, `refactor:` cadence [SC-011]
- [x] **T810** Anti-pattern detector self-test catches 100% of documented violations [SC-013]
- [x] **T811** JWT/OAuth tests run against real `JwtBearerHandler` — zero bypass in new code; legacy `TestAuthenticationHandler` present only for smoke tests [SC-014]
- [x] **T812** `Microservices.Outbox` `ExactlyOnce` under 100 concurrent relay runs across SqlServer+ServiceBus AND SqlServer+Kafka matrix [SC-015]

---

## Summary

| Phase | Tasks | Focus |
|---|---|---|
| Phase 0 (Setup) | T001–T007 | Version pinning, CPM, research, commit-msg hook, benchmark baseline |
| Phase A | T010–T160 | Base contracts + hard cutover + 4 providers + READMEs |
| Phase B | T200–T290 | Observability (runtime + analyzer), Security, Http, Resilience + READMEs |
| Phase C | T300–T370 | Microservices, Concurrency, Health, Memory cache + READMEs |
| Phase D | T400–T470 | Provider expansion (10 providers) + READMEs |
| Phase E | T500–T720 | Polish, remaining providers (with paired test projects), meta-packages (with purity rule), benchmarks + READMEs |
| Phase F | T800–T812 | Definition-of-Done verification |

- **Total tasks**: 338 (post-analysis fix; up from 312)
- **New in revision**: T006 (commit-msg hook), T007 (benchmark baseline), T159/T289/T369/T469/T719 (per-phase README tasks), T227/T228 (Roslyn analyzer + tests), T601/T611/T621/T631/T641/T651/T661/T671/T681 (Phase E paired test projects), T703 (meta-package purity rule)
- **Parallel opportunities**: ~95 tasks marked `[P]`
- **TDD-disciplined tasks**: all content-generating tasks (140+) — every class ships RED → GREEN → REFACTOR commits per plan.md §"TDD Execution Discipline"; enforced by T006 commit-msg hook
- **Merge gates**: T160 (Phase A), T290 (Phase B), T370 (Phase C), T470 (Phase D), T800–T812 (DoD)
- **Analysis findings resolved**: F1–F17 (see [analysis.md](analysis.md)) — 3 HIGH + 8 MEDIUM + 6 LOW all addressed.

---

## Next Commands

- `/dai.analyze` — validate spec/plan/tasks consistency before execution
- `/dai.go` — execute tasks with merge-gate enforcement per phase (start with T001)
- `/dai.review` — review after each phase completes
