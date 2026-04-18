# Rig.TUnit — Provider Consistency Remediation — Library Design

## 0. Status

**Stage:** post-003 gap-fix. Feature 003 (ecosystem expansion) landed 272/274 tasks and shipped the full base-package layer (`Rig.TUnit.Databases.{Sql,NoSql}`, `Rig.TUnit.Messaging`, `Rig.TUnit.Caching`, `Rig.TUnit.Storage`, `Rig.TUnit.Observability`, `Rig.TUnit.Security`). What did **not** land is **uniform provider surface area** — most providers ship only a fixture (+ options in some cases), leaving the per-provider builder, helpers, and DSL hooks promised by the 003 design doc (§4) unimplemented.

This feature fills those gaps. **No feature from the 003 design is deleted.** Packages that 003 promised but never created (`Cosmos`, `MySql`, `Oracle`, `AppInsights`, `Docker`) are created here. Provider-driver compatibility gaps on .NET 10 are worked around with pinned alternates — see §6.

Naming: feature branch `feat/provider-consistency-remediation`. Work lives under `planning/provider-consistency-remediation/`.

---

## 1. The three problems

### 1.1 Provider surface-area inconsistency

The 003 design doc specifies every provider ships `Fixture + Options + Builder (+ Helpers/Assertions as applicable)` (003 §4.3–4.9). Current reality (verified by file inventory in `src/`):

| Family | Complete | Missing builder | Fixture-only (minimum) |
|---|---|---|---|
| **Databases.Sql** | SqlServer, Sqlite | Postgresql (no extensions file) | — |
| **Databases.NoSql** | Redis (KV, no fixture — borrows `Caching.Redis`) | Mongo | **Cassandra, Dynamo, ElasticSearch, EventStore** |
| **Messaging** | ServiceBus | Kafka, RabbitMq | **Nats, Sqs** |
| **Caching** | Redis, Memory, Hybrid | — | **Fusion** |
| **Storage** | — | AzureBlob, S3 | **FileSystem, MinIO** |
| **Security** | Jwt (no builder base), OAuth (no builder base) | — | Mtls (builder only, no fixture), Policies (assert only) |
| **Observability** | Logging, Seq, Tracing | — | **Metrics (assert only, no `MeterListener` fixture)** |
| **Microservices.*** | Outbox | — | **Contracts, EventSourcing, Saga** (one file each) |

The bases themselves are healthy — `MessageAssert`, `BlobAssert`, `CacheAssert`, `MigrationAssert`, `StampedeTester`, `BackplaneCapture`, `ChangeFeedCapture`, `ListenerBase`, `EventSenderBase` all exist in the base packages. The breakage is that **providers don't wire their fixtures into the base builder chain**, so end-users can't write `.UseCassandra()` / `.UseKafka()` / `.UseS3()` fluently.

### 1.2 Missing packages from 003 design (never created)

003 §2 package tree promises these; `src/` shows they never landed:

- `Rig.TUnit.Databases.NoSql.Cosmos`
- `Rig.TUnit.Databases.Sql.MySql`
- `Rig.TUnit.Databases.Sql.Oracle`
- `Rig.TUnit.Observability.AppInsights`
- `Rig.TUnit.Docker` (generic Testcontainers fixture + compose)

**These will be implemented, not deferred.** §6 covers the driver compat strategy.

### 1.3 Test files mixing tests with infrastructure

Test files currently mix test methods with inline test infrastructure: `SharedFixture` class declarations at the top, custom DbContext/entity classes inside `*Tests.cs`, handler stubs defined next to `[Test]` methods. Examples:

- [Rig.TUnit.Microservices.Outbox.Tests.Integration/OutboxTests.cs:185](tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/OutboxTests.cs) — test methods next to `OutboxMessage` builders and envelope types.
- [Rig.TUnit.Observability.Tracing.Tests.Integration/TraceAssertTests.cs:355](tests/Rig.TUnit.Observability.Tracing.Tests.Integration/TraceAssertTests.cs) — `ActivitySource`, `TracerProvider` setup inline before each test.
- [Rig.TUnit.Resilience.Tests.Integration/ResilienceTests.cs:251](tests/Rig.TUnit.Resilience.Tests.Integration/ResilienceTests.cs) — Polly policy builders inlined.
- [Rig.TUnit.Http.Tests.Unit/HttpMockTests.cs:231](tests/Rig.TUnit.Http.Tests.Unit/HttpMockTests.cs) — custom matchers and response builders mixed with assertions.

**Rule for this feature:** Test files contain **tests only** — setup objects, fake handlers, test entities, and shared fixtures move into a per-project `TestInfrastructure/` (or `Fixtures/`, `Fakers/`, `Helpers/`) subfolder. This matches what `Rig.TUnit.Core.Tests.Unit/`, `Rig.TUnit.Grpc.Tests.Unit/`, `Rig.TUnit.WebAPI.Tests.Unit/`, and `Rig.TUnit.Mediator.Tests.Unit/` already do correctly. **We do NOT split test files by method-under-test** — a 355-line `TraceAssertTests.cs` staying as one class is fine, as long as all the tracer-provider scaffolding is extracted.

---

## 2. Goals

1. **Uniform provider shape.** Every provider package exports exactly this surface:
   - `Fixtures/{Provider}Fixture.cs` — `Testcontainers` wrapper deriving from the right base fixture.
   - `Options/{Provider}FixtureOptions.cs` — `[Required]` + `ValidateOnStart()`-bound options.
   - `Builder/{Provider}RigBuilder.cs` — `: {Family}RigBuilder<{Provider}RigBuilder>`.
   - `Builder/{Provider}RigBuilderExtensions.cs` — `Use{Provider}(...)` on `RigBuilder`.
   - `Helpers/` — provider-specific helpers from 003 §4 (keyspace-per-test, GSI verifier, SAS builder, backplane, Listener/Sender).
   - `README.md` — 30-second quick-start.
2. **Fill the five missing packages** (Cosmos, MySql, Oracle, AppInsights, Docker) with full Goal 1 shape.
3. **Architecture-test enforced.** A `ProviderCompletenessTests` suite fails the build if any `Rig.TUnit.{Family}.{Provider}` package is missing its required types.
4. **Test files contain tests only.** Extract all shared fixtures, test entities, test handlers, builders, and setup helpers into per-project `TestInfrastructure/`. No test file declares more than one top-level class.
5. **Zero feature regression.** All currently-green tests stay green. All 003 contract suites still pass.

Non-goals: splitting test files by method-under-test; renaming existing public APIs; re-organizing `src/` folder structure; introducing new families (messaging/storage/etc. families are frozen).

---

## 3. The provider template

Every provider folder must end up in this shape. Example for Cassandra:

```
src/Rig.TUnit.Databases.NoSql.Cassandra/
├── Rig.TUnit.Databases.NoSql.Cassandra.csproj
├── README.md
├── Fixtures/
│   └── CassandraFixture.cs                 : DocumentFixtureBase
├── Options/
│   └── CassandraFixtureOptions.cs
├── Builder/
│   ├── CassandraRigBuilder.cs              : NoSqlRigBuilder<CassandraRigBuilder>
│   └── CassandraRigBuilderExtensions.cs    // UseCassandra(this RigBuilder, Action<...>)
└── Helpers/
    └── KeyspacePerTestHelper.cs            // provider-specific per 003 §4.4
```

Minor variations by family:

- **Databases.Sql.*** adds `Extensions/{Provider}BuilderExtensions.cs` for EF-specific shortcuts (already present in SqlServer/Sqlite).
- **Messaging.*** adds `Helpers/{Provider}Listener.cs` + `Helpers/{Provider}EventSender.cs` (per 003 §4.5).
- **Storage.*** adds `Helpers/{Provider}SasBuilder.cs` (per 003 §4.7).
- **Observability.*** providers that emit telemetry (Metrics, AppInsights) add `Fixtures/{Provider}Fixture.cs` using the appropriate in-process listener instead of a container.

