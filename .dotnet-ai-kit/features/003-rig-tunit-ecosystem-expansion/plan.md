# Implementation Plan: Rig.TUnit Ecosystem Expansion

**Feature ID**: 003-rig-tunit-ecosystem-expansion
**Generated**: 2026-04-17
**Mode**: Generic (single-repo library ecosystem)
**Complexity**: Complex (20+ entities, 5 phases, ~50 new packages, external services via Testcontainers)
**Source spec**: [spec.md](spec.md) — 5 clarifications resolved (C-001..C-005)

---

## Constitution Check

`.dotnet-ai-kit/memory/constitution.md` — **NOT PRESENT**. Gate skipped with warning. Run `/dai.learn` to generate a project constitution from detected patterns. In the meantime, the plan honors `.claude/rules/*.md` (architecture, testing, async, observability, security, configuration, naming, performance, tool-calls) as the de-facto rulebook.

Implied invariants (from `.claude/rules/`):
- Detect-first: plan uses existing `IRigConnectionSource`, `RigFixtureBase`, `WaitHelper`, `TestConfigurationBuilder`, `CompositeFixture` rather than reinventing them.
- Pattern fidelity: Base + Provider, fluent builder `UseX(source, x => …)`, Options-pattern configuration, file-scoped namespaces, `sealed` classes, `TimeProvider` injection.
- Architecture agnostic: this is a class-library ecosystem — no Clean Architecture / VSA / microservice constraints apply within the library; the library TESTS those architectures for consuming apps.

---

## Executive Summary

Transform Rig.TUnit from 8 source projects to ~50 via the **Base + Provider** pattern over 5 phases, delivered strictly test-first (RED-GREEN-REFACTOR). Phase A executes the hard cutover (delete `SqlServer`/`Redis`/`ServiceBus`, relocate code into providers, stand up 5 base packages + architecture tests). Phases B–E layer on observability, security, microservice patterns, concurrent providers, and polish. Every package ships its Unit + Contract + Integration test projects and must pass a 13-test contract suite, the parallel-isolation smoke, and the 90%/85% coverage gate before merge.

---

## Target Architecture

### Package Topology (final state)

