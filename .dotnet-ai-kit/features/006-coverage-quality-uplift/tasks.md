# Tasks: Coverage & Quality Uplift

**Feature**: 006-coverage-quality-uplift | **Mode**: Generic
**Generated**: 2026-04-21 | **Total Tasks**: 35
**Branch**: `feat/006-coverage-quality-uplift`

---

## Ordering Rules

- **Phase 1 MUST merge** before Phases 2–4 begin
- Phases 2, 3, 4 run in parallel after Phase 1 (separate sub-branches)
- Phases 5 and 6 are independent of all other phases
- Phase 7 is LAST — only after SC-060 and SC-061 are GREEN on `master`

---

## Phase 1 — CI Foundation (BLOCKING)

> Merge this phase first. All other phases depend on accurate CI measurement.

- [x] T001 Extend `integration-core` matrix to include 6 missing projects
      File: `.github/workflows/ci.yml` line 294
      Change: `area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]`
      →       `area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience, Core, Ci, Grpc, Http, WebAPI, Mediator]`
      Commit: `green(T001): CI change — no production code affected`

- [x] T002 Annotate coverage gate `continue-on-error` with re-enable reference
      File: `.github/workflows/ci.yml` line ~362
      Add comment above `continue-on-error: true`: `# Disabled 2026-04-20; re-enabled by feat/006 T090`
      Commit: `green(T002): CI change — no production code affected`

- [x] T003 [depends: T001, T002] Verify all 6 new integration projects PASS in CI run
      Action: Push Phase 1 PR, watch `Integration — Core` matrix, record run ID in PR description
      Result: Run 24719807423 — Core ✅ Ci ✅ Grpc ✅ Http ✅ WebAPI ✅ Mediator ✅ (all pass, no flakiness)

---

## Phase 2 — Pattern A: Builder API Coverage

> Prerequisite: Phase 1 merged. Sub-branches may be opened in parallel for each task.
> Reference: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresRigBuilderExtensionsTests.cs`
> Pattern: `services.AddRigTUnit(rig => captured = rig)` → `RigConnect.FromValue(cs)` → assert fluent chain + provider extension.

- [x] T010 [P] `Databases.Sql.SqlServer` — 51.4 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit/SqlServerBuilderTests.cs` (new)
      Tests:
        `UseSqlServer_NullRig_ThrowsArgumentNullException`
        `UseSqlServer_NullSource_ThrowsArgumentNullException`
        `UseSqlServer_NullConfigure_ThrowsArgumentNullException`
        `UseSqlServer_WithValidArgs_ReturnsSameRigBuilderForFluentChain`
        `UseSqlServer_ConfigureReceivesSqlServerRigBuilderInstance`
        `ReplaceDbContext_WiresUpSqlServerViaUseProvider_RegistersSqlServerOptionsExtension`
      Source: `SqlServerRigBuilder`, `SqlServerRigBuilderExtensions` (already exist)
      Commits: `red(T010):` (tests, failing) → `green(T010):` (tests only — production code already existed)

- [x] T011 [P] `Databases.Sql.MySql` — 72.9 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Unit/MySqlBuilderTests.cs` (new or extend)
      Tests: mirror T010 with `MySql` substitution; cover missing branches in `MySqlRigBuilder`
      Source: `MySqlRigBuilder`, `MySqlRigBuilderExtensions`
      Commits: `red(T011):` → `green(T011):`

- [x] T012 [P] `Databases.Sql.Oracle` — 62.5 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Unit/OracleBuilderTests.cs` (new or extend)
      Tests: mirror T010 with `Oracle`; cover `OracleRigBuilder` (33.3 %) + `OracleRigBuilderExtensions` (0 %)
      Source: `OracleRigBuilder`, `OracleRigBuilderExtensions`
      Commits: `red(T012):` → `green(T012):`