A provider is "complete" iff every file in its family template compiles, its extension method is registered in the `RigBuilder` fluent chain, and it passes the family's `{Family}RigContract` test suite in `tests/Rig.TUnit.{Family}.Tests.Contract`.

---

## 4. Provider-by-provider gap list

### 4.1 Databases.Sql

| Provider | Builder | BuilderExtensions | Notes |
|---|---|---|---|
| SqlServer | ✓ | ✓ | done |
| Sqlite | ✓ | ✓ | done |
| Postgresql | ✓ | **add** | add `PostgresBuilderExtensions` for `UsePostgresInMemory`-style EF quickstart |
| MySql | **add** | **add** | new package — driver pinned per §6 |
| Oracle | **add** | **add** | new package — driver pinned per §6 |

### 4.2 Databases.NoSql

| Provider | Fixture | Options | Builder | Extensions | Helper |
|---|---|---|---|---|---|
| Mongo | ✓ | ✓ | **add** `MongoRigBuilder` | **add** `UseMongo` | **add** `CollectionPerTestHelper` + `BsonDiff` |
| Redis (KV) | reuses `Caching.Redis` fixture via project ref | — | ✓ | ✓ | ✓ |
| Cassandra | ✓ | **add** | **add** | **add** | **add** `KeyspacePerTestHelper` |
| Dynamo | ✓ | **add** | **add** | **add** | **add** `GsiVerifier` (LocalStack) |
| ElasticSearch | ✓ | **add** | **add** | **add** | **add** `IndexRefreshHelper` + `DslAssert` |
| EventStore | ✓ | **add** | **add** | **add** | **add** `StreamAssert`, `ProjectionAssert` |
| **Cosmos** (new) | — | — | — | — | — → full template; use `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` |

### 4.3 Messaging

| Provider | Fixture | Options | Builder | Extensions | Helpers |
|---|---|---|---|---|---|
| ServiceBus | ✓ | ✓ | ✓ | ✓ | ✓ |
| Kafka | ✓ | ✓ | **add** | **add** | **add** `KafkaListener : ListenerBase`, `KafkaEventSender : EventSenderBase` |
| RabbitMq | ✓ | ✓ | **add** | **add** | **add** `RabbitMqListener`, `RabbitMqEventSender` |
| Nats | ✓ | **add** | **add** | **add** | **add** `NatsListener`, `NatsEventSender` |
| Sqs | ✓ | **add** | **add** | **add** | **add** `SqsListener`, `SqsEventSender` (LocalStack) |

### 4.4 Caching

| Provider | Fixture | Options | Builder | Extensions | Helpers |
|---|---|---|---|---|---|
| Memory | ✓ | — | ✓ | — need | — |
| Redis | ✓ | ✓ | ✓ | ✓ | ✓ (backplane) |
| Hybrid | ✓ | — | — | — | — → full template |
| Fusion | ✓ | — | — | — | — → full template |

### 4.5 Storage

All providers need `{Provider}RigBuilder : StorageRigBuilder<…>`, `Use{Provider}` extension, and `{Provider}SasBuilder`.

| Provider | Fixture | Options | Builder | Extensions | SasBuilder |
|---|---|---|---|---|---|
| AzureBlob | ✓ | ✓ | **add** | **add** | **add** |
| S3 | ✓ | ✓ | **add** | **add** | **add** |
| MinIO | ✓ | **add** | **add** | **add** | **add** |
| FileSystem | ✓ | **add** | **add** | **add** | — (N/A, expose path sandbox helper instead) |

### 4.6 Security

The 003 design promises `SecurityRigBuilder<TSelf>` base (§3.3) — doesn't exist. Add it.

