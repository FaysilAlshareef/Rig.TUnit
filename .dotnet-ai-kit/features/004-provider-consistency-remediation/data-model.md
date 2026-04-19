# Data Model: Provider Consistency Remediation

**Feature ID**: 004-provider-consistency-remediation
**Generated**: 2026-04-18

No database entities (Rig.TUnit is a test library). This file catalogs the **types and files** each provider must expose, plus the architecture-test rule invariants.

---

## E1 — Provider package (canonical shape)

Every `Rig.TUnit.{Family}.{Provider}` package exposes this surface:

| File | Type | Purpose | Enforcement |
|------|------|---------|-------------|
| `{Provider}.csproj` | MSBuild project | Target `net10.0`, ProjectReference to `Rig.TUnit.{Family}` base | Phase 4 per-package gate |
| `README.md` | Markdown | 30-sec quick-start, > 100 chars | `ReadmeCompletenessTests` (FR-003) |
| `Fixtures/{Provider}Fixture.cs` | `sealed class` | Inherits `{Family}FixtureBase`; wraps Testcontainer (or in-process) | `ProviderCompletenessTests` (FR-001) + `CodeOrganizationTests.AllFixtures_ExtendFixtureBase` |
| `Options/{Provider}FixtureOptions.cs` | `sealed class` | `public const string SectionName`; `[Required]` on mandatory props; default values for common props | `ProviderCompletenessTests` (FR-001) |
| `Builder/{Provider}RigBuilder.cs` | `sealed class` | CRTP: `: {Family}RigBuilder<{Provider}RigBuilder>` | `ProviderCompletenessTests` (FR-001) |
| `Builder/{Provider}RigBuilderExtensions.cs` | `static class` | Public `Use{Provider}(this RigBuilder, ...)` returning `RigBuilder` | `ProviderCompletenessTests` (FR-001) |
| `Extensions/` (SQL only) | `static class` | EF-provider wire-up (`UseMySql`, `UseOracle`, etc.) | Convention — sealed static |
| `Helpers/` | varied | Family-specific helpers per design §4.x | Family-specific |

---

## E2 — Family base package (existing, unchanged)

Each family already ships a base package containing its CRTP builder, fixture base, and shared assertions/helpers. **This feature does NOT modify them.**

| Family | Base package | CRTP builder | Fixture base |
|--------|--------------|--------------|--------------|
| Databases.Sql | `Rig.TUnit.Databases.Sql` | `SqlRigBuilder<TSelf>` | `SqlFixtureBase` |
| Databases.NoSql | `Rig.TUnit.Databases.NoSql` | `NoSqlRigBuilder<TSelf>` | `DocumentFixtureBase` |
| Messaging | `Rig.TUnit.Messaging` | `MessagingRigBuilder<TSelf>` | `MessagingFixtureBase` |
| Caching | `Rig.TUnit.Caching` | `CacheRigBuilder<TSelf>` | `CacheFixtureBase` |
| Storage | `Rig.TUnit.Storage` | `StorageRigBuilder<TSelf>` | `StorageFixtureBase` |
| Security | `Rig.TUnit.Security` | `SecurityRigBuilder<TSelf>` | `SecurityFixtureBase` |
| Observability | `Rig.TUnit.Observability` | `TelemetryRigBuilder<TSelf>` | `TelemetryFixtureBase` |

**Base CRTP contract** (common shape across all 7 families, verified from source):

```csharp
public abstract class {Family}RigBuilder<TSelf> where TSelf : {Family}RigBuilder<TSelf>
{
    protected {Family}RigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }
    public RigBuilder And() => Root;
}
```

SQL additionally adds `ReplaceDbContext<TContext>()`, promoted to the base.

---

## E3 — Per-provider inventory (Phase 3 + Phase 4 targets)

### E3.0 Databases.Sql (Phase 3 — existing-provider remediation)

| Provider | Options | Builder | BuilderExtensions (add?) | EF BuilderExtensions (add?) | README |
|----------|---------|---------|--------------------------|------------------------------|--------|
| SqlServer | Present | Present | Present | Present | Present ✓ |
| Sqlite | Present | Present | Present | Present | Missing — add |
| Postgresql | Present | Present (`PostgresRigBuilder`) | **Add** `PostgresRigBuilderExtensions` (`UsePostgres` fluent) | **Add** `PostgresBuilderExtensions` (`UsePostgresInMemory`-style EF quickstart per design §4.1) | **Add** README |

