# Data Model: Rig.TUnit Ecosystem Expansion

**Feature**: 003-rig-tunit-ecosystem-expansion
**Scope**: class / type design only (no persistent data model — this is a test library).

---

## Type Hierarchy Overview

```
RigFixtureBase                          [existing, unchanged]
 ├── DbFixtureBase                      [NEW — Rig.TUnit.Databases]
 │    ├── SqlFixtureBase                [NEW — Rig.TUnit.Databases.Sql]
 │    │    ├── SqlServerFixture         [RELOCATED — .Sql.SqlServer]
 │    │    ├── SqliteFixture            [NEW — .Sql.Sqlite]
 │    │    ├── PostgresFixture          [Phase D — .Sql.Postgresql]
 │    │    ├── MySqlFixture             [Phase D — .Sql.MySql]
 │    │    └── OracleFixture            [Phase E — .Sql.Oracle]
 │    └── DocumentFixtureBase           [NEW — Rig.TUnit.Databases.NoSql]
 │         ├── CosmosFixture            [Phase D — .NoSql.Cosmos]
 │         ├── MongoFixture             [Phase D — .NoSql.Mongo]
 │         ├── DynamoFixture            [Phase E — .NoSql.Dynamo]
 │         ├── CassandraFixture         [Phase E — .NoSql.Cassandra]
 │         ├── EventStoreFixture        [Phase E — .NoSql.EventStore]
 │         ├── ElasticSearchFixture     [Phase E — .NoSql.ElasticSearch]
 │         └── (Redis — via Caching.Redis)
 ├── MessagingFixtureBase               [NEW — Rig.TUnit.Messaging]
 │    ├── ServiceBusFixture             [RELOCATED — .Messaging.ServiceBus]
 │    ├── KafkaFixture                  [Phase D]
 │    ├── RabbitMqFixture               [Phase D]
 │    ├── SqsFixture                    [Phase E — LocalStack]
 │    └── NatsFixture                   [Phase E]
 ├── CacheFixtureBase                   [NEW — Rig.TUnit.Caching]
 │    ├── MemoryCacheFixture            [Phase C]
 │    ├── RedisFixture                  [RELOCATED — .Caching.Redis]
 │    ├── HybridCacheFixture            [Phase D]
 │    └── FusionCacheFixture            [Phase D]
 ├── StorageFixtureBase                 [Phase D]
 │    ├── AzureBlobFixture              [Phase D — Azurite]
 │    ├── S3Fixture                     [Phase D — LocalStack]
 │    ├── MinIOFixture                  [Phase E]
 │    └── FileSystemFixture             [Phase E — System.IO.Abstractions]
 ├── TelemetryFixtureBase               [Phase B]
 │    ├── TracingFixture                [Phase B]
 │    ├── LoggingFixture                [Phase B]
 │    ├── SeqFixture                    [Phase B]
 │    ├── MetricsFixture                [Phase E]
 │    └── AppInsightsFixture            [Phase E]
 └── SecurityFixtureBase                [Phase B]
      ├── JwtFixture                    [Phase B]
      ├── OAuthFixture                  [Phase B]
      ├── MtlsFixture                   [Phase E]
      └── PoliciesFixture               [Phase E]
```

---

## Core Contracts

### `IRigConnectionSource` (existing — unchanged)

```csharp
public interface IRigConnectionSource
{
    string ConnectionString { get; }
}
```

### Area marker contracts (new)

```csharp
public interface IDbRig         { IsolationKey Key { get; } }
public interface ISqlRig        : IDbRig { IDbConnection Connection { get; } }
public interface INoSqlRig      : IDbRig { Uri Endpoint { get; } }
public interface IMessagingRig  { IsolationKey Key { get; } Uri BrokerUri { get; } }
public interface ICacheRig      { IsolationKey Key { get; } Uri Endpoint { get; } }
public interface IStorageRig    { IsolationKey Key { get; } Uri Endpoint { get; } }
public interface ITelemetryRig  { IsolationKey Key { get; } }
public interface ISecurityRig   { IsolationKey Key { get; } }
```

### `IsolationKey` record (new — lives in `Rig.TUnit.Core`)

**Location**: `src/Rig.TUnit.Core/IsolationKey.cs`.