- [x] T013 [P] `Databases.Sql.Sqlite` — 74.3 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit/SqliteBuilderTests.cs` (new or extend)
      Tests: mirror T010 with `Sqlite`; use `"Data Source=:memory:"` (no container needed)
      Source: `SqliteRigBuilder`, `SqliteRigBuilderExtensions`
      Commits: `red(T013):` → `green(T013):`

- [x] T014 [P] `Databases.NoSql.Redis` — 23.5 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Unit/RedisKvBuilderTests.cs` (new)
      Tests:
        Builder null-guard tests (5) — `RedisKvRigBuilder`, `RedisKvRigBuilderExtensions`
        `KeyScanHelper_ScanAsync_WithPattern_ReturnsMatchingKeys` (mock `IDatabase` via NSubstitute)
        `KeyScanHelper_ScanAsync_EmptyResult_ReturnsEmptyList`
      Source: `RedisKvRigBuilder`, `RedisKvRigBuilderExtensions`, `KeyScanHelper`
      Commits: `red(T014):` → `green(T014):`

- [x] T015 [P] `Caching.Redis` — 38.0 % → ≥ 90 %
      Files:
        `tests/Rig.TUnit.Caching.Redis.Tests.Unit/RedisCacheBuilderTests.cs` (new — builder tests)
        `tests/Rig.TUnit.Caching.Fusion.Tests.Integration/BackplaneCaptureTests.cs` (extend — backplane tests need Redis)
      Tests:
        Builder null-guard tests (5) — `RedisCacheRigBuilder`, `RedisCacheRigBuilderExtensions`
        `RedisBackplaneCapture_PublishedMessage_IsRecorded` (integration)
        `RedisBackplaneCapture_Clear_RemovesAllMessages` (integration)
      Source: `RedisCacheRigBuilder`, `RedisCacheRigBuilderExtensions`, `RedisBackplaneCapture`
      Commits: `red(T015):` → `green(T015):`

- [x] T016 [P] `Caching.Memory` — 63.1 % → ≥ 90 %
      File: `tests/Rig.TUnit.Caching.Memory.Tests.Unit/MemoryCacheBuilderTests.cs` (new)
      Tests:
        Builder null-guard tests (5) — `MemoryCacheRigBuilder`, `MemoryCacheRigBuilderExtensions`
        `InMemoryConnectionSource_GetConnectionAsync_ReturnsNonNullCache`
        `InMemoryConnectionSource_ImplementsIRigConnectionSource`
      Source: `MemoryCacheRigBuilder`, `MemoryCacheRigBuilderExtensions`, `InMemoryConnectionSource`
      Commits: `red(T016):` → `green(T016):`

---

## Phase 3 — Pattern B: Base-Family Assertion Coverage

> Prerequisite: Phase 1 merged. May run in parallel with Phase 2.
> Pattern: mock `IMemoryCache`/`IDatabase`/etc. via NSubstitute; use `System.Text.Json` or `InMemoryDatabase` for EF helpers.

- [x] T020 [P] `Caching` — 18.0 % → ≥ 90 %
      ⚠️ Pre-step: create `tests/Rig.TUnit.Caching.Tests.Unit/` project + register in `Rig.TUnit.slnx`
      Files:
        `tests/Rig.TUnit.Caching.Tests.Unit/CacheAssertTests.cs` (new)
        `tests/Rig.TUnit.Caching.Tests.Unit/ClockControlTests.cs` (new)
        `tests/Rig.TUnit.Caching.Fusion.Tests.Integration/BackplaneCaptureTests.cs` (extend for StampedeTester — project confirmed in slnx)
      Tests:
        `CacheAssert_ContainsKey_WhenKeyPresent_DoesNotThrow` (mock IMemoryCache)
        `CacheAssert_ContainsKey_WhenKeyAbsent_ThrowsAssertionException`
        `BackplaneMessage_Properties_AreSetCorrectly` (construct record)
        `ClockControl_Advance_MovesTimeTo` (pure logic)
        `StampedeTester_Concurrent_NoRaceCondition` (integration, Redis available)
      Commits: `red(T020):` → `green(T020):`

- [x] T021 [P] `Databases` — 46.9 % → ≥ 90 %
      File: `tests/Rig.TUnit.Databases.Tests.Unit/DatabaseAssertTests.cs` (new)
      Tests:
        `DatabaseAssert_TableExists_WhenEntityRegistered_ReturnsTrue` (InMemoryDatabase)
        `DatabaseAssert_TableExists_WhenEntityNotRegistered_ReturnsFalse`
        `MigrationAssert_HasPendingMigrations_WhenUnmigrated_ReturnsTrue`
      Commits: `red(T021):` → `green(T021):`

