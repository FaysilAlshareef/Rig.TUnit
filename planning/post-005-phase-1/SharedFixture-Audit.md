# Shared-Fixture Audit — Feature 005 A005

**Audit date**: 2026-04-20 (Phase 1 scope)
**Scope**: every `Shared*Fixture.cs` under `tests/`
**FR coverage**: FR-011, SC-013
**Resolution path**: Phase 3 sub-thread T066 + T067

## Classification key

- **(a) safe-because-IsolationKey** — the consumers derive per-test names (database, collection, keyspace, key-prefix, topic suffix) from `IsolationKey` or an equivalent primitive. The shared container is fine; fixture does not need converting.
- **(b) unsafe-needs-Phase-3-conversion** — the shared container is handed to every test as-is; consumers don't derive per-test names. Phase 3 T066 converts these to per-test ephemeral helpers (DB / schema / keyspace / bucket-prefix / collection per test).
- **(c) needs-`[NotInParallel]`-stopgap with Phase-3 ticket** — race is acknowledged but cannot be fixed in Phase 3's budget; serialise in the meantime and file the ticket.

## Inventory

| # | Fixture path | Backing resource | Per-test primitive in tests? | Classification | Notes |
|---|---|---|---|---|---|
| 1 | `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/SharedPostgresFixture.cs` | 1× Postgres container, 1× DB | Yes — T004 added `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` | **(a) safe** | Feature 005 T004 resolved |
| 2 | `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SharedSqlServerFixture.cs` | 1× SQL Server container, 1× DB | No helper equivalent to `CreateEphemeralDatabaseAsync` exists yet | **(b) unsafe** | Phase 3 T066: add `SqlServerDbContextHelper.CreateEphemeralDatabaseAsync` mirroring the Postgres pattern. Same race class — sibling tests share `Samples`-equivalent tables. |
| 3 | `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/SharedMySqlFixture.cs` | 1× MySQL container, 1× DB | No | **(b) unsafe** | Phase 3 T066: `MySqlDbContextHelper.CreateEphemeralDatabaseAsync`. `CREATE DATABASE` + `DROP DATABASE` — same template as Postgres; adjust quoting to backticks. |
| 4 | `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/SharedOracleFixture.cs` | 1× Oracle container, 1× user/schema | No | **(b) unsafe** | Phase 3 T066: per-test PL/SQL user via `CREATE USER IDENTIFIED BY …` + `GRANT`. Heavier than other SQLs; doc the extra container-startup cost. |
| 5 | `tests/Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration/SharedMongoFixture.cs` | 1× Mongo container | Yes — consumers wire `CollectionPerTestHelper` (existing, 004 FR-005) | **(a) safe** | Confirmed — [`src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/CollectionPerTestHelper.cs`](../../src/Rig.TUnit.Databases.NoSql.Mongo/Helpers/CollectionPerTestHelper.cs) drops the collection on dispose. Add `// Intentional reuse …` comment on the fixture in Phase 4. |
| 6 | `tests/Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration/SharedCassandraFixture.cs` | 1× Cassandra container | Yes — `KeyspacePerTestHelper` (existing) | **(a) safe** | Confirmed — [`src/Rig.TUnit.Databases.NoSql.Cassandra/Helpers/KeyspacePerTestHelper.cs`](../../src/Rig.TUnit.Databases.NoSql.Cassandra/Helpers/KeyspacePerTestHelper.cs). |
| 7 | `tests/Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration/SharedDynamoFixture.cs` | 1× DynamoDB-Local container | Partial — per-test table prefixes exist but not universally | **(b) unsafe** | Phase 3 T066: introduce `TablePerTestHelper` that provisions `{name}_{IsolationKey}` tables and drops them on dispose. |
| 8 | `tests/Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration/SharedElasticSearchFixture.cs` | 1× ES container | Partial — index-name suffixing in some tests | **(b) unsafe** | Phase 3 T066: `IndexPerTestHelper` (`DELETE` the index on dispose; no CAS contention). |
| 9 | `tests/Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration/SharedKurrentDbFixture.cs` | 1× KurrentDb container | Partial — stream-name suffixing via `IsolationKey` in existing tests | **(a) safe** | KurrentDb has no schema to collide on; streams are append-only and named per test. Add `// Intentional reuse per 004 stream-append semantics` comment. |
| 10 | `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Integration/SharedRedisKvFixture.cs` | 1× Redis (KV use) | Yes — key-prefix pattern via `IsolationKey.ForRedisKeyPrefix()` | **(a) safe** | Also doc-reuses `Rig.TUnit.Caching.Redis` container binary (per spec 003 §4.4 edge case). Comment: `// Intentional reuse of Caching.Redis container per 003 §4.4`. |
| 11 | `tests/Rig.TUnit.Caching.Redis.Tests.Integration/SharedRedisFixture.cs` | 1× Redis (cache use) | Yes — `IMemoryCache`/`IDistributedCache` key-prefix helpers | **(a) safe** | Complementary to #10 above. |
| 12 | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/SharedKafkaFixture.cs` | 1× Kafka broker | Partial — per-test topic names via `IsolationKey` in consumer-facing tests; not in listener-lifecycle tests | **(c) stopgap** | Phase 3 T066: extract `TopicPerTestHelper` that creates `topic_{IsolationKey}` on demand and deletes on dispose. Interim: add `[NotInParallel]` to `ListenerLifecycleTests` until the helper ships. |
| 13 | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/SharedNatsFixture.cs` | 1× NATS server | Partial — subject-name suffixing in most tests | **(b) unsafe** | Phase 3 T066: `SubjectPerTestHelper`. NATS has no server-side index so cleanup is trivial. |
| 14 | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/SharedRabbitMqFixture.cs` | 1× RabbitMQ container | Partial — queue-name suffixing variable across tests | **(b) unsafe** | Phase 3 T066: `QueuePerTestHelper` + `ExchangePerTestHelper` (both delete on dispose via AMQP). |
| 15 | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/SharedServiceBusFixture.cs` | 1× Azure Service Bus emulator | Partial — topic/queue suffixing incomplete | **(b) unsafe** | Phase 3 T066: equivalent helper. Slowest provisioning of any broker — doc the per-test cost on CI. |
| 16 | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/SharedSqsFixture.cs` | 1× LocalStack (SQS) | Yes — per-test queue URLs via `IsolationKey` throughout | **(a) safe** | LocalStack cleanup is free; queues vanish on container stop. |
| 17 | `tests/Rig.TUnit.Storage.AzureBlob.Tests.Integration/SharedAzureBlobFixture.cs` | 1× Azurite emulator | Partial — container-name suffixing in most tests | **(b) unsafe** | Phase 3 T066: `ContainerPerTestHelper`. |
| 18 | `tests/Rig.TUnit.Storage.S3.Tests.Integration/SharedS3Fixture.cs` | 1× LocalStack (S3) | Partial — bucket-prefix pattern in most tests | **(b) unsafe** | Phase 3 T066: `BucketPerTestHelper` (delete on dispose; LocalStack is forgiving). |
| 19 | `tests/Rig.TUnit.Storage.MinIO.Tests.Integration/SharedMinIOFixture.cs` | 1× MinIO container | Partial — bucket-prefix same as S3 | **(b) unsafe** | Phase 3 T066: same helper class reused with MinIO client. |
| 20 | `tests/Rig.TUnit.Observability.Seq.Tests.Integration/SharedSeqFixture.cs` | 1× Seq container | Yes — per-test API key / log stream derived from `IsolationKey` | **(a) safe** | Seq log events are append-only and time-series; no reset race. Add `// Intentional reuse per 004 append-only semantics` comment. |