**Rationale**: `IsolationKey` is consumed by every area (Databases, Messaging, Caching, Storage, Observability, Security). Placing it in `Rig.TUnit.Core` means every area base transitively references Core only, preserving the "base NEVER references sibling base" dependency rule enforced by `Rig.TUnit.Architecture.Tests`.

```csharp
public sealed record IsolationKey
{
    public string Value { get; init; }
    public string ShortName { get; init; }
    public string HashSuffix { get; init; }

    public static IsolationKey FromExecutionContext(string fullMethodName)
    {
        var shortName = Truncate(ExtractMethodName(fullMethodName), 20);
        var hash = Sha256(fullMethodName).Substring(0, 8);
        return new IsolationKey
        {
            ShortName = shortName,
            HashSuffix = hash,
            Value = $"{shortName}_{hash}"
        };
    }

    // Per-platform truncation helpers
    public string ForDockerContainer() => Truncate(Value, 63);
    public string ForPostgresDatabase() => Truncate(Value, 63);
    public string ForSqlServerDatabase() => Truncate(Value, 128);
    public string ForRedisKeyPrefix() => Value;
}
```

Per C-004.

---

## Fixture Base Hierarchy (contracts)

### `RigFixtureBase` (existing — unchanged)

```csharp
public abstract class RigFixtureBase : IAsyncInitializer, IAsyncDisposable
{
    public abstract ValueTask InitializeAsync();
    public abstract ValueTask DisposeAsync();
}
```

### `DbFixtureBase` (new)

```csharp
public abstract class DbFixtureBase : RigFixtureBase, IDbRig
{
    public IsolationKey Key { get; protected set; } = default!;
    public abstract string ConnectionString { get; }
}
```

### `SqlFixtureBase` (new)

```csharp
public abstract class SqlFixtureBase : DbFixtureBase, ISqlRig
{
    public abstract IDbConnection Connection { get; }
    public abstract ValueTask<TContext> CreateDbContextAsync<TContext>(CancellationToken ct)
        where TContext : DbContext;
}
```

### `DocumentFixtureBase` (new)

```csharp
public abstract class DocumentFixtureBase : DbFixtureBase, INoSqlRig
{
    public abstract Uri Endpoint { get; }
    public abstract ValueTask<TClient> CreateClientAsync<TClient>(CancellationToken ct);
}
```

### `MessagingFixtureBase`, `CacheFixtureBase`, `StorageFixtureBase`, `TelemetryFixtureBase`, `SecurityFixtureBase`

Same pattern — each exposes its area-specific primary primitive (broker URI / endpoint / exporter handle).

---

## Builder Base Hierarchy

### `RigBuilder` (existing — unchanged as root)

### `{Area}RigBuilder<TSelf>` pattern (new)

```csharp
public abstract class DatabaseRigBuilder<TSelf> : RigBuilder where TSelf : DatabaseRigBuilder<TSelf>
{
    protected IRigConnectionSource? _source;

    public TSelf UseContainer(IRigConnectionSource source) { _source = source; return (TSelf)this; }
    public TSelf UseConfig(IConfiguration config, string key) { _source = new ConfigConnectionSource(config, key); return (TSelf)this; }
    public TSelf UseOptions<TOptions>(IOptions<TOptions> options, Func<TOptions, string> selector) where TOptions : class { ... }
    public TSelf UseValue(string connectionString) { _source = new ValueConnectionSource(connectionString); return (TSelf)this; }
    public TSelf UseAuto(IRigConnectionSource fallback, IConfiguration config, string key) { ... }
    public TSelf ForceContainersInCi() { ... }
}

public abstract class SqlRigBuilder<TSelf> : DatabaseRigBuilder<TSelf> where TSelf : SqlRigBuilder<TSelf>
{
    // PROMOTED from old SqlServerRigBuilder (feature 002) — now available to every SQL provider
    // (SqlServer, Sqlite, Postgres, MySql, Oracle) without reimplementation.
    public TSelf ReplaceDbContext<TContext>() where TContext : DbContext { ... }
    public TSelf ReplaceDbContext<TContext>(Action<DbContextOptionsBuilder> configure) where TContext : DbContext { ... }
}

public abstract class NoSqlRigBuilder<TSelf> : DatabaseRigBuilder<TSelf> where TSelf : NoSqlRigBuilder<TSelf>
{
    public TSelf UseContainerEndpoint(string path) { ... }
}

public abstract class MessagingRigBuilder<TSelf> : RigBuilder where TSelf : MessagingRigBuilder<TSelf>
{
    public TSelf ReplaceClient() { ... }
    public TSelf ReplaceClient<TWrapper>(Func<…> factory) { ... }
}

public abstract class CacheRigBuilder<TSelf> : RigBuilder where TSelf : CacheRigBuilder<TSelf>
{
    public TSelf ReplaceConnectionMultiplexer() { ... }
}

public abstract class StorageRigBuilder<TSelf> : RigBuilder ... { }
public abstract class TelemetryRigBuilder<TSelf> : RigBuilder ... { }
public abstract class SecurityRigBuilder<TSelf> : RigBuilder ... { }
```