- [x] T022 [P] `Databases.NoSql` — 12.5 % → ≥ 90 %
      ⚠️ Pre-step: create `tests/Rig.TUnit.Databases.NoSql.Tests.Unit/` project + register in `Rig.TUnit.slnx`
      Files:
        `tests/Rig.TUnit.Databases.NoSql.Tests.Unit/JsonDocumentAssertTests.cs` (new)
        `tests/Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration/ChangeFeedCaptureTests.cs` (extend — Cosmos emulator confirmed in CI matrix via slnx line 181)
      Tests:
        `JsonDocumentAssert_HasProperty_WhenPresent_DoesNotThrow` (JsonDocument.Parse)
        `JsonDocumentAssert_HasProperty_WhenAbsent_ThrowsAssertionException`
        `JsonDocumentAssert_HasPropertyValue_WhenCorrect_Passes`
        `ChangeFeedCapture_Capture_RecordsDocument` (integration, Cosmos emulator)
      Commits: `red(T022):` → `green(T022):`

- [x] T023 [P] `Databases.Sql` — 43.5 % → ≥ 90 %
      Files:
        `tests/Rig.TUnit.Databases.Sql.Tests.Unit/RawSqlAssertTests.cs` (new — Sqlite in-memory)
        `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/DeadlockSimulatorTests.cs` (extend)
      Tests:
        `RawSqlAssert_ExecuteScalar_ReturnsExpectedValue` (Sqlite in-memory IDbConnection)
        `RawSqlAssert_ExecuteScalar_WithWrongValue_ThrowsAssertionException`
        `RawSqlAssert_T_DeserializesRowCorrectly`
        `DbContextHelper_ExecuteRawSql_ReturnsTypedResult` (cover 55.8 % branches)
        `DeadlockSimulator_Simulate_ThrowsDeadlockException` (Sqlite integration)
        `TransactionScope_Rollback_RevertsChanges` (Sqlite integration)
      Commits: `red(T023):` → `green(T023):`

- [x] T024 [P] `Messaging` — 30.9 % → ≥ 90 %
      ⚠️ Pre-step: create `tests/Rig.TUnit.Messaging.Tests.Unit/` project + register in `Rig.TUnit.slnx`
      File: `tests/Rig.TUnit.Messaging.Tests.Unit/MessagingAssertTests.cs` (new)
      Tests:
        `MessageAssert_HasSubject_WhenCorrect_DoesNotThrow`
        `MessageAssert_HasSubject_WhenWrong_ThrowsAssertionException`
        `OrderingAssert_IsOrdered_WhenSequential_Passes`
        `OrderingAssert_IsOrdered_WhenOutOfOrder_ThrowsAssertionException`
        `OrderingAssert_T_IsOrdered_WithSelector_Passes`
        `DeadLetterAssert_HasDeadLetter_WhenPresent_Passes`
        `DeadLetterAssert_HasDeadLetter_WhenAbsent_ThrowsAssertionException`
        `EventEnvelope_Properties_AreSetCorrectly`
        `EventEnvelope_Serialize_RoundTrips`
      All via `List<CapturedMessage<T>>` — no broker container.
      Commits: `red(T024):` → `green(T024):`

- [x] T025 [P] `Security` — 25.9 % → ≥ 90 %
      ⚠️ Pre-step: create `tests/Rig.TUnit.Security.Tests.Unit/` project + register in `Rig.TUnit.slnx`
      File: `tests/Rig.TUnit.Security.Tests.Unit/SecurityAssertTests.cs` (new)
      Tests:
        `SecurityAssert_ReturnsUnauthorized_WhenStatus401_DoesNotThrow` (mock HttpResponseMessage)
        `SecurityAssert_ReturnsUnauthorized_WhenStatus200_ThrowsSecurityAssertionException`
        `SecurityAssert_ReturnsForbidden_WhenStatus403_DoesNotThrow`
        `SecurityAssertionException_Message_ContainsExpectedAndActualStatus`
        `SecurityAssertionException_IsExceptionSubtype`
      Commits: `red(T025):` → `green(T025):`