## Totals

- **Category (a) — safe**: 7 fixtures (Postgres now resolved, Mongo, Cassandra, KurrentDb, Redis KV, Redis cache, Sqs, Seq)
- **Category (b) — unsafe (Phase 3 T066 conversion)**: 12 fixtures (SqlServer, MySql, Oracle, Dynamo, ElasticSearch, Nats, RabbitMq, ServiceBus, AzureBlob, S3, MinIO, plus ticketed others)
- **Category (c) — stopgap `[NotInParallel]`**: 1 fixture (Kafka listener-lifecycle subset)

## Deliverables Phase 3 T066 will add

Each (b)/(c) entry gets a matching `{Family}PerTestHelper.cs` in `src/Rig.TUnit.{Family}.{Provider}/Helpers/`, following the Postgres/Mongo/Cassandra pattern:

1. Factory method `CreateAsync(client/connection, IsolationKey, CancellationToken) -> THelper`
2. `THelper` implements `IAsyncDisposable` — tears down on dispose
3. Matching integration test `{Family}PerTestHelperTests.cs` that forks 10 parallel tasks and asserts zero cross-talk
4. `// Intentional reuse per 004 edge case: <reason>` comment on the fixture for category (a) entries

## FR-011 close-out criteria

- Every category (b) fixture has its `PerTestHelper` shipped and adopted by its consumer tests.
- Every category (a) fixture carries the `Intentional reuse …` rationale comment (enforced by Phase 4 T104b `SharedFixtureGuardTests`).
- Every category (c) stopgap has a follow-up Phase 3 PR landing the helper and removing the `[NotInParallel]` marker (enforced by Phase 4 T104b `NoSkipMarkersTests`).

## Commit note

This document lands as `A005` (audit namespace). No RED/GREEN partner per analysis #7 resolution — A-prefix audit tasks are exempt from FR-001 RED/GREEN cadence (see spec.md).
