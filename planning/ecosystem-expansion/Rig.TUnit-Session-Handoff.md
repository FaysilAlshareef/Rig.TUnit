# Rig.TUnit — Ecosystem Expansion — Session Handoff

## What this is

The complete implementation handoff for the Rig.TUnit ecosystem expansion. Pair this with `Rig.TUnit-Library-Design.md` in the same directory to execute the work in a fresh session.

**Scope:** hard cutover + new ecosystem (Databases / Messaging / Caching / Storage / Observability / Security / Http / Resilience / HealthChecks / Concurrency / Docker / Parallelism / Ci / Microservices.*). No backwards-compat shims — old packages are deleted.

**Prerequisite state:** the base library and 002 fluent-builder expansion are implemented (56 passing tests). This handoff rewrites/relocates them into the new structure.

---

## Versions (pin in `Directory.Build.props`)

- `<TargetFramework>net10.0</TargetFramework>`
- `TUnit` — 1.34.5+
- `Testcontainers` — 4.6.0+
- `Mediator.Abstractions` — 3.0.2
- `Microsoft.EntityFrameworkCore` — 10.0.0
- `Microsoft.Extensions.*` — 10.0.0
- `Serilog` — 4.x (+ `Serilog.Sinks.Seq` 8.x)
- `OpenTelemetry` — 1.9.x (+ `OpenTelemetry.Exporter.InMemory`)
- `Microsoft.Extensions.TimeProvider.Testing` — 10.0.0
- `Microsoft.IdentityModel.Tokens` / `System.IdentityModel.Tokens.Jwt` — 8.x
- `StackExchange.Redis` — 2.8.x
- `Microsoft.Extensions.Caching.Hybrid` — 9.x
- `ZiggyCreatures.FusionCache` — 2.x
- `Bogus` — 35.x
- `NetArchTest.Rules` — 1.x (architecture tests)
- `BenchmarkDotNet` — 0.14.x
- `System.IO.Abstractions` — 21.x

---

## Hard-delete list (execute BEFORE new work)

Delete these directories/files outright:

```
src/Rig.TUnit.SqlServer/
src/Rig.TUnit.Redis/
src/Rig.TUnit.ServiceBus/
src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs   (merge logic into Core.ServiceRemovalExtensions)

tests/Rig.TUnit.SqlServer.Tests.Unit/
tests/Rig.TUnit.SqlServer.Tests.Integration/
tests/Rig.TUnit.Redis.Tests.Integration/
tests/Rig.TUnit.ServiceBus.Tests.Integration/
```

Also strip from `Rig.TUnit.slnx` the four deleted source + four deleted test project references.

---

## New source projects to create

### Phase A (base contracts + hard cutover)