- [x] T026 [P] `Storage` — 16.6 % → ≥ 90 %
      ⚠️ Pre-step: create `tests/Rig.TUnit.Storage.Tests.Unit/` project + register in `Rig.TUnit.slnx`
      Files:
        `tests/Rig.TUnit.Storage.Tests.Unit/BlobAssertTests.cs` (new)
        `tests/Rig.TUnit.Storage.Tests.Unit/BlobValueObjectTests.cs` (new)
      Tests:
        `BlobDescriptor_Properties_AreSetCorrectly` (value object)
        `LifecycleRule_Properties_AreSetCorrectly` (value object)
        `SasBuilder_Build_ReturnsUriWithSasToken` (value object)
        `BlobAssert_BlobExists_WhenPresent_DoesNotThrow` (mock BlobContainerClient via NSubstitute)
        `BlobAssert_BlobExists_WhenAbsent_ThrowsBlobAssertionException`
        `BlobAssertion_HasContentType_WhenCorrect_Passes`
        `BlobAssertionException_Message_ContainsExpectedBlobName`
      Commits: `red(T026):` → `green(T026):`

---

## Phase 4 — Pattern C: Targeted Helper Coverage

> Prerequisite: Phase 1 merged. May run in parallel with Phases 2 and 3.

- [ ] T030 [P] `Grpc` — 40.4 % → ≥ 90 %
      File: `tests/Rig.TUnit.Grpc.Tests.Integration/GrpcClientHelperTests.cs` (new or extend)
      Tests:
        `GrpcClientHelper_CreateClient_ReturnsTypedChannel`
        `EndpointMappingStartupFilter_Configure_RegistersEndpoints`
        `WebApplicationFactoryExtensions_CreateGrpcChannel_ReturnsWorkingChannel` (cover uncovered branches)
      Note: Project is now in CI matrix after T001.
      Commits: `red(T030):` → `green(T030):`

- [x] T031 [P] `Observability.Seq` — 25.5 % → ≥ 90 %
      Files:
        `tests/Rig.TUnit.Observability.Seq.Tests.Unit/SeqAssertTests.cs` (new)
        `tests/Rig.TUnit.Observability.Seq.Tests.Integration/SeqFixtureTests.cs` (extend)
      Tests:
        `SeqAssert_HasEvent_WhenPresent_DoesNotThrow` (mock HTTP response)
        `SeqAssert_HasEvent_WhenAbsent_ThrowsSeqAssertionException`
        `SeqAssertionException_Message_ContainsExpectedLevel`
        `SeqQueryAssertion_Build_ReturnsCorrectQueryString`
        `SeqFixture_QueryLogs_ReturnsMatchingEvents` (integration — Seq container)
      Commits: `red(T031):` → `green(T031):`

- [ ] T032 [P] `Microservices.Contracts` — 35.0 % → ≥ 90 % (see C-001 — no WireMock.Net)
      File: `tests/Rig.TUnit.Microservices.Contracts.Tests.Unit/ContractsAssertTests.cs` (new)
      Tests:
        `PactBrokerClientStub_Load_WhenFileExists_ReturnsPact` (temp dir + File.WriteAllText)
        `PactBrokerClientStub_Load_WhenFileNotFound_ThrowsFileNotFoundException`
        `PactBrokerClientStub_DiscoverPacts_ReturnsAllJsonFileNames`
        `ProviderVerificationHarness_VerifyAllAsync_AllPass_ReturnsSuccessReport` (inline lambda)
        `ProviderVerificationHarness_VerifyAllAsync_StatusMismatch_RecordsFailure`
        `ProviderVerificationHarness_VerifyAllAsync_BodyMismatch_RecordsFailure`
        `ProviderVerificationReport_Success_TrueWhenNoFailures`
        `ProviderVerificationReport_Success_FalseWhenHasFailures`
      Commits: `red(T032):` → `green(T032):`