### E3.a Databases.NoSql (Phase 3)

| Provider | Options (add?) | Builder (add?) | Extensions (add?) | Family-specific helpers (add) |
|----------|---------------|----------------|-------------------|-------------------------------|
| Mongo | Present | **Add** `MongoRigBuilder` | **Add** `UseMongo` | `CollectionPerTestHelper`, `BsonDiff` |
| Cassandra | **Add** `CassandraFixtureOptions` | **Add** `CassandraRigBuilder` | **Add** `UseCassandra` | `KeyspacePerTestHelper` |
| Dynamo | **Add** `DynamoFixtureOptions` | **Add** `DynamoRigBuilder` | **Add** `UseDynamo` | `GsiVerifier` (LocalStack) |
| ElasticSearch | **Add** `ElasticSearchFixtureOptions` | **Add** `ElasticSearchRigBuilder` | **Add** `UseElasticSearch` | `IndexRefreshHelper`, `DslAssert` |
| KurrentDb (was EventStore) | **Add** `KurrentDbFixtureOptions` | **Add** `KurrentDbRigBuilder` | **Add** `UseKurrentDb` | `StreamAssert`, `ProjectionAssert` — built against `KurrentDB.Client 1.3.x` (drop-in replacement for the obsolete `EventStore.Client.Grpc.Streams`). Package renamed in Phase 1 T002c per the upstream KurrentDB rebrand. |

### E3.b Messaging (Phase 3)

| Provider | Options (add?) | Builder (add?) | Extensions (add?) | Listener / EventSender |
|----------|---------------|----------------|-------------------|------------------------|
| Kafka | Present | **Add** `KafkaRigBuilder` | **Add** `UseKafka` | `KafkaListener : ListenerBase`, `KafkaEventSender : EventSenderBase` |
| RabbitMq | Present | **Add** `RabbitMqRigBuilder` | **Add** `UseRabbitMq` | `RabbitMqListener`, `RabbitMqEventSender` |
| Nats | **Add** `NatsFixtureOptions` | **Add** `NatsRigBuilder` | **Add** `UseNats` | `NatsListener`, `NatsEventSender` |
| Sqs | **Add** `SqsFixtureOptions` | **Add** `SqsRigBuilder` | **Add** `UseSqs` | `SqsListener`, `SqsEventSender` (LocalStack) |

### E3.c Caching (Phase 3)

| Provider | Options (add?) | Builder (add?) | Extensions (add?) | Helpers |
|----------|---------------|----------------|-------------------|---------|
| Memory | N/A | Present | **Add** `UseMemoryCache` | N/A |
| Hybrid | **Add** `HybridCacheFixtureOptions` | **Add** `HybridCacheRigBuilder` | **Add** `UseHybridCache` | — |
| Fusion | **Add** `FusionCacheFixtureOptions` | **Add** `FusionCacheRigBuilder` | **Add** `UseFusionCache` | fail-safe helper, eager-refresh helper |

### E3.d Storage (Phase 3)

| Provider | Options (add?) | Builder (add?) | Extensions (add?) | Helpers |
|----------|---------------|----------------|-------------------|---------|
| AzureBlob | Present | **Add** `AzureBlobRigBuilder` | **Add** `UseAzureBlob` | `AzureBlobSasBuilder` |
| S3 | Present | **Add** `S3RigBuilder` | **Add** `UseS3` | `S3SasBuilder` |
| MinIO | **Add** `MinIOFixtureOptions` | **Add** `MinIORigBuilder` | **Add** `UseMinIO` | `MinIOSasBuilder` |
| FileSystem | **Add** `FileSystemFixtureOptions` | **Add** `FileSystemRigBuilder` | **Add** `UseFileSystem` | `PathSandboxHelper` (N/A for SAS) |

### E3.e Security (Phase 3)