```
src/Rig.TUnit.Databases/Rig.TUnit.Databases.csproj
src/Rig.TUnit.Databases/Contracts/IDbRig.cs
src/Rig.TUnit.Databases/Fixtures/DbFixtureBase.cs
src/Rig.TUnit.Databases/Builder/DatabaseRigBuilder.cs
src/Rig.TUnit.Databases/Assertions/DatabaseAssert.cs
src/Rig.TUnit.Databases/Assertions/MigrationAssert.cs
src/Rig.TUnit.Databases/Seeding/SeedBuilder.cs

src/Rig.TUnit.Databases.Sql/Rig.TUnit.Databases.Sql.csproj
src/Rig.TUnit.Databases.Sql/Contracts/ISqlRig.cs
src/Rig.TUnit.Databases.Sql/Fixtures/SqlFixtureBase.cs
src/Rig.TUnit.Databases.Sql/Builder/SqlRigBuilder.cs
src/Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs          (moved from SqlServer, made generic)
src/Rig.TUnit.Databases.Sql/Helpers/TransactionScope.cs
src/Rig.TUnit.Databases.Sql/Helpers/DeadlockSimulator.cs
src/Rig.TUnit.Databases.Sql/Assertions/RawSqlAssert.cs
src/Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs  (KEPT — relocated from old Rig.TUnit.SqlServer)

src/Rig.TUnit.Databases.Sql.SqlServer/Rig.TUnit.Databases.Sql.SqlServer.csproj
src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs             (moved)
src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs           (moved)
src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilderExtensions.cs (moved)

src/Rig.TUnit.Databases.Sql.Sqlite/Rig.TUnit.Databases.Sql.Sqlite.csproj       (NEW — companion fast path to InMemoryDbExtensions)
src/Rig.TUnit.Databases.Sql.Sqlite/Fixtures/SqliteFixture.cs
src/Rig.TUnit.Databases.Sql.Sqlite/Builder/SqliteRigBuilder.cs
src/Rig.TUnit.Databases.Sql.Sqlite/Builder/SqliteRigBuilderExtensions.cs

src/Rig.TUnit.Databases.NoSql/Rig.TUnit.Databases.NoSql.csproj
src/Rig.TUnit.Databases.NoSql/Contracts/INoSqlRig.cs
src/Rig.TUnit.Databases.NoSql/Fixtures/DocumentFixtureBase.cs
src/Rig.TUnit.Databases.NoSql/Builder/NoSqlRigBuilder.cs
src/Rig.TUnit.Databases.NoSql/Assertions/JsonDocumentAssert.cs
src/Rig.TUnit.Databases.NoSql/Helpers/ChangeFeedCapture.cs

src/Rig.TUnit.Messaging/Rig.TUnit.Messaging.csproj
src/Rig.TUnit.Messaging/Contracts/IMessagingRig.cs
src/Rig.TUnit.Messaging/Fixtures/MessagingFixtureBase.cs
src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs                (extracted from ListenerHelper)
src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs             (extracted from ServiceBusEventSender)
src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs
src/Rig.TUnit.Messaging/Assertions/MessageAssert.cs
src/Rig.TUnit.Messaging/Assertions/DeadLetterAssert.cs
src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs
src/Rig.TUnit.Messaging/Conventions/TopicNamingConvention.cs

src/Rig.TUnit.Messaging.ServiceBus/Rig.TUnit.Messaging.ServiceBus.csproj
src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs               (moved)
src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs               (extracts ListenerBase)
src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs            (extracts EventSenderBase)
src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs             (moved, now inherits MessagingRigBuilder)

src/Rig.TUnit.Caching/Rig.TUnit.Caching.csproj
src/Rig.TUnit.Caching/Contracts/ICacheRig.cs
src/Rig.TUnit.Caching/Fixtures/CacheFixtureBase.cs
src/Rig.TUnit.Caching/Builder/CacheRigBuilder.cs
src/Rig.TUnit.Caching/Assertions/CacheAssert.cs
src/Rig.TUnit.Caching/Helpers/StampedeTester.cs
src/Rig.TUnit.Caching/Helpers/BackplaneCapture.cs
src/Rig.TUnit.Caching/Helpers/ClockControl.cs

src/Rig.TUnit.Caching.Redis/Rig.TUnit.Caching.Redis.csproj
src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs                           (moved from Rig.TUnit.Redis)
src/Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilder.cs                    (moved, inherits CacheRigBuilder)
src/Rig.TUnit.Caching.Redis/Helpers/RedisBackplaneCapture.cs

src/Rig.TUnit.Databases.NoSql.Redis/Rig.TUnit.Databases.NoSql.Redis.csproj
src/Rig.TUnit.Databases.NoSql.Redis/Builder/RedisKvRigBuilder.cs               (project-references Caching.Redis for fixture)
src/Rig.TUnit.Databases.NoSql.Redis/Helpers/KeyScanHelper.cs
```

### Phase B (missing capabilities)

```
src/Rig.TUnit.Observability/*
src/Rig.TUnit.Observability.Tracing/*
src/Rig.TUnit.Observability.Metrics/*
src/Rig.TUnit.Observability.Logging/*
src/Rig.TUnit.Observability.Seq/*
src/Rig.TUnit.Security/*
src/Rig.TUnit.Security.Jwt/*
src/Rig.TUnit.Security.OAuth/*
src/Rig.TUnit.Http/*
src/Rig.TUnit.Resilience/*
```

### Phase C (microservices + cross-provider)

```
src/Rig.TUnit.Microservices.Outbox/*
src/Rig.TUnit.Microservices.Inbox/*
src/Rig.TUnit.Microservices.EventSourcing/*
src/Rig.TUnit.Microservices.Snapshots/*
src/Rig.TUnit.Concurrency/*
src/Rig.TUnit.HealthChecks/*
```

### Phase D (provider expansion)

