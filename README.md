# Rig.TUnit

**A TUnit-first integration-testing rig ecosystem for .NET.**

[![CI](https://github.com/FaysilAlshareef/Rig.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/FaysilAlshareef/Rig.TUnit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Rig.TUnit.svg?label=NuGet)](https://www.nuget.org/packages/Rig.TUnit)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## What is Rig.TUnit?

Rig.TUnit is a modular library of test helpers, fixtures, and assertion extensions built specifically
for the [TUnit](https://tunit.dev) test framework. It eliminates boilerplate from integration tests
by providing:

- **Container fixtures** — one-line Testcontainers setup for every major datastore and message broker.
- **Assertion extensions** — fluent assertions tailored to each provider (database tables, message
  ordering, blob existence, JWT claims, …).
- **A unified builder API** — `AddRigTUnit(rig => rig.UsePostgres(…))` wires fixtures into your
  DI-first test setup.
- **Per-test isolation** — every fixture derives an `IsolationKey` from the test execution context,
  guaranteeing zero cross-test state leakage even under full parallelism.

---

## Provider families

| Family | Packages |
|--------|---------|
| **SQL databases** | `SqlServer` · `MySql` · `Postgresql` · `Oracle` · `Sqlite` |
| **NoSQL databases** | `Redis` · `Mongo` · `Cosmos` · `Cassandra` · `Dynamo` · `ElasticSearch` · `KurrentDb` |
| **Messaging** | `ServiceBus` · `Kafka` · `RabbitMq` · `Nats` · `Sqs` — with a shared `SendContext` (session / partition / deduplication keys) and provider-specific `WithTopology(...)` hooks |
| **Caching** | `Redis` · `Memory` · `Fusion` · `Hybrid` |
| **Storage** | `AzureBlob` · `FileSystem` · `MinIO` · `S3` |
| **Observability** | `Logging` · `AppInsights` · `Metrics` · `Tracing` · `Seq` |
| **Security** | `Jwt` · `Policies` · `OAuth` · `Mtls` |
| **Microservices** | `EventSourcing` · `Outbox` · `Inbox` · `Saga` · `Snapshots` · `Contracts` |
| **Infrastructure** | `Http` · `Grpc` · `HealthChecks` · `Resilience` · `Mediator` · `Docker` · `WebAPI` |

---

## Quick-start

Install the core package and at least one provider:

```bash
dotnet add package Rig.TUnit
dotnet add package Rig.TUnit.Databases.Sql.Postgresql
```

Wire the fixture into your test:

```csharp
public class OrderRepositoryTests
{
    [Test]
    public async Task CreateOrder_PersistsToDatabase(CancellationToken ct)
    {
        // Arrange — spin up a real Postgres container
        await using var db = new PostgresFixture();
        await db.InitializeAsync();

        // Act
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.ExecuteAsync("INSERT INTO orders (id) VALUES (gen_random_uuid())");

        // Assert
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders");
        await Assert.That(count).IsEqualTo(1);
    }
}
```

Or compose multiple providers using the builder API:

```csharp
services.AddRigTUnit(rig =>
    rig.UsePostgres(RigConnect.FromValue(cs), pg =>
        pg.ReplaceDbContext<AppDbContext>())
       .UseServiceBus(RigConnect.FromValue(sbCs), sb => { }));
```

---

## Builder API

The builder is the primary composition point. Register it during `IServiceCollection` setup:

```csharp
services.AddRigTUnit(rig =>
{
    rig.UsePostgres(RigConnect.FromValue(connectionString), pg =>
        pg.ReplaceDbContext<OrderDbContext>());

    rig.UseGrpc(factory, grpc =>
        grpc.ReplaceClient<OrderService.OrderServiceClient>());

    rig.UseServiceBus(RigConnect.FromValue(sbCs), _ => { });
});
```

`RigConnect` has two source modes:

| Source | When to use |
|--------|-------------|
| `RigConnect.FromValue(cs)` | Hard-coded connection string (unit / integration tests) |
| `RigConnect.FromConfig(configuration, "ConnectionStrings:Db")` | Reads from `IConfiguration` (config-driven environments) |

---

## Messaging topology & sessions

All messaging providers share a unified `SendContext` record that carries cross-cutting keys:

| Field | Provider mapping | Purpose |
|-------|-----------------|---------|
| `SessionKey` | ServiceBus session ID · SQS MessageGroupId · NATS `x-session-key` header | FIFO ordering per group |
| `PartitionKey` | Kafka message key · RabbitMQ routing key | Partition / exchange routing |
| `DeduplicationKey` | ServiceBus `MessageId` · SQS deduplication ID | Idempotent send |

Pass a `SendContext` to any `*EventSender.SendAsync` overload:

```csharp
await sender.SendAsync("payload", context: new SendContext(SessionKey: "order-42"), ct: ct);
```

### Provider examples

**Azure Service Bus — session FIFO**

```csharp
await using var sender   = new ServiceBusEventSender(client, "orders");
await using var listener = new ServiceBusSessionListener(client, "orders", "shipping-sessions");
await listener.StartAsync(ct);
await sender.SendAsync("msg", context: new SendContext(SessionKey: "cust-1"), ct: ct);
```

**Apache Kafka — partition-key routing**

```csharp
await using var sender   = new KafkaEventSender(fixture.ConnectionString, "orders");
var listener = new KafkaListener(fixture.ConnectionString, "orders", "grp");
await listener.StartAsync(ct);
await sender.SendAsync("msg", context: new SendContext(PartitionKey: "cust-1"), ct: ct);
```

**Amazon SQS — FIFO group ordering**

```csharp
var sender   = new SqsEventSender(fixture.Client, fifoQueueUrl);
var listener = new SqsListener(fixture.Client, fifoQueueUrl);
await listener.StartAsync(ct);
await sender.SendAsync("msg", context: new SendContext(SessionKey: "cust-1"), ct: ct);
```

**RabbitMQ — topic exchange fan-out**

```csharp
builder.WithTopology(t =>
    t.Exchange("events", ExchangeType.Topic)
     .BindQueue("order-queue", "order.*"));
await captured.ApplyTopologyAsync(ct);
await sender.SendAsync("msg", context: new SendContext(PartitionKey: "order.created"), ct: ct);
```

**NATS JetStream — ordered consumer**

```csharp
var fx = await SharedNatsJetStreamFixture.GetAsync();
builder.WithTopology(t => t.Stream("events", cfg => cfg.WithSubjects("events.>")));
await captured.ApplyTopologyAsync(ct);
await using var sender   = new NatsJetStreamEventSender(fx.JetStream, "events.orders");
await using var listener = new NatsJetStreamListener(fx.JetStream, "events", "events.orders");
await listener.StartAsync(ct);
await sender.SendAsync("msg", context: new SendContext(SessionKey: "cust-1"), ct: ct);
```

### `WithTopology` hook

Every messaging provider exposes a strongly-typed `WithTopology` builder hook on its
`{Provider}RigBuilder`. The base `MessagingRigBuilder<TSelf>` deliberately does **not**
declare a generic `WithTopology` — each provider's surface is legitimately different and
the type system carries that constraint.

| Provider | Hook returns | Top-level concepts |
|----------|-------------|--------------------|
| Azure Service Bus | `IServiceBusTopologyBuilder` | `Topic` · `Subscription` (with `RequiresSession`, `MaxDeliveryCount`, SQL rule filter) · `Queue` · `WithDeadLetter` |
| Apache Kafka | `IKafkaTopologyBuilder` | `Topic` (with `WithPartitions`, `WithReplicationFactor`, `WithConfig`) |
| RabbitMQ | `IRabbitMqTopologyBuilder` | `Exchange(name, ExchangeType)` · `Queue` (with `WithMessageTtl`, `WithMaxLength`, `WithMaxPriority`, `WithDeadLetterExchange`, `WithQuorum`) · `Binding` |
| Amazon SQS | `ISqsTopologyBuilder` | `Queue` (with `WithFifo`, `WithVisibilityTimeout`, `WithDeadLetter`, `WithMessageRetentionPeriod`) |
| NATS JetStream | `INatsTopologyBuilder` | `Stream` (with `WithSubjects`, `WithMaxMessages`, `WithRetentionPolicy`) · `Consumer` |

```csharp
services.AddRigTUnit(rig =>
    rig.UseServiceBus(source, sb =>
        sb.WithTopology(t =>
            t.Topic("orders")
             .Subscription("orders", "shipping", s => s
                 .WithRequiresSession()
                 .WithMaxDeliveryCount(5)
                 .WithDeadLetter("orders-dlq")))));
```

`ApplyAsync` is invoked automatically by the rig at fixture init and is **idempotent**:
create-or-update on every provider, so tests can run in any order without pre-provisioning
infrastructure and re-runs of the same declaration never throw.

### Administration helpers (provision without the rig)

When you don't own the rig but still need to provision broker topology — typical for
parameterised integration tests, custom fixtures, or migration verification — the
provider-specific admin helper is callable directly:

```csharp
// Service Bus — declarative topic + session subscription, no JSON seed.
var admin  = new ServiceBusAdministrationClient(connectionString);
var helper = new ServiceBusAdministrationHelper(admin);
var subName = $"sess-{Guid.NewGuid():N}";
await helper.CreateSubscriptionIfNotExistsAsync(
    "orders", subName, requiresSession: true, ct);
// ... use the subscription ...
await admin.DeleteSubscriptionAsync("orders", subName, ct);
```

Each provider exposes the equivalent surface: Kafka via `Confluent.Kafka.AdminClient`
(wrapped in `KafkaListener.EnsureTopicExistsAsync`), RabbitMQ via `IChannel` declarations
in `RabbitMqListener.StartAsync`, NATS JetStream via `INatsJSContext` operations in
`NatsTopologyBuilder.ApplyAsync`, SQS via `IAmazonSQS` in `SqsTopologyBuilder.ApplyAsync`.
Each helper:

- Returns gracefully when the resource already exists (create-or-update semantics).
- Throws specifically when the broker rejects the operation for a real reason (e.g.
  NATS `JSStreamNameExistErr` / 10058 → fall back to update; any other 400 → propagate).
- Does **not** create test-state assumptions — every helper is callable from any test
  framework, not just TUnit.

### Session-aware listeners

For providers with native session/group semantics, ship a dedicated listener that
populates `CapturedMessage.SessionKey` from the broker's per-message ordering field:

| Provider | Listener | Populates `SessionKey` from |
|----------|----------|----------------------------|
| Azure Service Bus | `ServiceBusSessionListener` | `ServiceBusReceivedMessage.SessionId` |
| Apache Kafka | `KafkaListener` (with `Assign(int partition)`) | `Message.Key` (per partition) |
| Amazon SQS FIFO | `SqsListener` | `MessageGroupId` attribute |
| NATS JetStream | `NatsJetStreamListener` | `x-session-key` header |
| RabbitMQ | `RabbitMqListener` (binding-aware) | `x-partition-key` header |

```csharp
await using var listener = new ServiceBusSessionListener(client, "orders", "shipping");
await listener.StartAsync(ct);
// ... messages flow ...
OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey!, m => /* sequence */);
```

`ServiceBusSessionListener` also surfaces broker errors (lost session locks, auth
failures) via `ObservedErrors` / `LastError` so they're assertable instead of silently
discarded. `RabbitMqListener.Errors` carries decode failures (autoAck path).

See the per-provider docs in [`docs/providers/`](docs/providers/) for full option
references and the [ordering-assertions capability matrix](docs/ordering-assertions.md)
for which provider supports which `OrderingAssert` mode.

---

## Isolation

Every fixture automatically derives an `IsolationKey` from TUnit's execution context:

```csharp
// Two fixtures running in parallel get distinct keys
var a = IsolationKey.FromExecutionContext();  // e.g. "OrderTests.CreateOrder-abc12"
var b = IsolationKey.FromExecutionContext();  // e.g. "OrderTests.ReadOrder-def34"
```

Each provider uses the isolation key to namespace its resources — table names, topic names,
bucket prefixes, cache key prefixes — so 50 parallel tests never touch each other's data.

You can also pin a key explicitly for shared-fixture scenarios:

```csharp
var key = IsolationKey.FromName("shared-read-model");
```

---

## Provider catalogue

| Package | NuGet ID | Fixtures | Assertions |
|---------|----------|----------|------------|
| Core | `Rig.TUnit.Core` | `RigFixtureBase` | — |
| PostgreSQL | `Rig.TUnit.Databases.Sql.Postgresql` | `PostgresFixture` | `DatabaseAssert` |
| SQL Server | `Rig.TUnit.Databases.Sql.SqlServer` | `SqlServerFixture` | `DatabaseAssert` |
| MySQL | `Rig.TUnit.Databases.Sql.MySql` | `MySqlFixture` | `DatabaseAssert` |
| Oracle | `Rig.TUnit.Databases.Sql.Oracle` | `OracleFixture` | `DatabaseAssert` |
| SQLite | `Rig.TUnit.Databases.Sql.Sqlite` | `SqliteFixture` | `DatabaseAssert` |
| Redis (KV) | `Rig.TUnit.Databases.NoSql.Redis` | `RedisKvFixture` | `KeyScanHelper` |
| MongoDB | `Rig.TUnit.Databases.NoSql.Mongo` | `MongoFixture` | `JsonDocumentAssert` |
| Cosmos DB | `Rig.TUnit.Databases.NoSql.Cosmos` | `CosmosFixture` | `ChangeFeedCapture` |
| Cassandra | `Rig.TUnit.Databases.NoSql.Cassandra` | `CassandraFixture` | — |
| DynamoDB | `Rig.TUnit.Databases.NoSql.Dynamo` | `DynamoFixture` | — |
| Elasticsearch | `Rig.TUnit.Databases.NoSql.ElasticSearch` | `ElasticSearchFixture` | — |
| KurrentDB | `Rig.TUnit.Databases.NoSql.KurrentDb` | `KurrentDbFixture` | — |
| Azure Service Bus | `Rig.TUnit.Messaging.ServiceBus` | `ServiceBusFixture` | `MessageAssert`, `OrderingAssert` |
| Apache Kafka | `Rig.TUnit.Messaging.Kafka` | `KafkaFixture` | `MessageAssert` |
| RabbitMQ | `Rig.TUnit.Messaging.RabbitMq` | `RabbitMqFixture` | `MessageAssert` |
| NATS | `Rig.TUnit.Messaging.Nats` | `NatsFixture` · `NatsJetStreamFixture` | `MessageAssert` |
| Amazon SQS | `Rig.TUnit.Messaging.Sqs` | `SqsFixture` | `MessageAssert` |
| Redis Cache | `Rig.TUnit.Caching.Redis` | `RedisCacheFixture` | — |
| Memory Cache | `Rig.TUnit.Caching.Memory` | `MemoryCacheFixture` | `CacheAssert` |
| FusionCache | `Rig.TUnit.Caching.Fusion` | `FusionCacheFixture` | `StampedeTester` |
| HybridCache | `Rig.TUnit.Caching.Hybrid` | — | — |
| Azure Blob | `Rig.TUnit.Storage.AzureBlob` | `AzureBlobFixture` | `BlobAssert` |
| MinIO | `Rig.TUnit.Storage.MinIO` | `MinIOFixture` | `BlobAssert` |
| Amazon S3 | `Rig.TUnit.Storage.S3` | `S3Fixture` | `BlobAssert` |
| File System | `Rig.TUnit.Storage.FileSystem` | — | — |
| gRPC | `Rig.TUnit.Grpc` | `GrpcClientHelper<TClient>` | — |
| HTTP | `Rig.TUnit.Http` | `HttpMockVerifier` | `CapturedRequest` |
| Health Checks | `Rig.TUnit.HealthChecks` | — | `HealthAssert` |
| Resilience | `Rig.TUnit.Resilience` | — | `BulkheadAssert` |
| MediatR | `Rig.TUnit.Mediator` | — | — |
| Event Sourcing | `Rig.TUnit.Microservices.EventSourcing` | — | `AggregateAssert` |
| Outbox | `Rig.TUnit.Microservices.Outbox` | — | `OutboxEntryAssertion` |
| Saga | `Rig.TUnit.Microservices.Saga` | `SagaHarness` | `SagaAssert` |
| Seq | `Rig.TUnit.Observability.Seq` | `SeqFixture` | `SeqAssert` |
| App Insights | `Rig.TUnit.Observability.AppInsights` | — | `AppInsightsDependencyAssertion` |
| JWT | `Rig.TUnit.Security.Jwt` | `JwtFixture` | — |
| Auth Policies | `Rig.TUnit.Security.Policies` | `PolicyFixture` | `PolicyAssert` |
| WebAPI | `Rig.TUnit.WebAPI` | `WebApiFactory<TProgram>` | — |

---

## Running tests

> **TUnit uses [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)**
> (MTP). Unlike xUnit/NUnit, `--filter "Category!=Integration"` and
> `dotnet run` do **not** work. Tests must be run project-by-project with
> `dotnet test`, and MTP-specific flags are passed after `--`.

Run a single unit-test project:

```bash
dotnet test tests/Rig.TUnit.Core.Tests.Unit -c Release
```

Run all unit tests (bash — Linux/macOS):

```bash
find tests -name "*.csproj" \
  | grep -Ev "Integration|Benchmarks|Contract" \
  | xargs -I{} dotnet test {} -c Release
```

Run all unit tests (PowerShell — Windows):

```powershell
Get-ChildItem tests -Recurse -Filter "*.csproj" |
  Where-Object { $_.Name -notmatch "Integration|Benchmarks|Contract" } |
  ForEach-Object { dotnet test $_.FullName -c Release }
```

Run integration tests (requires Docker):

```bash
dotnet test tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration -c Release
```

Filter by test name (TUnit MTP syntax — pass flags after `--`):

```bash
dotnet test tests/Rig.TUnit.Core.Tests.Unit -- --filter "IsolationKey"
```

### Development setup

After cloning, install the commit-message hook:

```bash
git config core.hooksPath .githooks
```

---

## Benchmarks

Allocation and throughput benchmarks live in `tests/Rig.TUnit.Benchmarks/`.
They use [BenchmarkDotNet](https://benchmarkdotnet.org/) with an in-process, single-iteration
(`Job.Dry`) configuration to keep CI fast.

Run benchmarks locally:

```bash
dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- \
  --filter "*" --exporters json --artifacts ./benchmark-results
```

Historical baselines are stored in `benchmarks/baseline-NNN.json`. CI compares each PR
run against the current baseline and blocks on regressions exceeding 20%.

---

## CI pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) enforces:

| Job | What it checks |
|-----|----------------|
| `build` | Solution builds on .NET 10 |
| `unit-tests` | All `*.Tests.Unit` projects pass |
| `integration-core` | Core, CI, Grpc, Http, WebAPI, Mediator and more pass against live containers |
| `benchmark-regression` | BDN smoke run; blocks on non-zero exit (no `|| true` guards) |
| `coverage-gate` | Every package ≥ 90% line coverage |
| `red-commit-verification` | Every `red(T###):` commit fails at its SHA |
| `commit-msg-lint` | All commits follow Conventional Commits |

---

## TDD discipline

Rig.TUnit is developed with a strict red-green cycle enforced by tooling:

1. **Write a failing test** — commit with `test(NNN): T<n> — RED`
2. **Make it pass** — commit with `green(T<n>): <description>`
3. **CI verifies the red commit fails** at its original SHA

The `.githooks/commit-msg` hook enforces valid prefixes on every commit:
`test:`, `feat:`, `refactor:`, `fix:`, `chore:`, `docs:`, `style:`, `perf:`,
`build:`, `ci:`, `revert:` — plus the TDD-specific `green(T###):` and `red(T###):` forms.

---

## Roadmap

Active feature work follows a Spec-Driven Development pipeline (`/dai.spec` → `/dai.plan` → `/dai.tasks` → `/dai.implement` → `/dai.verify` → `/dai.pr`). Planned features that haven't been picked up yet live as short briefs under [`planning/<slug>/README.md`](planning/).

The full roadmap (43 planned features grouped by family — cross-cutting, SQL, NoSQL, caching, storage, security, observability, microservices, HTTP/gRPC, health-checks/resilience/concurrency/WebAPI/CI) lives at **[docs/ROADMAP.md](docs/ROADMAP.md)**. Numbering and stage rules are formalised in [ADR-009](docs/adr/ADR-009-feature-numbering-after-007.md).

---

## Contributing

1. Fork the repository and create a feature branch: `feat/NNN-short-name`
2. Follow the TDD red-green cycle (see above)
3. Ensure unit tests pass (`dotnet test {YourProject} -c Release`) and coverage stays ≥ 90%
4. Open a pull request — CI will run the full suite automatically

For significant changes, open an issue first to discuss the approach. To pick up a planned feature, read its brief under [`planning/<slug>/README.md`](planning/), copy the **Build prompt** section into `/dai.spec`, and update [docs/ROADMAP.md](docs/ROADMAP.md) when the status flips.

---

## How testing works

Every public API in `Rig.TUnit.*` is covered by unit tests (≥ 90 % line / ≥ 85 % branch) and,
for each provider, by integration tests that run against a real containerised broker via
Testcontainers.

Provider completeness is enforced by the architecture-test suite. A file called
`.parity-coverage.txt` (in `tests/Rig.TUnit.Architecture.Tests/`) lists the messaging-provider
assembly names that have reached full parity with the Feature-007 shape:

```
Rig.TUnit.Messaging.ServiceBus
Rig.TUnit.Messaging.Kafka
Rig.TUnit.Messaging.Sqs
Rig.TUnit.Messaging.RabbitMq
Rig.TUnit.Messaging.Nats
```

Each time a provider phase ships, its assembly name is appended to that file and the
`ProviderCompletenessTests` suite immediately enforces three new invariants for it:

1. **`WithTopology`** — the provider's `{Provider}RigBuilder` exposes a strongly-typed
   `WithTopology(Action<I{Provider}TopologyBuilder>)` hook.
2. **`SendContext` overload** — at least one `{Provider}EventSender` exposes a `SendAsync`
   overload that accepts `SendContext` (carries `SessionKey`, `PartitionKey`, `DeduplicationKey`).
3. **Session listener** (session/partition-capable providers only) — the assembly declares a
   concrete `ListenerBase<T>` subtype that populates `CapturedMessage.SessionKey`.

The file is intentionally empty at Phase 0 exit; all three assertions pass vacuously until the
first provider phase appends its name. This keeps CI green during the phased rollout without
requiring every provider to ship simultaneously.

---

## Architecture

```mermaid
graph TD
    Core["Rig.TUnit.Core<br/>(IsolationKey, RigFixtureBase,<br/>Builder API)"]

    SQL["SQL Databases<br/>(SqlServer · MySql ·<br/>Postgresql · Oracle · Sqlite)"]
    NoSQL["NoSQL Databases<br/>(Redis · Mongo · Cosmos ·<br/>Cassandra · Dynamo · …)"]
    Msg["Messaging<br/>(ServiceBus · Kafka ·<br/>RabbitMq · Nats · Sqs)"]
    Cache["Caching<br/>(Redis · Memory ·<br/>Fusion · Hybrid)"]
    Storage["Storage<br/>(AzureBlob · MinIO ·<br/>S3 · FileSystem)"]
    Obs["Observability<br/>(Logging · AppInsights ·<br/>Metrics · Tracing · Seq)"]
    Sec["Security<br/>(Jwt · Policies ·<br/>OAuth · Mtls)"]
    Svc["Microservices<br/>(EventSourcing · Outbox ·<br/>Saga · Contracts · …)"]
    Infra["Infrastructure<br/>(Http · Grpc · HealthChecks ·<br/>Resilience · WebAPI · …)"]

    Core --> SQL
    Core --> NoSQL
    Core --> Msg
    Core --> Cache
    Core --> Storage
    Core --> Obs
    Core --> Sec
    Core --> Svc
    Core --> Infra
```

All provider packages take `Rig.TUnit.Core` as their only required dependency. Providers are
self-contained; consuming a SQL provider does not pull in messaging packages.

---

## License

MIT — see [LICENSE](LICENSE).