- [ ] T033 [P] `Messaging.ServiceBus` — 59.7 % → ≥ 90 %
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/ServiceBusListenerTests.cs` (extend)
      Tests:
        `ServiceBusEventSender_Send_DeliversMessageToQueue`
        `ServiceBusEventSender_Send_WithProperties_SetsHeaders`
        `ServiceBusListener_Ack_CompletesMessage`
        `ServiceBusListener_Nack_AbandonsMessage`
        `ServiceBusListener_DeadLetter_MovesMessageToDeadLetterQueue`
        `ServiceBusListener_Retry_RedeliversAfterDelay`
      Add `[Retry(3)]` to all tests in this file.
      Commits: `red(T033):` → `green(T033):`

- [ ] T034 [P] `Http` — 85.1 % → ≥ 90 %
      File: `tests/Rig.TUnit.Http.Tests.Unit/HttpHelperTests.cs` (new or extend)
      Tests:
        `CapturedRequest_Properties_AreSetCorrectly`
        `CapturedRequest_ToString_IncludesMethodAndPath`
        `NoopHandler_SendAsync_PassesRequestThrough`
        `HttpMockVerifier_Verify_WhenExpectedCallsMade_DoesNotThrow` (uncovered branches)
        `HttpMockVerifier_Verify_WhenUnexpectedCall_ThrowsVerificationException`
      Commits: `red(T034):` → `green(T034):`

- [ ] T035 [P] `HealthChecks` — 83.7 % → ≥ 90 %
      File: `tests/Rig.TUnit.HealthChecks.Tests.Unit/HealthAssertionExceptionTests.cs` (new)
      Tests:
        `HealthAssertionException_Message_ContainsCheckNameAndStatus`
        `HealthAssertionException_IsExceptionSubtype`
        `HealthAssertionException_Ctor_SetsAllProperties`
      Commits: `red(T035):` → `green(T035):`

- [ ] T036 [P] `Resilience` — 81.7 % → ≥ 90 %
      File: `tests/Rig.TUnit.Resilience.Tests.Unit/BulkheadAssertTests.cs` (new)
      Tests:
        `BulkheadAssert_IsRejecting_WhenAtCapacity_DoesNotThrow` (mock policy via NSubstitute)
        `BulkheadAssert_IsRejecting_WhenBelowCapacity_ThrowsAssertionException`
      Commits: `red(T036):` → `green(T036):`

- [x] T037 [P] `Microservices.Saga` — 77.8 % → ≥ 90 %
      Files:
        `tests/Rig.TUnit.Microservices.Saga.Tests.Unit/SagaAssertTests.cs` (new)
        `tests/Rig.TUnit.Microservices.Saga.Tests.Integration/SagaHarnessTests.cs` (extend)
      Tests:
        `SagaAssert_IsCompleted_WhenCompleted_DoesNotThrow`
        `SagaAssert_IsCompensated_WhenCompensated_DoesNotThrow` (uncovered branch)
        `CompensationFailure_Properties_AreSetCorrectly`
        `SagaAssertionException_Message_ContainsSagaName`
        `SagaHarness_Execute_WithCompensation_RecordsCompensationSteps` (integration)
      Commits: `red(T037):` → `green(T037):`

- [x] T038 [P] `Microservices.Outbox` — 82.7 % → ≥ 90 %
      File: `tests/Rig.TUnit.Microservices.Outbox.Tests.Unit/OutboxAssertTests.cs` (new or extend)
      Tests:
        `OutboxEntryAssertion_HasEntry_WhenPresent_DoesNotThrow` (uncovered branches)
        `OutboxEntryAssertion_HasEntry_WhenAbsent_ThrowsOutboxAssertionException`
        `CustomOutboxStore_Add_PersistsEntry` (uncovered branch — in-memory list)
        `CustomOutboxStore_GetPending_ReturnsUnpublishedEntries`
        `OutboxAssertionException_Message_ContainsEntryType`
      Commits: `red(T038):` → `green(T038):`

- [x] T039 [P] `Observability.AppInsights` — 71.7 % → ≥ 90 %
      Files:
        `tests/Rig.TUnit.Observability.AppInsights.Tests.Unit/AppInsightsAssertTests.cs` (new)
        `tests/Rig.TUnit.Observability.AppInsights.Tests.Unit/AppInsightsRigBuilderTests.cs` (new)
      Tests:
        `AppInsightsDependencyAssertion_HasDependency_WhenPresent_DoesNotThrow` (mock telemetry client)
        `AppInsightsEventAssertion_HasEvent_WhenPresent_DoesNotThrow` (uncovered branches)
        `AppInsightsExceptionAssertion_HasException_WhenPresent_DoesNotThrow` (uncovered branches)
        `AppInsightsRigBuilder_FromConfig_ResolvesConnectionSource` (builder pattern — T039)
        `AppInsightsRigBuilder_FromValue_ResolvesConnectionSource`
      Commits: `red(T039):` → `green(T039):`

- [ ] T039b [P] `Microservices.EventSourcing` — 88.7 % → ≥ 90 %
      File: `tests/Rig.TUnit.Microservices.EventSourcing.Tests.Unit/EventSourcingAssertTests.cs` (new or extend)
      Tests:
        `AggregateAssert_HasRaisedEvent_WhenPresent_DoesNotThrow` (cover 66.6 % branch)
        `EventCatalogueAssert_Contains_WhenEventInCatalogue_DoesNotThrow` (cover 62.5 % branch)
        `RaisedAssertion_T_WithCount_PassesWhenCountMatches` (cover 66.6 % branch)
      Commits: `red(T039b):` → `green(T039b):`

- [ ] T039c [P] `Security.Jwt` — 87.6 % → ≥ 90 %
      File: `tests/Rig.TUnit.Security.Jwt.Tests.Unit/JwtRigBuilderTests.cs` (new or extend)
      Tests:
        `JwtRigBuilder_FromConfig_ResolvesConfigConnectionSource`
        `JwtRigBuilder_FromValue_ResolvesValueConnectionSource`
      Pattern: builder Pattern A using `RigConnect.FromConfig()` and `RigConnect.FromValue("secret-key")`.
      Commits: `red(T039c):` → `green(T039c):`

- [ ] T039d [P] `Security.Policies` — 88.8 % → ≥ 90 %
      File: `tests/Rig.TUnit.Security.Policies.Tests.Unit/PolicyAssertTests.cs` (new or extend)
      Tests:
        `PolicyAssertionException_Message_ContainsPolicyName`
        `PolicyAssertionException_Ctor_SetsProperties`
        `PolicyAssert_Overload_WithHttpContext_Passes` (uncovered overload)
        `PolicyAssert_Overload_WithClaimsPrincipal_Passes` (uncovered overload)
      Commits: `red(T039d):` → `green(T039d):`

- [x] T039e [P] `Messaging.Tests.Contract` — 78.4 % → ≥ 90 %
      File: `tests/Rig.TUnit.Messaging.Tests.Contract/` (extend existing contract base)
      Tests: Add contract scenarios covering uncovered branches in the contract base class.
      Commits: `red(T039e):` → `green(T039e):`

---

## Phase 5 — Benchmark Remediation

> Independent — may run in parallel with Phases 2–4.

- [x] T040 Fix `CoreRuntime.Core80` → `CoreRuntime.Core90`
      File: `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` line 18
      Change: `.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)`
      →       `.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core90)`
      Note: BDN 0.14.0 only defines up to Core90 — Core100 does not exist in the installed version.
      Verified: `dotnet build` passes clean.

- [ ] T041 [depends: T040] Populate `benchmarks/baseline-006.json`
      Command:
        `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --filter "*" --exporters json --artifacts benchmarks/baseline-tmp`
      Verify: output JSON has ≥ 50 entries; all `runtime` fields contain `.NET 10.`
      Copy result to `benchmarks/baseline-006.json`
      Commit: `green(T041): Baseline JSON populated from local .NET 10 run`

- [ ] T042 [depends: T041] Update CI regression step to reference `baseline-006.json`
      File: `.github/workflows/ci.yml` — benchmark regression step
      Changes:
        1. Replace `baseline-005.json` reference with `baseline-006.json`
        2. Remove `|| echo "::warning::..."` guard so non-zero exit blocks the job
      Commit: `green(T042): CI change — regression step now blocking`

- [ ] T043 [depends: T042] Add `benchmark-action/github-action-benchmark@v1`
      File: `.github/workflows/ci.yml` — add step to benchmark job
      Manual pre-req: Create `gh-pages` branch; enable GitHub Pages in repo settings (one-time, see C-003)
      Step to add:
        ```yaml
        - name: Store benchmark result
          uses: benchmark-action/github-action-benchmark@v1
          with:
            tool: 'benchmarkdotnet'
            output-file-path: benchmarks/baseline-tmp/results/*.json
            github-token: ${{ secrets.GITHUB_TOKEN }}
            auto-push: true
            alert-threshold: '120%'
            comment-on-alert: true
            fail-on-alert: true
        ```
      Commit: `green(T043): CI change — GitHub Pages benchmark trend chart enabled`

---

## Phase 6 — Root README Rewrite

> Independent — may run in parallel with Phases 2–5.

- [ ] T060 Root README — Sections 1–4
      File: `README.md`
      Sections: 1) Headline + badges, 2) What is Rig.TUnit, 3) Provider families table, 4) Quick-start
      Commit: `green(T060): docs — sections 1–4`

- [ ] T061 [depends: T060] Root README — Sections 5–7
      File: `README.md`
      Sections: 5) Builder API, 6) Isolation, 7) Provider catalogue
      Commit: `green(T061): docs — sections 5–7`

- [ ] T062 [depends: T061] Root README — Sections 8–11
      File: `README.md`
      Sections: 8) Running tests, 9) Benchmarks, 10) CI pipeline, 11) TDD discipline
      Commit: `green(T062): docs — sections 8–11`

- [ ] T063 [depends: T062] Root README — Sections 12–14
      File: `README.md`
      Sections: 12) Contributing, 13) Architecture diagram (mermaid), 14) License
      Commit: `green(T063): docs — sections 12–14`

- [ ] T064 [depends: T063] README review pass
      Actions:
        - Compile all code snippets against current source
        - Verify all NuGet package IDs resolve
        - Verify all internal file links (`[file.cs](path/file.cs)`) exist
        - Render mermaid diagram in GitHub preview
      Commit: `green(T064): docs — review pass`

- [ ] T065 [depends: T064] Add link-checker CI job
      File: `.github/workflows/ci.yml`
      Add `linkcheck` job using `lycheeverse/lychee-action@v2` targeting `README.md`
      Verify job passes with no broken links
      Commit: `green(T065): CI change — link-checker job added`

---

## Phase 7 — Gate Hardening (LAST)

> Start only after SC-060 and SC-061 are demonstrably GREEN on `master`.

- [ ] T090 [depends: T001-T039e pass all CI] Remove `continue-on-error: true` from coverage gate
      File: `.github/workflows/ci.yml` line ~362–363
      Changes:
        1. Remove `continue-on-error: true` line
        2. In Python script offenders branch: change `sys.exit(0)` → `sys.exit(1)`
        3. Change `::warning::` to `::error::` in offenders output
      Commit: `green(T090): coverage gate is now a hard block`

- [ ] T091 [depends: T090] Deliberate-regression verification
      Actions:
        1. Create branch `test/deliberate-regression` from `master`
        2. Delete one test method to lower a package below 90 %
        3. Push PR; wait for CI
        4. Confirm coverage gate step fails (non-zero exit)
        5. Record CI run ID in `planning/post-005-coverage-quality-uplift/Feature-006-Roadmap.md` wrap-up
        6. Close PR without merging; delete branch `test/deliberate-regression`
      Exit gate: PR is blocked by CI; evidence recorded in wrap-up

---

## Progress Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1 — CI Foundation | T001–T003 | 🟡 In progress (T001 ✅ T002 ✅ T003 pending CI run) |
| Phase 2 — Pattern A Builders | T010–T016 | ⬜ Not started |
| Phase 3 — Pattern B Assertions | T020–T026 | ⬜ Not started |
| Phase 4 — Pattern C Helpers | T030–T039e | ⬜ Not started |
| Phase 5 — Benchmarks | T040–T043 | ⬜ Not started |
| Phase 6 — README | T060–T065 | ⬜ Not started |
| Phase 7 — Gate Hardening | T090–T091 | ⬜ Not started |
| **Total** | **35** | **0 / 35 complete** |
