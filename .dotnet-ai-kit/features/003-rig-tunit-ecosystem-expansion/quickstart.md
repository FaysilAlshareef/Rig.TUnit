# Quickstart: Rig.TUnit Ecosystem (post-003)

**Audience**: developers adopting Rig.TUnit 2.0.0+ after the ecosystem expansion ships.

---

## Install

### For a typical microservice test project

```xml
<PackageReference Include="Rig.TUnit.Microservices" />
```

This opinionated meta-package pulls: `Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq` (per C-002). ~150MB `datalust/seq` image + ~3-5s test startup overhead — be aware.

### For a smaller REST-API test project

```xml
<PackageReference Include="Rig.TUnit" />
<PackageReference Include="Rig.TUnit.Databases.Sql.SqlServer" />
<PackageReference Include="Rig.TUnit.Security.Jwt" />
```

### For the kitchen sink (discouraged)

```xml
<PackageReference Include="Rig.TUnit.All" />
```

---

## Fluent builder entry point

```csharp
services.AddRigTUnit(rig => rig
    .UseSqlServer(RigConnect.Auto(sqlFixture, config, "ConnectionStrings:OrderDb"), sql => sql
        .ReplaceDbContext<OrderDbContext>())
    .UseServiceBus(RigConnect.FromContainer(sbFixture), sb => sb
        .ReplaceClient())
    .UseRedisCache(RigConnect.FromContainer(redisFixture))      // Caching role (Rig.TUnit.Caching.Redis)
    // .UseRedisKv(RigConnect.FromContainer(redisFixture))      // Alternative: KV role (Rig.TUnit.Databases.NoSql.Redis)
    .UseSeq(RigConnect.FromContainer(seqFixture))
    .UseJwt(jwt => jwt.WithIssuer("https://issuer"))
    .ForceContainersInCi());
```

---

## Pick your SQL fast path (new in 003)

Three options, ordered by fidelity:

```csharp
// Fastest, lowest fidelity — EF InMemory (no SQL engine, LINQ-to-objects)
rig.UseInMemoryDb<OrderDbContext>();

// Fast + real SQL engine — SQLite :memory: (new in 003)
rig.UseSqlite(RigConnect.FromContainer(sqliteFixture), sql => sql
    .ReplaceDbContext<OrderDbContext>());

// Full fidelity — Testcontainers SqlServer
rig.UseSqlServer(RigConnect.FromContainer(sqlServerFixture), sql => sql
    .ReplaceDbContext<OrderDbContext>());
```

The same `DbContextHelper<OrderDbContext>` CRUD API works identically against all three.

---

## Your first test

First, define a `CompositeFixture` subclass composing the resources your tests need (the `RigFixtureBase` + `CompositeFixture` primitives live in `Rig.TUnit.Core`):

```csharp
public sealed class MyTestRig : CompositeFixture
{
    public SqlServerFixture SqlServer { get; } = new();
    public ServiceBusFixture ServiceBus { get; } = new();
    public SeqFixture Seq { get; } = new();
    public IMediator Mediator => /* composed during InitializeAsync */;
    public string TenantId { get; } = "tenant-" + Guid.NewGuid().ToString("N").Substring(0, 8);
}
```

Then consume it in test classes:

```csharp
public sealed class OrderHandlerTests
{
    private readonly MyTestRig _rig;

    public OrderHandlerTests(MyTestRig rig) => _rig = rig;

    [Test]
    public async Task CreateOrder_WithValidData_PublishesOrderCreated()
    {
        // Arrange
        var cmd = new CreateOrderCommand(CustomerId: Guid.NewGuid(), Total: 99.99m);

        // Act
        await _rig.Mediator.Send(cmd, default);

        // Assert — domain event published
        await MessageAssert
            .Published<OrderCreated>()
            .ExactlyOnce()
            .OnTopic("order-commands")
            .WithCorrelation(cmd.CorrelationId)
            .Within(TimeSpan.FromSeconds(5));

        // Assert — log entry captured (anti-pattern detector active)
        LogAssert
            .Logged(LogLevel.Information)
            .WithProperty("OrderId")
            .InScope("TenantId", _rig.TenantId)
            .Once();

        // Assert — Seq dashboard snapshot (artifact on CI)
        await SeqAssert
            .Query($"Level=@Information and CorrelationId='{cmd.CorrelationId}'")
            .Count(1)
            .Within(TimeSpan.FromSeconds(5));
    }
}
```