```
Rig.TUnit.Core                              [MODIFIED — absorbs Grpc service-removal logic]
Rig.TUnit.Mediator                          [UNCHANGED]
Rig.TUnit.Grpc                              [MODIFIED — GrpcServiceReplacementExtensions deleted if present]
Rig.TUnit.WebAPI                            [UNCHANGED — TestAuthenticationHandler stays smoke-only]

# Base + Provider (DRY)
Rig.TUnit.Databases                         [NEW base]
├─ Rig.TUnit.Databases.Sql                  [NEW base — owns DbContextHelper + InMemoryDbExtensions]
│  ├─ Rig.TUnit.Databases.Sql.SqlServer     [NEW provider — RELOCATED from old src/Rig.TUnit.SqlServer]
│  ├─ Rig.TUnit.Databases.Sql.Sqlite        [NEW provider — real SQLite :memory:]
│  ├─ Rig.TUnit.Databases.Sql.Postgresql    [NEW provider — Phase D]
│  ├─ Rig.TUnit.Databases.Sql.MySql         [NEW provider — Phase D]
│  └─ Rig.TUnit.Databases.Sql.Oracle        [NEW provider — Phase E]
└─ Rig.TUnit.Databases.NoSql                [NEW base]
   ├─ Rig.TUnit.Databases.NoSql.Redis       [NEW — refs Caching.Redis fixture]
   ├─ Rig.TUnit.Databases.NoSql.Cosmos      [NEW — Phase D]
   ├─ Rig.TUnit.Databases.NoSql.Mongo       [NEW — Phase D]
   ├─ Rig.TUnit.Databases.NoSql.Dynamo      [NEW — Phase E]
   ├─ Rig.TUnit.Databases.NoSql.Cassandra   [NEW — Phase E]
   ├─ Rig.TUnit.Databases.NoSql.EventStore  [NEW — Phase E]
   └─ Rig.TUnit.Databases.NoSql.ElasticSearch[NEW — Phase E]

Rig.TUnit.Messaging                         [NEW base]
├─ Rig.TUnit.Messaging.ServiceBus           [NEW provider — RELOCATED from old src/Rig.TUnit.ServiceBus]
├─ Rig.TUnit.Messaging.Kafka                [NEW — Phase D]
├─ Rig.TUnit.Messaging.RabbitMq             [NEW — Phase D]
├─ Rig.TUnit.Messaging.Sqs                  [NEW — Phase E]
└─ Rig.TUnit.Messaging.Nats                 [NEW — Phase E]

Rig.TUnit.Caching                           [NEW base]
├─ Rig.TUnit.Caching.Memory                 [NEW — Phase C]
├─ Rig.TUnit.Caching.Redis                  [NEW — PRIMARY HOME for RedisFixture]
├─ Rig.TUnit.Caching.Hybrid                 [NEW — Phase D]
└─ Rig.TUnit.Caching.Fusion                 [NEW — Phase D]

Rig.TUnit.Storage                           [NEW base — Phase D]
├─ Rig.TUnit.Storage.AzureBlob              [NEW — Phase D]
├─ Rig.TUnit.Storage.S3                     [NEW — Phase D]
├─ Rig.TUnit.Storage.MinIO                  [NEW — Phase E]
└─ Rig.TUnit.Storage.FileSystem             [NEW — Phase E]

Rig.TUnit.Observability                     [NEW base — Phase B]
├─ Rig.TUnit.Observability.Tracing          [NEW — Phase B]
├─ Rig.TUnit.Observability.Logging          [NEW — Phase B]
├─ Rig.TUnit.Observability.Logging.Analyzers [NEW Roslyn analyzer — Phase B, per C-006]
├─ Rig.TUnit.Observability.Seq              [NEW — Phase B]
├─ Rig.TUnit.Observability.Metrics          [NEW — Phase E]
└─ Rig.TUnit.Observability.AppInsights      [NEW — Phase E]

Rig.TUnit.Security                          [NEW base — Phase B]
├─ Rig.TUnit.Security.Jwt                   [NEW — Phase B]
├─ Rig.TUnit.Security.OAuth                 [NEW — Phase B]
├─ Rig.TUnit.Security.Mtls                  [NEW — Phase E]
└─ Rig.TUnit.Security.Policies              [NEW — Phase E]

# Single-provider
Rig.TUnit.Http                              [NEW — Phase B]
Rig.TUnit.Resilience                        [NEW — Phase B]
Rig.TUnit.HealthChecks                      [NEW — Phase C]
Rig.TUnit.Concurrency                       [NEW — Phase C]
Rig.TUnit.Docker                            [NEW — Phase E]
Rig.TUnit.Parallelism                       [NEW — Phase E]
Rig.TUnit.Ci                                [NEW — Phase E]

# Microservice patterns (compose bases)
Rig.TUnit.Microservices.Outbox              [NEW — Phase C]
Rig.TUnit.Microservices.Inbox               [NEW — Phase C]
Rig.TUnit.Microservices.EventSourcing       [NEW — Phase C]
Rig.TUnit.Microservices.Snapshots           [NEW — Phase C — Verify-compatible]
Rig.TUnit.Microservices.Saga                [NEW — Phase E]
Rig.TUnit.Microservices.Contracts           [NEW — Phase E]

# Meta
Rig.TUnit                                   [MODIFIED — Core + Mediator + Grpc + WebAPI + common]
Rig.TUnit.Microservices                     [NEW — Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq]
Rig.TUnit.All                               [NEW — everything, discouraged]
```

**Deleted**: `src/Rig.TUnit.SqlServer/`, `src/Rig.TUnit.Redis/`, `src/Rig.TUnit.ServiceBus/` and their test projects.

### Dependency Flow (base → provider, microservice → bases)

