# Rig.TUnit — Ecosystem Expansion — Library Design

## 0. Status — hard cutover, no compatibility shims

**Stage:** in development, no public release has shipped. Therefore:

- Old packages (`Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus`) are **deleted outright**, not shimmed.
- Old extension methods (`UseSqlServerContainerIsolated`, `UseRedisContainer`, `UseServiceBusContainer`, `ReplaceGrpcClient`, `GrpcServiceReplacementExtensions`) are **removed** — the fluent builder is the single public API.
- `InMemoryDbExtensions` is **kept** (relocated to `Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs`) — developers can still pick EF Core's in-memory provider when they want the fastest possible path and don't care about SQL-dialect fidelity.
- `Rig.TUnit.Databases.Sql.Sqlite` is added as an **additional** fast-path option — real SQLite `:memory:` gives higher-fidelity SQL semantics than EF InMemory while staying containerless. Developers choose per-scenario: EF InMemory (fastest, lowest fidelity) vs Sqlite in-memory (fast, real SQL) vs container (full fidelity).
- Internal tests and the 56 existing passing tests will be rewritten/renamespaced to match the new structure — none are preserved "for compatibility."

---

## 1. Vision

Turn Rig.TUnit into a **full microservice test platform** built on one uniform mental model:

```
Rig.TUnit.{Area}               ← base contracts + shared helpers
Rig.TUnit.{Area}.{Provider}    ← provider-specific fixture/impl
```

Every new provider reuses the base via inheritance (DRY). Every test scenario a .NET microservice team faces — data, messaging, caching, storage, observability, security, resilience, HTTP, outbox, snapshots, health, concurrency — has a first-class package.

### Design principles

1. **Base + Provider** — `Rig.TUnit.Databases.Sql` defines the contract; `Rig.TUnit.Databases.Sql.Postgresql` implements it. ~80% shared code, ~20% provider-specific.
2. **Fluent-first** — every public API returns a builder or `IServiceCollection` for chaining.
3. **Connection-source agnostic** — `IRigConnectionSource` (Container / Config / Options / Value / Auto) works for every area.
4. **TDD-first** — no production class is written before its failing test. Every base contract is test-specified before any provider implements it.
5. **Minimal dependencies per package** — consumer pays only for what they import.
6. **Parallel-safe by default** — every fixture generates an `IsolationKey` (db name, topic suffix, cache prefix, container name) derived from the test's execution context.
7. **Microservice patterns are cross-cutting** — Outbox, Inbox, EventSourcing, Saga, Snapshots live under `Rig.TUnit.Microservices.*` and sit **on top of** infrastructure packages.

---

## 2. Package tree (final state)

