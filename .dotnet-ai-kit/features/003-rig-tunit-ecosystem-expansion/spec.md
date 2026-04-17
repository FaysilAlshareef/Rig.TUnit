# Feature Specification: Rig.TUnit Ecosystem Expansion

**Feature ID**: 003-rig-tunit-ecosystem-expansion
**Created**: 2026-04-17
**Status**: Draft
**Input**: "Use ecosystem-expansion planning docs (prompt, design, handoff). Focus on TDD (RED-GREEN-REFACTOR) for every feature."

---

## Overview

Transform Rig.TUnit from six packages (Core, Mediator, Grpc, WebAPI, SqlServer, Redis, ServiceBus) into a full microservice test platform of ~50 packages organized by the **Base + Provider** pattern covering: Databases (SQL + NoSQL), Messaging, Caching, Storage, Observability, Security, Resilience, HTTP mocking, Health Checks, Concurrency, Parallelism, CI, and Microservice patterns (Outbox / Inbox / Event Sourcing / Saga / Snapshots / Contracts).

**Delivery mode**: **hard cutover** — the library is pre-release. Old packages (`Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus`) are deleted outright. Their code is relocated into provider packages with updated namespaces. No `[Obsolete]` shims, no backwards-compatibility wrappers.

**Delivery discipline**: **strictly test-first**. Every production class ships in the same commit as its failing test (RED). The minimum code to pass follows (GREEN). Refinements keep tests green (REFACTOR).

---

## User Stories

### User Story 1 - TDD Discipline & RED-GREEN-REFACTOR Cycle (Priority: P1)

As a contributor to Rig.TUnit, I need every feature, class, assertion, and provider to ship under a strict RED-GREEN-REFACTOR cadence so that the library's core promise — "trustworthy test infrastructure" — is itself test-proven and the merge gate prevents untested code from entering `master`.

**Acceptance Scenarios**:

1. **Given** a new production class (fixture, builder, helper, assertion), **When** I open a pull request, **Then** the same PR MUST contain the class AND its failing test (RED) committed together; a PR with production code but no matching test is blocked by the PR template and CI.
2. **Given** a RED test committed at `tHEAD-2`, **When** I commit the minimum implementation at `tHEAD-1`, **Then** the test becomes GREEN and any further refinements at `tHEAD` keep the test GREEN — the commit log MUST show RED → GREEN → REFACTOR cadence.
3. **Given** a base contract (`ISqlRig`, `ICacheRig`, etc.), **When** I define it, **Then** its abstract contract test class (`SqlRigContract`, `CacheRigContract`) MUST exist BEFORE any provider implements the contract — the contract test is the single source of truth for provider compliance.
4. **Given** a provider (e.g., `Rig.TUnit.Caching.Redis`), **When** I add it, **Then** it MUST inherit the base contract test class in its `Tests.Integration` project and implement the fixture-provider abstract method; no provider ships if a single contract test fails.
5. **Given** a package PR, **When** CI runs, **Then** the merge gate MUST enforce: line coverage ≥ 90%, branch coverage ≥ 85%, contract suite 100% green, parallel-isolation smoke green, XML docs on all public API.
6. **Given** the full solution, **When** `Rig.TUnit.Architecture.Tests` runs, **Then** it MUST verify every public type in `src/` has at least one referencing test assembly, catching orphan "untested" public surface.
7. **Given** a REFACTOR phase, **When** I extract a helper or tighten a signature, **Then** no test is modified to accommodate the refactor — if tests must change, it is a behavior change, not a refactor, and requires a new RED test.

---

### User Story 2 - Phase A: Base Contracts + Hard Cutover (Priority: P1)

As the library maintainer, I need Phase A to (a) establish the Base + Provider pattern through contract-first base packages and (b) execute the hard cutover so old packages are deleted and their code relocated into the new provider packages, with all 56 existing tests ported and GREEN under the new layout.

**Acceptance Scenarios**:

1. **Given** a clean checkout, **When** I inspect `src/`, **Then** `Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus` directories MUST NOT exist — they are deleted outright.
2. **Given** the deleted `SqlServerContainerExtensions`, `RedisContainerExtensions`, `ServiceBusContainerExtensions`, `GrpcServiceReplacementExtensions` files, **When** I search for them, **Then** no such files exist; `GrpcServiceReplacementExtensions`' generic logic MUST be merged into `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`.
3. **Given** Phase A base packages (`Rig.TUnit.Databases`, `.Databases.Sql`, `.Databases.NoSql`, `.Messaging`, `.Caching`), **When** I build them, **Then** each MUST define: its `I{Area}Rig` contract, its `{Area}FixtureBase`, its `{Area}RigBuilder<TSelf>`, its `{Area}Assert` static DSL, and its contract test abstract class — BEFORE any provider is created.
4. **Given** the relocated `SqlServerFixture`, **When** it is defined, **Then** it MUST live in `src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`, inherit `SqlFixtureBase`, and pass the full `SqlRigContract` test suite.
5. **Given** the relocated `DbContextHelper<TContext>`, **When** promoted, **Then** it MUST live in `src/Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` and be EF-provider-agnostic (works with EF InMemory, SQLite, SqlServer, and future providers without modification).
6. **Given** the relocated `RedisFixture`, **When** it is placed, **Then** its primary home MUST be `src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs`; `Rig.TUnit.Databases.NoSql.Redis` MUST project-reference `Rig.TUnit.Caching.Redis` for the shared fixture and add only KV-role helpers (`KeyScanHelper`, `RedisKvRigBuilder`).
7. **Given** the relocated `ServiceBusFixture`, **When** placed, **Then** it lives in `src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`; `ListenerHelper` is SPLIT into `Rig.TUnit.Messaging/Helpers/ListenerBase.cs` (base, provider-agnostic) + `Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs` (provider-specific); same split for `ServiceBusEventSender`.
8. **Given** Phase A completion, **When** I run `dotnet test` on the full solution, **Then** at least 56 tests MUST be GREEN — the pre-existing test count is preserved or increased, all under the new namespaces.
9. **Given** `Rig.TUnit.slnx`, **When** inspected, **Then** it MUST NOT reference the four deleted source projects or the four deleted test projects.
10. **Given** `InMemoryDbExtensions` (EF InMemory provider wiring), **When** relocated, **Then** it MUST live at `src/Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` — KEPT as the fastest fast-path option; it is not deleted.

---

### User Story 3 - Phase A: Three-Way SQL Fast-Path Choice (Priority: P1)

As a developer writing database tests, I need three fast-path options (EF InMemory, SQLite `:memory:`, Testcontainers SqlServer) ordered by fidelity so that I can choose the right trade-off per scenario (pure-logic test → container-fidelity integration test) and the same `DbContextHelper` CRUD contract passes against all three paths.

**Acceptance Scenarios**:

1. **Given** a `DbContextHelper<TContext>` test class inheriting `SqlFastPathContract`, **When** I run it against `InMemoryDbExtensions` (EF InMemory), `Rig.TUnit.Databases.Sql.Sqlite` (SQLite `:memory:`), and `Rig.TUnit.Databases.Sql.SqlServer` (container), **Then** all three MUST pass the shared CRUD contract tests (insert, query, update, delete, seed, transaction-scope rollback).
2. **Given** the new `Rig.TUnit.Databases.Sql.Sqlite` package, **When** I call `rig.UseSqlite(source, sql => ...)`, **Then** it wires `Microsoft.EntityFrameworkCore.Sqlite` against an in-memory SQLite connection, with a `SqliteFixture`, `SqliteRigBuilder`, and `SqliteRigBuilderExtensions`.
3. **Given** dialect-specific behaviors, **When** tests run against each path, **Then** documented differences (no FK enforcement in EF InMemory, case-insensitive collation in SQLite default, `rowversion` only in SqlServer) MUST be explicitly asserted in provider-specific test classes — not swept under shared contracts.
4. **Given** a developer opening the fluent API, **When** they browse `IntelliSense`, **Then** `UseInMemoryDb`, `UseSqlite`, `UseSqlServer` MUST all appear on the builder, with XML docs explaining when to use each.
5. **Given** migration assertions (`MigrationAssert.AllApplied()`), **When** run against SQLite, **Then** they MUST pass for real migrations; **When** run against EF InMemory, **Then** they MUST skip with a documented "in-memory provider does not apply migrations" signal (not fail).