```
┌──────────────────────────┐
│  Rig.TUnit.Core          │ ← root (IRigConnectionSource, RigBuilder, WaitHelper, RigFixtureBase)
└────────────┬─────────────┘
             │
┌────────────┴─────────────────────────────────────────────────┐
│                                                              │
▼                                                              ▼
┌───────────────────────┐    ┌───────────────────────┐    ┌──────────────────┐
│ Rig.TUnit.Databases   │    │ Rig.TUnit.Messaging   │    │ Rig.TUnit.Caching│
│   .Sql                │    │   .ServiceBus         │    │   .Redis         │
│     .SqlServer        │    │   .Kafka (D)          │    │   .Memory (C)    │
│     .Sqlite           │    │   .RabbitMq (D)       │    │   .Hybrid (D)    │
│     .Postgresql (D)   │    │                       │    │   .Fusion (D)    │
│   .NoSql              │    └───────────────────────┘    └──────────────────┘
│     .Redis→Caching    │              ▲                          ▲
│     .Cosmos (D)       │              │                          │
│     .Mongo (D)        │              │                          │
└───────────────────────┘              │                          │
             ▲                         │                          │
             │                         │                          │
             └────────────┬────────────┴──────────────────────────┘
                          │
             ┌────────────┴─────────────────────────┐
             │                                       │
             ▼                                       ▼
┌──────────────────────────┐           ┌────────────────────────────┐
│ Rig.TUnit.Microservices  │           │ Rig.TUnit.Observability    │
│   .Outbox (compose       │           │   .Tracing, .Logging, .Seq │
│     Databases+Messaging) │           │                            │
│   .Inbox                 │           └────────────────────────────┘
│   .EventSourcing         │
│   .Snapshots             │
└──────────────────────────┘
```

Rules enforced by `Rig.TUnit.Architecture.Tests`:
- Base packages MUST NOT reference their providers.
- Provider packages MUST reference only their own base (never siblings).
- Microservice packages MUST depend on BASES only (never concrete providers).
- No circular references anywhere.

---

## Phased Delivery

Each phase ships independently. No phase starts until the previous phase's merge gate is met.

### Phase A — Base Contracts + Hard Cutover (foundation)

**Scope**: 10 new packages + 4 deletions + 56 existing tests ported.

**Packages**: `Databases`, `Databases.Sql`, `Databases.Sql.SqlServer`, `Databases.Sql.Sqlite`, `Databases.NoSql`, `Databases.NoSql.Redis`, `Messaging`, `Messaging.ServiceBus`, `Caching`, `Caching.Redis`, `Architecture.Tests`.

**Execution order**:

1. **Version pinning** — update `Directory.Build.props` with all pinned versions (TUnit 1.34.5+, Testcontainers 4.6.0+, Mediator.Abstractions 3.0.2, EF Core 10.0.0, Serilog 4.x, OTEL 1.9.x, `Microsoft.Extensions.TimeProvider.Testing` 10.0.0, Microsoft.IdentityModel 8.x, StackExchange.Redis 2.8.x, Bogus 35.x, NetArchTest.Rules 1.x, BenchmarkDotNet 0.14.x, System.IO.Abstractions 21.x, `GenerateDocumentationFile`=true, `TreatWarningsAsErrors`=true). Add `Central Package Management` (CPM) via `Directory.Packages.props`.
2. **Base contracts (TDD contract-first)** — for each base area (`Databases`, `Databases.Sql`, `Databases.NoSql`, `Messaging`, `Caching`):
   - RED: write abstract contract test class (`SqlRigContract`, `MessagingRigContract`, `CacheRigContract`, `DbRigContract`, `NoSqlRigContract`) with the 13 mandatory tests — fails to compile (no base types yet).
   - GREEN: implement `I{Area}Rig`, `{Area}FixtureBase`, `{Area}RigBuilder<TSelf>`, `{Area}Assert` static DSL with minimal method bodies — contract compiles but fails (no providers).
   - REFACTOR: extract shared helpers (`WaitHelper` reuse, `ListenerBase`, `EventSenderBase`, `BackplaneCapture`, `SeedBuilder`, `DbContextHelper`).
3. **Hard deletions**:
   - Delete `src/Rig.TUnit.SqlServer/`, `src/Rig.TUnit.Redis/`, `src/Rig.TUnit.ServiceBus/`.
   - Delete `tests/Rig.TUnit.SqlServer.Tests.Unit/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/`, `tests/Rig.TUnit.Redis.Tests.Integration/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`.
   - Delete `src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs` if present (current repo shows `WebApplicationFactoryExtensions.cs` instead — confirm during execution).
   - Strip 8 removed project refs from `Rig.TUnit.slnx`; add 10+ new refs.