| Provider | Current | Gaps |
|---|---|---|
| Jwt | `JwtBuilder` + `JwtBuilderOptions` | Add `JwtRigBuilder : SecurityRigBuilder<JwtRigBuilder>` + `UseJwt` extension. Keep `JwtBuilder` as-is (it's a token builder, not a rig builder). |
| OAuth | `MockOAuthServer` + options | Add `OAuthRigBuilder` + `UseOAuthServer` extension. |
| Mtls | `MtlsCertificateBuilder` only | Add `MtlsFixture` (generates CA + leaf on setup) + `MtlsRigBuilder` + `UseMtls` extension. |
| Policies | `PolicyAssert` only | Add `PolicyRigBuilder` that registers an in-memory `IAuthorizationService` + `UsePolicies` extension. |

### 4.7 Observability

| Provider | Current | Gaps |
|---|---|---|
| Logging | full | — |
| Tracing | full | — |
| Seq | full | — |
| Metrics | `MetricAssert` only | Add `MetricsFixture` wrapping `MeterListener` + `MetricsFixtureOptions` + `MetricsRigBuilder : TelemetryRigBuilder<…>` + `UseMetricsCapture` extension + tag-cardinality guard helper. |
| **AppInsights** (new) | — | Full template. Use in-process `TelemetryChannel` capture (no container) — no AppInsights Testcontainer exists; emulate via capturing `ITelemetryChannel` implementation. |

### 4.8 Microservices

003 §4.11 specifies rich surfaces. Current reality is one file each for Contracts/EventSourcing/Saga.

| Package | Current | Minimum add |
|---|---|---|
| Outbox | ✓ (near-complete) | — |
| Inbox | ✓ | — |
| EventSourcing | `EventSourcingHarness.cs` only | Add `AggregateAssert.Raised<T>().WithData(...)`, event-catalogue verifier, schema-evolution test helper. |
| Saga | `SagaHarness.cs` only | Add `SagaAssert.Step(...).Compensated()`, timeout helper. |
| Contracts | `ContractPact.cs` only | Add `ProviderVerificationFixture`, Pact broker integration stub. |
| Snapshots | ✓ | — |

### 4.9 New single-provider packages

| Package | Template |
|---|---|
| `Rig.TUnit.Docker` (new) | Generic `ContainerFixture` wrapping `Testcontainers` + `DockerComposeFixture`. No builder base — ships its own `DockerRigBuilder`. Image-pull cache reuse, per-test networks, healthcheck ready-detection (per 003 §4.10). |

---

## 5. Test-file hygiene plan

**Rule:** a test `.cs` file must declare exactly one top-level class, and that class must contain only `[Test]` / `[Before]` / `[After]` methods plus private helpers referenced only by those methods. Shared fixtures, test entities, fake handlers, builders, and long setup constants move to `TestInfrastructure/` or existing subfolders.

**Targets (by project, worst offenders first):**

1. [Rig.TUnit.Observability.Tracing.Tests.Integration/TraceAssertTests.cs](tests/Rig.TUnit.Observability.Tracing.Tests.Integration/TraceAssertTests.cs) — extract `ActivitySource` + `TracerProvider` factory into `TestInfrastructure/TracingTestHarness.cs`.
2. [Rig.TUnit.Http.Tests.Unit/HttpMockTests.cs](tests/Rig.TUnit.Http.Tests.Unit/HttpMockTests.cs) — extract custom matchers into `TestInfrastructure/`.
3. [Rig.TUnit.Resilience.Tests.Integration/ResilienceTests.cs](tests/Rig.TUnit.Resilience.Tests.Integration/ResilienceTests.cs) — extract Polly pipeline builders.
4. [Rig.TUnit.Security.OAuth.Tests.Integration/MockOAuthServerTests.cs](tests/Rig.TUnit.Security.OAuth.Tests.Integration/MockOAuthServerTests.cs) — extract JWKS/key-generation helpers.
5. [Rig.TUnit.Microservices.Outbox.Tests.Integration/OutboxTests.cs](tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/OutboxTests.cs) — extract `OutboxMessage` seed builders, envelope fakers.
6. All `*Contract.cs` files — they're already well-structured but verify no inline fixture registration.
7. Every `*QuirkTests.cs` — scan for inline test entities; move to `TestInfrastructure/`.

**Naming inside `TestInfrastructure/`:** `{Project}TestHarness.cs`, `Test{Entity}.cs`, `Test{Handler}.cs`, `Fake{Xxx}.cs`. Matches the pattern used in `Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/`.

**Enforcement:** extend `Rig.TUnit.Architecture.Tests/Rules/CodeOrganizationTests.cs` with a rule that fails if a file under `tests/**/*.cs` outside a `TestInfrastructure/` folder contains more than one top-level class declaration.

---

## 6. .NET 10 driver strategy for new provider packages

### 6.1 MySql

The canonical EF Core MySQL provider is `Pomelo.EntityFrameworkCore.MySql`. Latest stable is **9.0.0** (EF Core 9 + .NET 8+). A .NET 10 / EF Core 10 release is in PR #2019 but not merged; the maintainer has been quiet. A community fork (Microting) has shipped EF 10 support.

**Decision:** use **Pomelo 9.0.0 running on .NET 10**, not the fork. Pomelo 9 is forward-compatible with .NET 10 runtime — only the EF Core major version moves (9 vs 10), and EF Core 9 packages run on the .NET 10 TFM without issues for test-harness scenarios. Pin `Pomelo.EntityFrameworkCore.MySql` to `9.0.*` and document the pinning in `Directory.Packages.props` with a comment pointing to this plan.

If Pomelo 10 ships before merge, the upgrade is a single `Directory.Packages.props` bump — no code change needed. If it still isn't out at merge time, revisit the Microting fork as a fallback only after the feature is otherwise complete.

Testcontainer: `Testcontainers.MySql` 4.11+ (already .NET 10-compatible; used by our existing providers).

### 6.2 Oracle

`Oracle.EntityFrameworkCore` 9.23.90+ supports .NET 8 and runs on .NET 10. Testcontainer: `Testcontainers.Oracle` 4.11+ using `gvenzl/oracle-free:23.5-slim-faststart` (Oracle-Free replaced Oracle-XE — the XE image is no longer maintained by Oracle's container team).

**Known risk:** Aspire project reports intermittent container-init hangs (aspire#12036). Mitigate with a 5-minute startup-probe timeout + explicit `WithWaitStrategy(Wait.ForListeningPorts())` in the fixture.

### 6.3 Cosmos

Use the Linux-based vNext emulator: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`. Testcontainers for .NET has a known readiness issue (dotnet/testcontainers-dotnet #1306) — work around with a custom wait strategy that probes `/_explorer/emulator.pem` over HTTPS with self-signed cert trust.

SDK: `Microsoft.Azure.Cosmos` latest (runs on .NET 10). No Pomelo-style pinning concerns.

### 6.4 AppInsights

No container exists. Implement `AppInsightsFixture` as an in-process `ITelemetryChannel` capture using `Microsoft.ApplicationInsights` 2.22+. `AppInsightsAssert` mirrors `TraceAssert` / `MetricAssert` surface.

### 6.5 Docker

`Testcontainers` 4.11+ is already in `Directory.Packages.props`. Add `DotNet.Testcontainers` compose support via `Testcontainers.Compose` (or fall back to `Ductus.FluentDocker` if Testcontainers' compose support regresses on .NET 10 — verify at implementation).

---

## 7. Architecture-test enforcement

New tests added to `tests/Rig.TUnit.Architecture.Tests/Rules/`:

- **`ProviderCompletenessTests.cs`** — For each `Rig.TUnit.{Family}.{Provider}` assembly, assert the existence of:
  - `{Provider}Fixture` deriving from the family's fixture base.
  - `{Provider}FixtureOptions` with `SectionName` + `[Required]` properties.
  - `{Provider}RigBuilder` deriving from the family's `{Family}RigBuilder<TSelf>`.
  - A public `Use{Provider}` extension method on `RigBuilder`.
- **`TestFileOrganizationTests.cs`** — Fails if any file under `tests/**/*.cs` outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `obj/`, `bin/` contains >1 top-level class.
- **`ReadmeCompletenessTests.cs`** — Fails if a provider package ships without `README.md` > 100 chars.

These tests are added to CI's default test suite so the gates fire on every PR.

---

## 8. Phased delivery

No phase starts before the previous is green + architecture tests passing.

### Phase 1 — Enforcement scaffolding (foundation)
- Add `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` — initially skipping the providers we're about to fix, so green stays green.
- Add `SecurityRigBuilder<TSelf>` base.
- Document template in `src/Rig.TUnit/Contributing-ProviderTemplate.md`.

### Phase 2 — Test hygiene sweep
- Move inline test infrastructure into `TestInfrastructure/` subfolders across all target projects (§5).
- Turn on `TestFileOrganizationTests` across every test project.

### Phase 3 — Close gaps in existing providers
- Databases.NoSql: Mongo, Cassandra, Dynamo, ElasticSearch, EventStore — add builder + extension + helper.
- Messaging: Kafka, RabbitMq, Nats, Sqs — add builder + extension + Listener/EventSender.
- Storage: all four — add builder + extension + SasBuilder.
- Caching: Hybrid, Fusion — complete template.
- Security: Jwt, OAuth, Mtls, Policies — wire into `SecurityRigBuilder` base.
- Observability.Metrics: add fixture + builder.
- Turn on `ProviderCompletenessTests` for every existing provider.

### Phase 4 — New packages
- `Rig.TUnit.Databases.Sql.MySql` (Pomelo 9 pinned).
- `Rig.TUnit.Databases.Sql.Oracle`.
- `Rig.TUnit.Databases.NoSql.Cosmos`.
- `Rig.TUnit.Observability.AppInsights`.
- `Rig.TUnit.Docker`.
- Each lands with full template + contract-suite compliance + README + architecture-test pass.

### Phase 5 — Microservices depth
- EventSourcing: `AggregateAssert` + catalogue verifier + schema-evolution helper.
- Saga: `SagaAssert.Step(...).Compensated()` + timeout helper.
- Contracts: `ProviderVerificationFixture` + Pact broker client stub.

### Phase 6 — Polish
- Add README to every provider that lacks one (57 of 59 today).
- Update `Rig.TUnit.All` meta-package to reference the 5 new packages.
- Update `Rig.TUnit.Microservices` meta-package if applicable.

---

## 9. Acceptance criteria (merge gate for the whole feature)

- `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` all green.
- Every `{Family}RigContract` test suite passes against every provider in the family.
- All 5 new packages present in `Rig.TUnit.slnx` and `Directory.Packages.props`.
- `Rig.TUnit.All` meta-package transitively references every provider.
- Line coverage ≥ 90% / branch ≥ 85% per new or modified package (003 §5.6).
- Zero regressions — 003 test count stays ≥ its final green count.
- Every provider ships a `README.md` > 100 chars.

---

## 10. Out of scope

- Renaming existing public APIs (e.g., `RedisKvFixture` → `SharedRedisKvFixture`) — deferred to a separate naming-cleanup feature.
- Splitting any test file by method-under-test.
- Publishing to NuGet (this is still a pre-release library).
- Introducing new infrastructure families (GraphQL, SignalR, FeatureFlags per 003 §8 remain future work).

---

## 11. References

- `planning/ecosystem-expansion/Rig.TUnit-Library-Design.md` — 003 feature design (baseline).
- `planning/ecosystem-expansion/Rig.TUnit-Session-Handoff.md` — 003 implementation handoff.
- `.claude/rules/*.md` — project-wide constraints (coding style, async, configuration, testing).
- Audit findings: `src/` inventory verified 2026-04-18.
- .NET 10 driver research:
  - [Pomelo.EntityFrameworkCore.MySql PR #2019](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019)
  - [Testcontainers.MySql 4.11.0](https://www.nuget.org/packages/Testcontainers.MySql)
  - [Testcontainers.Oracle 4.11.0](https://www.nuget.org/packages/Testcontainers.Oracle)
  - [Azure Cosmos DB Linux emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux)
  - [Testcontainers-dotnet Cosmos discussion #1306](https://github.com/testcontainers/testcontainers-dotnet/discussions/1306)
