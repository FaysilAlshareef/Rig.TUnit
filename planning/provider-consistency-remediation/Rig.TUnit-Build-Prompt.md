# Build Prompt — Rig.TUnit Provider Consistency Remediation

Copy everything below the line and pass it to `/dai.spec` to generate the formal specification.

---

## Context

This is the **fourth feature** for Rig.TUnit, following 003 (ecosystem expansion). The 003 feature landed the base-package layer (`Rig.TUnit.Databases.{Sql,NoSql}`, `Rig.TUnit.Messaging`, `Rig.TUnit.Caching`, `Rig.TUnit.Storage`, `Rig.TUnit.Observability`, `Rig.TUnit.Security`) and most provider packages, but it left provider surface area inconsistent: SqlServer / Sqlite / ServiceBus / Redis ship full `Fixture + Options + Builder + Extensions + Helpers`; most other providers (Cassandra, Dynamo, ElasticSearch, EventStore, Nats, Sqs, Fusion, FileSystem, MinIO, Mtls, Policies, Metrics) ship only a fixture or a single class. Five packages promised by the 003 design never landed: `Cosmos`, `MySql`, `Oracle`, `AppInsights`, `Docker`. Test files mix production tests with inline setup infrastructure.

**This feature closes all of those gaps. No existing feature is deferred or deleted. Every package promised by 003 will be implemented here.**

**Read before generating the spec:**
- `planning/provider-consistency-remediation/Rig.TUnit-Library-Design.md` — complete architectural design for this remediation feature (problem statement, provider template, per-provider gap list, .NET 10 driver strategy, phased delivery).
- `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` — file-by-file checklist, package pins, test-infrastructure move plan, acceptance criteria.
- `planning/ecosystem-expansion/Rig.TUnit-Library-Design.md` — 003 design (baseline, defines the bases and contracts this feature wires providers into).
- `planning/ecosystem-expansion/Rig.TUnit-Session-Handoff.md` — 003 handoff (namespaces, test matrix, conventions).
- `.claude/rules/*.md` — project rules (coding style, async, configuration, observability, security, testing).
- `src/` — current source layout.
- `tests/` — current test layout (target of the hygiene sweep).

## Feature

### Name
`004-provider-consistency-remediation`

### One-line summary
Bring every Rig.TUnit provider package up to a uniform `Fixture + Options + Builder + Extensions + Helpers + README` shape, implement the five packages 003 promised but never created (Cosmos, MySql, Oracle, AppInsights, Docker), add `SecurityRigBuilder<TSelf>` base, enforce the uniformity with architecture tests, and extract inline test-setup infrastructure out of test files — all **strictly test-first**.

---

## Hard requirements

### R1 — TDD non-negotiable
- Architecture tests (`ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`) land **before** the production changes they police — they must fail initially for every unfinished provider, then go green as each is completed.
- Every new provider builder lands with a contract-suite test that runs the family's shared `{Family}RigContract` suite against it. No production class without a failing test in the same commit (matches 003 R1).
- Per-package merge gate unchanged from 003: line ≥ 90%, branch ≥ 85%, contract suite 100%, parallel-isolation smoke passes.