### Concrete provider builders (sealed; ≤ ~200 LOC each)

```csharp
public sealed class SqlServerRigBuilder  : SqlRigBuilder<SqlServerRigBuilder> { ... }
public sealed class SqliteRigBuilder     : SqlRigBuilder<SqliteRigBuilder>    { ... }
public sealed class RedisCacheRigBuilder : CacheRigBuilder<RedisCacheRigBuilder> { ... }
public sealed class RedisKvRigBuilder    : NoSqlRigBuilder<RedisKvRigBuilder>   { ... }
public sealed class ServiceBusRigBuilder : MessagingRigBuilder<ServiceBusRigBuilder> { ... }
// ...one per provider
```

### Fluent entry-point naming (Redis dual role)

Because Redis fills BOTH cache and NoSQL/KV roles, the fluent extension methods are explicitly disambiguated:

```csharp
rig.UseRedisCache(source, cache => cache.ReplaceConnectionMultiplexer());   // Rig.TUnit.Caching.Redis
rig.UseRedisKv(source, kv => kv.WithKeyPrefix("test:"));                     // Rig.TUnit.Databases.NoSql.Redis
```

A bare `UseRedis(...)` method MUST NOT exist on `RigBuilder` — consumers MUST pick a role explicitly. `Rig.TUnit.Architecture.Tests` verifies the absence.

### `RigBuilder` fluent-chain entry-points (one extension file per area)

Each area owns its own extensions file that decorates `RigBuilder`. Top-level `RigBuilderExtensions` in `Rig.TUnit.Core` is NOT modified per-area; instead each area ships its own extension class in its own assembly:

```
src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilderExtensions.cs  → UseSqlServer(...)
src/Rig.TUnit.Databases.Sql.Sqlite/Builder/SqliteRigBuilderExtensions.cs        → UseSqlite(...)
src/Rig.TUnit.Databases.NoSql.Redis/Builder/RedisKvRigBuilderExtensions.cs      → UseRedisKv(...)
src/Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilderExtensions.cs           → UseRedisCache(...)
src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs    → UseServiceBus(...)
src/Rig.TUnit.Observability.Seq/Builder/SeqRigBuilderExtensions.cs              → UseSeq(...)
src/Rig.TUnit.Observability.Tracing/Builder/TracingRigBuilderExtensions.cs      → UseTracing(...)
src/Rig.TUnit.Observability.Logging/Builder/LoggingRigBuilderExtensions.cs      → UseLogging(...)
src/Rig.TUnit.Security.Jwt/Builder/JwtRigBuilderExtensions.cs                   → UseJwt(...)
src/Rig.TUnit.Security.OAuth/Builder/OAuthRigBuilderExtensions.cs               → UseOAuth(...)
src/Rig.TUnit.Http/Builder/HttpMockBuilderExtensions.cs                         → UseHttpMock(...)
src/Rig.TUnit.Resilience/Builder/ResilienceBuilderExtensions.cs                 → UseResilience(...)
// ... per package
```

Pattern: consumers bring in the package they need; extension methods light up via `using` imports. Core stays minimal.

---

## Assertion DSL Entries

Each area exposes a `static class` entry point:

```csharp
public static class DatabaseAssert { ... }        // Rig.TUnit.Databases
public static class MigrationAssert { ... }       // Rig.TUnit.Databases
public static class JsonDocumentAssert { ... }    // Rig.TUnit.Databases.NoSql
public static class RawSqlAssert { ... }          // Rig.TUnit.Databases.Sql
public static class MessageAssert { ... }         // Rig.TUnit.Messaging
public static class DeadLetterAssert { ... }      // Rig.TUnit.Messaging
public static class OrderingAssert { ... }        // Rig.TUnit.Messaging
public static class CacheAssert { ... }           // Rig.TUnit.Caching
public static class BlobAssert { ... }            // Rig.TUnit.Storage
public static class TraceAssert { ... }           // Rig.TUnit.Observability.Tracing
public static class MetricAssert { ... }          // Rig.TUnit.Observability.Metrics
public static class LogAssert { ... }             // Rig.TUnit.Observability.Logging
public static class SeqAssert { ... }             // Rig.TUnit.Observability.Seq
public static class HealthAssert { ... }          // Rig.TUnit.HealthChecks
public static class ConcurrencyAssert { ... }     // Rig.TUnit.Concurrency
public static class OutboxAssert { ... }          // Rig.TUnit.Microservices.Outbox
public static class InboxAssert { ... }           // Rig.TUnit.Microservices.Inbox
public static class AggregateAssert { ... }       // Rig.TUnit.Microservices.EventSourcing
public static class SnapshotAssert { ... }        // Rig.TUnit.Microservices.Snapshots
public static class SagaAssert { ... }            // Rig.TUnit.Microservices.Saga
public static class SecurityAssert { ... }        // Rig.TUnit.Security
public static class PolicyAssert { ... }          // Rig.TUnit.Security.Policies
```

Each `XxxAssert` static class exposes fluent entry methods that return a private builder type so chains compose:

```csharp
// CacheAssert usage
await CacheAssert
    .Stampede(cache, key: "order:42")
    .ConcurrentMisses(100)
    .ProducerCalledOnce()
    .Within(TimeSpan.FromSeconds(2));
```

Every assertion method has 5 mandatory tests (positive / negative / boundary / async-timeout / cancellation) per FR-040..043.

---

## Shared Helper Types

### `WaitHelper` (existing — unchanged)

### `ListenerBase<T>` (new — `Rig.TUnit.Messaging`)

```csharp
public abstract class ListenerBase<T> : IAsyncDisposable
{
    protected readonly List<ReceivedMessage<T>> _received = [];
    public IReadOnlyList<ReceivedMessage<T>> Received => _received;

    public async ValueTask<IReadOnlyList<ReceivedMessage<T>>> WaitForMessagesAsync(
        int expected,
        TimeSpan timeout,
        CancellationToken ct) => await WaitHelper.WaitForResultAsync(...);

    public abstract ValueTask StartAsync(CancellationToken ct);
    public abstract ValueTask DisposeAsync();
}

public sealed record ReceivedMessage<T>(
    T Body,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Headers,
    string? CorrelationId,
    string? CausationId,
    string? TraceParent);
```

### `EventSenderBase` (new — `Rig.TUnit.Messaging`)

```csharp
public abstract class EventSenderBase
{
    protected readonly TimeProvider _time;
    protected EventSenderBase(TimeProvider time) { _time = time; }

    protected EventEnvelope BuildEnvelope<T>(
        T body,
        string? correlationId = null,
        string? causationId = null,
        string? traceParent = null) { ... }

    public abstract ValueTask SendAsync<T>(T body, CancellationToken ct);
}

public sealed record EventEnvelope(
    string Id,
    string Type,
    int Version,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? CausationId,
    string? TraceParent,
    byte[] Body);
```

### `BackplaneCapture` (new — `Rig.TUnit.Caching`)

```csharp
public abstract class BackplaneCapture : IAsyncDisposable
{
    protected readonly List<BackplaneMessage> _messages = [];
    public IReadOnlyList<BackplaneMessage> Messages => _messages;
    public abstract ValueTask StartCaptureAsync(CancellationToken ct);
    public abstract ValueTask DisposeAsync();
}

public sealed record BackplaneMessage(string Channel, string Payload, DateTimeOffset ReceivedAt);
```

### `StampedeTester` (new — `Rig.TUnit.Caching`)