```
src/Rig.TUnit.Databases.Sql.Postgresql/*
src/Rig.TUnit.Databases.Sql.MySql/*
# NOTE: Rig.TUnit.Databases.Sql.Sqlite is created in Phase A (alongside InMemoryDbExtensions) —
# it is NOT a Phase D addition. Both fast paths coexist: developers choose EF InMemory OR Sqlite OR container.
src/Rig.TUnit.Databases.NoSql.Cosmos/*
src/Rig.TUnit.Databases.NoSql.Mongo/*
src/Rig.TUnit.Messaging.Kafka/*
src/Rig.TUnit.Messaging.RabbitMq/*
src/Rig.TUnit.Caching.Memory/*
src/Rig.TUnit.Caching.Hybrid/*
src/Rig.TUnit.Caching.Fusion/*
src/Rig.TUnit.Storage/*
src/Rig.TUnit.Storage.AzureBlob/*
src/Rig.TUnit.Storage.S3/*
```

### Phase E (polish)

```
src/Rig.TUnit.Docker/*
src/Rig.TUnit.Parallelism/*
src/Rig.TUnit.Ci/*
src/Rig.TUnit.Observability.AppInsights/*
src/Rig.TUnit.Security.Mtls/*
src/Rig.TUnit.Security.Policies/*
src/Rig.TUnit.Microservices.Saga/*
src/Rig.TUnit.Microservices.Contracts/*
src/Rig.TUnit.Databases.Sql.Oracle/*
src/Rig.TUnit.Databases.NoSql.Dynamo/*
src/Rig.TUnit.Databases.NoSql.Cassandra/*
src/Rig.TUnit.Databases.NoSql.EventStore/*
src/Rig.TUnit.Databases.NoSql.ElasticSearch/*
src/Rig.TUnit.Messaging.Sqs/*
src/Rig.TUnit.Messaging.Nats/*
src/Rig.TUnit.Storage.MinIO/*
src/Rig.TUnit.Storage.FileSystem/*
```

---

## Matching test projects (one unit + one contract + one integration per package)

For every `Rig.TUnit.X` source package create:

```
tests/Rig.TUnit.X.Tests.Unit/            ← pure logic, builders, assertions
tests/Rig.TUnit.X.Tests.Contract/        ← abstract base class re-run by every provider
tests/Rig.TUnit.X.Tests.Integration/     ← container / real-service tests (providers only)
```

Base packages (`Rig.TUnit.Databases`, `.Databases.Sql`, `.Messaging`, `.Caching`, etc.) only need `Tests.Unit` + `Tests.Contract`. Provider packages need all three.

One new cross-cutting test project:

```
tests/Rig.TUnit.Architecture.Tests/      ← NetArchTest rules: no circular deps, correct naming,
                                            public types have tests, namespace conventions, sealed classes
```

---

## Namespace convention

- Source namespace matches folder: `Rig.TUnit.Databases.Sql.SqlServer.Fixtures`, `Rig.TUnit.Messaging.ServiceBus.Helpers`.
- Test namespace matches source: `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration.Fixtures`.
- File-scoped namespaces everywhere.

---

## TDD execution order (every class, every package)

1. **Red** — write failing test for the contract or behavior.
2. **Green** — write the minimum code to pass.
3. **Refactor** — extract/rename/tighten; tests stay green.
4. **Commit** — production code + test code in the SAME commit. Never one without the other.

**Per-package ship gate:**
- Contract suite passes (every abstract test implemented)
- Integration suite passes (where applicable)
- Parallel-isolation contract passes (20 concurrent fixtures, no cross-talk)
- Line coverage ≥ 90%, branch coverage ≥ 85%
- Benchmark regression budget met (fixture startup < 10% slower than baseline)
- Public API has XML docs (warning-as-error on missing)

**Per-phase ship gate:** every package in the phase passes its own gate + the full solution `dotnet test` is green.

---

## Mandatory tests per provider (contract suite)

Every provider's contract test class MUST implement:

1. `Fixture_InitializeAsync_IsIdempotent`
2. `Fixture_DisposeAsync_IsSafeToCallTwice`
3. `Builder_UseContainer_ResolvesConnectionSource`
4. `Builder_UseConfig_ResolvesFromIConfiguration`
5. `Builder_UseOptions_ResolvesFromIOptions`
6. `Builder_UseValue_UsesRawConnectionString`
7. `Builder_UseAuto_SelectsContainerInCi`
8. `Builder_UseAuto_SelectsConfigLocally`
9. `Builder_ForceContainersInCi_RejectsConfigInCi`
10. `IsolationKey_PerTest_DoesNotCollide` (run 20 in parallel)
11. `CancellationToken_Honored_ThrowsOperationCanceled`
12. `EventualConsistency_WaitHelper_DetectsStateChange`
13. Provider-specific quirk tests (at least 3 documented differences).