### R2 — Uniform provider template
Every `Rig.TUnit.{Family}.{Provider}` package MUST contain:
- `Fixtures/{Provider}Fixture.cs` deriving from the family's fixture base (`SqlFixtureBase`, `DocumentFixtureBase`, `MessagingFixtureBase`, `CacheFixtureBase`, `StorageFixtureBase`, `TelemetryFixtureBase`, `SecurityFixtureBase`).
- `Options/{Provider}FixtureOptions.cs` with `public const string SectionName`, `[Required]` properties, and registered via `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.
- `Builder/{Provider}RigBuilder.cs` deriving from `{Family}RigBuilder<{Provider}RigBuilder>` (CRTP pattern already in use).
- `Builder/{Provider}RigBuilderExtensions.cs` exposing `Use{Provider}(this RigBuilder, Action<{Provider}RigBuilder>?)`.
- Family-specific `Helpers/` (see §5 of design doc).
- `README.md` > 100 chars with a 30-second quick-start.

### R3 — Five new packages (no deferrals)
Create, with full R2 shape + contract-suite compliance:
- `src/Rig.TUnit.Databases.Sql.MySql/` — Testcontainers.MySql 4.11+; EF provider `Pomelo.EntityFrameworkCore.MySql` pinned to `9.0.*` (PR #2019 tracking the official .NET 10 release — Pomelo 9 works on .NET 10 TFM).
- `src/Rig.TUnit.Databases.Sql.Oracle/` — Testcontainers.Oracle 4.11+; EF provider `Oracle.EntityFrameworkCore` 9.23.90+; image `gvenzl/oracle-free:23.5-slim-faststart`; 5-min startup probe to mitigate aspire#12036.
- `src/Rig.TUnit.Databases.NoSql.Cosmos/` — `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`; custom readiness probe hitting `/_explorer/emulator.pem`.
- `src/Rig.TUnit.Observability.AppInsights/` — in-process `ITelemetryChannel` capture (no container); assertion surface mirrors `TraceAssert`/`MetricAssert`.
- `src/Rig.TUnit.Docker/` — generic `ContainerFixture` + `DockerComposeFixture` (per 003 §4.10).

### R4 — Fill provider gaps (no new deletions)
For every existing provider listed in design doc §4.1–4.8, add the missing pieces per the family gap table. Explicit list:
- **Databases.NoSql:** Mongo + Cassandra + Dynamo + ElasticSearch + EventStore → add Builder + Extensions + provider helper (keyspace/GSI/index-refresh/stream-assert per 003 §4.4).
- **Messaging:** Kafka + RabbitMq + Nats + Sqs → add Builder + Extensions + `{Provider}Listener : ListenerBase` + `{Provider}EventSender : EventSenderBase`.
- **Caching:** Hybrid + Fusion → full template (Builder + Extensions + Options).
- **Storage:** AzureBlob + S3 + MinIO + FileSystem → add Builder + Extensions + `{Provider}SasBuilder` (FileSystem gets a path-sandbox helper instead).
- **Security:** add `SecurityRigBuilder<TSelf>` base (missing from 003 §3.3); wire Jwt/OAuth/Mtls/Policies as `{Provider}RigBuilder : SecurityRigBuilder<…>` + `Use{Provider}` extension. Mtls also gets a `MtlsFixture`.
- **Observability.Metrics:** add `MetricsFixture` wrapping `MeterListener` + `MetricsRigBuilder` + `UseMetricsCapture` extension + tag-cardinality guard.
- **Microservices:** EventSourcing, Saga, Contracts — fill out to match 003 §4.11 (`AggregateAssert`, `SagaAssert.Compensated()`, `ProviderVerificationFixture`).

### R5 — Test-file hygiene
- Every test `.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/` folders declares exactly one top-level class containing only `[Test]`, `[Before]`, `[After]` methods plus private helpers referenced only by those methods.
- All inline shared fixtures, test entities, fake handlers, builder helpers, and setup constants move to a per-project `TestInfrastructure/` subfolder using names `{Project}TestHarness.cs`, `Test{Entity}.cs`, `Test{Handler}.cs`, `Fake{Xxx}.cs`.
- **Test files are NOT split by method-under-test.** A 355-line `TraceAssertTests.cs` staying as one class is acceptable — only extract setup infrastructure.
- Enforced by `TestFileOrganizationTests` in `Rig.TUnit.Architecture.Tests/Rules/`.

### R6 — Architecture-test enforcement
Land these in `tests/Rig.TUnit.Architecture.Tests/Rules/` before the production changes:
- `ProviderCompletenessTests` — every `Rig.TUnit.{Family}.{Provider}` assembly exposes the required four types (Fixture, Options, Builder, Use-extension).
- `TestFileOrganizationTests` — R5 enforcement.
- `ReadmeCompletenessTests` — every provider package ships `README.md` > 100 chars.

### R7 — No feature deletion, no deferrals
- No package from the 003 design tree is removed.
- No "future work" deferrals — the five missing packages ship in this feature.
- No feature flags, no opt-in rollouts. The architecture tests are on by default from Phase 1.

### R8 — .NET 10 compatibility
- MySql: pin `Pomelo.EntityFrameworkCore.MySql` to `9.0.*` in `Directory.Packages.props` with a comment citing PR #2019; upgrade to Pomelo 10 becomes a packages-props-only change when released.
- Oracle: `Oracle.EntityFrameworkCore` 9.23.90+; `gvenzl/oracle-free:23.5-slim-faststart`; `WithWaitStrategy(Wait.ForListeningPorts())` + 5-min timeout.
- Cosmos: Linux emulator + custom readiness probe.
- AppInsights: `Microsoft.ApplicationInsights` 2.22+.
- Docker: `Testcontainers` 4.11+ (already pinned).

### R9 — Parallel-safety, versioning, rule compliance
- All 003 R5 (rule compliance), R6 (parallel safety), R9 (versioning lockstep) carry over unchanged.
- `Rig.TUnit.All` meta-package updated to reference the 5 new packages.
- CI matrix extended for MySql 8.x, Oracle 23, Cosmos vnext.

---

## Deliverables

### Source projects created
Per the 5-new-package list in R3. Each with full R2 template.

### Source projects modified
Per the gap list in R4. Only files added — no renaming or deletion of existing public APIs.

### Test projects created
- `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/`
- `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/`
- `tests/Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration/`
- `tests/Rig.TUnit.Observability.AppInsights.Tests.Integration/`
- `tests/Rig.TUnit.Docker.Tests.Integration/`

Each inherits `{Family}RigContract`, `ParallelIsolationContract`, and adds provider-specific quirk tests.

### Test projects modified (hygiene sweep per R5)
Every `tests/*` project gets a `TestInfrastructure/` folder where applicable. Specifically target the files listed in design doc §5 first:
- `Rig.TUnit.Observability.Tracing.Tests.Integration/TraceAssertTests.cs`
- `Rig.TUnit.Http.Tests.Unit/HttpMockTests.cs`
- `Rig.TUnit.Resilience.Tests.Integration/ResilienceTests.cs`
- `Rig.TUnit.Security.OAuth.Tests.Integration/MockOAuthServerTests.cs`
- `Rig.TUnit.Microservices.Outbox.Tests.Integration/OutboxTests.cs`
- All `*QuirkTests.cs` files.

### Architecture tests added
Per R6.

### Docs added
- `src/Rig.TUnit/Contributing-ProviderTemplate.md` — canonical template a new provider copies.
- `README.md` per provider package (see R2).

---

## Acceptance criteria

1. `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` all green across every provider package.
2. Every family's `{Family}RigContract` test suite runs against every provider in that family, 100% pass.
3. Every new package listed in R3 ships, is referenced by `Rig.TUnit.slnx`, pinned in `Directory.Packages.props`, and transitively referenced from `Rig.TUnit.All`.
4. Solution builds on .NET 10 with zero warnings above the baseline.
5. Coverage gates met per R1.
6. No regression in existing 003 tests — the final green test count is strictly greater than or equal to the 003 baseline.
7. `planning/provider-consistency-remediation/Rig.TUnit-Session-Handoff.md` checkboxes all ticked.