```csharp
public sealed class StampedeTester
{
    public async ValueTask<StampedeResult> RunAsync(
        Func<CancellationToken, ValueTask> producer,
        int concurrentMisses,
        CancellationToken ct) { ... }
}

public sealed record StampedeResult(int ProducerCallCount, int CacheHits, TimeSpan Duration);
```

### `ClockControl` (new — `Rig.TUnit.Caching`)

```csharp
public sealed class ClockControl
{
    private readonly FakeTimeProvider _fake;
    public TimeProvider TimeProvider => _fake;
    public ClockControl(DateTimeOffset start) { _fake = new FakeTimeProvider(start); }
    public void Advance(TimeSpan by) => _fake.Advance(by);
}
```

### `SeedBuilder<T>` (new — `Rig.TUnit.Databases`)

```csharp
public sealed class SeedBuilder<T> where T : class
{
    public SeedBuilder<T> WithDependencies(Func<T, IEnumerable<object>> deps);
    public SeedBuilder<T> WithFaker(Faker<T> faker);
    public async ValueTask<IReadOnlyList<T>> BuildAsync(int count, CancellationToken ct);
}
```

Dependency-ordered, Bogus-integrated.

### `DbContextHelper<TContext>` (existing — PROMOTED + EF-provider-agnostic)

```csharp
public sealed class DbContextHelper<TContext> where TContext : DbContext
{
    public ValueTask<IReadOnlyList<T>> QueryAsync<T>(...);
    public ValueTask InsertAsync<T>(...);
    public ValueTask UpdateAsync<T>(...);
    public ValueTask DeleteAsync<T>(...);
    public ValueTask SeedAsync(Func<TContext, ValueTask> seed);
    public ValueTask SeedAsync(Action<TContext> seed);  // existing overload
    public ValueTask WithTransactionAsync(Func<TContext, ValueTask> work); // auto-rollback
}
```

---

## Options Classes (configuration pattern)

Every fixture MUST expose a paired `Options` class (per `.claude/rules/configuration.md`):

```csharp
public sealed class SqlServerFixtureOptions
{
    public const string SectionName = "RigTUnit:SqlServer";
    [Required] public required string ImageTag { get; init; } = "2022-latest";
    [Range(1, 600)] public int StartupTimeoutSeconds { get; init; } = 120;
}
```

Applies to every fixture: `SqlServerFixtureOptions`, `SqliteFixtureOptions`, `ServiceBusFixtureOptions`, `RedisFixtureOptions`, `SeqFixtureOptions`, `JwtFixtureOptions`, `OAuthFixtureOptions`, etc.

Special: `LoggingDetectorOptions` (per C-005 + C-006):

```csharp
public sealed class LoggingDetectorOptions
{
    public const string SectionName = "RigTUnit:Observability:Logging:Detector";
    public bool DetectInterpolatedTemplates { get; init; } = true;
    public bool DetectConsoleWrite { get; init; } = true;
    public bool DetectPii { get; init; } = true;

    /// <summary>
    /// Additive ECMAScript regex patterns (case-insensitive, compiled once at detector startup).
    /// Strengthen — never weaken — PII detection. Built-in canonical PII list cannot be overridden.
    /// </summary>
    public IReadOnlyList<string> AdditionalPiiPatterns { get; init; } = [];
}
```

### Anti-pattern detector mechanism (C-006)

Two complementary implementations ship:

**(A) Runtime detector** — `Rig.TUnit.Observability.Logging`. An `ILoggerProvider` that wraps captured `LogMessage` entries and inspects `OriginalFormat` + structured-property dictionary. Catches:
- `$"..."` literals passed to `ILogger.Log*` (test-time failure).
- Property names matching the canonical PII list OR user regex patterns (test-time failure).
Limitation: cannot observe `Console.Write` because the process's `Console.Out` isn't routed through the logger.

**(B) Roslyn analyzer** — `Rig.TUnit.Observability.Logging.Analyzers` (new NuGet package, ships in Phase B). Compile-time detection:
- Diagnostic `RTU001` — `$"..."` argument to `ILogger.Log*` invocation.
- Diagnostic `RTU002` — `Console.Write*` call in a non-test source assembly.
- Diagnostic `RTU003` — PII-shaped property name in a log call (uses same canonical list + regex extension).

Consumers may install either or both; the two are independent and consistent.