---

## JWT authenticated endpoint test

```csharp
[Test]
public async Task GetOrders_WithAdminToken_Returns200()
{
    // Arrange
    var token = JwtBuilder
        .Issuer("https://issuer")
        .Audience("api")
        .Claim("role", "admin")
        .Claim("sub", "user-1")
        .ExpiresIn(TimeSpan.FromMinutes(5))
        .SignedWithHs256(_rig.JwtKey)
        .Build();

    var client = _rig.HttpClient.WithBearerToken(token);

    // Act
    var response = await client.GetAsync<OrderListDto>("/api/orders");

    // Assert
    await Assert.That(response).IsNotNull();
}
```

No `TestAuthenticationHandler` bypass — this hits real `JwtBearerHandler` middleware (per C-001 / FR-093 / US6).

---

## Outbox pattern test (microservice)

```csharp
[Test]
public async Task CreateOrder_PersistsOutbox_RelaysExactlyOnce()
{
    // Arrange
    var cmd = new CreateOrderCommand(...);

    // Act
    await _rig.Mediator.Send(cmd, default);
    await _rig.OutboxRelay.DrainAsync(default);

    // Assert
    await OutboxAssert
        .Contains<OrderCreated>()
        .WithAggregateId(cmd.OrderId)
        .OnTopic("order-commands")
        .ExactlyOnce()
        .Relayed()
        .Within(TimeSpan.FromSeconds(5));
}
```

Same test passes over ServiceBus OR Kafka — switch providers in one line.

---

## Caching stampede test

```csharp
[Test]
public async Task GetCustomer_WhenColdCache_ProducerCalledOnce()
{
    await CacheAssert
        .Stampede(_rig.Cache, key: "customer:42")
        .ConcurrentMisses(100)
        .ProducerCalledOnce()
        .Within(TimeSpan.FromSeconds(2));
}
```

---

## Snapshot test (Verify-compatible, per C-003)

```csharp
[Test]
public async Task OrderSummary_ProjectionShape_MatchesSnapshot()
{
    var summary = await _rig.Queries.GetOrderSummaryAsync(orderId);
    await SnapshotAssert.Match(summary);
    // First run writes: OrderSummary_ProjectionShape_MatchesSnapshot.received.json
    // Rename to .verified.json after review; second run passes.
    // Scrubbers redact GUIDs / timestamps / correlation-IDs automatically.
}
```

---

## Concurrency test (cross-provider)

```csharp
[Test]
public async Task UpdateOrder_TwoWriters_OneWinsWithConcurrencyException()
{
    await ConcurrencyAssert
        .TwoWriters(_rig.Orders, orderId)
        .OneWinsWith<DbUpdateConcurrencyException>();
    // Passes against SqlServer, Postgres, Cosmos, Mongo — same test.
}
```

---

## Parallel safety (automatic)

Every fixture exposes an `IsolationKey` (per C-004):

```csharp
// Under 20 parallel tests — zero collisions guaranteed.
public sealed class OrderHandlerParallelTests : ParallelIsolationContract
{
    protected override ValueTask<IRigFixture> CreateFixtureAsync(CancellationToken ct)
        => new SqlServerRigBuilder().BuildAsync(ct);
}
```

---

## Observability anti-pattern detector (per C-005)

If production code accidentally logs a property named `Password`, `Token`, `ConnectionString`, `Ssn`, etc. — **your tests will fail**. The detector is ADDITIVE-ONLY; you cannot disable the canonical PII list. Remedy false positives by renaming the property.

To **strengthen** (never weaken) detection:

```json
// appsettings.Test.json
{
  "RigTUnit:Observability:Logging:Detector": {
    "AdditionalPiiPatterns": [
      "^x-auth-.*$",
      "^internal-user-id-.*$"
    ]
  }
}
```

---

## Migrating from Rig.TUnit 1.x (Phase 002)

Breaking changes — there are no backwards-compatibility shims (hard cutover per FR-001..004):