---

## Mandatory tests per assertion-DSL method

1. Positive — assertion holds.
2. Negative — fails with expected message + structured detail.
3. Boundary — near-miss equality, just-over-threshold.
4. Async/timeout — eventual consistency with `WaitHelper`.
5. Cancellation — `CancellationToken` honored.

---

## Area-specific test coverage requirements

### Databases.Sql
- Migration applied / pending / idempotent.
- Transaction scope auto-rollback.
- `DbContextHelper` CRUD across 3+ entity shapes.
- `SeedBuilder` with FK ordering (3-level deep graph).
- Deadlock simulator repeatable.
- RawSqlAssert.
- **Three-way fast-path parity**: the same `DbContextHelper` CRUD suite MUST pass against all three: `InMemoryDbExtensions` (EF InMemory), `Rig.TUnit.Databases.Sql.Sqlite` (SQLite `:memory:`), and a container provider (e.g., `Rig.TUnit.Databases.Sql.SqlServer`). Each path gets its own concrete test class that inherits the same abstract contract.

### Databases.NoSql
- `JsonDocumentAssert` ignores `_etag`/`_ts`/`_rid`/`__v`.
- Partition-key distribution check.
- Change-feed capture.
- Eventual consistency polling.

### Messaging
- `MessageAssert.Published<T>().ExactlyOnce()` across 100 fire-and-forget sends.
- Correlation ID propagation.
- Traceparent W3C propagation (integrates with `Rig.TUnit.Observability.Tracing`).
- Dead-letter branch.
- Per-key ordering.

### Caching
- Stampede: 100 concurrent misses → producer called exactly once.
- Tag invalidation: tagged keys purged, untagged kept.
- Backplane coherency: two host instances → invalidation propagates.
- Fail-safe: backend throws → stale served.
- Negative caching: null cached with shorter TTL.
- Eager refresh inside soft/hard TTL window.

### Observability.Seq
- Seq container starts + Serilog sink wired.
- `SeqAssert.Query(...)` returns expected hits.
- Anti-pattern detector fires on interpolated template.
- Anti-pattern detector fires on PII-shaped property.
- Dashboard snapshot captured for CI artifact.

### Security.Jwt / OAuth
- HS256 + RS256 builders produce valid tokens accepted by real `JwtBearerHandler`.
- Expired / tampered / not-yet-valid rejected with expected error.
- Key rotation (kid) handled.
- OAuth `/token` with client_credentials returns valid JWT.
- OIDC discovery document served.
- Real `.AddJwtBearer(...)` middleware integrates end-to-end (no bypass).

### Http
- Matchers (method/path/query/header/JSON path/regex) positive + negative.
- Scenario state machine across 3 calls.
- Delay / intermittent-failure sim.
- Record/replay round-trip.
- `DelegatingHandler` variant.

### Resilience
- `FakeTimeProvider` advances Polly backoff deterministically.
- Circuit state transitions (Closed → Open → HalfOpen → Closed).
- Retry count asserted.
- Rate-limit policy asserted.

### HealthChecks
- `/health/live` + `/health/ready` distinguished.
- Dependency-down simulator flips Ready to Unhealthy.
- Startup probe timing asserted.

### Concurrency
- Two writers → one wins with `DbUpdateConcurrencyException` across SqlServer + Postgres + Cosmos + Mongo.
- `If-Match` → 412; `If-None-Match` → 304 verified against real handler.
- Sequence-number idempotency.

### Microservices.Outbox
- Relay simulator drains outbox → publishes via `Rig.TUnit.Messaging.ServiceBus` + `Kafka` in matrix.
- `ExactlyOnce` across concurrent relay runs.
- Dead-letter branch.
- `OutboxReplay` backfill.

### Microservices.Snapshots
- First run creates `.received.*`.
- Second run passes.
- Scrubbers applied (GUID/timestamp/correlation-ID).
- Mismatch produces readable diff.

### Parallelism
- Port allocator survives 100 concurrent requests without collisions.
- Per-test schema name unique across 20 parallel tests.
- Shared-state detector flags static-field write.

---

## Directory.Build.props additions

Centralize:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>    <!-- allow missing doc only inside obj/ -->
</PropertyGroup>
```

Test projects add:

```xml
<PropertyGroup>
  <IsPackable>false</IsPackable>
  <GenerateDocumentationFile>false</GenerateDocumentationFile>