4. **Relocations** (RED-GREEN-REFACTOR preserved per file):
   - `SqlServerFixture` → `src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs` + inherit `SqlFixtureBase`.
   - `DbContextHelper<TContext>` → `src/Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` (promoted, EF-provider-agnostic).
   - `InMemoryDbExtensions` → `src/Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (KEPT).
   - `SqlServerRigBuilder` + extensions → `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/` + inherit `SqlRigBuilder<SqlServerRigBuilder>`.
   - `RedisFixture` → `src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs` (primary home).
   - `RedisRigBuilder` → `src/Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilder.cs` + inherit `CacheRigBuilder<RedisCacheRigBuilder>`.
   - `Rig.TUnit.Databases.NoSql.Redis` project-references `Rig.TUnit.Caching.Redis` for the fixture; adds `RedisKvRigBuilder` + `KeyScanHelper`.
   - `ServiceBusFixture` → `src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`.
   - `ListenerHelper` split → `src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs` + `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs`.
   - `ServiceBusEventSender` split → `src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs` + `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs`.
   - `ServiceBusRigBuilder` → `src/Rig.TUnit.Messaging.ServiceBus/Builder/` + inherit `MessagingRigBuilder<ServiceBusRigBuilder>`.
   - `ServiceBusFixture.InitializeAsync` pulls Microsoft emulator image `mcr.microsoft.com/azure-messaging/servicebus-emulator` + SQL Edge backend (per C-001), `ACCEPT_EULA=Y` set.
5. **Grpc generic logic merge** — if `GrpcServiceReplacementExtensions` exists, merge its generic service-removal logic into `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`; otherwise confirm absence is correct.
6. **New Sqlite provider (Phase A addition)** — `src/Rig.TUnit.Databases.Sql.Sqlite/{Fixtures/SqliteFixture.cs, Builder/SqliteRigBuilder.cs, Builder/SqliteRigBuilderExtensions.cs}`. `SqliteFixture` owns a single `SqliteConnection` (stays open for fixture lifetime). `UseSqlite(source, sql => …)` on the RigBuilder.
7. **Port existing 56 tests** — rewrite under new namespaces; count ≥ 56 GREEN.
8. **Architecture tests** — `tests/Rig.TUnit.Architecture.Tests/` using `NetArchTest.Rules` with the 10 rules from US13. All GREEN.
9. **Coverage + XML docs** — every public type documented; coverage gate (line ≥ 90%, branch ≥ 85%).
10. **Commit Phase A** with sub-commits showing RED → GREEN → REFACTOR cadence per class.

**Phase A merge gate**:
- [ ] All 10 new packages build, zero warnings.
- [ ] 56+ existing tests GREEN under new namespaces.
- [ ] Contract suites 100% pass for SqlServer + Sqlite + Redis + ServiceBus.
- [ ] `Rig.TUnit.Architecture.Tests` GREEN (10 rules).
- [ ] Parallel-isolation contract GREEN (20 parallel, zero collisions).
- [ ] Coverage gate met per package.
- [ ] `Rig.TUnit.slnx` clean of deleted refs; new refs present.
- [ ] Public API XML-documented.

### Phase B — Rule-Mandated Capabilities

**Scope**: 10 new packages.

**Packages**: `Observability`, `.Tracing`, `.Logging`, `.Seq`, `Security`, `.Jwt`, `.OAuth`, `Http`, `Resilience`.

**Execution order**:
1. `Observability` base + `ITelemetryRig` + `TelemetryFixtureBase`.
2. `.Tracing` — in-memory OTEL exporter + `TraceAssert` DSL (HasSpan, WithTag, WithStatus, WithParent, DurationLessThan, Baggage, W3C traceparent propagation).
3. `.Logging` — in-memory `ILoggerProvider` + `LogAssert` + runtime anti-pattern detector (interpolated templates, PII list per C-005) + `LoggingDetectorOptions.AdditionalPiiPatterns` regex extension point. Ships with a companion Roslyn analyzer package `.Logging.Analyzers` (per C-006) for compile-time detection of `$"..."` in `ILogger` calls and `Console.Write*` in source.
4. `.Seq` — `datalust/seq` Testcontainer + Serilog sink + `SeqAssert.Query(...)` DSL + dashboard-snapshot capture to `TestResults/seq-dashboards/`.
5. `Security` base + `ISecurityRig`.
6. `.Jwt` — `JwtBuilder` (HS256/RS256, kid rotation, expired/tampered/not-yet-valid variants, JWKS endpoint stub).
7. `.OAuth` — `MockOAuthServer` (`/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration`, client credentials, auth code + PKCE, refresh token).
8. `Http` — in-proc WireMock-style + matchers + scenario state machine + record/replay + `DelegatingHandler` variant.
9. `Resilience` — `FakeTimeProvider` + Polly assertions (retry, circuit, rate-limit, bulkhead).

**Phase B merge gate**:
- [ ] Anti-pattern detector fails on all documented violations in self-test.
- [ ] Real `JwtBearerHandler` integration — zero-bypass verified.
- [ ] HTTP mock passes matcher/scenario/replay matrix.
- [ ] Polly tests advance via `FakeTimeProvider` deterministically (no `Task.Delay`).
- [ ] Coverage + architecture gate met per package.

### Phase C — Microservice Patterns + Concurrency/Health + Memory Cache

**Scope**: 7 new packages.

**Packages**: `Microservices.Outbox`, `.Inbox`, `.EventSourcing`, `.Snapshots`, `Concurrency`, `HealthChecks`, `Caching.Memory`.

**Execution order**:
1. `Caching.Memory` — fastest provider, completes the caching base test matrix (stampede, tag, fail-safe, negative, coherency N/A for single-node).
2. `Concurrency` — `ConcurrencyAssert` + cross-provider contracts (SqlServer + Postgres-if-available + Cosmos-if-available + Mongo-if-available) for `TwoWriters` + `If-Match 412` + `If-None-Match 304` + sequence idempotency.
3. `HealthChecks` — `HealthAssert` + dependency-down simulator + live/ready/startup probe distinction.
4. `Microservices.Outbox` — `OutboxFixture` composed over `Databases` + `Messaging` bases; `OutboxRelaySimulator`; `OutboxAssert` (Contains, ExactlyOnce, Relayed, InDeadLetter, Replayed).
5. `Microservices.Inbox` — sequence tracker + `InboxAssert`.
6. `Microservices.EventSourcing` — `When/Then` aggregate harness, event catalogue, schema evolution.
7. `Microservices.Snapshots` — Verify-compatible on-disk format (C-003), `SnapshotAssert.Match`, scrubbers (correlation/causation/event IDs, timestamps, sequence numbers, connection strings, paths), diff tool hooks.

**Phase C merge gate**:
- [ ] Outbox `ExactlyOnce` under 100 concurrent relay runs across SqlServer+ServiceBus matrix.
- [ ] Snapshot format Verify-compatible — round-trip test with real Verify.TUnit on same files.
- [ ] Concurrency contract GREEN on available providers.
- [ ] Health probes distinguish live/ready/startup.

### Phase D — Provider Expansion

**Scope**: 10 new providers.

**Packages**: `Databases.Sql.Postgresql`, `.MySql`; `Databases.NoSql.Cosmos`, `.Mongo`; `Messaging.Kafka`, `.RabbitMq`; `Caching.Hybrid`, `.Fusion`; `Storage` (base), `Storage.AzureBlob`, `Storage.S3`.

**Execution order**: one provider at a time, each RED-GREEN-REFACTOR. All inherit existing bases — provider-specific code ≤ ~200 LOC.

**Phase D merge gate**:
- [ ] Every new provider passes base contract + 3 quirk tests.
- [ ] CI matrix GREEN: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x.
- [ ] Three-way SQL fast-path parity: `DbContextHelper` CRUD contract passes against EF InMemory, Sqlite, SqlServer, Postgresql, MySql.

### Phase E — Polish

**Scope**: ~18 new packages (remaining providers + tooling).

**Packages**: `Docker`, `Parallelism`, `Ci`, `Observability.Metrics`, `.AppInsights`, `Security.Mtls`, `.Policies`, `Microservices.Saga`, `.Contracts`, `Databases.Sql.Oracle`, `Databases.NoSql.Dynamo`, `.Cassandra`, `.EventStore`, `.ElasticSearch`, `Messaging.Sqs`, `.Nats`, `Storage.MinIO`, `.FileSystem`.

**Execution order**: tooling (Docker / Parallelism / Ci) first since other packages consume them; remaining providers in parallel tracks.

**Phase E merge gate**:
- [ ] Full ~50-package ecosystem builds + tests GREEN.
- [ ] BenchmarkDotNet within regression budget (< 110% of 002-baseline).
- [ ] All docs complete; meta-packages composed; README per package.
- [ ] Version bump to 2.0.0 across all packages (lockstep minor).

---

## Technology Choices & Rationale

| Concern | Choice | Why |
|---|---|---|
| Target framework | `net10.0` | Matches existing; latest LTS-track. |
| Test framework | TUnit 1.34.5+ | Pre-existing; modern C# parallelism model. |
| Containers | Testcontainers 4.6.0+ | Pre-existing; aligned across providers. |
| Central Package Management | `Directory.Packages.props` | Lockstep minor bumps; single source of truth. |
| Time abstraction | `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider` | Standard; enables deterministic TTL/retry testing. |
| JWT | `Microsoft.IdentityModel` + `System.IdentityModel.Tokens.Jwt` 8.x | Same library used by `Microsoft.AspNetCore.Authentication.JwtBearer`. |
| OTEL | `OpenTelemetry` 1.9.x + `OpenTelemetry.Exporter.InMemory` | In-memory exporter is test-native. |
| Serilog → Seq | Serilog 4.x + `Serilog.Sinks.Seq` 8.x | Aligns with `observability.md` structured-logging rule. |
| Architecture tests | `NetArchTest.Rules` 1.x | Industry-standard for `.NET`. |
| Benchmarks | `BenchmarkDotNet` 0.14.x | Pre-existing; regression budget baselined from Phase 002. |
| Redis | `StackExchange.Redis` 2.8.x | Pre-existing. |
| HybridCache | `Microsoft.Extensions.Caching.Hybrid` 9.x | `.NET 9+` native. |
| FusionCache | `ZiggyCreatures.FusionCache` 2.x | Covers fail-safe, eager refresh, tagging beyond HybridCache. |
| Fakers | `Bogus` 35.x | Pre-existing via `CustomConstructorFaker`. |
| SNS/SQS/DynamoDB | LocalStack | Single emulator for AWS services. |
| Azurite | `mcr.microsoft.com/azure-storage/azurite` | Official Azure Blob emulator. |
| ServiceBus | `mcr.microsoft.com/azure-messaging/servicebus-emulator` + SQL Edge | Per C-001. |
| Snapshot format | Verify-compatible | Per C-003. |
| Filesystem | `System.IO.Abstractions` 21.x | Mockable filesystem for `Storage.FileSystem`. |

---

## TDD Execution Discipline (every phase, every class)

Every commit on `feat/003-*` branches MUST exhibit RED → GREEN → REFACTOR cadence. Enforcement:

1. **Commit-message prefixes** (enforced by hook/reviewer):
   - `test: red — {behavior}` — failing test lands first.
   - `feat: green — {behavior}` — minimum code to pass.
   - `refactor: {improvement}` — structural change, no behavior change.
2. **PR template checklist** — reviewer confirms RED/GREEN/REFACTOR presence per new class.
3. **Architecture test — `PublicTypeHasTest`** — every public type in `src/` MUST have a referencing test assembly. Violations fail the build.
4. **Coverage gate** — line ≥ 90%, branch ≥ 85% per package. Enforced in CI via `dotnet test --collect:"XPlat Code Coverage"` + coverage-delta enforcer.
5. **Contract-suite gate** — every provider's `Tests.Integration` project MUST inherit its base's abstract contract test and implement all 13 mandatory methods + ≥ 3 quirk tests.
6. **Parallel-isolation gate** — every provider fixture MUST pass the shared `ParallelIsolationContract` (20 parallel, zero collisions).

---

## Testing Strategy

### Test-project layout per source package

```
tests/Rig.TUnit.X.Tests.Unit         ← pure logic, builders, assertions (every package)
tests/Rig.TUnit.X.Tests.Contract     ← abstract contract class (base packages only)
tests/Rig.TUnit.X.Tests.Integration  ← real containers/services (provider packages only)
tests/Rig.TUnit.Architecture.Tests   ← single cross-cutting — NetArchTest rules
tests/Rig.TUnit.Benchmarks           ← expanded BenchmarkDotNet suite (existing)
```

### Contract test pattern

```csharp
// Rig.TUnit.Databases.Sql.Tests.Contract/SqlRigContract.cs
public abstract class SqlRigContract
{
    protected abstract ValueTask<ISqlRig> CreateRigAsync(CancellationToken ct);