| Provider | Fixture (add?) | Options (add?) | RigBuilder (add?) | Extensions (add?) | Notes |
|----------|---------------|---------------|-------------------|-------------------|-------|
| Jwt | — | Present | **Add** `JwtRigBuilder` | **Add** `UseJwt` | Keep existing `JwtBuilder` (token builder) untouched |
| OAuth | — | Present | **Add** `OAuthRigBuilder` | **Add** `UseOAuthServer` | Wraps existing `MockOAuthServer` |
| Mtls | **Add** `MtlsFixture` (generates CA + leaf) | **Add** `MtlsFixtureOptions` | **Add** `MtlsRigBuilder` | **Add** `UseMtls` | Keep existing `MtlsCertificateBuilder` as helper |
| Policies | **Add** `PolicyFixture` (in-memory `IAuthorizationService`) | **Add** `PolicyFixtureOptions` | **Add** `PolicyRigBuilder` | **Add** `UsePolicies` | Keep existing `PolicyAssert` |

### E3.f Observability (Phase 3)

| Provider | Fixture (add?) | Options (add?) | RigBuilder (add?) | Extensions (add?) | Helpers |
|----------|---------------|---------------|-------------------|-------------------|---------|
| Metrics | **Add** `MetricsFixture` (wraps `MeterListener`) | **Add** `MetricsFixtureOptions` | **Add** `MetricsRigBuilder` | **Add** `UseMetricsCapture` | `TagCardinalityGuard` |

### E3.g New packages (Phase 4) — full canonical shape

| Package | Required types |
|---------|---------------|
| `Rig.TUnit.Databases.Sql.MySql` | `MySqlFixture`, `MySqlFixtureOptions`, `MySqlRigBuilder`, `MySqlRigBuilderExtensions`, `MySqlBuilderExtensions` (EF wire), `README.md` |
| `Rig.TUnit.Databases.Sql.Oracle` | `OracleFixture`, `OracleFixtureOptions`, `OracleRigBuilder`, `OracleRigBuilderExtensions`, `OracleBuilderExtensions` (EF wire), `README.md` |
| `Rig.TUnit.Databases.NoSql.Cosmos` | `CosmosFixture`, `CosmosFixtureOptions`, `CosmosRigBuilder`, `CosmosRigBuilderExtensions`, `RuChargeCapture`, `PartitionKeyDistributionChecker`, `README.md` |
| `Rig.TUnit.Observability.AppInsights` | `AppInsightsFixture`, `AppInsightsFixtureOptions`, `AppInsightsRigBuilder`, `AppInsightsRigBuilderExtensions`, `AppInsightsAssert`, `CapturingTelemetryChannel` (internal), `README.md` |
| `Rig.TUnit.Docker` (complete template) | `DockerFixtureOptions`, `DockerRigBuilder`, `DockerRigBuilderExtensions`, `DockerComposeFixture`, `README.md` (ContainerFixture already present) |

### E3.h Microservices depth (Phase 5)

| Package | Types to add |
|---------|--------------|
| `Rig.TUnit.Microservices.EventSourcing` | `AggregateAssert` (fluent: `.Raised<TEvent>().WithData(predicate)`), `EventCatalogueVerifier`, `SchemaEvolutionHelper` |
| `Rig.TUnit.Microservices.Saga` | `SagaAssert` (fluent: `.Step(name).Compensated()`), `SagaTimeoutHelper` |
| `Rig.TUnit.Microservices.Contracts` | `ProviderVerificationFixture`, `PactBrokerClientStub` (file-based per C-002) |

---

## E4 — Options class pattern

Every `{Provider}FixtureOptions` follows this shape (proven by `SqlServerFixtureOptions.cs`):

```csharp
using System.ComponentModel.DataAnnotations;
namespace Rig.TUnit.{Family}.{Provider}.Options;

public sealed class {Provider}FixtureOptions
{
    public const string SectionName = "RigTUnit:{Provider}";

    [Required]
    public string ImageTag { get; init; } = "{default}";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;

    // provider-specific properties with [Required] + sensible defaults
}
```

**Registration pattern** (from `.claude/rules/configuration.md`):