</PropertyGroup>
```

---

## Architecture tests (`Rig.TUnit.Architecture.Tests`)

Using `NetArchTest.Rules`:

1. `Rig.TUnit.Databases` never references any `*.Sql.*` or `*.NoSql.*` package.
2. `Rig.TUnit.Databases.Sql` never references any provider (`.SqlServer`, `.Postgresql`, …).
3. Providers reference their own base, never siblings.
4. Microservices packages depend only on bases, never concrete providers.
5. No class named `*Helper` is `public static` without also being `sealed`.
6. All `*Fixture` classes extend a `*FixtureBase`.
7. All `*RigBuilder<TSelf>` classes are abstract or sealed.
8. No type uses `DateTime.Now` (reflect over loaded assemblies, whitelist tests).
9. No method is `async void` (except event handlers, whitelisted).
10. Every public type in `src/` has at least one referencing test project.

---

## Relocation checklist (Phase A — do in this order)

1. Pin versions in `Directory.Build.props`.
2. Create Phase A base projects (`Databases`, `Databases.Sql`, `Databases.NoSql`, `Messaging`, `Caching`) with empty skeletons + failing contract tests.
3. Write contract test suites for each base (abstract classes + required methods listed above). All tests RED.
4. Implement base abstractions; all RED tests GREEN.
5. Create `Databases.Sql.SqlServer` project. Move SqlServer sources in. Update namespaces. Wire `SqlServerFixture : SqlFixtureBase`, `SqlServerRigBuilder : SqlRigBuilder<SqlServerRigBuilder>`. Run contract suite against SqlServer — GREEN.
6. Create `Messaging.ServiceBus` project. Move ServiceBus sources. Split `ListenerHelper` → `ListenerBase` + `ServiceBusListener`. Split `ServiceBusEventSender` → `EventSenderBase` + provider. Contract suite GREEN.
7. Create `Caching.Redis` project. Move Redis fixture. Wire `RedisCacheRigBuilder : CacheRigBuilder<…>`. Contract suite GREEN.
8. Create `Databases.NoSql.Redis` project. Project-reference `Caching.Redis` for the shared fixture; add KV builder.
9. Merge `GrpcServiceReplacementExtensions` → `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`.
10. Delete old projects + old tests. Remove from `Rig.TUnit.slnx`.
11. Update meta-package `Rig.TUnit` references to new packages.
12. Full solution build + test. All existing test logic now running under new layout; count ≥ 56 GREEN.
13. Add `Rig.TUnit.Architecture.Tests` — all rules pass.
14. Coverage gate met.
15. Commit Phase A.

Phases B–E follow the same cadence: base first, then providers, contract suite first, integration second.

---

## CI pipeline updates

1. `dotnet build` — warnings-as-errors, XML docs required.
2. `dotnet test` — full solution.
3. `dotnet test --collect:"XPlat Code Coverage"` — gate ≥ 90%/85%.
4. `dotnet test tests/Rig.TUnit.Benchmarks` — regression budgets.
5. Docker daemon required for integration tests; skip if unavailable with `[EnabledOnDocker]` filter.
6. Matrix: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x.
7. Artifacts: TRX, coverage HTML, Seq dashboard screenshots (from `Observability.Seq.Tests.Integration`).

---

## Open questions to resolve during spec

1. Naming: `Rig.TUnit.Databases.Sql.Postgresql` vs `…Postgres`? (Spec chooses `Postgresql` — matches NuGet `Npgsql` + Testcontainers module.)
2. Should `Caching.Memory` include `IMemoryCache` only, or also `ObjectPool`? (Spec: cache only; pool tests live in `Rig.TUnit.Resilience` if needed.)
3. Should `Microservices.EventSourcing` depend on `Databases.NoSql.EventStore`? (Spec: no — event sourcing harness is provider-agnostic; EventStore is one adapter.)
4. Should `Observability.Seq` be default for `Rig.TUnit.Microservices` meta-package? (Spec: yes — Seq is the recommended structured-log store.)
5. Snapshot format compatibility with Verify.Xunit? (Spec: yes — share on-disk conventions so users can migrate in/out.)

---

## Definition of done

- All packages listed build with `dotnet build` — zero warnings.
- All packages ship XML documentation.
- `dotnet test` full solution — 100% pass, coverage gate met.
- `NetArchTest` suite passes — zero architectural violations.
- `BenchmarkDotNet` suite within regression budget.
- Every public API demonstrated in `README.md` + an example test.
- Old projects deleted, `Rig.TUnit.slnx` clean.
- CI matrix green across provider versions.
