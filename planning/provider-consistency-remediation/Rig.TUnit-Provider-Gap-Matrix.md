# Rig.TUnit — Provider Gap Matrix (evidence)

Source-of-truth snapshot verified by file inventory of `src/` on 2026-04-18. This is the raw evidence that drives `Rig.TUnit-Library-Design.md` §4. Update when a provider is completed.

Legend: ✓ = present, — = missing, N/A = not applicable for this family.

---

## Databases.Sql

| Provider | Fixture | Options | Builder | BuilderExt | EF Ext | Helpers | README |
|---|---|---|---|---|---|---|---|
| SqlServer | ✓ | ✓ | ✓ | ✓ | ✓ | via base | ✓ |
| Postgresql | ✓ | ✓ | ✓ | **→ in scope (tasks T174–T176)** | **→ in scope (tasks T174–T176)** | via base | **→ in scope (tasks T174–T176)** |
| Sqlite | ✓ | ✓ | ✓ | ✓ | ✓ | via base | — |
| **MySql** (new) | — | — | — | — | — | — | — |
| **Oracle** (new) | — | — | — | — | — | — | — |

Base (`Rig.TUnit.Databases.Sql`): `ISqlRig`, `SqlFixtureBase`, `SqlRigBuilder<TSelf>`, `DbContextHelper`, `DeadlockSimulator`, `TransactionScope`, `RawSqlAssert`, `InMemoryDbExtensions` — all ✓.

---

## Databases.NoSql

| Provider | Fixture | Options | Builder | BuilderExt | Provider Helper | README |
|---|---|---|---|---|---|---|
| Mongo | ✓ | ✓ | — | — | `CollectionPerTest` + `BsonDiff` — | — |
| Redis (KV) | *reuses Caching.Redis* | N/A | ✓ | ✓ | `KeyScanHelper` ✓ | — |
| Cassandra | ✓ | — | — | — | `KeyspacePerTest` — | — |
| Dynamo | ✓ | — | — | — | `GsiVerifier` — | — |
| ElasticSearch | ✓ | — | — | — | `IndexRefreshHelper` + `DslAssert` — | — |
| EventStore | ✓ | — | — | — | `StreamAssert` + `ProjectionAssert` — | — |
| **Cosmos** (new) | — | — | — | — | `RuChargeCapture` + `PartitionKeyDistributionChecker` — | — |

Base (`Rig.TUnit.Databases.NoSql`): `INoSqlRig`, `DocumentFixtureBase`, `NoSqlRigBuilder<TSelf>`, `JsonDocumentAssert`, `ChangeFeedCapture` — all ✓.

---

## Messaging

| Provider | Fixture | Options | Builder | BuilderExt | Listener | EventSender | README |
|---|---|---|---|---|---|---|---|
| ServiceBus | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Kafka | ✓ | ✓ | — | — | — | — | — |
| RabbitMq | ✓ | ✓ | — | — | — | — | — |
| Nats | ✓ | — | — | — | — | — | — |
| Sqs | ✓ | — | — | — | — | — | — |

Base (`Rig.TUnit.Messaging`): `IMessagingRig`, `MessagingFixtureBase`, `MessagingRigBuilder<TSelf>`, `MessageAssert`, `DeadLetterAssert`, `OrderingAssert`, `ListenerBase`, `EventSenderBase`, `TopicNamingConvention`, `EventEnvelope` — all ✓.

---

## Caching

| Provider | Fixture | Options | Builder | BuilderExt | Provider Helper | README |
|---|---|---|---|---|---|---|
| Memory | ✓ | N/A | ✓ | — | N/A | — |
| Redis | ✓ | ✓ | ✓ | ✓ | `RedisBackplaneCapture` ✓ | ✓ |
| Hybrid | ✓ | — | — | — | — | — |
| Fusion | ✓ | — | — | — | fail-safe + eager-refresh — | — |

Base (`Rig.TUnit.Caching`): `ICacheRig`, `CacheFixtureBase`, `CacheRigBuilder<TSelf>`, `CacheAssert`, `StampedeTester`, `BackplaneCapture`, `ClockControl` — all ✓.

---

## Storage

| Provider | Fixture | Options | Builder | BuilderExt | SasBuilder | README |
|---|---|---|---|---|---|---|
| AzureBlob | ✓ | ✓ | — | — | — | — |
| S3 | ✓ | ✓ | — | — | — | — |
| MinIO | ✓ | — | — | — | — | — |
| FileSystem | ✓ | — | — | — | N/A (PathSandboxHelper) | — |