```
Rig.TUnit.Core                              ← RigBuilder, RigConnect, WaitHelper, Fixtures, Fakers
Rig.TUnit.Mediator                          ← (unchanged)
Rig.TUnit.Grpc                              ← (minus GrpcServiceReplacementExtensions; merged into Core)
Rig.TUnit.WebAPI                            ← (TestAuthenticationHandler kept for smoke tests only)

# ---- Infrastructure: base + providers ----
Rig.TUnit.Databases                         ← IDbRig, DbFixtureBase, MigrationAssert, SeedBuilder
├─ Rig.TUnit.Databases.Sql                  ← ISqlRig, SqlFixtureBase, DbContextHelper, TransactionScope
│  ├─ Rig.TUnit.Databases.Sql.SqlServer         (absorbs old Rig.TUnit.SqlServer)
│  ├─ Rig.TUnit.Databases.Sql.Postgresql
│  ├─ Rig.TUnit.Databases.Sql.MySql
│  ├─ Rig.TUnit.Databases.Sql.Oracle
│  └─ Rig.TUnit.Databases.Sql.Sqlite            (real SQLite :memory: fast path; additional to EF InMemory)
│     # NOTE: Rig.TUnit.Databases.Sql also retains InMemoryDbExtensions (EF Core InMemory provider)
│     #       so developers can choose: EF InMemory (fastest) / Sqlite (fast + real SQL) / container (full fidelity)
└─ Rig.TUnit.Databases.NoSql                ← INoSqlRig, DocumentFixtureBase, JsonDocumentAssert
   ├─ Rig.TUnit.Databases.NoSql.Cosmos
   ├─ Rig.TUnit.Databases.NoSql.Mongo
   ├─ Rig.TUnit.Databases.NoSql.Dynamo          (LocalStack)
   ├─ Rig.TUnit.Databases.NoSql.Cassandra
   ├─ Rig.TUnit.Databases.NoSql.EventStore
   ├─ Rig.TUnit.Databases.NoSql.ElasticSearch
   └─ Rig.TUnit.Databases.NoSql.Redis           (KV role)

Rig.TUnit.Messaging                         ← IMessagingRig, ListenerBase, EventSenderBase, MessageAssert
├─ Rig.TUnit.Messaging.ServiceBus              (absorbs old Rig.TUnit.ServiceBus)
├─ Rig.TUnit.Messaging.Kafka
├─ Rig.TUnit.Messaging.RabbitMq
├─ Rig.TUnit.Messaging.Sqs                     (LocalStack)
└─ Rig.TUnit.Messaging.Nats

Rig.TUnit.Caching                           ← ICacheRig, CacheAssert, StampedeTester, BackplaneCapture
├─ Rig.TUnit.Caching.Memory
├─ Rig.TUnit.Caching.Redis                     (absorbs old Rig.TUnit.Redis cache role)
├─ Rig.TUnit.Caching.Hybrid                    (.NET 9+ HybridCache)
└─ Rig.TUnit.Caching.Fusion                    (FusionCache)

Rig.TUnit.Storage                           ← IStorageRig, BlobAssert, SasBuilder
├─ Rig.TUnit.Storage.AzureBlob                 (Azurite)
├─ Rig.TUnit.Storage.S3                        (LocalStack)
├─ Rig.TUnit.Storage.MinIO
└─ Rig.TUnit.Storage.FileSystem                (System.IO.Abstractions)

Rig.TUnit.Observability                     ← ITelemetryRig
├─ Rig.TUnit.Observability.Tracing             (OpenTelemetry in-memory exporter)
├─ Rig.TUnit.Observability.Metrics             (MeterListener)
├─ Rig.TUnit.Observability.Logging             (in-memory ILoggerProvider capture)
├─ Rig.TUnit.Observability.Seq                 (datalust/seq + query API)
└─ Rig.TUnit.Observability.AppInsights

Rig.TUnit.Security                          ← ISecurityRig
├─ Rig.TUnit.Security.Jwt                      (JwtBuilder HS256/RS256)
├─ Rig.TUnit.Security.OAuth                    (MockOAuthServer, OIDC discovery)
├─ Rig.TUnit.Security.Mtls                     (self-signed cert chain)
└─ Rig.TUnit.Security.Policies                 (PolicyAssert)

# ---- Single-provider packages ----
Rig.TUnit.Http                              ← WireMock-style in-proc stub
Rig.TUnit.Resilience                        ← FakeTimeProvider + Polly assertions
Rig.TUnit.HealthChecks                      ← /health probe assertions
Rig.TUnit.Concurrency                       ← RowVersion/ETag/optimistic-lock assertions
Rig.TUnit.Docker                            ← generic Testcontainers fixture + compose
Rig.TUnit.Parallelism                       ← port allocator, shared-state detector
Rig.TUnit.Ci                                ← TRX/JUnit enrichers, flaky quarantine

# ---- Microservices (cross-cutting patterns) ----
Rig.TUnit.Microservices.Outbox              ← OutboxMessage fixtures, relay assertions
Rig.TUnit.Microservices.Inbox               ← idempotency / sequence assertions
Rig.TUnit.Microservices.EventSourcing       ← aggregate apply/replay, event catalogue
Rig.TUnit.Microservices.Saga                ← saga step verifier, compensation
Rig.TUnit.Microservices.Snapshots           ← golden-file approval (Verify-compatible)
Rig.TUnit.Microservices.Contracts           ← Pact-style contracts

# ---- Meta packages ----
Rig.TUnit                                   ← Core + Mediator + Grpc + WebAPI + common
Rig.TUnit.Microservices                     ← Core + Mediator + Grpc + Outbox + Tracing + Jwt
Rig.TUnit.All                               ← everything (discouraged)
```

### Hard deletions

