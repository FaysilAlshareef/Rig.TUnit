# Rig.TUnit — Provider Consistency Remediation — Session Handoff

## What this is

The complete implementation handoff for the provider-consistency-remediation feature. Pair this with `Rig.TUnit-Library-Design.md` + `Rig.TUnit-Build-Prompt.md` in the same directory to execute the work in a fresh session.

**Scope:** bring every provider package up to a uniform shape, add the 5 packages 003 promised but never created (`Cosmos`, `MySql`, `Oracle`, `AppInsights`, `Docker`), add `SecurityRigBuilder<TSelf>` base, land architecture tests that enforce uniformity, and extract inline test-setup infrastructure into `TestInfrastructure/` folders.

**Prerequisite state:** feature 003 (ecosystem expansion) merged at commit `1534649` (272/274 tasks, 219 tests green). This handoff extends — it does not rewrite — what 003 delivered. Branch off `master` as `feat/provider-consistency-remediation`.

---

## Version pins (add/modify in `Directory.Packages.props`)

Add:
- `Pomelo.EntityFrameworkCore.MySql` — **9.0.\*** (comment: "Pomelo 10 tracked in PR #2019; 9.x runs on .NET 10 TFM")
- `MySqlConnector` — 2.4.\*
- `Testcontainers.MySql` — 4.11.\*
- `Oracle.EntityFrameworkCore` — 9.23.90+
- `Testcontainers.Oracle` — 4.11.\*
- `Microsoft.Azure.Cosmos` — latest 3.x
- `Microsoft.ApplicationInsights` — 2.22+
- `System.Security.Cryptography.X509Certificates` — (use .NET 10 built-in; confirm no separate pin needed)

Keep (already pinned by 003): `Testcontainers` 4.11+, `TUnit` 1.34.5+, `NetArchTest.Rules` 1.x.

---

## Phase 1 — Enforcement scaffolding (land FIRST, failing)

Write these architecture tests **before** any provider is completed so they fail for every gap and go green as each is fixed.

### New files under `tests/Rig.TUnit.Architecture.Tests/Rules/`

- [ ] `ProviderCompletenessTests.cs` — for each `Rig.TUnit.{Family}.{Provider}` assembly, assert presence of:
  - Type matching `{Provider}Fixture` deriving from the family's fixture base.
  - Type matching `{Provider}FixtureOptions` with `public const string SectionName`.
  - Type matching `{Provider}RigBuilder` deriving from `{Family}RigBuilder<{Provider}RigBuilder>`.
  - Extension method matching `Use{Provider}` on `RigBuilder`.
- [ ] `TestFileOrganizationTests.cs` — fails if any file under `tests/**/*.cs` outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, `obj/`, `bin/` declares >1 top-level class.
- [ ] `ReadmeCompletenessTests.cs` — fails if any `src/Rig.TUnit.{Family}.{Provider}/` directory lacks a `README.md` > 100 chars.

### Foundation source