---

## Event-Envelope Schema (Outbox / Messaging)

```csharp
public sealed record EventEnvelope
{
    public required string Id { get; init; }              // GUID
    public required string Type { get; init; }            // e.g., "OrderCreated"
    public required int Version { get; init; }            // schema version
    public required DateTimeOffset Timestamp { get; init; }
    public required string CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? TraceParent { get; init; }             // W3C traceparent
    public required byte[] Body { get; init; }            // serialized payload
}
```

### `OutboxMessage` (Microservices.Outbox)

```csharp
public sealed record OutboxMessage
{
    public required Guid Id { get; init; }
    public required string AggregateId { get; init; }
    public required long Sequence { get; init; }
    public required string Topic { get; init; }
    public required EventEnvelope Envelope { get; init; }
    public required OutboxState State { get; init; }    // Pending, Relayed, Failed, DeadLetter
    public DateTimeOffset? RelayedAt { get; init; }
    public int RetryCount { get; init; }
}

public enum OutboxState { Pending, Relayed, Failed, DeadLetter }
```

---

## Snapshot File Layout (Verify-compatible, per C-003)

```
tests/{TestProject}/Snapshots/
  {FullyQualifiedTestName}.verified.json    ← committed
  {FullyQualifiedTestName}.received.json    ← CI FAILS on presence
```

Scrubbers applied in order:
1. GUIDs → `{guid-1}`, `{guid-2}`, ...
2. Timestamps (ISO-8601) → `{timestamp-1}`, ...
3. CorrelationIds / CausationIds → `{correlation-id-1}`, ...
4. EventIds → `{event-id-1}`, ...
5. Sequence numbers → `{sequence-1}`, ...
6. Connection strings → `{connection-string}`
7. File paths → `{path}`

---

## Architecture Test Rules (NetArchTest)

```csharp
public sealed class ArchitectureTests
{
    [Test] public void Databases_DoesNotReferenceAnySqlOrNoSqlProvider() { ... }
    [Test] public void DatabasesSql_DoesNotReferenceAnyProvider() { ... }
    [Test] public void Providers_DoNotReferenceSiblings() { ... }
    [Test] public void Microservices_DependOnlyOnBases() { ... }
    [Test] public void PublicStaticHelpers_AreSealed() { ... }
    [Test] public void AllFixtures_ExtendFixtureBase() { ... }
    [Test] public void AllRigBuilders_AreAbstractOrSealed() { ... }
    [Test] public void NoSource_UsesDateTimeNow() { ... }
    [Test] public void NoSource_UsesAsyncVoid() { ... }
    [Test] public void EveryPublicType_HasReferencingTestAssembly() { ... }
}
```

---

## Cross-Reference: FR → Type

| FR | Type(s) |
|---|---|
| FR-010..012 | `I{Area}Rig` marker + `{Area}FixtureBase` + `{Area}RigBuilder<TSelf>` |
| FR-012 | `IsolationKey` record |
| FR-020..027 | `SqlServerFixture`, `DbContextHelper<TContext>`, `InMemoryDbExtensions`, `RedisFixture`, `ServiceBusFixture`, `ListenerBase`, `ServiceBusListener`, `EventSenderBase`, `ServiceBusEventSender` |
| FR-030..031 | `SqliteFixture`, `SqliteRigBuilder`, `DbContextHelperCrudContract<TFixture>` |
| FR-050..057 | All source types (rule compliance) |
| FR-070..074 | `TracingFixture`, `TraceAssert`, `LoggingFixture`, `LogAssert`, `LoggingDetectorOptions`, `SeqFixture`, `SeqAssert` |
| FR-080..082 | `CacheAssert`, `StampedeTester`, `BackplaneCapture`, `ClockControl` |
| FR-090..093 | `JwtBuilder`, `MockOAuthServer`, `PolicyAssert`, `SecurityAssert` |
| FR-100..102 | `OutboxFixture`, `OutboxMessage`, `InboxFixture`, `AggregateAssert`, `SnapshotAssert` |

---

## References

- Spec: [spec.md](spec.md) §"Key Entities"
- Plan: [plan.md](plan.md) §"Target Architecture"
- Research: [research.md](research.md) §"Reusable components in Rig.TUnit.Core"