- `src/Rig.TUnit.SqlServer/` — deleted. Code relocated.
- `src/Rig.TUnit.Redis/` — deleted. Code relocated.
- `src/Rig.TUnit.ServiceBus/` — deleted. Code relocated.
- `Rig.TUnit.SqlServer.Extensions.SqlServerContainerExtensions` — deleted.
- `Rig.TUnit.Redis.Extensions.RedisContainerExtensions` — deleted.
- `Rig.TUnit.ServiceBus.Extensions.ServiceBusContainerExtensions` — deleted.
- `Rig.TUnit.Grpc.Extensions.GrpcServiceReplacementExtensions` — deleted (generic logic merged into `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`).
- Matching test projects (`Rig.TUnit.SqlServer.Tests.Unit`, `Rig.TUnit.SqlServer.Tests.Integration`, `Rig.TUnit.Redis.Tests.Integration`, `Rig.TUnit.ServiceBus.Tests.Integration`) — deleted and rewritten under new namespaces.

### Kept classes (relocated only, no behavior change)

- `SqlServerFixture` → `Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`
- `DbContextHelper<TContext>` → **promoted** to `Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` (generic across EF providers)
- `InMemoryDbExtensions` → **kept** and relocated to `Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (EF Core InMemory provider path stays as the fastest, lowest-fidelity option)
- `SqlServerRigBuilder` → `Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs`; inherits `SqlRigBuilder<SqlServerRigBuilder>` (new base)
- `RedisFixture` → `Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs` (primary home); `Rig.TUnit.Databases.NoSql.Redis` references it via project reference
- `RedisRigBuilder` → `Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilder.cs`; inherits `CacheRigBuilder<…>`
- `ServiceBusFixture` → `Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`
- `ListenerHelper` → split: `Rig.TUnit.Messaging/Helpers/ListenerBase.cs` (base) + `Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs`
- `ServiceBusEventSender` → split: `Rig.TUnit.Messaging/Helpers/EventSenderBase.cs` + `Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs`
- Everything in `Rig.TUnit.Core/` — unchanged.
- Everything in `Rig.TUnit.Mediator/`, `Rig.TUnit.Grpc/`, `Rig.TUnit.WebAPI/` — kept (except the three deleted extension files).

---

## 3. Core contracts (shared across all bases)

### 3.1 `IRigConnectionSource` (existing — unchanged)

All new fixtures implement it. No area reinvents connection resolution.

### 3.2 Fixture base hierarchy

```csharp
RigFixtureBase                         (existing)
├─ DbFixtureBase                       new — migration entrypoint
│  ├─ SqlFixtureBase                   new — connection string / DbConnection
│  └─ DocumentFixtureBase              new — endpoint + key
├─ MessagingFixtureBase                new — broker URI
├─ CacheFixtureBase                    new — endpoint + backplane hook
├─ StorageFixtureBase                  new — endpoint + credentials
└─ TelemetryFixtureBase                new — exporter handle
```

### 3.3 Builder base hierarchy

```csharp
RigBuilder                             (existing, root)
├─ DatabaseRigBuilder<TSelf>           new
│  ├─ SqlRigBuilder<TSelf>             new
│  └─ NoSqlRigBuilder<TSelf>           new
├─ MessagingRigBuilder<TSelf>          new
├─ CacheRigBuilder<TSelf>              new
├─ StorageRigBuilder<TSelf>            new
├─ TelemetryRigBuilder<TSelf>          new
└─ SecurityRigBuilder<TSelf>           new
```

Every concrete provider builder is ≤ ~200 LOC — it only expresses provider-specific options. The Use*/Container/Config/Options/Value/Auto API lives on the base.

### 3.4 Per-test isolation

Every fixture MUST expose `IsolationKey` derived from the test's `ExecutionContext`. Used to generate: database names, topic suffixes, cache prefixes, blob containers, Docker networks. Parallel workers never collide.

### 3.5 Assertion DSL

Every area ships an `XxxAssert` static entry point: `DbAssert`, `JsonDocumentAssert`, `MessageAssert`, `CacheAssert`, `BlobAssert`, `TraceAssert`, `MetricAssert`, `LogAssert`, `HealthAssert`, `ConcurrencyAssert`, `OutboxAssert`, `InboxAssert`, `SagaAssert`. All fluent, chainable, async-aware, and `WaitHelper`-backed for eventual-consistency scenarios.

---

## 4. Area designs (condensed)

### 4.1 `Rig.TUnit.Databases` (root base)

- `IDbRig`, `DbFixtureBase`
- `MigrationAssert.AllApplied() / .NoPendingModelChanges() / .Idempotent()`
- `SeedBuilder<T>` — dependency-ordered, Bogus integration, scenario presets
- `DatabaseAssert.TableExists / .RowCount(x) / .ColumnType(...) / .IndexExists(...)`

### 4.2 `Rig.TUnit.Databases.Sql` (base)

- `ISqlRig`, `SqlFixtureBase<TContainer>`
- `DbContextHelper<TContext>` — `QueryAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `SeedAsync`, `WithTransactionAsync` (auto-rollback)
- `TransactionScope` — scoped-transaction test wrapper
- `DeadlockSimulator`
- `RawSqlAssert.Returns(...) / .Affects(rows)`
- `InMemoryDbExtensions` — EF Core InMemory provider wiring (fastest path, lowest SQL fidelity). Developers use this when they explicitly want the EF InMemory provider's in-memory behavior.