- [ ] `src/Rig.TUnit.Security/Contracts/ISecurityRig.cs` (new).
- [ ] `src/Rig.TUnit.Security/Fixtures/SecurityFixtureBase.cs` (new).
- [ ] `src/Rig.TUnit.Security/Builder/SecurityRigBuilder.cs` (new, CRTP).
- [ ] `src/Rig.TUnit.Security/Rig.TUnit.Security.csproj` (new — the base package doesn't exist yet; create it).

### Documentation

- [ ] `src/Rig.TUnit/Contributing-ProviderTemplate.md` — canonical template for new providers.

---

## Phase 2 — Test hygiene sweep

Move inline infrastructure into `TestInfrastructure/` subfolders. Target files (worst offenders first):

- [ ] `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/`
  - Create `TestInfrastructure/TracingTestHarness.cs` — move `ActivitySource`, `TracerProvider` factory helpers.
  - `TraceAssertTests.cs` contains only `[Test]` methods.
- [ ] `tests/Rig.TUnit.Http.Tests.Unit/`
  - Create `TestInfrastructure/HttpMockTestHarness.cs` — move matchers, response builder helpers.
- [ ] `tests/Rig.TUnit.Resilience.Tests.Integration/`
  - Create `TestInfrastructure/ResiliencePipelines.cs` — Polly pipeline builders.
- [ ] `tests/Rig.TUnit.Security.OAuth.Tests.Integration/`
  - Create `TestInfrastructure/OAuthTestHarness.cs` — JWKS + RSA key helpers.
- [ ] `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/`
  - Create `TestInfrastructure/OutboxTestData.cs` — seed builders, envelope fakers, custom stores.
- [ ] Scan every `*QuirkTests.cs`, `*Contract.cs`, `*Tests.cs` file in `tests/` for inline entities/fake handlers; move to `TestInfrastructure/`.
- [ ] Flip `TestFileOrganizationTests` from `SkipUntilFixed` to enforced.

---

## Phase 3 — Fill gaps in existing providers

### Databases.NoSql

Each gets `Fixtures/` (already there), `Options/{Provider}FixtureOptions.cs` (if missing), `Builder/{Provider}RigBuilder.cs`, `Builder/{Provider}RigBuilderExtensions.cs`, `Helpers/` per 003 §4.4, `README.md`.

- [ ] `Rig.TUnit.Databases.NoSql.Mongo` — add builder + extensions + `CollectionPerTestHelper` + `BsonDiff`.
- [ ] `Rig.TUnit.Databases.NoSql.Cassandra` — add options + builder + extensions + `KeyspacePerTestHelper`.
- [ ] `Rig.TUnit.Databases.NoSql.Dynamo` — add options + builder + extensions + `GsiVerifier` (LocalStack).
- [ ] `Rig.TUnit.Databases.NoSql.ElasticSearch` — add options + builder + extensions + `IndexRefreshHelper` + `DslAssert`.
- [ ] `Rig.TUnit.Databases.NoSql.KurrentDb` — add options + builder + extensions + `StreamAssert` + `ProjectionAssert`. (Package was renamed from `Rig.TUnit.Databases.NoSql.EventStore` in Phase 1 T002c per the upstream Kurrent rebrand — `Testcontainers.KurrentDb 4.11.0` + `KurrentDB.Client 1.3.1`, namespace `.KurrentDb`, class `KurrentDbFixture`, image `kurrentplatform/kurrentdb:25.1`.)
- [ ] Integration tests for every provider pass `NoSqlRigContract`.

### Messaging

- [ ] `Rig.TUnit.Messaging.Kafka` — add builder + extensions + `KafkaListener : ListenerBase` + `KafkaEventSender : EventSenderBase`.
- [ ] `Rig.TUnit.Messaging.RabbitMq` — add builder + extensions + `RabbitMqListener` + `RabbitMqEventSender`.
- [ ] `Rig.TUnit.Messaging.Nats` — add options + builder + extensions + `NatsListener` + `NatsEventSender`.
- [ ] `Rig.TUnit.Messaging.Sqs` — add options + builder + extensions + `SqsListener` + `SqsEventSender` (LocalStack).
- [ ] Every provider's integration test passes `MessagingRigContract`.

### Caching

- [ ] `Rig.TUnit.Caching.Memory` — add `UseMemoryCache` extension (options not required — memory cache is parameterless).
- [ ] `Rig.TUnit.Caching.Hybrid` — add `HybridCacheFixtureOptions`, `HybridCacheRigBuilder`, `UseHybridCache` extension.
- [ ] `Rig.TUnit.Caching.Fusion` — add `FusionCacheFixtureOptions`, `FusionCacheRigBuilder`, `UseFusionCache` extension, fail-safe & eager-refresh helpers per 003 §4.6.
- [ ] All pass `CacheRigContract`.

### Storage

- [ ] `Rig.TUnit.Storage.AzureBlob` — add `AzureBlobRigBuilder`, `UseAzureBlob` extension, `AzureBlobSasBuilder`.
- [ ] `Rig.TUnit.Storage.S3` — add `S3RigBuilder`, `UseS3` extension, `S3SasBuilder`.
- [ ] `Rig.TUnit.Storage.MinIO` — add `MinIOFixtureOptions`, `MinIORigBuilder`, `UseMinIO` extension, `MinIOSasBuilder`.
- [ ] `Rig.TUnit.Storage.FileSystem` — add `FileSystemFixtureOptions`, `FileSystemRigBuilder`, `UseFileSystem` extension, `PathSandboxHelper`.
- [ ] All pass `StorageRigContract`.

### Security

- [ ] `Rig.TUnit.Security.Jwt` — add `JwtRigBuilder : SecurityRigBuilder<JwtRigBuilder>` + `UseJwt` extension. Keep `JwtBuilder` as-is (token builder).
- [ ] `Rig.TUnit.Security.OAuth` — add `OAuthRigBuilder` + `UseOAuthServer` extension.
- [ ] `Rig.TUnit.Security.Mtls` — add `MtlsFixture` (generates CA + leaf on setup), `MtlsFixtureOptions`, `MtlsRigBuilder`, `UseMtls` extension.
- [ ] `Rig.TUnit.Security.Policies` — add `PolicyFixture` (registers in-memory `IAuthorizationService`), `PolicyRigBuilder`, `UsePolicies` extension.

### Observability

- [ ] `Rig.TUnit.Observability.Metrics` — add `MetricsFixture` wrapping `MeterListener`, `MetricsFixtureOptions`, `MetricsRigBuilder : TelemetryRigBuilder<MetricsRigBuilder>`, `UseMetricsCapture` extension, `TagCardinalityGuard` helper.

### Microservices

- [ ] `Rig.TUnit.Microservices.EventSourcing` — add `AggregateAssert.Raised<T>().WithData(...)`, `EventCatalogueVerifier`, `SchemaEvolutionHelper`.
- [ ] `Rig.TUnit.Microservices.Saga` — add `SagaAssert.Step(...).Compensated()`, `SagaTimeoutHelper`.
- [ ] `Rig.TUnit.Microservices.Contracts` — add `ProviderVerificationFixture`, `PactBrokerClientStub`.

### Gate

- [ ] Flip `ProviderCompletenessTests` from `SkipUntilFixed` to enforced.
- [ ] Flip `ReadmeCompletenessTests` to enforced.

---

## Phase 4 — Create the 5 missing packages

### `Rig.TUnit.Databases.Sql.MySql`

- [ ] `src/Rig.TUnit.Databases.Sql.MySql/Rig.TUnit.Databases.Sql.MySql.csproj`
- [ ] `Fixtures/MySqlFixture.cs` (`: SqlFixtureBase`, `Testcontainers.MySql` 4.11+).
- [ ] `Options/MySqlFixtureOptions.cs` — image tag, port, root password.
- [ ] `Builder/MySqlRigBuilder.cs` (`: SqlRigBuilder<MySqlRigBuilder>`).
- [ ] `Builder/MySqlRigBuilderExtensions.cs` — `UseMySql(...)`.
- [ ] `Extensions/MySqlBuilderExtensions.cs` — EF Core `UseMySql` wrapper using `Pomelo.EntityFrameworkCore.MySql` 9.0.\* (comment linking PR #2019).
- [ ] `README.md`.
- [ ] `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/` — passes `SqlRigContract`, `ParallelIsolationContract`, adds `MySqlQuirkTests` (AUTO_INCREMENT, timestamp behaviour).

### `Rig.TUnit.Databases.Sql.Oracle`

- [ ] Project + `OracleFixture` using `Testcontainers.Oracle` 4.11+ with image `gvenzl/oracle-free:23.5-slim-faststart`.
- [ ] Wait strategy: `Wait.ForListeningPorts()` + 5-min startup timeout (aspire#12036 mitigation).
- [ ] `OracleFixtureOptions`, `OracleRigBuilder`, `UseOracle` extension.
- [ ] `Extensions/OracleBuilderExtensions.cs` — EF Core wrapper using `Oracle.EntityFrameworkCore` 9.23.90+.
- [ ] README + integration tests passing `SqlRigContract` + PL/SQL quirk tests.

### `Rig.TUnit.Databases.NoSql.Cosmos`

- [ ] Project + `CosmosFixture` using `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`.
- [ ] Custom wait strategy probing `https://localhost:{port}/_explorer/emulator.pem` with self-signed cert trust (mitigation for dotnet/testcontainers-dotnet#1306).
- [ ] `CosmosFixtureOptions`, `CosmosRigBuilder`, `UseCosmos` extension.
- [ ] `Helpers/RuChargeCapture.cs`, `Helpers/PartitionKeyDistributionChecker.cs` (per 003 §4.4).
- [ ] SDK: `Microsoft.Azure.Cosmos` 3.x.
- [ ] README + integration tests passing `NoSqlRigContract` + RU-charge + partition-distribution quirk tests.

### `Rig.TUnit.Observability.AppInsights`

- [ ] Project + `AppInsightsFixture` (in-process, no container) — implements custom `ITelemetryChannel` capturing `ITelemetry` items.
- [ ] `AppInsightsFixtureOptions` — instrumentation key, channel flush interval.
- [ ] `AppInsightsRigBuilder : TelemetryRigBuilder<AppInsightsRigBuilder>`.
- [ ] `UseAppInsights` extension.
- [ ] `Assertions/AppInsightsAssert.cs` mirroring `TraceAssert` surface.
- [ ] README + integration tests passing `TelemetryRigContract`.

### `Rig.TUnit.Docker`

- [ ] Project + `ContainerFixture` — generic `Testcontainers` wrapper with image-pull cache reuse, per-test networks, healthcheck ready detection.
- [ ] `DockerComposeFixture` — multi-container topology. Primary: `Testcontainers.Compose` 4.11+; fallback: `Ductus.FluentDocker` if compose support regresses on .NET 10 (verify during implementation).
- [ ] `DockerFixtureOptions`, `DockerRigBuilder`, `UseDocker` extension.
- [ ] README + integration tests (basic `alpine:3` echo container + 2-container compose).

### Meta-package update

- [ ] `Rig.TUnit.All/Rig.TUnit.All.csproj` — add ProjectReferences to all 5 new packages.
- [ ] `Rig.TUnit.Microservices/Rig.TUnit.Microservices.csproj` — add `Docker` if not already transitive.
- [ ] `Rig.TUnit.slnx` — register all 5 new src projects + 5 test projects.

---

## Phase 5 — Polish

- [ ] `README.md` for every provider that lacks one (57 of 59 today) — 30-second quick-start each.
- [ ] `ReadmeCompletenessTests` fully green.
- [ ] Verify `Rig.TUnit.All` transitively references every provider.
- [ ] CI matrix extended: MySql 8.0 + 8.4; Oracle Free 23; Cosmos vnext-preview.

---

## Mandatory test matrix (per 003 §5.3, carried forward)

Every new provider integration test MUST include:
1. Lifecycle (init idempotent, dispose safe).
2. 20-parallel isolation smoke.
3. Connection-source matrix (Container / Config / Options / Value / Auto).
4. `ForceContainersInCi()` honored; Config source rejected in CI.
5. Happy path + error path + timeout + cancellation for every public helper.
6. Eventual consistency via `WaitHelper`.
7. One test per documented provider quirk.

---

## CI updates

- [ ] `.github/workflows/ci.yml` — add MySql, Oracle, Cosmos emulator service lines OR mark these Integration test projects as `[Category("containers")]` and run them only in the containers matrix job.
- [ ] Pull-image caching for the new images.
- [ ] Windows runner compatibility check — Cosmos Linux emulator requires Linux containers; document in the workflow.

---

## Definition of done

1. Every checkbox in Phases 1–5 ticked.
2. `ProviderCompletenessTests` green across all 59 provider packages.
3. `TestFileOrganizationTests` green across every test project.
4. `ReadmeCompletenessTests` green for every `Rig.TUnit.{Family}.{Provider}/`.
5. Every family's `{Family}RigContract` suite passes against every provider.
6. Line coverage ≥ 90% / branch ≥ 85% per new or modified package.
7. `dotnet build` clean on .NET 10 with zero new warnings.
8. Baseline-003 test count preserved; new count strictly greater.
9. `Rig.TUnit.All` transitively references every provider including the 5 new ones.
10. PR merged, branch deleted.