    [Test] public async Task Fixture_InitializeAsync_IsIdempotent() { ... }
    [Test] public async Task Fixture_DisposeAsync_IsSafeToCallTwice() { ... }
    [Test] public async Task Builder_UseContainer_ResolvesConnectionSource() { ... }
    // ... 13 total
}

// Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SqlServerContract.cs
public sealed class SqlServerContract : SqlRigContract
{
    protected override async ValueTask<ISqlRig> CreateRigAsync(CancellationToken ct)
        => await new SqlServerRigBuilder().BuildAsync(ct);

    // Provider-specific quirks (min 3)
    [Test] public async Task Rowversion_Column_IsBinary8() { ... }
    [Test] public async Task DateTimeOffset_IsNative() { ... }
    [Test] public async Task SequentialGuid_SupportsNewSequentialId() { ... }
}
```

### Shared parallel-isolation smoke

```csharp
// Rig.TUnit.Parallelism.Tests.Contract/ParallelIsolationContract.cs
public abstract class ParallelIsolationContract
{
    protected abstract ValueTask<IRigFixture> CreateFixtureAsync(CancellationToken ct);

    [Test, Timeout(60_000)]
    [ParallelLimiter<Unlimited>] // 20 parallel
    public async Task IsolationKey_PerTest_DoesNotCollide() { ... }
}
```

Every provider's integration test class inherits both `XxxContract` and `ParallelIsolationContract`.

### Assertion DSL test matrix

For every `XxxAssert` method, 5 tests are mandatory:
- Positive (assertion holds)
- Negative (fails with expected structured message)
- Boundary (near-miss)
- Async/timeout (eventual consistency)
- Cancellation (`CancellationToken` honored)

### Fast-path parity tests (Phase A)

```csharp
// Abstract CRUD contract for DbContextHelper
public abstract class DbContextHelperCrudContract<TFixture> where TFixture : ISqlRig
{
    [Test] public async Task Insert_ReturnsTracked() { ... }
    [Test] public async Task Query_ReturnsFiltered() { ... }
    [Test] public async Task Update_PersistsChanges() { ... }
    [Test] public async Task Delete_Removes() { ... }
    [Test] public async Task Seed_BatchesInScope() { ... }
    [Test] public async Task WithTransactionAsync_RollbackByDefault() { ... }
}