| 1.x namespace | 2.0 namespace |
|---|---|
| `Rig.TUnit.SqlServer.Fixtures.SqlServerFixture` | `Rig.TUnit.Databases.Sql.SqlServer.Fixtures.SqlServerFixture` |
| `Rig.TUnit.SqlServer.Helpers.DbContextHelper` | `Rig.TUnit.Databases.Sql.Helpers.DbContextHelper` |
| `Rig.TUnit.SqlServer.Extensions.InMemoryDbExtensions` | `Rig.TUnit.Databases.Sql.Extensions.InMemoryDbExtensions` |
| `Rig.TUnit.Redis.Fixtures.RedisFixture` | `Rig.TUnit.Caching.Redis.Fixtures.RedisFixture` |
| `Rig.TUnit.ServiceBus.Fixtures.ServiceBusFixture` | `Rig.TUnit.Messaging.ServiceBus.Fixtures.ServiceBusFixture` |
| `Rig.TUnit.ServiceBus.Helpers.ListenerHelper` | `Rig.TUnit.Messaging.ServiceBus.Helpers.ServiceBusListener` |
| `Rig.TUnit.SqlServer.Extensions.UseSqlServerContainerIsolated` | REMOVED — use fluent `rig.UseSqlServer(source, sql => ...)` |
| `Rig.TUnit.Redis.Extensions.UseRedisContainer` | REMOVED — use fluent `rig.UseRedisCache(source)` (cache role) or `rig.UseRedisKv(source)` (KV role). No bare `UseRedis`. |
| `Rig.TUnit.ServiceBus.Extensions.UseServiceBusContainer` | REMOVED — use fluent `rig.UseServiceBus(source, sb => ...)` |
| `Rig.TUnit.Grpc.Extensions.ReplaceGrpcClient` | REMOVED — use fluent `rig.UseGrpc<TProgram>(factory, grpc => grpc.ReplaceClient<T>())` |

Migration steps:
1. Update all `using Rig.TUnit.SqlServer...;` to `using Rig.TUnit.Databases.Sql.SqlServer...;` (and similar).
2. Replace extension-method calls with fluent-builder calls on `services.AddRigTUnit(rig => ...)`.
3. Delete direct references to old packages in `.csproj`; replace with new packages.
4. Run `dotnet test` — anti-pattern detector may flag existing code (intended).

---

## CI setup (GitHub Actions example)

```yaml
jobs:
  test:
    strategy:
      matrix:
        postgres: ['14', '15', '16']
        sqlserver: ['2019-latest', '2022-latest']
        mongo: ['6', '7']
        kafka: ['3.6', '3.7']
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --collect:"XPlat Code Coverage"
        env:
          ACCEPT_EULA: Y                          # Required for Microsoft ServiceBus emulator (C-001)
          POSTGRES_IMAGE: postgres:${{ matrix.postgres }}
          SQLSERVER_IMAGE: mcr.microsoft.com/mssql/server:${{ matrix.sqlserver }}
          MONGO_IMAGE: mongo:${{ matrix.mongo }}
          KAFKA_IMAGE: confluentinc/cp-kafka:${{ matrix.kafka }}
  test-no-docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet test --filter "Category!=RequiresDocker"
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `dotnet build` errors with "CS1591 missing XML doc" | New public type missing `///` docs | Add XML docs; `TreatWarningsAsErrors=true` |
| Contract test compilation fails in provider | Abstract method not implemented | Implement `CreateRigAsync` + quirk tests |
| `ParallelIsolationContract` test intermittently fails | Port / schema collision in fixture | Report as P0 — `IsolationKey` derivation bug |
| ServiceBus emulator fails to start | `ACCEPT_EULA=Y` not set in CI env | Set env var; re-run |
| Cosmos emulator fails on ARM | Linux variant is AMD64-only | Skip Cosmos on ARM runners; use SqlServer path |
| Seq container takes > 30s in CI | Image pull | Pre-pull image in CI init step |
| `LogAssert` test fails with "PII-shaped property" | Production code logs a detector-matched name | Rename property; do NOT weaken detector (per C-005) |
| Snapshot test fails with `.received.*` committed | PR contains unreviewed snapshot | Reviewer approves by renaming `.received.` → `.verified.` |

---

## Getting help

- Docs: `README.md` per package.
- Spec: `.dotnet-ai-kit/features/003-rig-tunit-ecosystem-expansion/spec.md`
- Plan: `.dotnet-ai-kit/features/003-rig-tunit-ecosystem-expansion/plan.md`
- Rules: `.claude/rules/*.md` (library obeys these; your consuming project should too).