---

### User Story 4 - Phase A: Parallel-Isolation Contract (Priority: P1)

As a test author running 20 fixtures in parallel, I need every fixture across every area to generate a unique `IsolationKey` derived from the test's execution context so that database names, topic suffixes, cache prefixes, blob containers, and Docker networks never collide.

**Acceptance Scenarios**:

1. **Given** any `{Area}FixtureBase`, **When** it initializes, **Then** it MUST expose an `IsolationKey` property derived from the test's `ExecutionContext` (e.g., test method full-name hash + short GUID suffix).
2. **Given** the shared `ParallelIsolationContract` test, **When** a provider's `Tests.Integration` project inherits it, **Then** 20 parallel fixture instances MUST exhibit zero cross-talk: distinct `IsolationKey`s, distinct container names / schemas / topics / cache prefixes.
3. **Given** `Rig.TUnit.Parallelism.Port.Allocator`, **When** 100 concurrent requests hit it, **Then** no two callers MUST receive the same port; collisions ARE a P0 defect.
4. **Given** `Rig.TUnit.Parallelism.SharedState.Detector`, **When** a test writes to a `static` field used across tests, **Then** the detector MUST flag it at test-run time with a readable diagnostic.

---

### User Story 5 - Phase B: Observability (Logging + Seq + Tracing) (Priority: P2)

As a developer asserting observability behavior, I need in-memory capture and query DSLs for logging, Seq, and OpenTelemetry tracing so that tests can express log/trace expectations declaratively and the library enforces `.claude/rules/observability.md` anti-patterns.

**Acceptance Scenarios**:

1. **Given** `Rig.TUnit.Observability.Logging`, **When** I register its in-memory `ILoggerProvider`, **Then** `LogAssert.Logged(Warning).WithProperty("OrderId", id).InScope("TenantId", t).Once()` MUST match structured entries captured from the test host.
2. **Given** the anti-pattern detector, **When** a production class logs with an interpolated template (`$"Processing order {id}"`), **Then** the detector MUST fail the test with a diagnostic referencing the offending call site.
3. **Given** the anti-pattern detector, **When** a production class logs a property named `Password`, `Token`, `ConnectionString`, `Ssn`, or similar PII-shaped key, **Then** the detector MUST fail the test.
4. **Given** `Rig.TUnit.Observability.Seq`, **When** I call `rig.UseSeq(...)`, **Then** a `datalust/seq` Testcontainer MUST boot, a Serilog `Seq` sink MUST be wired into the test host, and `SeqAssert.Query("Level=@Warning and OrderId=@id").Count(1).Within(5.Seconds())` MUST return expected hits.
5. **Given** `Rig.TUnit.Observability.Seq`, **When** a test completes, **Then** a dashboard snapshot (PNG or structured JSON) MUST be captured as a CI artifact under `TestResults/seq-dashboards/`.
6. **Given** `Rig.TUnit.Observability.Tracing`, **When** an in-memory OTEL exporter is registered, **Then** `TraceAssert.HasSpan("POST /orders").WithTag("http.status_code", 201).WithStatus(Ok).WithParent("API gateway").DurationLessThan(500.Milliseconds())` MUST match captured spans.
7. **Given** both `.Logging` and `.Seq`, **When** a test switches providers, **Then** the assertion surface MUST be shared (identical `LogAssert` / `SeqAssert` shape) so migration is a one-line change.

---

### User Story 6 - Phase B: Security (JWT + OAuth) (Priority: P2)

As a developer testing authenticated and authorized endpoints, I need real JWT tokens accepted by real `JwtBearerHandler` middleware and a `MockOAuthServer` implementing the OIDC discovery flow so that authentication tests exercise the genuine pipeline without bypass.

**Acceptance Scenarios**:

1. **Given** `JwtBuilder.Issuer("https://issuer").Audience("api").Claim("sub", "user-1").ExpiresIn(5.Minutes()).SignedWithHs256(key).Build()`, **When** I attach the token to an `HttpClient` request, **Then** real `.AddJwtBearer(...)` middleware MUST accept it and the `ClaimsPrincipal` MUST contain the configured claims.
2. **Given** `JwtBuilder.SignedWithRs256(cert).Build()`, **When** the endpoint is configured to validate via JWKS, **Then** the library's `JwksEndpoint` stub MUST serve the `kid`-matched public key and the token is accepted.
3. **Given** an expired / tampered / not-yet-valid token, **When** attached, **Then** middleware MUST reject with `401 Unauthorized` and `SecurityAssert.Rejected(Reason.Expired | Reason.InvalidSignature | Reason.NotYetValid)` MUST match.
4. **Given** `MockOAuthServer`, **When** started, **Then** it MUST expose `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration`; `rig.UseOAuth(oauth => oauth.ClientCredentials("id", "secret"))` MUST complete a full client-credentials flow and return a valid JWT accepted by the app.
5. **Given** the existing `TestAuthenticationHandler` from Phase 2 (002 feature), **When** a new test is authored for JWT/policy scenarios, **Then** it MUST use `Rig.TUnit.Security.Jwt` / `.OAuth` — the legacy handler is kept ONLY as a smoke-test helper (no new JWT/policy tests may use it).

---

### User Story 7 - Phase B: Http (WireMock-style) + Resilience (Priority: P2)

As a developer stubbing external HTTP dependencies and asserting resilience policies, I need an in-process HTTP mock with matcher/scenario/replay support and a Polly-aware resilience testkit that uses `FakeTimeProvider` so that tests are deterministic and fast.

**Acceptance Scenarios**:

1. **Given** `Rig.TUnit.Http`, **When** I declare `HttpMock.When(req => req.Method(POST).Path("/orders")).Respond(201, body).WithDelay(100.Milliseconds()).OnlyOnScenario("happy-path")`, **Then** the stub MUST match the request and respond accordingly; `HttpMock.Verify().Called(3).WithHeader("Idempotency-Key", ...)` MUST verify interaction counts.
2. **Given** a scenario state machine, **When** 3 sequential calls hit `/orders`, **Then** the mock MUST advance state (e.g., "first call 500, second 500, third 201") deterministically.
3. **Given** record/replay mode, **When** enabled against a real endpoint, **Then** requests MUST be recorded to disk and replayable without the real endpoint in subsequent runs.
4. **Given** `Rig.TUnit.Resilience`, **When** I inject `FakeTimeProvider` and call `Advance(30.Seconds())`, **Then** a Polly retry policy MUST advance through its backoff deterministically (no `Task.Delay` / `Thread.Sleep`).
5. **Given** a circuit-breaker policy, **When** I assert `CircuitBreakerAssert.State(Open).After(failures: 5)`, **Then** the assertion MUST verify the state transition Closed → Open → HalfOpen → Closed under controlled time.
6. **Given** a rate-limit policy, **When** tested, **Then** `RateLimitAssert.Permits(10).PerSecond().Rejects(11thCall)` MUST match.

---

### User Story 8 - Phase C: Microservice Patterns (Outbox + Inbox + EventSourcing + Snapshots) (Priority: P3)

As a microservice author, I need provider-agnostic fixtures for Outbox, Inbox, Event Sourcing, and Snapshot-based approval testing so that I can assert transactional messaging guarantees across any configured DB + broker combination.

**Acceptance Scenarios**:

1. **Given** `Rig.TUnit.Microservices.Outbox`, **When** I wire it over `Rig.TUnit.Databases.Sql.SqlServer` + `Rig.TUnit.Messaging.ServiceBus`, **Then** `OutboxAssert.Contains<OrderCreated>().WithAggregateId(id).OnTopic("order-commands").ExactlyOnce().Relayed().Within(5.Seconds())` MUST match.
2. **Given** a concurrent relay simulator, **When** run 100× in parallel against the same outbox row, **Then** the consumer MUST observe the message `ExactlyOnce`; duplicates are a P0 defect.
3. **Given** a poison message, **When** the relay detects it, **Then** the Dead-Letter branch MUST fire and `OutboxAssert.InDeadLetter<OrderCreated>().WithReason(...)` MUST match.
4. **Given** `OutboxReplay`, **When** I invoke it against a timestamp range, **Then** messages MUST be republished in order to support projection rebuilds.
5. **Given** `Rig.TUnit.Microservices.Inbox`, **When** the same sequence number arrives twice, **Then** `InboxAssert.SequenceApplied(aggregateId, seq).Idempotent()` MUST pass and downstream handlers MUST run exactly once.
6. **Given** `Rig.TUnit.Microservices.EventSourcing`, **When** I write `When(events).Then(state)` against an aggregate, **Then** the harness MUST apply events, assert resulting state, and verify the event catalogue (no unknown event, no version drift).
7. **Given** `Rig.TUnit.Microservices.Snapshots`, **When** I assert `SnapshotAssert.Match(actual, fileName)`, **Then** first run MUST create `fileName.received.*`, second run MUST pass if unchanged, mismatch MUST produce a readable diff, and scrubbers MUST redact GUIDs / timestamps / correlation-IDs / sequence-numbers by default.
8. **Given** the snapshot on-disk format, **When** a developer has existing `Verify.Xunit` snapshots, **Then** the format MUST be Verify-compatible so migration is frictionless.

---

### User Story 9 - Phase C: Concurrency + HealthChecks (Priority: P3)

As a developer asserting optimistic-concurrency, ETag-based HTTP preconditions, and health probes, I need first-class fixtures that exercise real middleware/data layers so that correctness under contention is provable.

**Acceptance Scenarios**:

1. **Given** `ConcurrencyAssert.TwoWriters(entity).OneWinsWith<DbUpdateConcurrencyException>()`, **When** run against SqlServer + Postgres + Cosmos + Mongo, **Then** exactly one writer MUST succeed and the other MUST throw the expected concurrency exception. **Note**: SqlServer coverage lands in Phase C (when `Rig.TUnit.Concurrency` ships); Postgres + Cosmos + Mongo coverage lands in Phase D (when those providers ship). Acceptance for this scenario is considered met at the end of Phase D, not Phase C.
2. **Given** an HTTP endpoint accepting `If-Match` preconditions, **When** the `If-Match` header does not equal the current ETag, **Then** the endpoint MUST return `412 Precondition Failed` and `ConcurrencyAssert.Precondition.IfMatchFails()` MUST match.
3. **Given** `If-None-Match` matching the current ETag, **When** `GET` is issued, **Then** `304 Not Modified` MUST be returned and `ConcurrencyAssert.Precondition.NotModified()` MUST match.
4. **Given** sequence-number idempotency (as in `architecture.md`), **When** the same sequence arrives twice, **Then** the second is a no-op and `InboxAssert.Idempotent()` passes.
5. **Given** `Rig.TUnit.HealthChecks`, **When** I call `HealthAssert.IsHealthy("/health/ready").Contains("sqlserver").InTime(2.Seconds())`, **Then** the probe MUST be hit, specific dependencies enumerated, and timing asserted.
6. **Given** a dependency-down simulator, **When** I disable the SqlServer dependency, **Then** `/health/ready` MUST flip to `Unhealthy` and `/health/live` MUST remain `Healthy` — startup vs liveness vs readiness MUST be distinguished.

---

### User Story 10 - Phase C: Caching Coherency (Priority: P2)

As a developer asserting real-world caching semantics, I need first-class primitives for stampede, tag invalidation, backplane coherency, fail-safe, and negative caching — not thin `IDistributedCache` wrappers.

**Acceptance Scenarios**:

1. **Given** 100 concurrent cache misses against the same key, **When** driven through `CacheAssert.Stampede(key).ConcurrentMisses(100).ProducerCalledOnce()`, **Then** the producer delegate MUST execute exactly once and 99 callers MUST receive the cached value.
2. **Given** a set of keys tagged "orders" and a set tagged "products", **When** `cache.InvalidateTag("orders")`, **Then** `CacheAssert.TagInvalidation("orders").Purges(orderKeys).Keeps(productKeys)` MUST match.
3. **Given** two cache nodes sharing a Redis backplane, **When** node A invalidates a key, **Then** `CacheAssert.Coherent(acrossNodes: 2).Within(500.Milliseconds())` MUST match for node B.
4. **Given** a cache backend that throws, **When** fail-safe mode is enabled, **Then** `CacheAssert.FailSafe().WhenBackendThrows.ServesStaleFor(softTtl)` MUST match — stale values are returned for the soft-TTL window.
5. **Given** a negative cache entry (null / 404), **When** cached, **Then** `CacheAssert.NegativeCached(key).WithShorterTtl()` MUST match — negative TTL is strictly less than positive TTL.
6. **Given** `ClockControl` paired with `FakeTimeProvider`, **When** advancing time past TTL, **Then** TTL expiration MUST be observable without `Task.Delay`.

---

### User Story 11 - Phase D: Provider Expansion (Priority: P3)

As a developer working across multiple databases and brokers, I need provider packages for Postgres, MySql, Cosmos, Mongo, Kafka, RabbitMQ, HybridCache, FusionCache, Azure Blob, and S3 so that I can test against the full microservice stack with one uniform API.

**Acceptance Scenarios**:

1. **Given** a new provider (e.g., `Rig.TUnit.Databases.Sql.Postgresql`), **When** I add it, **Then** provider-specific code MUST be ≤ ~200 LOC (fixture + wait strategy + dialect quirks only) and the provider MUST pass the base's full contract test suite.
2. **Given** provider-specific quirks (Postgres `xmin`, SqlServer `rowversion`, MySQL `AUTO_INCREMENT`, Oracle PL/SQL, Cosmos RU charge, Mongo BSON diff, Cassandra keyspaces), **When** tested, **Then** at least 3 documented differences MUST be explicitly asserted per provider in provider-specific test classes.
3. **Given** CI matrix, **When** the build runs, **Then** Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x MUST all be exercised against their providers.
4. **Given** `Rig.TUnit.Caching.Hybrid` (.NET 9+ HybridCache) and `.Fusion` (FusionCache), **When** tested, **Then** both MUST pass the shared `CacheRigContract` + the real-world coherency tests (stampede, tag, fail-safe, backplane, negative).
5. **Given** `Rig.TUnit.Storage.AzureBlob` (Azurite) and `.S3` (LocalStack), **When** tested, **Then** `BlobAssert.Exists(container, key).WithContentType(...).WithSize(x).WithMetadata(k, v)` MUST match; lifecycle rule assertions MUST match.

---

### User Story 12 - Phase E: Polish (Docker + Parallelism + Ci + Saga + Contracts + remaining providers) (Priority: P3)

As a maintainer finishing the ecosystem, I need Docker orchestration, parallelism guardrails, CI enrichers, Saga testing, Contract (Pact-style) testing, AppInsights, mTLS, Policies, and the remaining providers (Oracle, Dynamo, Cassandra, EventStore, ElasticSearch, SQS, NATS, MinIO, FileSystem) so that the ecosystem is complete.

**Acceptance Scenarios**:

1. **Given** `Rig.TUnit.Docker`, **When** I declare a compose topology, **Then** multi-container fixtures MUST spin up with healthcheck-based readiness and reuse image-pull caches between tests.
2. **Given** `Rig.TUnit.Parallelism`, **When** used, **Then** the port allocator, schema/topic/prefix generator, shared-state detector, and `[ExclusiveResource]` coordinator MUST all compose with every other area's fixtures.
3. **Given** `Rig.TUnit.Ci`, **When** a test run completes, **Then** TRX/JUnit outputs MUST be enriched with span IDs, container logs, and screenshots; flaky quarantine reports and coverage-delta enforcement MUST run; GitHub Actions / Azure DevOps annotations MUST be emitted.
4. **Given** `Rig.TUnit.Microservices.Saga`, **When** I define a saga, **Then** step verification, compensation on failure, and timeout (pair with Resilience) MUST be assertable.
5. **Given** `Rig.TUnit.Microservices.Contracts`, **When** I author a Pact-style contract over `Rig.TUnit.Http` (REST) or `Rig.TUnit.Grpc` (RPC), **Then** consumer and provider MUST verify independently and publish to a broker.
6. **Given** `Rig.TUnit.Observability.Metrics` + `.AppInsights`, **When** a test captures metrics, **Then** `MetricAssert.Counter("orders.created").Incremented(3).WithTag(...)` and histogram bucket/percentile verification MUST work; the tag-cardinality guard MUST fail tests that emit > N distinct tag combinations.
7. **Given** `Rig.TUnit.Security.Mtls` and `.Policies`, **When** a mTLS handshake is tested, **Then** self-signed CA + leaf cert generation MUST succeed and `PolicyAssert.Policy("AdminOnly").Allows(principal).Denies(other)` MUST evaluate real ASP.NET Core policies against synthetic `ClaimsPrincipal`.
8. **Given** remaining providers (Oracle, Dynamo, Cassandra, EventStore, ElasticSearch, SQS, NATS, MinIO, FileSystem), **When** they ship, **Then** each MUST pass its base's contract suite + at least 3 quirk tests.

---

### User Story 13 - NetArchTest Architecture Suite (Priority: P1)

As a maintainer, I need `Rig.TUnit.Architecture.Tests` using `NetArchTest.Rules` so that architectural invariants (layering, naming, no-circular-deps, no-`DateTime.Now`, no-`async-void`, public-type-has-test) are enforced on every build — not on code review.

**Acceptance Scenarios**:

1. **Given** `Rig.TUnit.Architecture.Tests`, **When** it runs, **Then** the following rules MUST pass:
   - `Rig.TUnit.Databases` MUST NOT reference any `*.Sql.*` or `*.NoSql.*` package.
   - `Rig.TUnit.Databases.Sql` MUST NOT reference any concrete SQL provider (`.SqlServer`, `.Postgresql`, `.MySql`, `.Sqlite`, `.Oracle`).
   - Every provider MUST reference its own base, never siblings (e.g., `.SqlServer` MUST NOT reference `.Postgresql`).
   - `Microservices.*` packages MUST depend only on bases, never concrete providers.
   - No class named `*Helper` is `public static` without being `sealed`.
   - All `*Fixture` classes MUST extend a `*FixtureBase`.
   - All `*RigBuilder<TSelf>` classes MUST be `abstract` or `sealed`.
   - No source type MUST use `DateTime.Now` (test/helper whitelists allowed).
   - No method MUST be `async void` (event handlers whitelisted).
   - Every public type in `src/` MUST have at least one referencing test project.
2. **Given** a violation (e.g., a new `*Helper` that is `public static` but not `sealed`), **When** the build runs, **Then** the architecture test MUST fail with a diagnostic naming the type and rule.

---

## Requirements

### Functional Requirements

**Hard cutover**
- **FR-001**: System MUST delete `src/Rig.TUnit.SqlServer/`, `src/Rig.TUnit.Redis/`, `src/Rig.TUnit.ServiceBus/`, and their matching test directories in the first commit of Phase A.
- **FR-002**: System MUST delete the files `SqlServerContainerExtensions`, `RedisContainerExtensions`, `ServiceBusContainerExtensions`, `GrpcServiceReplacementExtensions`.
- **FR-003**: System MUST merge `GrpcServiceReplacementExtensions`' generic service-removal logic into `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`.
- **FR-004**: System MUST strip the four deleted source + four deleted test project references from `Rig.TUnit.slnx`.

**Base contracts (Phase A)**
- **FR-010**: System MUST define base packages: `Rig.TUnit.Databases`, `.Databases.Sql`, `.Databases.NoSql`, `.Messaging`, `.Caching`.
- **FR-011**: Each base MUST define `I{Area}Rig`, `{Area}FixtureBase`, `{Area}RigBuilder<TSelf>`, `{Area}Assert` static DSL, an abstract contract test class, and shared helpers (Wait, Listener, EventSender, BackplaneCapture, SeedBuilder, etc.).
- **FR-012**: Every fixture MUST expose an `IsolationKey` derived from test execution context. The derivation formula is hybrid: `{short-test-name-truncated-to-20-chars}_{sha256(full-test-method-name).substring(0,8)}` (max ~29 chars total, deterministic across re-runs, human-readable in logs). Uniqueness MUST be verified by the shared `ParallelIsolationContract` test (20 parallel fixtures, zero collisions). All consumers (database names, topic suffixes, cache prefixes, container names) MUST further truncate or hash to respect platform limits (Postgres 63 chars, Docker 63 chars, SqlServer 128 chars).