// Three concrete implementations
public sealed class EfInMemoryDbHelperTests : DbContextHelperCrudContract<...> { }
public sealed class SqliteDbHelperTests : DbContextHelperCrudContract<...> { }
public sealed class SqlServerDbHelperTests : DbContextHelperCrudContract<...> { }
```

### Dockerless-CI fallback

Tests tagged `[EnabledOnDocker]` skip when no Docker daemon is available. Unit + Contract + anti-pattern tests still run. CI matrix publishes a "Docker-available" matrix leg and a "no-Docker" leg; both must pass for merge.

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| ServiceBus emulator image-pull flakiness in CI | Medium | High | Retry policy in `ServiceBusFixture`; cache image per runner; `[EnabledOnDocker]` skip for unreachable days. |
| Cosmos emulator Linux image ARM/AMD incompatibility | High | Medium | Use `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator` Linux variant; document ARM limitations; skip Cosmos on ARM runners. |
| Coverage gate drift as new code lands faster than tests | Medium | High | `Rig.TUnit.Ci` coverage-delta enforcer in every PR; reject PRs below gate. |
| Version drift between Directory.Build.props and Directory.Packages.props | Low | High | CPM eliminates duplicate version declarations; architecture test verifies all projects consume CPM. |
| Architecture tests flagging legitimate edge cases | Medium | Low | Whitelist file per rule (`architecture-whitelist.txt`) reviewed quarterly. |
| 50 packages → 50 NuGet feeds on publish | High | Medium | Out of scope for this spec (publish is future work); placeholder stub until then. |
| Snapshot format divergence if Verify changes | Low | Medium | Pin Verify version in `Directory.Packages.props`; test round-trip against pinned version; unpin only with a deliberate major. |
| PII detector false positives breaking downstream projects | Medium | Medium | C-005 resolution: additive-only, no allowlist — downstream projects rename the property. Document canonical list + regex extension point prominently in README. |
| 20-parallel isolation collisions under real-world CI load | Low | High | `IsolationKey` uses SHA256(test-method-full-name).8-chars; collision probability ~1 in 2³² (vanishingly small). `ParallelIsolationContract` catches regressions. |
| Benchmark regression budget exceeded by new provider overhead | Medium | Medium | Per-phase benchmark run; refactor before merge if > 110%. |
| `Rig.TUnit.All` meta-package bloat | High | Low | README warning; no Phase A-B consumption; teams opt-in via Microservices or specific packages. |
| Docker-less CI falls behind Docker-available CI in coverage | Medium | Medium | Separate coverage gates for each leg; architecture + unit tests alone must hit ≥ 80% (not 90%) on the no-Docker leg. |
| Interpolated-template detector triggering on Serilog's `LogInformation("{@Payload}", obj)` destructuring | Low | Low | Detector checks only `string` literals starting with `$"`; destructuring syntax (`{@…}`) is allowed. Unit test covers this case. |