**Three-way fast-path choice** (ordered from lowest to highest fidelity):

| Choice | Package | Fidelity | Speed | When to use |
|---|---|---|---|---|
| EF InMemory | `Rig.TUnit.Databases.Sql` (`InMemoryDbExtensions`) | none (LINQ-to-objects) | fastest | pure logic tests; aggregate behavior with no SQL semantics needed |
| SQLite `:memory:` | `Rig.TUnit.Databases.Sql.Sqlite` | real SQL engine | fast | integration tests needing real SQL without a container |
| Testcontainers | `Rig.TUnit.Databases.Sql.{SqlServer,Postgresql,…}` | production-grade | slower | full dialect fidelity, concurrency, provider quirks |

### 4.3 `Rig.TUnit.Databases.Sql.*` providers

Each provider delivers only:
- Testcontainers fixture (`PostgresFixture`, `MySqlFixture`, …)
- Wait strategy (ready signal)
- Connection-string format
- Dialect quirks (Postgres `xmin`, SqlServer `rowversion`, MySQL `AUTO_INCREMENT`, Oracle PL/SQL, Sqlite `:memory:`)

### 4.4 `Rig.TUnit.Databases.NoSql` (base) + providers

- `INoSqlRig`, `DocumentFixtureBase`
- `JsonDocumentAssert.DeepEquals(scrubSystemFields: true)` — ignores `_etag`, `_ts`, `_rid`, `__v`
- Eventual-consistency poll wrapper
- Change-feed / change-stream capture
- Partition-key distribution checker

| Provider | Emulator | Provider-specific helpers |
|---|---|---|
| Cosmos | `mcr.microsoft.com/cosmosdb/linux/...` | RU charge, partition-key seed |
| Mongo | `mongo:7` | collection-per-test, BSON diff |
| Dynamo | LocalStack | GSI query verifier |
| Cassandra | `cassandra:5` | keyspace-per-test |
| EventStore | `eventstore/eventstore:24.10` | stream/projection assertions |
| ElasticSearch | `elasticsearch:8` | index refresh, DSL assertions |
| Redis (KV) | `redis:7` | key scan, TTL assertions |

### 4.5 `Rig.TUnit.Messaging` (base) + providers

- `IMessagingRig`, `MessagingFixtureBase`
- `ListenerBase<T>` — captures timestamp, headers, body, correlation ID via `WaitHelper`
- `EventSenderBase` — correlation/causation injection, W3C traceparent propagation, serialization
- `MessageAssert.Published<T>().ExactlyOnce().OnTopic(...).WithCorrelation(...).WithHeader(k,v).Within(timeout)`
- `TopicNamingConvention` — `{company}-{domain}-{side}`
- `DeadLetterAssert`, `OrderingAssert`

| Provider | Fixture |
|---|---|
| ServiceBus | Microsoft ServiceBus emulator |
| Kafka | `confluentinc/cp-kafka` |
| RabbitMQ | `rabbitmq:3-management` |
| SQS | LocalStack |
| NATS | `nats:2` |

### 4.6 `Rig.TUnit.Caching` — real-world coherency

`IDistributedCache` is too thin. Focus on stampede, tag invalidation, fail-safe, multi-node coherency.

