# Rig.TUnit

A **TUnit-first integration-testing rig** for .NET — fixtures, builders, and assertions for
SQL, NoSQL, messaging, caching, storage, observability, security, and microservices.

```bash
dotnet add package Rig.TUnit --prerelease
dotnet add package Rig.TUnit.Databases.Sql.Postgresql --prerelease
```

```csharp
[Test]
public async Task Repository_Persists(CancellationToken ct)
{
    await using var db = new PostgresFixture();
    await db.InitializeAsync();

    await using var conn = new NpgsqlConnection(db.ConnectionString);
    await conn.ExecuteAsync("INSERT INTO orders (id) VALUES (gen_random_uuid())");

    var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders");
    await Assert.That(count).IsEqualTo(1);
}
```

## What's inside

- **Container fixtures** — one-line Testcontainers setup for every major datastore and broker.
- **Provider-specific assertions** — fluent assertions tailored to each backend (tables,
  message ordering, blob existence, JWT claims, …).
- **Unified builder API** — `services.AddRigTUnit(rig => rig.Use<X>(…))` wires fixtures into
  DI-first test setup.
- **Per-test isolation** — every fixture derives an `IsolationKey` from the test execution
  context, guaranteeing zero cross-test state leakage even under full parallelism.

## Provider families

| Family | Packages |
|--------|---------|
| SQL | `SqlServer` · `MySql` · `Postgresql` · `Oracle` · `Sqlite` |
| NoSQL | `Redis` · `Mongo` · `Cosmos` · `Cassandra` · `Dynamo` · `ElasticSearch` · `KurrentDb` |
| Messaging | `ServiceBus` · `Kafka` · `RabbitMq` · `Nats` · `Sqs` |
| Caching | `Redis` · `Memory` · `Fusion` · `Hybrid` |
| Storage | `AzureBlob` · `FileSystem` · `MinIO` · `S3` |
| Observability | `Logging` · `AppInsights` · `Metrics` · `Tracing` · `Seq` |
| Security | `Jwt` · `Policies` · `OAuth` · `Mtls` |
| Microservices | `EventSourcing` · `Outbox` · `Inbox` · `Saga` · `Snapshots` · `Contracts` |
| Infrastructure | `Http` · `Grpc` · `HealthChecks` · `Resilience` · `Mediator` · `Docker` · `WebAPI` |

## Documentation

- Full README + provider docs: <https://github.com/FaysilAlshareef/Rig.TUnit>
- Quick-start, builder API, messaging topology, isolation guarantees: see the repo `README.md`.
- Per-provider READMEs ship inside each leaf source folder.

## License

[MIT](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/LICENSE)