Base (`Rig.TUnit.Storage`): `IStorageRig`, `StorageFixtureBase`, `StorageRigBuilder<TSelf>`, `BlobAssert` — all ✓.

---

## Security

| Provider | Fixture | Options | Builder | BuilderExt | Provider-specific | README |
|---|---|---|---|---|---|---|
| Jwt | — (builder is token-builder, not rig-builder) | ✓ | — | — | `JwtBuilder` ✓ | — |
| OAuth | — | ✓ | — | — | `MockOAuthServer` ✓ | — |
| Mtls | — | — | — | — | `MtlsCertificateBuilder` ✓ | — |
| Policies | — | — | — | — | `PolicyAssert` ✓ | — |

Base (`Rig.TUnit.Security`) — **does not exist**. Add `ISecurityRig`, `SecurityFixtureBase`, `SecurityRigBuilder<TSelf>` in this feature.

---

## Observability

| Provider | Fixture | Options | Builder | BuilderExt | Assert | Additional | README |
|---|---|---|---|---|---|---|---|
| Logging | ✓ | ✓ | — | — | `LogAssert` ✓ | `InMemoryLoggerProvider` ✓, `AntiPatternDetector` ✓ | — |
| Tracing | ✓ | ✓ | — | — | `TraceAssert` ✓ | — | — |
| Seq | ✓ | ✓ | — | — | `SeqAssert` ✓ | — | — |
| Metrics | — | — | — | — | `MetricAssert` ✓ | `MeterListener` fixture — | — |
| **AppInsights** (new) | — | — | — | — | — | — | — |

Base (`Rig.TUnit.Observability`): `ITelemetryRig`, `TelemetryFixtureBase`, `TelemetryRigBuilder<TSelf>` — all ✓.

Note: Logging/Tracing/Seq providers presently expose their functionality directly via `Use{Provider}Fixture`-style plumbing rather than a `{Provider}RigBuilder : TelemetryRigBuilder<…>`. Provider completeness test should confirm the fluent surface exists regardless of whether it's a standalone builder or a pass-through extension.

---

## Microservices (cross-cutting — no provider variations)

| Package | Fixture | Assertions | Helpers | README |
|---|---|---|---|---|
| Outbox | ✓ | `OutboxAssert` ✓ | `OutboxReplay`, `OutboxRelaySimulator`, `OutboxSchema`, `CustomOutboxStore` ✓ | — |
| Inbox | N/A | `InboxAssert` ✓ | `SequenceTracker` ✓ | — |
| EventSourcing | N/A | — | `EventSourcingHarness` ✓; missing `AggregateAssert`, `EventCatalogueVerifier`, `SchemaEvolutionHelper` | — |
| Saga | N/A | — | `SagaHarness` ✓; missing `SagaAssert.Compensated()`, `SagaTimeoutHelper` | — |
| Snapshots | N/A | `SnapshotAssert` ✓ | `MicroserviceScrubbers` ✓ | — |
| Contracts | N/A | — | `ContractPact` ✓; missing `ProviderVerificationFixture`, Pact broker client | — |

---

## Single-provider packages

| Package | Surface | Gaps |
|---|---|---|
| Http | `HttpMock` | — |
| Resilience | Polly + `FakeTimeProvider` | — |
| HealthChecks | `HealthAssert` | — |
| Concurrency | `ConcurrencyAssert` | — |
| Parallelism | port allocator, shared-state detector | — |
| Ci | TRX/JUnit enricher, flaky quarantine | — |
| **Docker** (new) | — | full template needed |

---

## Packages 003 promised but never created

1. `Rig.TUnit.Databases.Sql.MySql`
2. `Rig.TUnit.Databases.Sql.Oracle`
3. `Rig.TUnit.Databases.NoSql.Cosmos`
4. `Rig.TUnit.Observability.AppInsights`
5. `Rig.TUnit.Docker`

All five land in this feature (no deferrals).

---

## Progress tracker

Update the tables above as work progresses. When every cell in a row is ✓ (or N/A for genuinely-not-applicable), that provider is "done" for this feature. Acceptance gate: every family's row table is fully ✓ + every new package row is fully ✓.