---

## Dependencies & External Surfaces

| External Surface | Impact | Notes |
|---|---|---|
| Testcontainers-dotnet | HIGH | Every Integration test depends on it. Pin 4.6.0+. |
| Microsoft ServiceBus emulator image | HIGH | EULA acceptance required in CI. |
| `datalust/seq` image | MEDIUM | Observability.Seq fixture. |
| Azurite / LocalStack / MinIO images | MEDIUM | Storage providers. Phase D. |
| Cosmos emulator Linux image | MEDIUM | Phase D. |
| Kafka `confluentinc/cp-kafka` + Zookeeper | MEDIUM | Phase D. |
| EventStore, Cassandra, Elastic, NATS images | LOW | Phase E. |
| Serilog + Seq sink | LOW | Phase B. |
| OpenTelemetry in-memory exporter | LOW | Phase B. |
| Polly | MEDIUM | Phase B Resilience. |
| Verify.TUnit | MEDIUM | Phase C snapshot format compatibility (round-trip test). |

---

## Definition of Done

1. [ ] All ~50 packages build with `dotnet build` — ZERO warnings.
2. [ ] Full solution `dotnet test` GREEN; coverage gate met per package (line ≥ 90%, branch ≥ 85%).
3. [ ] `Rig.TUnit.Architecture.Tests` suite GREEN; zero circular deps; no rule violations.
4. [ ] `BenchmarkDotNet` suite within regression budget (< 110% of 002 baseline).
5. [ ] Old packages (`Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus`) DELETED; `Rig.TUnit.slnx` clean.
6. [ ] Every package ships README + example test (generated per-phase: T159/T289/T369/T469/T719).
7. [ ] Every public API has XML docs.
8. [ ] CI matrix GREEN: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x.
9. [ ] 56 pre-existing tests ported + GREEN; final count expected several hundred.
10. [ ] Spec → task → test → source traceability produced by `/dai.tasks`.
11. [ ] Every feature-branch commit exhibits RED → GREEN → REFACTOR cadence.
12. [ ] All 5 clarifications (C-001..C-005) implemented literally.

---

## Next Commands

- `/dai.analyze` — validate plan-spec-task consistency before task generation.
- `/dai.tasks` — expand this plan into phase-by-phase, RED/GREEN/REFACTOR-tagged executable task list.
- `/dai.go` — execute tasks with merge-gate enforcement per phase.
