# Real Coverage Gap Matrix — Feature 006

**Scan date**: 2026-04-21  
**Source**: CI run `24712477011` on branch `ci/coverage-scan`  
**Artefacts**: `coverage-scan-results/summary.csv`, `coverage-scan-results/merged.cobertura.xml`  
**Parser**: MultiReport (105× Cobertura)  
**Assemblies scanned**: 68 | **Classes**: 412 | **Covered lines**: 4 155 | **Uncovered lines**: 1 007

---

## Overall metrics

| Metric | Actual | Gate | Status |
|--------|--------|------|--------|
| Line coverage | 80.4 % | ≥ 90 % | FAIL |
| Branch coverage | 66.4 % | ≥ 85 % | FAIL |
| Uncovered lines | 1 007 | — | — |

---

## Package-level summary (failing packages only)

Gate: ≥ 90 % line coverage per package. 29 of 40 packages fail.

### Critical — < 50 % (12 packages)

| Package | Line % | Key zero-coverage classes | Root cause |
|---------|--------|--------------------------|------------|
| `Rig.TUnit.Databases.NoSql` | 12.5 % | `JsonDocumentAssert` (8 %), `ChangeFeedCapture<TDocument>` (0 %) | Pattern B — base-family assertions never called by provider tests |
| `Rig.TUnit.Storage` | 16.6 % | `BlobAssert`, `BlobAssertion`, `BlobAssertionException`, `BlobDescriptor`, `LifecycleRule`, `SasBuilder` — all 0 % | Pattern B — base-family assertions never called by provider tests |
| `Rig.TUnit.Caching` | 18 % | `CacheAssert` (0 %), `BackplaneCapture` (20 %), `BackplaneMessage` (0 %), `ClockControl` (0 %), `StampedeTester` (0 %) | Pattern B — base-family helpers/assertions not exercised |
| `Rig.TUnit.Databases.NoSql.Redis` | 23.5 % | `RedisKvRigBuilder` (0 %), `RedisKvRigBuilderExtensions` (0 %), `KeyScanHelper` (50 %) | Pattern A — builder bypassed by integration tests |
| `Rig.TUnit.Observability.Seq` | 25.5 % | `SeqAssert` (0 %), `SeqAssertionException` (0 %), `SeqQueryAssertion` (0 %), `SeqFixture` (40.8 %) | Pattern C — Seq integration tests exist but do not exercise assertion API |
| `Rig.TUnit.Security` | 25.9 % | `SecurityAssert` (0 %), `SecurityAssertionException` (0 %) | Pattern B — base-family assertion never called |
| `Rig.TUnit.Messaging` | 30.9 % | `DeadLetterAssert` (0 %), `OrderingAssert` (0 %), `OrderingAssert<T>` (0 %), `MessageAssert` (20 %), `EventEnvelope` (0 %) | Pattern B — base-family assertions not exercised by provider tests |
| `Rig.TUnit.Microservices.Contracts` | 35 % | `PactBrokerClientStub` (0 %), `ProviderVerificationHarness` (0 %), `ProviderVerificationReport` (0 %) | Pattern C — contract harness helpers have zero test coverage |
| `Rig.TUnit.Caching.Redis` | 38 % | `RedisCacheRigBuilder` (0 %), `RedisCacheRigBuilderExtensions` (0 %), `RedisBackplaneCapture` (15.7 %) | Pattern A — builder bypassed; backplane capture partially exercised |
| `Rig.TUnit.Grpc` | 40.4 % | `GrpcClientHelper<TClient,TProgram,TResult>` (0 %), `GrpcClientHelper<TClient,TProgram>` (0 %), `EndpointMappingStartupFilter` (0 %), `WebApplicationFactoryExtensions` (26.6 %) | Pattern C — Grpc integration tests missing from production CI entirely |
| `Rig.TUnit.Databases.Sql` | 43.5 % | `RawSqlAssert` (0 %), `RawSqlAssert<T>` (0 %), `DeadlockSimulator` (0 %), `TransactionScope` (0 %), `DbContextHelper<TContext,T>` (55.8 %) | Pattern B — base-family assertions not exercised; advanced helpers untested |
| `Rig.TUnit.Databases` | 46.9 % | `DatabaseAssert` (0 %), `MigrationAssert` (0 %), `SeedBuilder<T>` (80 %) | Pattern B — base-family assertions not exercised |

### High — 50 – 74 % (7 packages)