**Relocation (Phase A)**
- **FR-020**: System MUST relocate `SqlServerFixture` → `src/Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`.
- **FR-021**: System MUST promote `DbContextHelper<TContext>` → `src/Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` (EF-provider-agnostic).
- **FR-022**: System MUST relocate `InMemoryDbExtensions` → `src/Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (KEPT, not deleted).
- **FR-023**: System MUST relocate `RedisFixture` → `src/Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs` with `Rig.TUnit.Databases.NoSql.Redis` project-referencing it.
- **FR-024**: System MUST relocate `ServiceBusFixture` → `src/Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`.
- **FR-025**: System MUST split `ListenerHelper` → `ListenerBase` (in `Rig.TUnit.Messaging`) + `ServiceBusListener` (in `Rig.TUnit.Messaging.ServiceBus`).
- **FR-026**: System MUST split `ServiceBusEventSender` → `EventSenderBase` (in `Rig.TUnit.Messaging`) + `ServiceBusEventSender` (in `Rig.TUnit.Messaging.ServiceBus`).
- **FR-027**: System MUST rewrite/move all 56 existing tests under the new layout; final count MUST be ≥ 56 GREEN.

**New SQL fast path (Phase A)**
- **FR-030**: System MUST add `Rig.TUnit.Databases.Sql.Sqlite` with `SqliteFixture`, `SqliteRigBuilder`, and builder extensions — real SQLite `:memory:` as an additional fast path.
- **FR-031**: `DbContextHelper<TContext>` CRUD contract MUST pass against EF InMemory, SQLite `:memory:`, and SqlServer container — all three fast paths.

**TDD enforcement (R1)**
- **FR-040**: No production class MUST be committed without its failing test in the same commit (RED).
- **FR-041**: Every base contract MUST have an abstract contract test class defined BEFORE any provider implementation.
- **FR-042**: Every provider MUST implement the contract test suite in its `Tests.Integration` project; merge is blocked if any contract test fails.
- **FR-043**: Per-package merge gate MUST enforce: line coverage ≥ 90%, branch coverage ≥ 85%, contract suite 100% GREEN, parallel-isolation smoke GREEN, XML docs on all public API (warning-as-error on missing).

**Rule compliance (R5)**
- **FR-050**: All packages MUST target `net10.0`, TUnit 1.34.5+, Testcontainers 4.6.0+, Mediator.Abstractions 3.0.2.
- **FR-051**: All code MUST use file-scoped namespaces, `sealed` on non-inheritable classes, records for value objects, `private set` on entities.
- **FR-052**: No source code MUST use `DateTime.Now`; `TimeProvider` MUST be injected.
- **FR-053**: No source code MUST use `async void`, `.Result`, or `.Wait()`.
- **FR-054**: Every fixture configuration MUST use the Options pattern (`[Required]` + `ValidateOnStart()`).
- **FR-055**: `CancellationToken` MUST propagate through every async API.
- **FR-056**: Only `ILogger<T>` MUST be used for logging; no `Console.Write*` in source.
- **FR-057**: Circular dependencies MUST be prevented by `NetArchTest` rules in `Rig.TUnit.Architecture.Tests`.

**Parallel safety (R6)**
- **FR-060**: Every provider fixture MUST pass the shared `ParallelIsolationContract` test (20 parallel executions, zero cross-talk) before merge.
- **FR-061**: Ports, schemas, topics, cache prefixes, and container names MUST be uniquely generated per test.

**Observability (R7)**
- **FR-070**: `Rig.TUnit.Observability.Tracing` MUST supply an in-memory OTEL exporter and `TraceAssert` DSL.
- **FR-071**: `Rig.TUnit.Observability.Metrics` MUST supply `MeterListener` capture, `MetricAssert`, and a tag-cardinality guard.
- **FR-072**: `Rig.TUnit.Observability.Logging` MUST supply an in-memory `ILoggerProvider` + `LogAssert` + anti-pattern detector. The detector MUST fail tests on: (1) interpolated log templates (`$"..."` in `ILogger` calls), (2) `Console.Write` / `Console.WriteLine` from source assemblies, (3) PII-shaped property names. PII detection is ADDITIVE-ONLY security — a fixed built-in canonical list SHALL be shipped and MUST NOT be disabled or removed by consumers. Consumers MAY extend (never narrow) detection via configurable regex patterns. Built-in canonical list (case-insensitive contains-match): `Password`, `Secret`, `ApiKey`, `Token`, `AccessToken`, `RefreshToken`, `IdToken`, `ClientSecret`, `ConnectionString`, `Ssn`, `SocialSecurity`, `Email`, `CreditCard`, `CardNumber`, `Pan`, `Cvv`, `Cvc`, `Pin`, `PrivateKey`, `PassPhrase`, `SessionId`, `AuthHeader`, `Bearer`. Additive regex via `LoggingDetectorOptions.AdditionalPiiPatterns` (e.g., internal ID shapes like `^x-auth-.*$`). No allowlist / opt-out mechanism exists — if a legitimate property (e.g., `ResetTokenId` for password-reset flows) triggers a false positive, the remedy is to rename the property, not weaken the detector.
- **FR-073**: `Rig.TUnit.Observability.Seq` MUST supply a `datalust/seq` Testcontainer, Serilog sink, `SeqAssert.Query(...)` DSL, and dashboard snapshot capture for CI artifacts.
- **FR-074**: Seq and Logging MUST share the same assertion surface so tests can swap providers in one line.

**Caching (R8)**
- **FR-080**: `CacheAssert` MUST provide: `Stampede`, `TagInvalidation`, `Coherent(acrossNodes)`, `FailSafe.ServesStaleFor`, `NegativeCached.WithShorterTtl`, `HitRate`, `EagerRefresh`.
- **FR-081**: `BackplaneCapture` MUST intercept Redis pub/sub invalidation messages.
- **FR-082**: `ClockControl` MUST integrate `FakeTimeProvider` so TTLs advance without `Task.Delay`.

**Security (R9)**
- **FR-090**: `JwtBuilder` MUST produce HS256 and RS256 tokens accepted by real `JwtBearerHandler`.
- **FR-091**: `MockOAuthServer` MUST implement `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration`; tests MUST run against genuine `.AddJwtBearer(...)` (no bypass).
- **FR-092**: `PolicyAssert` MUST evaluate real ASP.NET Core policies against synthetic `ClaimsPrincipal`.
- **FR-093**: The existing `TestAuthenticationHandler` from feature 002 MUST remain ONLY as a smoke-test helper; new JWT/policy tests MUST use `Rig.TUnit.Security.*`.

**Microservices (R10)**
- **FR-100**: `Rig.TUnit.Microservices.Outbox` MUST depend only on base `Databases` + `Messaging` (no specific provider) and work over any DB + broker combo.
- **FR-101**: `Rig.TUnit.Microservices.Snapshots` MUST use a Verify-compatible on-disk format with microservice-opinionated scrubbers (correlation/causation IDs, event IDs, timestamps, sequence numbers).
- **FR-102**: `Rig.TUnit.Microservices.EventSourcing` MUST provide a `When(event).Then(state)` aggregate harness and event-catalogue verification.

**Meta-packages**
- **FR-110**: Meta-package `Rig.TUnit` MUST bundle `Rig.TUnit.Core` + `Rig.TUnit.Mediator` + `Rig.TUnit.Grpc` + `Rig.TUnit.WebAPI`. No provider packages, no base packages — this is the minimum-viable "Rig.TUnit" surface for general test projects.
- **FR-111**: Meta-package `Rig.TUnit.Microservices` MUST bundle Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq.
- **FR-112**: Meta-package `Rig.TUnit.All` MUST bundle everything (discouraged — documented as kitchen-sink).

**Versioning**
- **FR-120**: All packages MUST move to 2.0.0 at Phase A cutover; minor versions MUST move together per phase; patches MUST bump independently.

### Key Entities

**Base contracts**
- **IRigConnectionSource** — existing contract, unchanged; resolves connection values from Container / Config / Options / Value / Auto.
- **I{Area}Rig** (`IDbRig`, `ISqlRig`, `INoSqlRig`, `IMessagingRig`, `ICacheRig`, `IStorageRig`, `ITelemetryRig`, `ISecurityRig`) — per-area marker + shared operations.
- **{Area}FixtureBase** — abstract base exposing `IsolationKey`, `InitializeAsync`, `DisposeAsync`, and per-area entrypoints.
- **{Area}RigBuilder\<TSelf\>** — abstract builder carrying `Use*` source-resolution API; provider builders inherit and express only provider-specific options.
- **{Area}Assert** — static assertion DSL entry-point per area.

**Shared helpers**
- **WaitHelper** (existing) — eventual-consistency polling.
- **ListenerBase\<T\>** — captures message timestamp, headers, body, correlation ID; `Rig.TUnit.Messaging`.
- **EventSenderBase** — correlation / causation / W3C traceparent propagation; `Rig.TUnit.Messaging`.
- **BackplaneCapture** — Redis pub/sub invalidation interceptor; `Rig.TUnit.Caching`.
- **StampedeTester** — N concurrent misses → producer called once; `Rig.TUnit.Caching`.
- **ClockControl** — `FakeTimeProvider` wrapper for TTL tests; `Rig.TUnit.Caching`.
- **SeedBuilder\<T\>** — dependency-ordered, Bogus-integrated; `Rig.TUnit.Databases`.
- **DbContextHelper\<TContext\>** — EF-provider-agnostic CRUD / transaction / seed; `Rig.TUnit.Databases.Sql`.

**Provider-specific entities (non-exhaustive)**
- **SqlServerFixture**, **SqliteFixture**, **PostgresFixture**, **MySqlFixture**, **OracleFixture** — per-SQL-engine fixtures.
- **CosmosFixture**, **MongoFixture**, **DynamoFixture**, **CassandraFixture**, **EventStoreFixture**, **ElasticSearchFixture**, **RedisFixture** — per-NoSQL-engine fixtures.
- **ServiceBusFixture**, **KafkaFixture**, **RabbitMqFixture**, **SqsFixture**, **NatsFixture** — per-broker fixtures.
- **AzureBlobFixture**, **S3Fixture**, **MinIOFixture**, **FileSystemFixture** — per-storage fixtures.
- **SeqFixture**, **AppInsightsFixture** — observability store fixtures.

**Microservice harness**
- **OutboxFixture**, **OutboxRelaySimulator**, **OutboxReplay** — outbox pattern.
- **InboxFixture**, **SequenceTracker** — idempotency.
- **EventSourcingHarness**, **AggregateAssert**, **EventCatalogueAssert** — event sourcing.
- **SnapshotAssert**, **SnapshotScrubber** — approval testing.
- **SagaAssert**, **SagaStepVerifier** — saga testing.
- **ContractBroker**, **ConsumerContract**, **ProviderContract** — Pact-style.

**Architecture**
- **NetArchTest rules** (enumerated in US13) — enforced in `Rig.TUnit.Architecture.Tests`.

---

## Architecture Scope

**Generic mode** — this feature does NOT cross service/repo boundaries; it's a single-repo library ecosystem.

**Affected layers / directories**:

| Layer | Scope | New / Modified / Deleted |
|---|---|---|
| `src/Rig.TUnit.Core` | existing | MODIFIED — absorbs `GrpcServiceReplacementExtensions` generic logic into `ServiceRemovalExtensions` |
| `src/Rig.TUnit.Mediator` | existing | UNCHANGED |
| `src/Rig.TUnit.Grpc` | existing | MODIFIED — `Extensions/GrpcServiceReplacementExtensions.cs` DELETED |
| `src/Rig.TUnit.WebAPI` | existing | UNCHANGED (`TestAuthenticationHandler` stays as smoke-test helper) |
| `src/Rig.TUnit.SqlServer` | existing | DELETED ENTIRELY |
| `src/Rig.TUnit.Redis` | existing | DELETED ENTIRELY |
| `src/Rig.TUnit.ServiceBus` | existing | DELETED ENTIRELY |
| `src/Rig.TUnit.Databases` + `.Sql` + `.NoSql` | new base packages | NEW (Phase A) |
| `src/Rig.TUnit.Databases.Sql.{SqlServer,Sqlite}` | new providers | NEW (Phase A) |
| `src/Rig.TUnit.Databases.Sql.{Postgresql,MySql,Oracle}` | new providers | NEW (Phase D/E) |
| `src/Rig.TUnit.Databases.NoSql.{Cosmos,Mongo,Dynamo,Cassandra,EventStore,ElasticSearch,Redis}` | new providers | NEW (Phase A for Redis; D/E for others) |
| `src/Rig.TUnit.Messaging` + `.ServiceBus` | new base + provider | NEW (Phase A) |
| `src/Rig.TUnit.Messaging.{Kafka,RabbitMq,Sqs,Nats}` | new providers | NEW (Phase D/E) |
| `src/Rig.TUnit.Caching` + `.Redis` | new base + provider | NEW (Phase A) |
| `src/Rig.TUnit.Caching.{Memory,Hybrid,Fusion}` | new providers | NEW (Phase D) |
| `src/Rig.TUnit.Storage.*` | new base + providers | NEW (Phase D/E) |
| `src/Rig.TUnit.Observability.*` | new base + providers | NEW (Phase B/E) |
| `src/Rig.TUnit.Security.*` | new base + providers | NEW (Phase B/E) |
| `src/Rig.TUnit.Http` | new package | NEW (Phase B) |
| `src/Rig.TUnit.Resilience` | new package | NEW (Phase B) |
| `src/Rig.TUnit.HealthChecks` | new package | NEW (Phase C) |
| `src/Rig.TUnit.Concurrency` | new package | NEW (Phase C) |
| `src/Rig.TUnit.Docker` + `.Parallelism` + `.Ci` | new packages | NEW (Phase E) |
| `src/Rig.TUnit.Microservices.{Outbox,Inbox,EventSourcing,Snapshots}` | new packages | NEW (Phase C) |
| `src/Rig.TUnit.Microservices.{Saga,Contracts}` | new packages | NEW (Phase E) |
| `src/Rig.TUnit` (meta) | existing | MODIFIED — references updated |
| `src/Rig.TUnit.Microservices` (new meta) | new | NEW |
| `src/Rig.TUnit.All` (new meta) | new | NEW |
| `tests/Rig.TUnit.X.Tests.Unit` | per package | NEW or MOVED per package |
| `tests/Rig.TUnit.X.Tests.Contract` | per base package | NEW |
| `tests/Rig.TUnit.X.Tests.Integration` | per provider package | NEW or MOVED per provider |
| `tests/Rig.TUnit.Architecture.Tests` | single cross-cutting | NEW |
| `tests/Rig.TUnit.Benchmarks` | existing | MODIFIED — BenchmarkDotNet suite expanded per area |
| `Rig.TUnit.slnx` | root | MODIFIED — strip deleted refs, add ~50 new project refs |
| `Directory.Build.props` | root | MODIFIED — pin versions, warnings-as-errors, XML docs |

**Layer dependency direction** (enforced by architecture tests):
- Base → Provider: base NEVER references provider.
- Provider → Base: provider references its own base only.
- Microservices → Base: Microservices NEVER depends on a concrete provider.
- No circular references anywhere.

---

## Non-Functional Requirements

- **Performance**: BenchmarkDotNet fixture-startup regression budget — new fixture start time MUST be ≤ 110% of baseline (pre-expansion Phase 002 baseline).
- **Coverage**: line ≥ 90%, branch ≥ 85% per package. Gate enforced in CI.
- **Documentation**: Every public type has XML docs; warning-as-error on missing. Each package has a README + one example test.
- **Determinism**: No `Thread.Sleep` / `Task.Delay` in tests. Time controlled via `FakeTimeProvider`. Eventual consistency via `WaitHelper`.
- **CI matrix**: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x MUST all be exercised.
- **Warnings**: `dotnet build` MUST emit ZERO warnings (treat-as-errors).

---

## TDD Protocol — Applies to EVERY Class

Every new class, method, assertion, builder, fixture, or helper MUST follow this cycle. It is NOT optional; it is the library's defining discipline.

### 🔴 RED — Write the failing test FIRST

1. Identify the behavior to add (e.g., "JwtBuilder produces HS256 tokens accepted by real JwtBearerHandler").
2. Write the minimal test that proves the behavior. Name it `{Method}_{Scenario}_{ExpectedResult}` per `.claude/rules/testing.md`.
3. Run the test — it MUST fail (compilation failure or assertion failure are both acceptable RED states).
4. **Commit the RED state** with message `test: red — {behavior description}`.

### 🟢 GREEN — Write the MINIMUM code to pass

1. Write only the code that makes the RED test pass. No extras. No refactors. No unrelated cleanup.
2. Run the test — it MUST pass.
3. Run the whole test class / package — all tests MUST still pass.
4. **Commit the GREEN state** with message `feat: green — {behavior description}`.
5. Production code + test MUST sit in the same logical change-set (the RED commit and GREEN commit can be squashed into one before merge, but the working log on the feature branch MUST show both).

### 🔵 REFACTOR — Improve structure, keep tests GREEN

1. Refactor only after GREEN. Typical moves: extract helper, rename for clarity, tighten type, remove duplication, add XML docs.
2. Tests MUST NOT change. If tests must change to accommodate a refactor, it is a behavior change — go back to RED with a new test.
3. Run the whole test suite — all tests MUST pass.
4. **Commit the REFACTOR** with message `refactor: {what was improved}`.

### Contract-first TDD for bases

For every base area (`Databases`, `Databases.Sql`, `Databases.NoSql`, `Messaging`, `Caching`, `Storage`, `Observability`, `Security`):

1. RED — define the abstract contract test class (`SqlRigContract`, `CacheRigContract`, …) with all 13 mandatory provider tests as `[Test]` abstract methods.
2. GREEN — define the minimal `I{Area}Rig` + `{Area}FixtureBase` + `{Area}RigBuilder<TSelf>` that make the contract compile (not pass — PROVIDERS make it pass).
3. REFACTOR — extract shared helpers (Wait, Listener, EventSender, BackplaneCapture, SeedBuilder) once patterns emerge across 2+ providers.

### Provider TDD

For every provider:

1. RED — concrete test class inherits the base's contract test; all 13 tests fail (no fixture yet).
2. GREEN — implement the provider fixture (`PostgresFixture`, `KafkaFixture`, `RedisFixture`, etc.) and concrete `RigBuilder`; all 13 tests pass.
3. RED (quirks) — add at least 3 provider-specific quirk tests (e.g., `Postgres_Xmin_IsBigint`, `SqlServer_Rowversion_IsTimestamp`).
4. GREEN — implement / expose the quirk-handling code.
5. REFACTOR — tighten LOC budget (≤ ~200 LOC provider-specific code).

### Assertion DSL TDD

For every `XxxAssert` method (`CacheAssert.Stampede`, `TraceAssert.HasSpan`, `SnapshotAssert.Match`, etc.):

1. RED — positive case, negative case, boundary case, async/timeout case, cancellation case — all five tests written before the assertion exists.
2. GREEN — implement the assertion.
3. REFACTOR — extract shared fluent-chain machinery.

---

## Mandatory Per-Provider Contract Tests

Every provider's `Tests.Integration` project inherits the base's abstract contract and MUST implement:

| # | Test Name | Purpose |
|---|---|---|
| 1 | `Fixture_InitializeAsync_IsIdempotent` | Double-init does not double-allocate resources |
| 2 | `Fixture_DisposeAsync_IsSafeToCallTwice` | Double-dispose does not throw |
| 3 | `Builder_UseContainer_ResolvesConnectionSource` | Container source path |
| 4 | `Builder_UseConfig_ResolvesFromIConfiguration` | Config source path |
| 5 | `Builder_UseOptions_ResolvesFromIOptions` | Options source path |
| 6 | `Builder_UseValue_UsesRawConnectionString` | Raw value source path |
| 7 | `Builder_UseAuto_SelectsContainerInCi` | Auto picks container when `IsRunningInCiCd()` true |
| 8 | `Builder_UseAuto_SelectsConfigLocally` | Auto picks config locally |
| 9 | `Builder_ForceContainersInCi_RejectsConfigInCi` | Force flag honored |
| 10 | `IsolationKey_PerTest_DoesNotCollide` | 20 parallel — unique keys |
| 11 | `CancellationToken_Honored_ThrowsOperationCanceled` | Token propagation |
| 12 | `EventualConsistency_WaitHelper_DetectsStateChange` | Poll-based async visibility |
| 13+ | Provider-specific quirks (≥ 3) | Dialect / engine differences |

## Mandatory Per-Assertion-DSL Tests

For every `XxxAssert` method:

| # | Case | Purpose |
|---|---|---|
| 1 | Positive | Assertion holds |
| 2 | Negative | Fails with expected structured message |
| 3 | Boundary | Near-miss / just-over-threshold |
| 4 | Async/timeout | Eventual consistency |
| 5 | Cancellation | `CancellationToken` honored |

---

## Phased Delivery

Every phase ships independently and MUST NOT start until the previous phase's merge gate is met. Each phase has its own TDD cycle and its own merge gate.

### Phase A — Base contracts + hard cutover
**Packages**: `Rig.TUnit.Databases`, `.Sql`, `.NoSql`, `.Sql.SqlServer`, `.Sql.Sqlite`, `.Messaging`, `.Messaging.ServiceBus`, `.Caching`, `.Caching.Redis`, `.NoSql.Redis`.
**Scope**: hard delete of old packages; base contracts test-first; relocate SqlServer/ServiceBus/Redis into providers; keep `InMemoryDbExtensions`; add Sqlite fast path; add `Rig.TUnit.Architecture.Tests`.
**Exit criteria**: ≥ 56 pre-existing tests GREEN under new namespaces; new contract suites 100% GREEN for SqlServer, Sqlite (EF InMemory), Redis, ServiceBus; architecture tests 100% GREEN; coverage gate met.

### Phase B — Rule-mandated capabilities
**Packages**: `Observability` + `.Tracing` + `.Logging` + `.Seq`, `Security` + `.Jwt` + `.OAuth`, `Http`, `Resilience`.
**Exit criteria**: anti-pattern detector fires on all documented violations; JWT/OAuth tests run against real middleware; HTTP mock passes full matcher/scenario/replay matrix; Polly resilience tests advance via `FakeTimeProvider` deterministically.

### Phase C — Microservice patterns
**Packages**: `Microservices.Outbox` + `.Inbox` + `.EventSourcing` + `.Snapshots`, `Concurrency`, `HealthChecks`, `Caching.Memory`.
**Exit criteria**: Outbox ExactlyOnce under concurrent relay; snapshot format Verify-compatible; concurrency contract passes on SqlServer + Postgres + Cosmos + Mongo; health probes distinguish live / ready / startup.

### Phase D — Provider expansion
**Packages**: `Databases.Sql.Postgresql`, `.MySql`; `Databases.NoSql.Cosmos`, `.Mongo`; `Messaging.Kafka`, `.RabbitMq`; `Caching.Hybrid`, `.Fusion`; `Storage` + `.AzureBlob` + `.S3`.
**Exit criteria**: every new provider passes its base contract + 3 quirk tests + parallel-isolation contract; CI matrix green.

### Phase E — Polish
**Packages**: `Docker`, `Parallelism`, `Ci`, `Observability.Metrics` + `.AppInsights`, `Security.Mtls` + `.Policies`, `Microservices.Saga` + `.Contracts`, remaining providers (Oracle, Dynamo, Cassandra, EventStore, ElasticSearch, SQS, NATS, MinIO, FileSystem).
**Exit criteria**: full ~50-package ecosystem GREEN; benchmarks within budget; docs complete.

---

## Edge Cases

- **Docker unavailable** — Integration tests MUST skip gracefully via `[EnabledOnDocker]` filter; unit + contract tests MUST still run. Docker-less CI fallback MUST pass a subset of tests.
- **Flaky emulators** (Cosmos emulator, Azurite) — automatic retry (≤ 3) with `Rig.TUnit.Ci` flaky-quarantine reporting; flaky tests MUST NOT block merges beyond N consecutive failures.
- **Version drift** — `Directory.Build.props` pins all package versions; `NetArchTest` rule verifies no package in `src/` references an un-pinned version.
- **Coverage drift** — `Rig.TUnit.Ci` coverage-delta enforcer blocks merges that drop coverage below the per-package gate.
- **Shared mutable state in tests** — `Rig.TUnit.Parallelism` shared-state detector flags static-field writes; violations fail the test suite.
- **Port collisions** — `Rig.TUnit.Parallelism` port allocator survives 100 concurrent requests; collisions are P0 defects.
- **Snapshot format** — first-run creates `.received.*` (not `.approved.*`); CI fails on `.received.*` presence to force reviewer to approve.
- **InMemoryDb fidelity trap** — `InMemoryDbExtensions` MUST emit a test-time warning (not failure) when used with migrations-based assertions, steering developers to SQLite or container.
- **Sqlite `:memory:` lifetime** — connection MUST stay open for the fixture lifetime; `SqliteFixture` MUST own the connection.
- **Testcontainer image pull** — first-run slow; `Rig.TUnit.Docker` MUST cache image pulls per CI agent.
- **Microservice package dependency direction** — microservice patterns depend on BASE packages only; architecture test prevents accidental dependency on a concrete provider.
- **`Rig.TUnit.All` meta-package** — discouraged but supported; README MUST warn that pulling it in bloats test projects.

---

## Clarification Items (resolved)

The design doc raised five open questions; three required explicit confirmation and are resolved in the Clarifications section below. Original text retained with RESOLVED annotations for traceability:

1. ~~[NEEDS CLARIFICATION] ServiceBus emulator choice~~ — **RESOLVED (C-001)**: Microsoft's official emulator container `mcr.microsoft.com/azure-messaging/servicebus-emulator` is pinned, with the companion `mcr.microsoft.com/azure-sql-edge` container for the emulator's required SQL backend. `ACCEPT_EULA=Y` env var is set by the fixture; exact image tag pinned in `Directory.Build.props`. Topic / subscription / dead-letter / session / transaction semantics mirror production ServiceBus. No third-party emulator, no live-Azure-only CI path.
2. ~~[NEEDS CLARIFICATION] Meta-package `Rig.TUnit.Microservices` Seq default~~ — **RESOLVED (C-002)**: Include `Seq` by default. `Rig.TUnit.Microservices` meta = `Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq` (7 packages, per FR-111). Opinionated stack — Seq is the recommended structured-log store. Teams that don't want the `datalust/seq` container pull can reference `.Logging` directly without the meta. README warns about the ~150MB image pull + ~3-5s startup cost.
3. ~~[NEEDS CLARIFICATION] Snapshot on-disk format Verify-compatible~~ — **RESOLVED (C-003)**: Verify-compatible on-disk format. File naming (`{name}.received.{ext}` / `{name}.verified.{ext}`), JSON structure, diff-tool hook points, and CI-fail-on-`.received.*` behavior all mirror `Verify.Xunit` / `Verify.TUnit`. Users with existing Verify snapshots can migrate by copying files; BeyondCompare / VS Code diff extensions work out-of-the-box. Microservice-opinionated scrubbers (correlation/causation IDs, event IDs, timestamps, sequence numbers, connection strings, paths) layered on top of Verify's base scrubber pipeline.

Resolved in design, no clarification needed:
- ✅ `Rig.TUnit.Databases.Sql.Postgresql` (not `.Postgres`) — matches NuGet `Npgsql` and Testcontainers module naming.
- ✅ `Caching.Memory` scope — cache only, not `ObjectPool`.
- ✅ `Microservices.EventSourcing` provider independence — does NOT depend on `Databases.NoSql.EventStore`; EventStore is one adapter among many.

---

## Success Criteria

- **SC-001**: All ~50 packages build with `dotnet build` — ZERO warnings (warnings-as-errors active).
- **SC-002**: Full solution `dotnet test` GREEN; coverage gate (line ≥ 90%, branch ≥ 85%) met per package.
- **SC-003**: `Rig.TUnit.Architecture.Tests` suite GREEN; zero circular dependencies; no rule violations.
- **SC-004**: `BenchmarkDotNet` suite within regression budget (< 110% of 002-feature baseline for fixture startup).
- **SC-005**: Old packages `Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus` and their tests DELETED; `Rig.TUnit.slnx` clean of their references.
- **SC-006**: Every package ships a README + one example test demonstrating its public API.
- **SC-007**: Every public type has XML docs (enforced by `GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true`).
- **SC-008**: CI matrix GREEN across: Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x.
- **SC-009**: 56 pre-existing tests ported into new layout and GREEN; expected final test count several hundred (contract × providers + integration + unit).
- **SC-010**: Spec-to-task-to-code traceability — every spec FR / User Story links to at least one test AND one source class; generated by `/dai.tasks`.
- **SC-011**: Every commit on the feature branches exhibits RED → GREEN → REFACTOR cadence (verifiable via commit-message prefixes `test:` `feat:` `refactor:`).
- **SC-012**: Merge gate per PR: RED test present, GREEN implementation present, REFACTOR optional, contract suite 100%, parallel-isolation smoke GREEN, coverage gate met, XML docs present.
- **SC-013**: Anti-pattern detector in `Observability.Logging` catches 100% of documented violations in a self-test (interpolated templates, `Console.Write`, PII property names).
- **SC-014**: JWT/OAuth tests run against real `JwtBearerHandler` middleware — zero bypass mechanisms in new code; legacy `TestAuthenticationHandler` present only for smoke tests.
- **SC-015**: `Rig.TUnit.Microservices.Outbox` delivers `ExactlyOnce` under 100 concurrent relay runs across SqlServer+ServiceBus and SqlServer+Kafka matrix; zero duplicates.

---

## Out of Scope (explicit)

- GraphQL, SignalR, Feature Flags, Email, Scheduling, AI, BackgroundServices packages — future features.
- IDE tooling (VS Code / Rider extensions).
- Commercial cloud-backed providers beyond emulators.
- Visual test-reporting dashboards beyond CI enrichers.
- Publishing to public NuGet.org — this spec focuses on internal readiness; publish is a separate feature.

---

## Traceability

| Hard Requirement | User Story | Functional Requirement | Success Criterion |
|---|---|---|---|
| R1 TDD non-negotiable | US1 | FR-040..043 | SC-011, SC-012 |
| R2 Base + Provider | US2, US11 | FR-010..012, FR-030..031 | SC-003 |
| R3 Hard cutover | US2 | FR-001..004, FR-020..027 | SC-005, SC-009 |
| R4 Package tree | US2, US5..12 | FR-010..112 | SC-001, SC-002 |
| R5 Rule compliance | US13 | FR-050..057 | SC-003, SC-007 |
| R6 Parallel safety | US4 | FR-060..061 | SC-002, SC-012 |
| R7 Observability first-class | US5 | FR-070..074 | SC-013 |
| R8 Caching coherency | US10 | FR-080..082 | SC-002 |
| R9 Security real middleware | US6 | FR-090..093 | SC-014 |
| R10 Microservices compose bases | US8 | FR-100..102 | SC-015 |

---

## Clarifications

- **C-001** [Service Communication] (2026-04-17): ServiceBus emulator choice → Microsoft's official emulator container `mcr.microsoft.com/azure-messaging/servicebus-emulator` + companion `mcr.microsoft.com/azure-sql-edge` backend; `ACCEPT_EULA=Y` set by fixture; image tags pinned in `Directory.Build.props`. No third-party emulator; no live-Azure-only CI path.
- **C-002** [Architecture Scope] (2026-04-17): `Rig.TUnit.Microservices` meta-package contents → include Seq by default. Final composition: `Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq` (7 packages). Opinionated stack; README documents the ~150MB `datalust/seq` image pull + startup cost. Teams opting out reference `.Logging` directly.
- **C-003** [Domain & Data Model] (2026-04-17): Snapshot on-disk format → Verify-compatible. File naming (`{name}.received.{ext}` / `{name}.verified.{ext}`), JSON structure, diff-tool hooks, and CI-fail-on-`.received.*` behavior mirror `Verify.Xunit` / `Verify.TUnit`. Microservice-opinionated scrubbers layered on top of Verify's base scrubber pipeline.
- **C-004** [Edge Cases] (2026-04-17): `IsolationKey` derivation formula → hybrid `{short-test-name-truncated-to-20-chars}_{sha256(full-test-method-name).substring(0,8)}`. Deterministic across re-runs (debuggable), human-readable in logs, max ~29 chars. Consumers (db names / topic suffixes / cache prefixes / container names) further truncate or hash per-platform limits (Postgres 63, Docker 63, SqlServer 128).
- **C-005** [Edge Cases] (2026-04-17): PII detector policy → additive-only security. Fixed built-in canonical list (`Password`, `Secret`, `ApiKey`, `Token`, `AccessToken`, `RefreshToken`, `IdToken`, `ClientSecret`, `ConnectionString`, `Ssn`, `SocialSecurity`, `Email`, `CreditCard`, `CardNumber`, `Pan`, `Cvv`, `Cvc`, `Pin`, `PrivateKey`, `PassPhrase`, `SessionId`, `AuthHeader`, `Bearer`) cannot be disabled. Consumers MAY add regex patterns via `LoggingDetectorOptions.AdditionalPiiPatterns` (ECMAScript regex syntax, case-insensitive, compiled once at detector startup) to STRENGTHEN detection; no allowlist / opt-out exists. False positives remedied by renaming properties, never by weakening the detector.
- **C-006** [Edge Cases] (2026-04-17): Anti-pattern detector implementation mechanism → **hybrid**. (a) Runtime detector in `Rig.TUnit.Observability.Logging` inspects captured `LogMessage.OriginalFormat` + structured property names — catches interpolated-template literals passed as log messages AND PII-shaped property names at test-time. (b) Separate Roslyn analyzer NuGet package `Rig.TUnit.Observability.Logging.Analyzers` ships in Phase B — compile-time detection of `$"..."` in `ILogger` calls and any `Console.Write`/`Console.WriteLine` in source assemblies. Consumers can use one or both; the runtime detector is sufficient for most scenarios, the analyzer adds compile-time enforcement. Both use the same canonical PII list + regex extension point for consistency.

---

**Status**: All 5 clarifications resolved (C-001..C-005). Spec ready for planning.

**Next commands**:
- `/dai.plan` — generate the technical plan (Phases A-E task breakdown, file lists, dependency diagram, risk register).
- `/dai.tasks` — expand the plan into RED/GREEN/REFACTOR-tagged executable task list.
- `/dai.go` — execute tasks per phase with merge-gate enforcement.