- `ICacheRig`, `CacheFixtureBase`
- `CacheAssert.HitRate / .Stampede / .Coherent / .TagInvalidation / .FailSafe / .NegativeCached`
- `StampedeTester` — N concurrent misses → producer called exactly once
- `BackplaneCapture` — intercepts Redis pub/sub invalidation messages
- `ClockControl` — pairs with `FakeTimeProvider`; TTLs advance without `Task.Delay`

Providers: `Memory`, `Redis` (backplane), `Hybrid` (.NET 9+), `Fusion` (fail-safe, eager refresh, tagging).

### 4.7 `Rig.TUnit.Storage` (base) + providers

- `IStorageRig`
- `BlobAssert.Exists(container, key).WithContentType(...).WithSize(x).WithMetadata(k,v)`
- `BlobAssert.LifecycleRule(rule).AppliesTo(key)`
- `SasBuilder` (per-provider variant)
- Upload/download/list/delete with eventual-consistency polling

Providers: `AzureBlob` (Azurite), `S3` (LocalStack), `MinIO`, `FileSystem` (`System.IO.Abstractions.TestingHelpers`).

### 4.8 `Rig.TUnit.Observability`

Three pillars + Seq + vendor bridges.

- **Tracing** — in-memory OTEL exporter. `TraceAssert.HasSpan(name).WithTag(k,v).WithStatus(Ok|Error).WithParent(...).DurationLessThan(x)`. Baggage + W3C traceparent propagation.
- **Metrics** — `MeterListener`. `MetricAssert.Counter("orders.created").Incremented(3).WithTag(...)`. Histogram bucket/percentile verification. Tag-cardinality guard.
- **Logging** — in-memory `ILoggerProvider` capturing structured entries w/ scope stack. `LogAssert.Logged(Warning).WithProperty("OrderId", id).InScope("TenantId", t)`. Anti-pattern detector (interpolated templates, `Console.Write`, PII-shaped property names — enforces `observability.md`).
- **Seq** — Testcontainers `datalust/seq`. Serilog sink wired into test host. `SeqAssert.Query("Level=@Warning and OrderId=@id").Count(1).Within(5s)`. Signal-based assertions. Same DSL surface as `.Logging` so swap is one line.
- **AppInsights** — telemetry-channel capture + end-to-end trace correlation.

### 4.9 `Rig.TUnit.Security`

- **Jwt** — `JwtBuilder.Issuer(...).Audience(...).Claim(...).ExpiresIn(...).SignedWithHs256(key) / .SignedWithRs256(cert)`. Key rotation (`kid`). Expired/tampered/not-yet-valid variants. JWKS endpoint stub.
- **OAuth** — `MockOAuthServer`. Endpoints: `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration`. Flows: client credentials, auth code + PKCE, refresh token. Works with real `JwtBearer` middleware (no bypass).
- **Mtls** — self-signed CA + leaf cert generator; mTLS handshake verifier for gRPC/HttpClient.
- **Policies** — `PolicyAssert.Policy("AdminOnly").Allows(principal).Denies(other)`. Requirement-handler coverage tracking. Role/scope matrix generator.

### 4.10 Single-provider packages

- **Http** — in-proc WireMock-style stub + `DelegatingHandler` variant. Matchers (method/path/query/header/JSON path/regex). Response builders (status/headers/JSON/binary/SSE). Scenario state machine. Delay/jitter/intermittent-failure. Record/replay. `HttpMock.Verify().Called(3).WithHeader(...)`.
- **Resilience** — integrates `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider`. `CircuitBreakerAssert.State(Closed|Open|HalfOpen)`. Retry/backoff/bulkhead/rate-limit assertions. Chaos injector (pairs with Http).
- **HealthChecks** — `HealthAssert.IsHealthy("/health/ready").Contains("sqlserver").InTime(2s)`. Dependency-down simulator. Startup-probe timing. Contract test: every registered `IHealthCheck` has a paired scenario.
- **Concurrency** — `ConcurrencyAssert.TwoWriters(entity).OneWinsWith<DbUpdateConcurrencyException>()`. RowVersion/ETag capture across SqlServer/Postgres/Cosmos/Mongo. HTTP preconditions (`If-Match` → 412; `If-None-Match` → 304). Sequence-number idempotency.
- **Docker** — generic `ContainerFixture` wrapping `Testcontainers`. `DockerComposeFixture` for multi-container topologies. Image-pull cache reuse. Per-test networks. Healthcheck-based ready detection.
- **Parallelism** — OS-level port allocator. Per-test schema/database/topic/cache-prefix generator. Shared-state detector. `[ExclusiveResource]` coordinator. Worker-count hints.
- **Ci** — TRX/JUnit enricher (adds span IDs, container logs, screenshot artifacts). Fail-fast gate. Flaky-test quarantine + report. Coverage-delta enforcer. GitHub Actions / Azure DevOps annotation writer.