| Package | Line % | Key zero/partial classes | Root cause |
|---------|--------|--------------------------|------------|
| `Rig.TUnit.Databases.Sql.SqlServer` | 51.4 % | `SqlServerRigBuilder` (0 %), `SqlServerRigBuilderExtensions` (0 %) | Pattern A — builder bypassed |
| `Rig.TUnit.Messaging.ServiceBus` | 59.7 % | `ServiceBusEventSender` (35 %), `ServiceBusListener` (26.6 %) | Pattern C — Azure Service Bus emulator tests partial; sender/listener hot-paths not exercised |
| `Rig.TUnit.Databases.Sql.Oracle` | 62.5 % | `OracleRigBuilder` (33.3 %), `OracleBuilderExtensions` (0 %) | Pattern A — builder partially bypassed |
| `Rig.TUnit.Caching.Memory` | 63.1 % | `MemoryCacheRigBuilder` (0 %), `MemoryCacheRigBuilderExtensions` (0 %), `InMemoryConnectionSource` (0 %) | Pattern A — builder bypassed |
| `Rig.TUnit.Observability.AppInsights` | 71.7 % | `AppInsightsDependencyAssertion` (0 %), `AppInsightsAssertionException` (0 %), `AppInsightsEventAssertion` (33.3 %), `AppInsightsExceptionAssertion` (33.3 %), `AppInsightsRigBuilder` (50 %) | Pattern C — partial AppInsights test coverage; assertion API underused |
| `Rig.TUnit.Databases.Sql.MySql` | 72.9 % | `MySqlRigBuilder` (20 %) | Pattern A — builder partially bypassed |
| `Rig.TUnit.Databases.Sql.Sqlite` | 74.3 % | `SqliteRigBuilder` (0 %), `SqliteRigBuilderExtensions` (0 %) | Pattern A — builder bypassed |

### Moderate — 75 – 89 % (10 packages)

| Package | Line % | Key partial/zero classes | Root cause |
|---------|--------|--------------------------|------------|
| `Rig.TUnit.Microservices.Saga` | 77.8 % | `SagaAssert` (50 %), `SagaHarness` (69.2 %), `CompensationFailure` (0 %), `SagaAssertionException` (0 %) | Partial saga error-path coverage |
| `Rig.TUnit.Messaging.Tests.Contract` | 78.4 % | `MessagingRigContract` (78.4 %) | Contract base class edge cases not hit |
| `Rig.TUnit.Resilience` | 81.7 % | `BulkheadAssert` (0 %) | Bulkhead assertion never called |
| `Rig.TUnit.Microservices.Outbox` | 82.7 % | `OutboxEntryAssertion<T>` (48.2 %), `CustomOutboxStore<TRow>` (33.3 %), `OutboxAssertionException` (0 %) | Exception/edge paths untested |
| `Rig.TUnit.HealthChecks` | 83.7 % | `HealthAssertionException` (0 %), `HealthAssert` (84.8 %) | Exception path never hit |
| `Rig.TUnit.Http` | 85.1 % | `CapturedRequest` (0 %), `NoopHandler` (0 %), `HttpMockVerifier` (64.7 %) | Request capture and noop handler untested |
| `Rig.TUnit.Security.Jwt` | 87.6 % | `JwtRigBuilder` (66.6 %) | Builder edge cases not covered |
| `Rig.TUnit.Microservices.EventSourcing` | 88.7 % | `AggregateAssert` (66.6 %), `EventCatalogueAssert` (62.5 %), `RaisedAssertion<T>` (66.6 %) | Assertion overloads not fully exercised |
| `Rig.TUnit.Observability` | 88.8 % | `TelemetryRigBuilder<TSelf>` (80 %) | Builder edge cases not covered |
| `Rig.TUnit.Security.Policies` | 88.8 % | `PolicyAssertionException` (0 %), `PolicyAssert` (76.1 %) | Exception path + assertion edge cases |

---

## Passing packages (≥ 90 %) — reference

