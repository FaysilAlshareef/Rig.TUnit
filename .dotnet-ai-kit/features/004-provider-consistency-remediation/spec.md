# Feature Specification: Rig.TUnit Provider Consistency Remediation

**Feature ID**: 004-provider-consistency-remediation
**Created**: 2026-04-18
**Status**: Draft
**Input**: "Use `@planning/provider-consistency-remediation` as reference — readme + codebase scan. Use TDD in every task. Everything must be clean, clear, reusable, maintainable. Search web / ask questions where unclear."

---

## Overview

Feature 003 (ecosystem expansion) landed the **Base + Provider** pattern with 59 packages and 219 green tests, but provider surface area is **inconsistent**:

- SqlServer, Sqlite, ServiceBus, Redis caching ship full `Fixture + Options + Builder + Extensions + Helpers + README`.
- Postgresql has `Fixture + Options + Builder` but is **missing `PostgresRigBuilderExtensions` and `PostgresBuilderExtensions`** (EF quickstart) and has no README — per library design §4.1.
- Mongo, Cassandra, Dynamo, ElasticSearch, EventStore, Kafka, RabbitMq, Nats, Sqs, Hybrid, Fusion, AzureBlob, S3, MinIO, FileSystem, Mtls, Policies, Metrics ship **only a fixture** (sometimes + options).
- `Rig.TUnit.Docker` has only a `ContainerFixture`; four packages promised by 003 (`Cosmos`, `MySql`, `Oracle`, `AppInsights`) never shipped.
- Test files mix tests with inline infrastructure (ActivitySource setup, Polly pipelines, JWKS helpers, outbox seed builders) — contradicts the `TestInfrastructure/` pattern already used by `Rig.TUnit.Grpc.Tests.Unit`, `Rig.TUnit.Core.Tests.Unit`.
- **20 of 32 leaf provider packages** ship without a README (verified 2026-04-18) — plus the 4 new packages to create in Phase 4, bringing the total Phase-6 README backlog to **~24 provider READMEs**. Providers that DO have a README today: Caching.Memory, Caching.Redis, Databases.NoSql.Redis, Databases.Sql.SqlServer, Databases.Sql.Sqlite, Messaging.ServiceBus, Observability.Logging, Observability.Logging.Analyzers, Observability.Seq, Observability.Tracing, Security.Jwt, Security.OAuth. (Planning docs' "57 of 59" figure is stale — superseded by this count.)

**Delivery mode: strictly test-first (RED → GREEN → REFACTOR) — carried forward from 003 R1.** Every architecture-test rule lands **initially failing** for every unfinished provider, then goes green as each provider reaches uniform shape. Every new provider builder, fixture, and helper ships in the same commit as its failing contract test.

**Scope discipline:** no feature from 003 is deferred or deleted. No feature flags. No rename of existing public APIs. No new families (Messaging/Caching/Storage/etc. are frozen). The architecture tests are on by default from Phase 1.

**Observed deltas from planning docs (verified 2026-04-18):**

- `Rig.TUnit.Security` **already exists** with `Contracts/ISecurityRig.cs`, `Fixtures/SecurityFixtureBase.cs`, `Builder/SecurityRigBuilder.cs` — the planning gap matrix is stale on this row. This feature enhances and wires providers to the existing base; it does NOT recreate it.
- `Rig.TUnit.Docker/Fixtures/ContainerFixture.cs` **already exists** — this package is partial (fixture-only), not absent. This feature completes the template.
- `Directory.Packages.props` already pins `Pomelo.EntityFrameworkCore.MySql 9.0.0`, `Oracle.EntityFrameworkCore 10.0.0`, `Microsoft.Azure.Cosmos 3.44.0`, `Microsoft.ApplicationInsights 2.23.0`. `Testcontainers.*` is pinned at 4.6.0 and MUST be bumped to `4.11.x` across every `Testcontainers.*` pin as the first commit of Phase 1 (see Clarification C-001).
- **`Testcontainers.EventStoreDb` is obsolete from 4.9 onward** — Event Store rebranded to **KurrentDB** on 2026-11-20 (https://www.kurrent.io/blog/kurrent-re-brand-faq) and upstream Testcontainers follows. Phase 1 now includes a full alignment with the upstream rename:
  - Dependency swap: `Testcontainers.EventStoreDb 4.9.0` → `Testcontainers.KurrentDb 4.11.0`; `EventStore.Client.Grpc.Streams 23.3.8` → `KurrentDB.Client 1.3.1`.
  - **Rig.TUnit package rename** (breaking — intentional for consistency with the upstream rebrand, since this feature is labelled Provider **Consistency** Remediation): `src/Rig.TUnit.Databases.NoSql.EventStore/` → `src/Rig.TUnit.Databases.NoSql.KurrentDb/`; `tests/…EventStore.Tests.Integration/` → `tests/…KurrentDb.Tests.Integration/`; class `EventStoreFixture` → `KurrentDbFixture`; namespace suffix `.EventStore` → `.KurrentDb`.
  - Image: `eventstore/eventstore:24.10.0-bookworm-slim` → `kurrentplatform/kurrentdb:25.1` (KurrentDb 4.11 default).
  - Tracked by tasks T002b (Directory.Packages.props swap), T002c (rename + fixture rewrite), T002d (slnx / All / AssemblyLoader cross-refs). Release notes for 004 MUST call out the breaking rename under a "Provider rename" heading.
- **Testcontainers 4.11 tightened the Builder API** — every `new XxxBuilder()` parameterless constructor is now `[Obsolete]` and requires the image argument at construction (`new XxxBuilder("image:tag")`). With `TreatWarningsAsErrors=true` at the repo root this manifests as CS0618 build failures across 18 existing fixtures. Repair is bundled into T002 alongside the version bump.

---

## User Stories

### User Story 1 - TDD RED-GREEN-REFACTOR Carried Forward (Priority: P1)

As a contributor, I need every production change in this feature to land **test-first** so the library's "trustworthy test infrastructure" promise is itself test-proven and no untested uniformity fix can sneak into `master`.

**Acceptance Scenarios:**

1. **Given** a new provider builder (e.g., `CassandraRigBuilder`), **When** I open a PR, **Then** the PR MUST contain a failing contract test (`NoSqlRigContract`) running against that builder **in the same commit as the production class** — commit log shows RED → GREEN → REFACTOR.
2. **Given** the three architecture tests added by this feature (`ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`), **When** Phase 1 lands, **Then** they MUST fail for every provider that still has gaps, and go green progressively as each provider is completed — a "skip-until-fixed" taxonomy on the first pass is NOT permitted beyond Phase 1 end.
3. **Given** a REFACTOR phase (e.g., extracting a shared `SasBuilderBase`), **When** I apply it, **Then** no test MUST be modified to accommodate the refactor — test changes signal behavior change, which demands a new RED test.
4. **Given** a per-package merge, **When** CI runs, **Then** the gate MUST enforce: line coverage ≥ 90%, branch coverage ≥ 85%, contract suite 100% green, parallel-isolation smoke green (same gate 003 enforced).
5. **Given** the baseline of 219 green tests from 003, **When** Feature 004 merges, **Then** the final green count MUST be strictly greater — no regressions, every 003 test still passes.

---

### User Story 2 - Phase 1: Enforcement Scaffolding Lands First (Priority: P1)

As the library maintainer, I need the three architecture-test rules to land **before** any provider is fixed so the gaps become machine-visible and cannot regress silently during the remediation.

**Acceptance Scenarios:**

1. **Given** `tests/Rig.TUnit.Architecture.Tests/Rules/`, **When** Phase 1 lands, **Then** it MUST contain `ProviderCompletenessTests.cs`, `TestFileOrganizationTests.cs`, `ReadmeCompletenessTests.cs` — each initially marked `[Category("SkipUntilFixed")]` for providers known to be in-flight, with a tracking issue for each skip.
2. **Given** `ProviderCompletenessTests`, **When** it runs against every `Rig.TUnit.{Family}.{Provider}` assembly, **Then** it MUST assert presence of all four required types: `{Provider}Fixture` (deriving from the family's fixture base), `{Provider}FixtureOptions` (with `public const string SectionName`), `{Provider}RigBuilder` (deriving from `{Family}RigBuilder<{Provider}RigBuilder>`), and a public `Use{Provider}` extension on `RigBuilder`.
3. **Given** `TestFileOrganizationTests`, **When** it scans `tests/**/*.cs`, **Then** it MUST fail for any file that declares >1 top-level class and is NOT under `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, `obj/`, `bin/`.
4. **Given** `ReadmeCompletenessTests`, **When** it scans `src/Rig.TUnit.{Family}.{Provider}/`, **Then** it MUST fail for any provider directory lacking a `README.md` or whose README is ≤ 100 chars.
5. **Given** `src/Rig.TUnit/Contributing-ProviderTemplate.md`, **When** a new contributor reads it, **Then** it MUST fully specify the canonical provider layout (Fixtures/Options/Builder/Extensions/Helpers/README) with copy-paste-ready code for a hypothetical `Rig.TUnit.{Family}.Example` provider.

---

### User Story 3 - Phase 2: Test-File Hygiene Sweep (Priority: P1)

As a test author, I need test files to contain **tests only** so new contributors can read a `*Tests.cs` and understand the test surface without wading through inline ActivitySource setup, Polly pipelines, JWKS key factories, or outbox envelope builders.

**Acceptance Scenarios:**

1. **Given** any `tests/**/*Tests.cs`, `*Contract.cs`, `*QuirkTests.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, **When** opened, **Then** it MUST declare exactly one top-level class and that class MUST contain only `[Test]` / `[Before]` / `[After]` methods plus `private` helpers used only by those methods.
2. **Given** `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/`, **When** Phase 2 completes, **Then** `TestInfrastructure/TracingTestHarness.cs` MUST contain the extracted `ActivitySource` + `TracerProvider` factories, and `TraceAssertTests.cs` MUST be tests-only (355 lines staying as one class is acceptable — the rule is extract setup, not split tests).
3. **Given** `tests/Rig.TUnit.Http.Tests.Unit/`, **When** Phase 2 completes, **Then** custom matchers and response-builder helpers MUST live in `TestInfrastructure/HttpMockTestHarness.cs`.
4. **Given** `tests/Rig.TUnit.Resilience.Tests.Integration/`, **When** Phase 2 completes, **Then** Polly pipeline builders MUST live in `TestInfrastructure/ResiliencePipelines.cs`.
5. **Given** `tests/Rig.TUnit.Security.OAuth.Tests.Integration/`, **When** Phase 2 completes, **Then** JWKS + RSA key factories MUST live in `TestInfrastructure/OAuthTestHarness.cs`.
6. **Given** `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/`, **When** Phase 2 completes, **Then** `OutboxMessage` seed builders, envelope fakers, and custom store stubs MUST live in `TestInfrastructure/OutboxTestData.cs`.
7. **Given** every `*QuirkTests.cs` across the test tree, **When** Phase 2 completes, **Then** inline test entities / fake handlers / shared fixtures MUST be extracted to `TestInfrastructure/`.
8. **Given** `TestFileOrganizationTests`, **When** Phase 2 ends, **Then** every `[Category("SkipUntilFixed")]` marker on that rule MUST be removed — the rule is fully enforced across the test tree.
9. **Given** a 355-line `TraceAssertTests.cs`, **When** this feature closes, **Then** it MUST remain one class — test files are NOT split by method-under-test. Only setup infrastructure is extracted.

---

### User Story 4 - Phase 3: Close Gaps in Existing Providers (Priority: P1)

As a developer writing integration tests, I need every existing provider to expose a uniform `Use{Provider}(rig, configure)` fluent method so I can switch providers without learning per-provider wiring, and `rig.UseCassandra()` / `rig.UseKafka()` / `rig.UseS3()` all work consistently.

**Acceptance Scenarios (Databases.Sql — Postgresql remediation):**

0. **Given** `Rig.TUnit.Databases.Sql.Postgresql` (which already has `Fixture + Options + PostgresRigBuilder`), **When** Phase 3 closes, **Then** it MUST additionally ship `PostgresRigBuilderExtensions.cs` exposing `UsePostgres(this RigBuilder, IRigConnectionSource, Action<PostgresRigBuilder>)` plus `PostgresBuilderExtensions.cs` exposing a `UsePostgresInMemory`-style EF quickstart shortcut (per library design §4.1), and `README.md` > 100 chars. All `SqlRigContract` tests MUST continue to pass against `PostgresFixture`.

**Acceptance Scenarios (Databases.NoSql):**

1. **Given** the 5 NoSql providers (Mongo, Cassandra, Dynamo, ElasticSearch, EventStore), **When** Phase 3 closes, **Then** each MUST ship `{Provider}FixtureOptions` (with `SectionName` + `[Required]` + `ValidateOnStart()`), `{Provider}RigBuilder : NoSqlRigBuilder<{Provider}RigBuilder>`, a public `Use{Provider}` extension on `RigBuilder`, and a provider-specific helper per 003 §4.4 (`CollectionPerTestHelper` + `BsonDiff` for Mongo, `KeyspacePerTestHelper` for Cassandra, `GsiVerifier` for Dynamo via LocalStack, `IndexRefreshHelper` + `DslAssert` for ElasticSearch, `StreamAssert` + `ProjectionAssert` for EventStore).
2. **Given** the integration test project for each NoSql provider, **When** it inherits `NoSqlRigContract`, **Then** all contract tests MUST pass end-to-end against the real container.

**Acceptance Scenarios (Messaging):**

3. **Given** the 4 Messaging providers without builders (Kafka, RabbitMq, Nats, Sqs), **When** Phase 3 closes, **Then** each MUST ship `{Provider}RigBuilder : MessagingRigBuilder<{Provider}RigBuilder>`, `Use{Provider}` extension, `{Provider}Listener : ListenerBase`, `{Provider}EventSender : EventSenderBase`, and Nats/Sqs add the missing `{Provider}FixtureOptions` with `[Required]` properties.
4. **Given** each provider's integration test project, **When** it inherits `MessagingRigContract`, **Then** it MUST pass (publish, consume, dead-letter, ordering).

**Acceptance Scenarios (Caching):**

5. **Given** `Rig.TUnit.Caching.Memory`, **When** Phase 3 closes, **Then** it MUST expose `UseMemoryCache` extension (no options required — memory cache is parameterless).
6. **Given** `Rig.TUnit.Caching.Hybrid`, **When** Phase 3 closes, **Then** it MUST ship `HybridCacheFixtureOptions`, `HybridCacheRigBuilder`, `UseHybridCache` extension.
7. **Given** `Rig.TUnit.Caching.Fusion`, **When** Phase 3 closes, **Then** it MUST ship `FusionCacheFixtureOptions`, `FusionCacheRigBuilder`, `UseFusionCache` extension plus fail-safe + eager-refresh helpers per 003 §4.6.
8. **Given** every caching provider integration test, **When** it inherits `CacheRigContract`, **Then** it MUST pass (get/set/expire/stampede/backplane).

**Acceptance Scenarios (Storage):**

9. **Given** the 4 Storage providers (AzureBlob, S3, MinIO, FileSystem), **When** Phase 3 closes, **Then** each MUST ship `{Provider}RigBuilder : StorageRigBuilder<{Provider}RigBuilder>`, `Use{Provider}` extension, and `{Provider}SasBuilder` (except FileSystem, which ships a `PathSandboxHelper` instead — N/A for SAS). MinIO + FileSystem also add missing `{Provider}FixtureOptions`.
10. **Given** each provider's integration test project, **When** it inherits `StorageRigContract`, **Then** it MUST pass (put, get, list, delete, presign).

**Acceptance Scenarios (Security):**

11. **Given** `Rig.TUnit.Security.Jwt`, **When** Phase 3 closes, **Then** it MUST ship `JwtRigBuilder : SecurityRigBuilder<JwtRigBuilder>` + `UseJwt` extension. The existing `JwtBuilder` (token builder) MUST NOT be renamed — the rig-builder is a separate type.
12. **Given** `Rig.TUnit.Security.OAuth`, **When** Phase 3 closes, **Then** it MUST ship `OAuthRigBuilder : SecurityRigBuilder<OAuthRigBuilder>` + `UseOAuthServer` extension wrapping the existing `MockOAuthServer`.
13. **Given** `Rig.TUnit.Security.Mtls`, **When** Phase 3 closes, **Then** it MUST ship `MtlsFixture` (generates CA + leaf cert on initialize), `MtlsFixtureOptions`, `MtlsRigBuilder : SecurityRigBuilder<MtlsRigBuilder>`, `UseMtls` extension. The existing `MtlsCertificateBuilder` stays as a helper.
14. **Given** `Rig.TUnit.Security.Policies`, **When** Phase 3 closes, **Then** it MUST ship `PolicyFixture` (registers an in-memory `IAuthorizationService`), `PolicyFixtureOptions`, `PolicyRigBuilder`, `UsePolicies` extension. The existing `PolicyAssert` stays as an assertion DSL.

**Acceptance Scenarios (Observability):**

15. **Given** `Rig.TUnit.Observability.Metrics`, **When** Phase 3 closes, **Then** it MUST ship `MetricsFixture` wrapping `System.Diagnostics.Metrics.MeterListener`, `MetricsFixtureOptions`, `MetricsRigBuilder : TelemetryRigBuilder<MetricsRigBuilder>`, `UseMetricsCapture` extension, and a `TagCardinalityGuard` helper that fails tests emitting > N distinct tag values.

**Acceptance Scenarios (Microservices depth — Phase 5 but traced here for clarity):**

16. **Given** `Rig.TUnit.Microservices.EventSourcing`, **When** Phase 5 closes, **Then** it MUST ship `AggregateAssert.Raised<T>().WithData(...)`, `EventCatalogueVerifier` (validates every event in the catalogue is producible), and a `SchemaEvolutionHelper` (asserts old-version events still deserialize).
17. **Given** `Rig.TUnit.Microservices.Saga`, **When** Phase 5 closes, **Then** it MUST ship `SagaAssert.Step(name).Compensated()` fluent assertions plus `SagaTimeoutHelper` advancing virtual time.
18. **Given** `Rig.TUnit.Microservices.Contracts`, **When** Phase 5 closes, **Then** it MUST ship `ProviderVerificationFixture` + `PactBrokerClientStub`.

**Gate:** `ProviderCompletenessTests` flipped from SkipUntilFixed to enforced at Phase 3 end.

---

### User Story 5 - Phase 4: Create the 4 Missing Packages (+ Complete Docker) (Priority: P1)

As a developer needing test coverage for MySQL / Oracle / Cosmos / AppInsights backends or a generic Docker harness, I need those packages to ship with the full provider template so my test fixtures feel identical to SqlServer / Sqlite / Redis ones.

**Acceptance Scenarios:**

1. **Given** `src/Rig.TUnit.Databases.Sql.MySql/`, **When** Phase 4 closes, **Then** it MUST exist with `MySqlFixture : SqlFixtureBase` (using `Testcontainers.MySql`), `MySqlFixtureOptions`, `MySqlRigBuilder : SqlRigBuilder<MySqlRigBuilder>`, `UseMySql` extension, `MySqlBuilderExtensions` (EF wiring via `Pomelo.EntityFrameworkCore.MySql 9.0.0` — pinned in `Directory.Packages.props` with a comment pointing to PR #2019 tracking .NET 10 release), `README.md`, and a Tests.Integration project passing `SqlRigContract` + `ParallelIsolationContract` + `MySqlQuirkTests` (AUTO_INCREMENT, timestamp behaviour).
2. **Given** `src/Rig.TUnit.Databases.Sql.Oracle/`, **When** Phase 4 closes, **Then** it MUST exist with `OracleFixture` using image `gvenzl/oracle-free:23.5-slim-faststart`, wait strategy `Wait.ForListeningPorts()` + 5-min startup timeout (aspire#12036 mitigation), `OracleFixtureOptions`, `OracleRigBuilder`, `UseOracle` extension, `OracleBuilderExtensions` (EF wiring via `Oracle.EntityFrameworkCore 10.0.0`), `README.md`, and Tests.Integration passing `SqlRigContract` + PL/SQL quirk tests.
3. **Given** `src/Rig.TUnit.Databases.NoSql.Cosmos/`, **When** Phase 4 closes, **Then** it MUST exist with `CosmosFixture` using `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` + custom wait strategy probing `https://localhost:{port}/_explorer/emulator.pem` with self-signed cert trust (workaround for dotnet/testcontainers-dotnet#1306), `CosmosFixtureOptions`, `CosmosRigBuilder`, `UseCosmos` extension, `Helpers/RuChargeCapture.cs`, `Helpers/PartitionKeyDistributionChecker.cs`, SDK `Microsoft.Azure.Cosmos 3.44.0`, `README.md`, and Tests.Integration passing `NoSqlRigContract` + RU-charge + partition-distribution quirk tests.
4. **Given** `src/Rig.TUnit.Observability.AppInsights/`, **When** Phase 4 closes, **Then** it MUST exist with `AppInsightsFixture` (in-process — no container — capturing `ITelemetry` items via custom `ITelemetryChannel`), `AppInsightsFixtureOptions` (instrumentation key, channel flush interval), `AppInsightsRigBuilder : TelemetryRigBuilder<AppInsightsRigBuilder>`, `UseAppInsights` extension, `Assertions/AppInsightsAssert.cs` (mirroring `TraceAssert`/`MetricAssert` surface), uses `Microsoft.ApplicationInsights 2.23.0`, `README.md`, and Tests.Integration passing `TelemetryRigContract`.
5. **Given** `src/Rig.TUnit.Docker/` (currently fixture-only), **When** Phase 4 closes, **Then** it MUST complete the template: `ContainerFixture` refactored to expose image-pull cache reuse, per-test networks, healthcheck ready detection; new `DockerComposeFixture` for multi-container topologies (primary: `Testcontainers` compose support; fallback: `Ductus.FluentDocker` if compose regresses on .NET 10 — verify during implementation); `DockerFixtureOptions`, `DockerRigBuilder`, `UseDocker` extension; `README.md`; Tests.Integration with basic `alpine:3` echo container + 2-container compose.
6. **Given** `Rig.TUnit.slnx`, **When** Phase 4 closes, **Then** it MUST reference all 4 new src projects + 4 new Tests.Integration projects.
7. **Given** `src/Rig.TUnit.All/Rig.TUnit.All.csproj`, **When** Phase 4 closes, **Then** it MUST project-reference all 4 new packages + reference the completed Docker package, so `PackageReference Include="Rig.TUnit.All"` transitively pulls them.

---

### User Story 6 - Phase 5: Microservices Depth (Priority: P2)

As a developer testing event-sourced aggregates, sagas, or contract tests, I need richer assertion/helper surfaces that match the 003 design (§4.11) — currently each of these packages ships only a single file.

**Acceptance Scenarios:**

1. **Given** `Rig.TUnit.Microservices.EventSourcing`, **When** Phase 5 closes, **Then** `AggregateAssert.Raised<TEvent>().WithData(predicate)` MUST chain fluently over any `IEventSourcedAggregate`, `EventCatalogueVerifier` MUST walk the catalogue and confirm every event type is producible through its factory, and `SchemaEvolutionHelper` MUST load a legacy-JSON payload and assert the current type deserializes it without data loss.
2. **Given** `Rig.TUnit.Microservices.Saga`, **When** Phase 5 closes, **Then** `SagaAssert.Step(stepName).Compensated()` MUST read the saga history and fail if the named step's compensation was not invoked, and `SagaTimeoutHelper` MUST advance the injected `TimeProvider` until the saga's timeout fires, asserting the correct compensation.
3. **Given** `Rig.TUnit.Microservices.Contracts`, **When** Phase 5 closes, **Then** `ProviderVerificationFixture` MUST load a Pact file, stand up the producer endpoints under test, and verify every interaction — with `PactBrokerClientStub` returning fixed Pact files for deterministic CI runs.

---

### User Story 7 - Phase 6: README Polish & Meta-Package Sync (Priority: P2)

As a first-time user browsing `src/`, I need every provider package to ship a 30-second quick-start README so I can decide whether to adopt it without opening the fixture source file.

**Acceptance Scenarios:**

1. **Given** every `src/Rig.TUnit.{Family}.{Provider}/` directory, **When** Phase 6 closes, **Then** each MUST have a `README.md` > 100 chars with a runnable quick-start snippet (`[Test] public async Task Sample() { using var rig = new RigBuilder().Use{Provider}().Build(); ... }`).
2. **Given** `ReadmeCompletenessTests`, **When** Phase 6 closes, **Then** it MUST be fully enforced (no skips).
3. **Given** `Rig.TUnit.All`, **When** Phase 6 closes, **Then** it MUST transitively reference every provider package including the 4 new ones and the completed Docker package.
4. **Given** `Rig.TUnit.Microservices` meta-package, **When** Phase 6 closes, **Then** it MUST reference the Docker package if not already transitive (for fixtures needing containers).

---

### User Story 8 - .NET 10 Compatibility Preserved (Priority: P1)

As a library maintainer, I need the driver strategy documented in the planning doc to be executed verbatim so the MySql/Oracle/Cosmos/AppInsights packages work today on .NET 10 and upgrade cleanly when upstream catches up.

**Acceptance Scenarios:**

1. **Given** `Directory.Packages.props`, **When** Phase 4 closes, **Then** `Pomelo.EntityFrameworkCore.MySql` MUST be pinned to `9.0.*` with a `<!-- comment -->` citing PR #2019 (Pomelo's .NET 10 / EF Core 10 release PR) and explaining that Pomelo 9 runs on .NET 10 TFM — upgrade to 10.x is a packages-props-only bump when it ships.
2. **Given** the Oracle package, **When** it initializes, **Then** it MUST use `gvenzl/oracle-free:23.5-slim-faststart` (Oracle-Free replaced Oracle-XE; XE image is unmaintained), `WithWaitStrategy(Wait.ForListeningPorts())`, and a 5-min startup timeout (aspire#12036 mitigation).
3. **Given** the Cosmos package, **When** it initializes, **Then** it MUST use the Linux emulator `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` with a custom wait probe (dotnet/testcontainers-dotnet#1306 workaround).
4. **Given** the AppInsights package, **When** tests run, **Then** it MUST NOT require a container — telemetry is captured in-process via a custom `ITelemetryChannel`.
5. **Given** the Docker package, **When** it is implemented, **Then** the primary compose backend MUST be `Testcontainers` 4.x native compose; fallback to `Ductus.FluentDocker` is allowed ONLY if compose regresses on .NET 10 and the regression is documented in the package README.

---

### User Story 9 - CI Matrix Extension (Priority: P2)

As a CI maintainer, I need the MySql / Oracle / Cosmos emulator containers in the pipeline so the new integration tests run on every PR — without making every developer on Windows run Linux-only containers locally.

**Acceptance Scenarios:**

1. **Given** `.github/workflows/ci.yml`, **When** Phase 6 closes, **Then** it MUST either (a) add MySql 8.0 + 8.4, Oracle Free 23, Cosmos vnext-preview service lines, OR (b) tag these Integration test projects `[Category("containers")]` and run them only in the containers matrix job.
2. **Given** the Cosmos tests, **When** they run on a Windows agent, **Then** they MUST be skipped with a documented "Cosmos Linux emulator requires Linux containers" signal — not fail.
3. **Given** pull-image caching, **When** CI runs twice in a row, **Then** the second run MUST reuse cached MySql / Oracle / Cosmos images, keeping test startup under the 003 baseline.

---

## Requirements

### Functional Requirements

**Uniformity enforcement (architecture tests)**

- **FR-001**: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` MUST assert every `Rig.TUnit.{Family}.{Provider}` assembly exports `{Provider}Fixture` (from correct family base), `{Provider}FixtureOptions` (with `SectionName` + `[Required]`), `{Provider}RigBuilder` (from correct family CRTP base), and a public `Use{Provider}` extension method on `RigBuilder`.
- **FR-002**: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs` MUST fail any `tests/**/*.cs` file declaring >1 top-level class outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, `obj/`, `bin/` — and the rule MUST apply uniformly to `*Contract.cs` files (per C-003); contract base classes with inline helper types extract those helpers to `TestInfrastructure/ContractHelpers/`.
- **FR-003**: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` MUST fail any `src/Rig.TUnit.{Family}.{Provider}/` directory whose `README.md` is missing or ≤ 100 chars.
- **FR-004**: All three rules above MUST land in Phase 1 with `[Category("SkipUntilFixed")]` markers only for providers known to be in-flight, with no skips remaining at the end of the phase in which each provider is completed.

**Provider template uniformity (filling gaps)**

- **FR-005**: Every `Rig.TUnit.{Family}.{Provider}` package listed in the gap matrix (§4 of library design) MUST end with: `Fixtures/{Provider}Fixture.cs`, `Options/{Provider}FixtureOptions.cs` (with `public const string SectionName`, `[Required]` annotations, registered via `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`), `Builder/{Provider}RigBuilder.cs` (CRTP: `{Provider}RigBuilder : {Family}RigBuilder<{Provider}RigBuilder>`), `Builder/{Provider}RigBuilderExtensions.cs` (`Use{Provider}(this RigBuilder, Action<{Provider}RigBuilder>?)`), family-specific `Helpers/`, and `README.md` > 100 chars.
- **FR-006**: Messaging providers (Kafka, RabbitMq, Nats, Sqs) MUST additionally ship `Helpers/{Provider}Listener.cs : ListenerBase` + `Helpers/{Provider}EventSender.cs : EventSenderBase`.
- **FR-007**: Storage providers (AzureBlob, S3, MinIO) MUST additionally ship `Helpers/{Provider}SasBuilder.cs`. FileSystem MUST ship `Helpers/PathSandboxHelper.cs` in lieu of a SasBuilder (not applicable).
- **FR-008**: Security providers (Jwt, OAuth, Mtls, Policies) MUST derive their RigBuilders from `SecurityRigBuilder<TSelf>` (base already in `src/Rig.TUnit.Security/Builder/`). Existing token/certificate/assertion types (`JwtBuilder`, `MtlsCertificateBuilder`, `PolicyAssert`, `MockOAuthServer`) MUST NOT be renamed — new RigBuilder types are added alongside.
- **FR-009**: `Rig.TUnit.Observability.Metrics` MUST additionally ship `Helpers/TagCardinalityGuard.cs` that fails tests emitting > N distinct tag values on a single meter (configurable, default N=100).

**Test-file hygiene**

- **FR-010**: Every test `.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` MUST declare exactly one top-level class containing only `[Test]` / `[Before]` / `[After]` methods plus private helpers referenced only by those methods.
- **FR-011**: Inline shared fixtures, test entities, fake handlers, builder helpers, and setup constants MUST move to a per-project `TestInfrastructure/` subfolder using names `{Project}TestHarness.cs`, `Test{Entity}.cs`, `Test{Handler}.cs`, `Fake{Xxx}.cs` (matches `Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/`).
- **FR-012**: Test files MUST NOT be split by method-under-test. A 355-line `TraceAssertTests.cs` staying as one class is acceptable — only setup infrastructure is extracted.

**Missing packages**

- **FR-013**: `src/Rig.TUnit.Databases.Sql.MySql/` MUST ship with full R2 template + Pomelo 9 pinned + `Testcontainers.MySql` + README + Tests.Integration passing `SqlRigContract` + `ParallelIsolationContract` + `MySqlQuirkTests`.
- **FR-014**: `src/Rig.TUnit.Databases.Sql.Oracle/` MUST ship with full R2 template + `Oracle.EntityFrameworkCore` + `gvenzl/oracle-free:23.5-slim-faststart` + 5-min wait strategy + README + Tests.Integration passing `SqlRigContract` + `ParallelIsolationContract` + PL/SQL quirks.
- **FR-015**: `src/Rig.TUnit.Databases.NoSql.Cosmos/` MUST ship with full R2 template + vnext-preview Linux emulator + custom `/_explorer/emulator.pem` wait probe + `Microsoft.Azure.Cosmos` SDK + `RuChargeCapture` + `PartitionKeyDistributionChecker` helpers + README + Tests.Integration passing `NoSqlRigContract` + `ParallelIsolationContract` + RU-charge + partition-distribution quirk tests.
- **FR-016**: `src/Rig.TUnit.Observability.AppInsights/` MUST ship with full R2 template + in-process `ITelemetryChannel` capture + `AppInsightsAssert` + README + Tests.Integration passing `TelemetryRigContract` + `ParallelIsolationContract`.
- **FR-017**: `src/Rig.TUnit.Docker/` (currently fixture-only) MUST be completed: add `DockerFixtureOptions`, `DockerRigBuilder`, `UseDocker` extension, `DockerComposeFixture`, README, Tests.Integration with alpine echo + 2-container compose + `ParallelIsolationContract` smoke (20 parallel `alpine:3` containers, zero cross-talk).

**Microservices depth (Phase 5)**

- **FR-018**: `Rig.TUnit.Microservices.EventSourcing` MUST ship `AggregateAssert.Raised<TEvent>().WithData(predicate)`, `EventCatalogueVerifier`, `SchemaEvolutionHelper`.
- **FR-019**: `Rig.TUnit.Microservices.Saga` MUST ship `SagaAssert.Step(name).Compensated()`, `SagaTimeoutHelper`.
- **FR-020**: `Rig.TUnit.Microservices.Contracts` MUST ship `ProviderVerificationFixture`, `PactBrokerClientStub`.

**Meta-package & CI**

- **FR-021**: `Rig.TUnit.slnx` MUST reference all 4 new src projects + 4 new Tests.Integration projects by end of Phase 4.
- **FR-022**: `Rig.TUnit.All` MUST project-reference all 4 new packages + completed Docker package by end of Phase 6.
- **FR-023**: CI workflow MUST run MySql 8.x, Oracle Free 23, Cosmos vnext tests — either in the default matrix with service containers, or behind a `[Category("containers")]` gate in a dedicated job.

**TDD discipline**

- **FR-024**: Every new production class MUST ship in the same commit as its failing test (RED). Commit log MUST show RED → GREEN → REFACTOR order.
- **FR-025**: Per-package merge gate: line coverage ≥ 90%, branch coverage ≥ 85%, contract suite 100% green, parallel-isolation smoke passes. (Identical to 003.)
- **FR-026**: Zero regressions — final green test count MUST be strictly greater than the 003 baseline (219 tests).

**KurrentDB rename (Phase 1)**

- **FR-027**: Testcontainers 4.9+ marks the whole `EventStoreDb` module `[Obsolete]`; Phase 1 MUST swap `Testcontainers.EventStoreDb 4.9.0` for `Testcontainers.KurrentDb 4.11.0` and `EventStore.Client.Grpc.Streams 23.3.8` for `KurrentDB.Client 1.3.1` in `Directory.Packages.props` with the matching `PackageReference` swap in the provider's csproj.
- **FR-028**: The Rig.TUnit package MUST be renamed alongside the upstream rebrand: `src/Rig.TUnit.Databases.NoSql.EventStore/` → `src/Rig.TUnit.Databases.NoSql.KurrentDb/`; namespace `.EventStore` → `.KurrentDb`; class `EventStoreFixture` → `KurrentDbFixture`; test project + file names updated in lockstep; `Rig.TUnit.slnx` + `Rig.TUnit.All.csproj` + `AssemblyLoader.cs` seed list updated. Release notes MUST announce the breaking rename.
- **FR-029**: The new fixture MUST use image `kurrentplatform/kurrentdb:25.1` (KurrentDb 4.11 default) via `new KurrentDbBuilder("kurrentplatform/kurrentdb:25.1")` and expose `ConnectionString` returning the container's `kurrentdb://…?tls=false` URI (unchanged caller semantics — `KurrentDB.Client` consumes the scheme directly).

### Key Entities

- **Provider package** (`Rig.TUnit.{Family}.{Provider}`): A leaf NuGet-installable project following the R2 template. Attributes: Family (Databases.Sql | Databases.NoSql | Messaging | Caching | Storage | Security | Observability), Provider name, required files (Fixture, Options, Builder, Extensions, Helpers, README), contract test suite passed.
- **Family base package** (`Rig.TUnit.{Family}`): Defines `I{Family}Rig`, `{Family}FixtureBase`, `{Family}RigBuilder<TSelf>` (CRTP), family-shared assertions and helpers. All already exist post-003; this feature does NOT modify them.
- **Provider fixture** (`{Provider}Fixture`): Testcontainers (or in-process) wrapper exposing `InitializeAsync` / `DisposeAsync`, an `IsolationKey`, and the connection-source matrix (Container / Config / Options / Value / Auto).
- **Provider options** (`{Provider}FixtureOptions`): Strongly-typed options class with `public const string SectionName`, `[Required]` on mandatory properties, bound via `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.
- **Provider RigBuilder** (`{Provider}RigBuilder`): CRTP subclass of the family's builder, exposes provider-specific fluent configuration, always `sealed`.
- **Use extension** (`Use{Provider}`): Single public entry point `static RigBuilder Use{Provider}(this RigBuilder rig, Action<{Provider}RigBuilder>? configure = null)`.
- **Architecture test rule**: A class under `tests/Rig.TUnit.Architecture.Tests/Rules/` asserting a cross-cutting invariant on the `src/` + `tests/` tree. Three new rules this feature: `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`.
- **TestInfrastructure folder**: Per-test-project folder holding extracted setup types (fixtures, fakers, harnesses, test entities). Files contain types that are NOT `[Test]` methods.

---

## Architecture Scope

**Project mode**: **generic** — single-repo .NET 10 class-library solution (`Rig.TUnit.slnx`). No microservices, no cross-repo briefs to project.

**Affected directories:**

- `src/` — **modified**: existing provider packages gain `Builder/`, `Options/`, `Helpers/`, `README.md` files per gap matrix. No renames, no deletions.
- `src/` — **created**: 4 new provider packages (`Rig.TUnit.Databases.Sql.MySql`, `Rig.TUnit.Databases.Sql.Oracle`, `Rig.TUnit.Databases.NoSql.Cosmos`, `Rig.TUnit.Observability.AppInsights`). Docker completes its template.
- `src/Rig.TUnit/` — **created**: `Contributing-ProviderTemplate.md` canonical template doc.
- `src/Rig.TUnit.All/` — **modified**: add ProjectReferences to the 4 new packages + Docker.
- `tests/Rig.TUnit.Architecture.Tests/Rules/` — **created**: `ProviderCompletenessTests.cs`, `TestFileOrganizationTests.cs`, `ReadmeCompletenessTests.cs`.
- `tests/` — **modified**: every test project receives a `TestInfrastructure/` subfolder where inline setup infrastructure is extracted.
- `tests/` — **created**: 4 new Tests.Integration projects (`Rig.TUnit.Databases.Sql.MySql.Tests.Integration`, `...Oracle...`, `...Cosmos...`, `Rig.TUnit.Observability.AppInsights.Tests.Integration`, `Rig.TUnit.Docker.Tests.Integration`).
- `Rig.TUnit.slnx` — **modified**: register 4 new src projects + 5 new test projects (4 for new packages + 1 for Docker).
- `Directory.Packages.props` — **modified**: Phase 1 commit bumps every `Testcontainers.*` pin from `4.6.0` → `4.11.x` (per C-001). No other new pins required — all new-package dependencies already pinned.
- `.github/workflows/ci.yml` — **modified**: add MySql / Oracle / Cosmos matrix rows or `[Category("containers")]` job.

**Architectural constraints (carry-forward from 003):**

- Dependency flow: `Rig.TUnit.Core` ← family base ← provider package. Providers MUST NOT reference each other (except the documented `Rig.TUnit.Databases.NoSql.Redis → Rig.TUnit.Caching.Redis` shared-fixture case).
- Every provider's RigBuilder MUST use the CRTP pattern. No runtime type assertions against concrete builders.
- Every fixture MUST expose an `IsolationKey` derived from `ExecutionContext` and pass `ParallelIsolationContract`.
- Every public type MUST have XML docs (003 convention).

---

## Edge Cases

- **Partial packages already in src/**: `Rig.TUnit.Docker` has only `Fixtures/ContainerFixture.cs`; `Rig.TUnit.Security` base has foundation types but no providers wired through it. Plan treats these as "complete the template" not "create new package".
- **Redis KV reusing Caching.Redis fixture**: `Rig.TUnit.Databases.NoSql.Redis` project-references `Rig.TUnit.Caching.Redis` for the shared `RedisFixture`. Architecture test MUST accept a `RedisKvRigBuilder` that consumes an external fixture — the "has a fixture" check walks project references.
- **Logging/Tracing/Seq already work via `Use{Provider}Fixture` pass-through rather than dedicated `{Provider}RigBuilder`**: the library-design note clarifies the completeness test MUST accept pass-through extensions as equivalent to a dedicated RigBuilder. The test asserts a public fluent entry-point exists, not a specific class shape.
- **Observability.Logging.Analyzers is a Roslyn analyzer, not a fixture**: architecture test MUST exclude Analyzer projects from provider-completeness checks.
- **Cosmos emulator on Windows runners**: Cosmos Linux emulator requires Linux containers. Windows CI agents MUST skip (not fail) the Cosmos integration test with a documented reason.
- **Pomelo MySql + .NET 10**: Pomelo 9 runs on .NET 10 TFM but carries EF Core 9 dependencies. When Pomelo 10 ships (PR #2019), the upgrade is a packages-props bump. If Pomelo 10 is out by merge, pin it — otherwise stay at 9.0.*.
- **Oracle container startup hangs**: aspire#12036 reports intermittent init hangs. Mitigation: `Wait.ForListeningPorts()` + 5-min startup timeout + retry-on-init-failure in the fixture.
- **Dynamo GSI verifier using LocalStack**: DynamoDB Local doesn't support GSIs perfectly — LocalStack provides better fidelity but slower startup. Document the tradeoff in the Dynamo README.
- **FluentDocker compose fallback**: only activated if `Testcontainers`' native compose support regresses on .NET 10. Document activation criteria in the Docker README.
- **Test files > 300 lines with many inline helpers** (e.g., `TraceAssertTests.cs` at 355 lines): remain one class; only setup helpers move to `TestInfrastructure/`. The hygiene rule is about **file role**, not file length.

---

## Clarifications

- **C-001** [Domain & Data Model]: Testcontainers version pin (4.6 vs 4.11+) → **Bump the whole `Testcontainers.*` family to `4.11.x`** as the first commit of Phase 1, before any architecture test lands. Run full `dotnet test` against the 219-test baseline to catch regressions early. Central package management + transitive pinning make mixed versions risky; the minor bump within 4.x carries no breaking API changes. `Directory.Packages.props` gets one uniform version line for every `Testcontainers.*` pin.
- **C-002** [Edge Cases]: `PactBrokerClientStub` fidelity → **Option (a) — fixed `.json` Pact files from `TestInfrastructure/Pacts/*.json`.** No HTTP, no HAL emulation. `ProviderVerificationFixture` loads the named Pact file, stands up the producer endpoints under test, and verifies every interaction. Richer broker semantics (tags, versioning, webhooks, HAL endpoints) are deferred — teams needing live broker fidelity run against a real broker. Keeps the Phase 5 scope minimal and matches library design §10 (no scope expansion).
- **C-003** [Edge Cases]: `TestFileOrganizationTests` treatment of `*Contract.cs` → **Option (b) — enforce the rule uniformly, no carve-out for contract files.** Abstract contract base classes (`SqlRigContract`, `NoSqlRigContract`, `CacheRigContract`, etc.) that declare inline helper types MUST extract those helpers to `TestInfrastructure/ContractHelpers/` under their owning contract-test project. The architecture-test rule stays one sentence: "one top-level class per file outside infrastructure folders." Consistency is the feature's entire point; exceptions weaken the invariant.

---

## Success Criteria

- **SC-001**: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests` runs green across all 59 provider packages — no skips remaining.
- **SC-002**: `tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests` runs green across every test project — every non-infrastructure file declares exactly one top-level class.
- **SC-003**: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests` runs green for every `src/Rig.TUnit.{Family}.{Provider}/` directory — all 59 have a README > 100 chars.
- **SC-004**: Every family's `{Family}RigContract` test suite passes against every provider in the family (Databases.Sql, Databases.NoSql, Messaging, Caching, Storage, Security, Observability).
- **SC-005**: All 4 new src projects + completed Docker package present in `Rig.TUnit.slnx` and transitively referenced by `Rig.TUnit.All`.
- **SC-006**: `dotnet build` on the full solution under .NET 10 produces zero new warnings beyond the 003 baseline.
- **SC-007**: Line coverage ≥ 90% / branch coverage ≥ 85% per new or modified package.
- **SC-008**: Final green test count > 219 (003 baseline). No pre-existing 003 test regresses.
- **SC-009**: Every checkbox in `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` is ticked and the Phase 4-6 gates are green.
- **SC-010**: Commit history on `feat/provider-consistency-remediation` shows RED → GREEN → REFACTOR cadence — reviewers can verify each production class landed after (or with) its failing test.
- **SC-011**: `src/Rig.TUnit/Contributing-ProviderTemplate.md` is the canonical template a new contributor can copy to create a hypothetical `Rig.TUnit.{Family}.Example` provider that passes `ProviderCompletenessTests` without modification.

---

## References

- `planning/provider-consistency-remediation/README.md` — folder purpose.
- `planning/provider-consistency-remediation/Rig.TUnit-Library-Design.md` — architectural design (11 sections).
- `planning/provider-consistency-remediation/Rig.TUnit-Build-Prompt.md` — build prompt with R1–R9 hard requirements.
- `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` — file-by-file checklist for Phases 1–5 plus CI update list.
- `planning/provider-consistency-remediation/Rig.TUnit-Provider-Gap-Matrix.md` — per-family evidence matrix.
- `planning/ecosystem-expansion/Rig.TUnit-Library-Design.md` — 003 baseline design.
- `.claude/rules/*.md` — project conventions (coding style, async, configuration, observability, security, testing, tool-calls).
- `src/Rig.TUnit.slnx` — solution file (already tracking Rig.TUnit.Docker, Rig.TUnit.Security but NOT the 4 new packages).
- `Directory.Packages.props` — central version pins (already has Pomelo 9.0.0, Oracle 10.0.0, Cosmos 3.44.0, ApplicationInsights 2.23.0).

---

## Next

```
/dotnet-ai-kit:clarify    # resolve the 3 [NEEDS CLARIFICATION] markers
/dotnet-ai-kit:plan       # or skip to planning once clarifications answered
```