### 4.11 `Rig.TUnit.Microservices.*`

- **Outbox** — `OutboxFixture` bootstraps outbox table/collection over any configured DB provider. `OutboxAssert.Contains<OrderCreated>().WithAggregateId(id).OnTopic("order-commands").ExactlyOnce().Relayed()`. Relay simulator drains → publishes via any `Rig.TUnit.Messaging.*`. Dead-letter/poison branches. Event-envelope (version, correlation/causation, timestamps). `OutboxReplay` for backfill/projection rebuild.
- **Inbox** — sequence-based idempotency (matches query-side pattern from `architecture.md`). `InboxAssert.SequenceApplied(aggregateId, seq).Idempotent()`. Duplicate-replay safety.
- **EventSourcing** — `When(event).Then(state)` harness. Event catalogue verification. Schema evolution (v1 event applied by v2 handler). `AggregateAssert.Raised<OrderCreated>().WithData(...)`.
- **Saga** — step verifier, compensation on failure, timeout (pairs with Resilience).
- **Snapshots** — Verify-compatible on-disk format (easy interop). Microservice-opinionated scrubbers: correlation/causation IDs, event IDs, timestamps, sequence numbers, connection strings, paths. JSON / XML / SQL / text. CLI diff tool + VS Code hook.
- **Contracts** — Pact-style consumer-driven contracts over `Rig.TUnit.Http` (REST) and `Rig.TUnit.Grpc` (RPC). Provider-verification fixture. Broker integration.

---

## 5. Testing strategy — TDD end-to-end

### 5.1 Test-project layout (mirrors source)

For every source package `Rig.TUnit.X`:

```
tests/Rig.TUnit.X.Tests.Unit          ← unit tests (no container)
tests/Rig.TUnit.X.Tests.Contract      ← contract suite re-run by every provider
tests/Rig.TUnit.X.Tests.Integration   ← container / real-service tests
```

Contract suites are shared via a common abstract test class; every provider implements a tiny concrete class that provides the fixture. A new Postgres provider passes/fails the same base contract as SqlServer. This guarantees uniformity.

### 5.2 Iron law

No production class lands in a commit without its failing test landing in the same commit. Enforced by:
- PR template checklist
- `Rig.TUnit.Architecture.Tests` verifying every public type has at least one referencing test assembly
- Coverage gate (below)

### 5.3 Required test categories per provider

Every contract test suite MUST include:

1. **Lifecycle** — InitializeAsync / DisposeAsync; init is idempotent; dispose is safe to call twice.
2. **Isolation** — 20 parallel instances, zero cross-talk. `IsolationKey` derivation verified.
3. **Connection-source matrix** — Container / Config / Options / Value / Auto all succeed against the provider.
4. **CI mode** — `ForceContainersInCi()` honored; config source rejected in CI.
5. **Happy path + error path + timeout + cancellation** for every public helper method.
6. **Eventual consistency** — every async-visible state change tested with `WaitHelper`.
7. **Provider quirk coverage** — one test per documented dialect difference (e.g., Postgres `xmin` vs SqlServer `rowversion`).

### 5.4 Required assertion-DSL coverage

For every `XxxAssert` method:
- Positive case (assertion holds)
- Negative case (assertion fails with expected message)
- Boundary case (near-miss)
- Async/timeout case (eventual consistency)
- Cancellation case (`CancellationToken` honored)

### 5.5 Parallel-safety smoke

Every fixture MUST pass the shared `ParallelIsolationContract` test (20 parallel executions, zero cross-talk). Lives in `Rig.TUnit.Parallelism.Tests.Contract` and is inherited by every provider's test project.

### 5.6 Coverage targets (merge gate)