```csharp
services.AddOptions<{Provider}FixtureOptions>()
    .BindConfiguration({Provider}FixtureOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## E5 — RigBuilder pattern

Every `{Provider}RigBuilder` follows the exemplar from `SqlServerRigBuilder.cs`:

```csharp
public sealed class {Provider}RigBuilder : {Family}RigBuilder<{Provider}RigBuilder>
{
    public {Provider}RigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    // provider-specific fluent methods returning this
    // (for SQL, override UseProvider(DbContextOptionsBuilder, string))
}
```

---

## E6 — Use-extension pattern

Every `{Provider}RigBuilderExtensions` exposes a single public entry point, proven by `SqlServerRigBuilderExtensions.cs`:

```csharp
public static class {Provider}RigBuilderExtensions
{
    public static RigBuilder Use{Provider}(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<{Provider}RigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new {Provider}RigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
```

Some families (Caching, Security, Observability) omit the `IRigConnectionSource source` parameter — follow the family's existing convention.

---

## E7 — Architecture-test rule invariants

### E7.a `ProviderCompletenessTests` (FR-001)

For each assembly matching `Rig.TUnit.{Family}.{Provider}` (excluding analyzer and base assemblies):

- `IsClass && !IsAbstract && Name.EndsWith("Fixture")` MUST inherit `{Family}FixtureBase`
- `IsClass && !IsAbstract && Name.EndsWith("FixtureOptions")` MUST have `public const string SectionName`
- `IsClass && IsSealed && Name.EndsWith("RigBuilder") && !Name.EndsWith("RigBuilderExtensions")` MUST inherit `{Family}RigBuilder<{Self}>`
- Some `static class Name.EndsWith("RigBuilderExtensions")` MUST declare public static method `Use{Provider}` with `this RigBuilder` as the first parameter

**Exclusions:**
- `Rig.TUnit.Observability.Logging.Analyzers` — Roslyn analyzer, not a runtime fixture
- `Rig.TUnit.Databases.NoSql.Redis` — consumes shared `RedisFixture` from `Rig.TUnit.Caching.Redis`; the rule walks project references to accept an external fixture

### E7.b `TestFileOrganizationTests` (FR-002, C-003)

For each `*.cs` file under `tests/**/*.cs`:

- **Excluded folders** (file may declare multiple top-level types): `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, `obj/`, `bin/`
- **All other files**: MUST declare exactly one top-level class
- **Rule applies uniformly to `*Contract.cs`** (per C-003) — contract fixtures with inline helper types MUST extract those to `TestInfrastructure/ContractHelpers/`

Implementation: walk filesystem, read file, count top-level class declarations (regex or Roslyn syntax tree — prefer regex for simplicity per 003 pattern).

### E7.c `ReadmeCompletenessTests` (FR-003)

For each directory matching `src/Rig.TUnit.{Family}.{Provider}/`:

- Directory MUST contain `README.md`
- File MUST be > 100 chars (measured by `File.ReadAllText(path).Length > 100`)

Base packages (`src/Rig.TUnit.Databases.Sql/`, `src/Rig.TUnit.Messaging/`, etc.) and the root `src/Rig.TUnit/` are included because every user-facing package needs a quick-start.

---

## E8 — TestInfrastructure folder contract

Proven by `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/`. Contents:

| File name pattern | Purpose |
|-------------------|---------|
| `{Project}TestHarness.cs` | Shared `[Before]` / `[After]` setup for tests in the project |
| `Test{Entity}.cs` | Test-only DTO or entity (e.g., a DbContext entity used only for integration tests) |
| `Test{Handler}.cs` | Test-only handler stub for CQRS / mediator tests |
| `Fake{Xxx}.cs` | NSubstitute-backed or hand-rolled fakes |
| `ContractHelpers/` (new for C-003) | Helper types extracted from `*Contract.cs` files |
| `Pacts/*.json` (new for C-002) | Pact files for `ProviderVerificationFixture` |

**Rule:** test `[Test]` methods live in the project root or feature subfolders; `TestInfrastructure/` contains ONLY non-test types.

---

## Cross-references

- FR-001 ↔ E7.a
- FR-002 ↔ E7.b
- FR-003 ↔ E7.c
- FR-005 ↔ E1, E3
- FR-006 ↔ E3.b
- FR-007 ↔ E3.d
- FR-008 ↔ E3.e
- FR-009 ↔ E3.f
- FR-010 / FR-011 / FR-012 ↔ E8
- FR-013 / FR-014 / FR-015 / FR-016 / FR-017 ↔ E3.g
- FR-018 / FR-019 / FR-020 ↔ E3.h