| Package | Line % | Notes |
|---------|--------|-------|
| `Rig.TUnit.Caching.Fusion` | 100 % | Reference implementation |
| `Rig.TUnit.Caching.Hybrid` | 100 % | Reference implementation |
| `Rig.TUnit.Ci` | 100 % | |
| `Rig.TUnit.Concurrency` | 96.9 % | |
| `Rig.TUnit.Core` | 95.5 % | `WaitHelper` (90.3 %), `IsolationKey` (90.6 %) — just above gate |
| `Rig.TUnit.Databases.NoSql.Cassandra` | 90.7 % | |
| `Rig.TUnit.Databases.NoSql.Cosmos` | 95.4 % | |
| `Rig.TUnit.Databases.NoSql.Dynamo` | 97.3 % | |
| `Rig.TUnit.Databases.NoSql.ElasticSearch` | 93.3 % | |
| `Rig.TUnit.Databases.NoSql.KurrentDb` | 92.1 % | |
| `Rig.TUnit.Databases.NoSql.Mongo` | 95.6 % | |
| `Rig.TUnit.Databases.NoSql.Tests.Contract` | 100 % | |
| `Rig.TUnit.Databases.Sql.Postgresql` | 96.1 % | Gold standard for SQL providers |
| `Rig.TUnit.Databases.Sql.Tests.Contract` | 100 % | |
| `Rig.TUnit.Databases.Tests.Contract` | 93.8 % | |
| `Rig.TUnit.Docker` | 97.2 % | |
| `Rig.TUnit.Mediator` | 100 % | |
| `Rig.TUnit.Messaging.Kafka` | 96.2 % | |
| `Rig.TUnit.Messaging.Nats` | 93.2 % | |
| `Rig.TUnit.Messaging.RabbitMq` | 97.3 % | |
| `Rig.TUnit.Messaging.Sqs` | 94.4 % | |
| `Rig.TUnit.Microservices.Inbox` | 90.0 % | Exactly at gate |
| `Rig.TUnit.Microservices.Snapshots` | 100 % | |
| `Rig.TUnit.Observability.Logging` | 90.3 % | |
| `Rig.TUnit.Observability.Logging.Analyzers` | 93.5 % | |
| `Rig.TUnit.Observability.Metrics` | 96.0 % | |
| `Rig.TUnit.Observability.Tests.Contract` | 93.6 % | |
| `Rig.TUnit.Observability.Tracing` | 90.2 % | |
| `Rig.TUnit.Parallelism` | 100 % | |
| `Rig.TUnit.Parallelism.Tests.Contract` | 100 % | |
| `Rig.TUnit.Security.Mtls` | 100 % | |
| `Rig.TUnit.Security.OAuth` | 91.7 % | |
| `Rig.TUnit.Storage.AzureBlob` | 93.6 % | |
| `Rig.TUnit.Storage.FileSystem` | 94.0 % | |
| `Rig.TUnit.Storage.MinIO` | 96.4 % | |
| `Rig.TUnit.Storage.S3` | 96.2 % | |
| `Rig.TUnit.Storage.Tests.Contract` | 93.1 % | |
| `Rig.TUnit.WebAPI` | 100 % | Reference for communication testing |
| `Rig.TUnit.Caching.Tests.Contract` | 93.6 % | |

---

## Root-cause patterns

### Pattern A — Builder API bypassed by integration tests

Integration tests for SQL (non-Postgres), Redis KV, Redis cache, and Memory cache construct fixture
instances directly without routing through `{Provider}RigBuilder` / `{Provider}RigBuilderExtensions`.
The builder code exists but is never called, so it measures 0 %.

**Fix**: Add builder-path unit tests that call `RigConnect.FromContainer()`, `FromConfig()`,
`FromOptions()`, and `FromValue()` on each affected builder, asserting the resulting
`ConnectionSource` type.  The `Rig.TUnit.Databases.Sql.Postgresql` builder tests (100 % line) are
the reference implementation.

**Affected packages (7)**: `Databases.Sql.SqlServer`, `Databases.Sql.MySql`, `Databases.Sql.Oracle`,
`Databases.Sql.Sqlite`, `Databases.NoSql.Redis`, `Caching.Redis`, `Caching.Memory`.

### Pattern B — Base-family assertion/helper classes never reached from provider tests

Base packages (`Rig.TUnit.Caching`, `Rig.TUnit.Databases`, `Rig.TUnit.Databases.NoSql`,
`Rig.TUnit.Databases.Sql`, `Rig.TUnit.Messaging`, `Rig.TUnit.Security`, `Rig.TUnit.Storage`)
expose assertion helpers and utility classes that provider integration tests never import.

**Fix**: Each base package needs dedicated unit tests that exercise the assertion helpers directly.
`Rig.TUnit.Caching.Tests.Contract` and `Rig.TUnit.Databases.Sql.Tests.Contract` (both 100 %)
demonstrate the contract-test approach.  For helpers that cannot be tested in isolation (e.g.
`BackplaneTester` requires a real Redis), add a call from the Fusion or Hybrid integration suite
which already has a working Redis container.

**Affected packages (6)**: `Caching`, `Databases`, `Databases.NoSql`, `Databases.Sql`, `Messaging`,
`Security`, `Storage`.

### Pattern C — Specific helper classes with zero coverage (miscellaneous)

Classes where the feature exists but the test suite does not call it: `Grpc.GrpcClientHelper`,
`Grpc.EndpointMappingStartupFilter`, `Microservices.Contracts` harness helpers, `Observability.Seq`
assertion API, `Messaging.ServiceBus` sender/listener hot-paths, `Http.CapturedRequest`,
`Resilience.BulkheadAssert`.

**Fix**: Case-by-case targeted unit or integration tests per class.  Many of these are blocked by
missing integration-test projects in CI (see `CI-Pipeline-Gap-Audit.md`).

---

## Effort summary by pattern

| Pattern | Affected packages | Approx uncovered lines | Primary fix |
|---------|------------------|----------------------|-------------|
| A — Builder bypass | 7 | ~210 | Unit tests for builder API |
| B — Base assertions | 7 | ~380 | Dedicated assertion unit tests + contract extension |
| C — Specific helpers | mixed | ~417 | Targeted tests per class |