- Line coverage ≥ 90% per package
- Branch coverage ≥ 85% per package
- Contract suite 100% pass rate per provider
- Public API 100% documented (XML doc comments)

### 5.7 Test naming

Follows `.claude/rules/testing.md`: `{Method}_{Scenario}_{ExpectedResult}` — e.g., `Publish_WithCorrelationId_PropagatesToConsumer`.

### 5.8 Benchmarks

`tests/Rig.TUnit.Benchmarks` is expanded with a BenchmarkDotNet suite per area: fixture startup time, per-test isolation overhead, assertion-DSL throughput. CI enforces regression budgets (fail if startup > 10% slower than baseline).

---

## 6. Cross-cutting rules (from `.claude/rules/*`)

- **.NET 10** + **TUnit 1.34.5+** — aligned via `Directory.Build.props`.
- **No `DateTime.Now`** — fixtures/helpers take `TimeProvider` via DI.
- **Options pattern** — every fixture config is a `XxxOptions` class with `[Required]` + `ValidateOnStart()`.
- **CancellationToken** — propagated through every async API.
- **Structured logging** — `ILogger<T>` only; no `Console.Write`.
- **Localization** — user-facing messages in `Phrases.resx` when consuming project uses that pattern.
- **No circular dependencies** — enforced by `NetArchTest` rules in `Rig.TUnit.Architecture.Tests`.
- **File-scoped namespaces**, `sealed` classes, records for value objects, `private set` on entities.
- **No `async void`**, no `.Result`, no `.Wait()`.

---

## 7. Phased delivery

No phase starts until the previous phase's tests are green and coverage targets met.

### Phase A — Base contracts & hard cutover
- Create `Rig.TUnit.Databases` + `.Sql` + `.NoSql` bases **test-first**.
- Relocate `SqlServer` → `Databases.Sql.SqlServer` (delete old project, move files, rename namespaces).
- Create `Rig.TUnit.Messaging` base; relocate `ServiceBus` → `Messaging.ServiceBus`.
- Create `Rig.TUnit.Caching` base; relocate `Redis` → `Caching.Redis` + reference from `Databases.NoSql.Redis`.
- Delete old extension methods.
- All 56 existing tests are rewritten/moved into the new layout; final count should be ≥ 56 green.

### Phase B — Rule-mandated missing capabilities
- `Rig.TUnit.Observability.Logging` + `.Seq` + `.Tracing`
- `Rig.TUnit.Security.Jwt` + `.OAuth`
- `Rig.TUnit.Http`
- `Rig.TUnit.Resilience`

### Phase C — Microservice patterns
- `Rig.TUnit.Microservices.Outbox` + `.Inbox` + `.EventSourcing`
- `Rig.TUnit.Microservices.Snapshots`
- `Rig.TUnit.Concurrency`
- `Rig.TUnit.HealthChecks`

### Phase D — Provider expansion
- `Databases.Sql.Postgresql`, `.MySql`, `.Sqlite`
- `Databases.NoSql.Cosmos`, `.Mongo`
- `Messaging.Kafka`, `.RabbitMq`
- `Caching.Hybrid`, `.Fusion`
- `Storage.AzureBlob`, `.S3`

### Phase E — Polish
- `Rig.TUnit.Docker`, `.Parallelism`, `.Ci`
- `Observability.Metrics`, `.AppInsights`
- `Security.Mtls`, `.Policies`
- `Microservices.Saga`, `.Contracts`
- Remaining providers (`Databases.Sql.Oracle`, `Databases.NoSql.Dynamo/Cassandra/EventStore/ElasticSearch`, `Messaging.Sqs/Nats`, `Storage.MinIO/FileSystem`)

---

## 8. Out of scope for this spec

- GraphQL / SignalR / FeatureFlags / Email / Scheduling / Ai / BackgroundServices packages — future work.
- IDE tooling (VS Code / Rider extensions).
- Commercial cloud-backed providers beyond emulators.
- Visual test reporting dashboards beyond Ci enrichers.

---

## 9. Versioning & release

- **Lockstep minor** — all packages move to 2.0.0 at the Phase A cutover, then bump minor together per phase.
- **Patch independent** per package for bug fixes.
- Meta-packages pin exact versions of children.
- CI matrix runs each provider against multiple engine versions (Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x).
- No `[Obsolete]` shims — this is a pre-release hard cutover.
