# Build Prompt — Rig.TUnit Ecosystem Expansion

Copy everything below the line and pass it to `/dai.spec` to generate the formal specification.

---

## Context

This is the **third and largest feature** for Rig.TUnit. The library is pre-release (no public NuGet publish yet), so this spec performs a **hard cutover** — no backwards-compatibility shims, no `[Obsolete]` stubs. Old packages are deleted; their code is relocated and namespaces are rewritten in place.

**Read before generating the spec:**
- `planning/ecosystem-expansion/Rig.TUnit-Library-Design.md` — complete architectural design (Base + Provider pattern, package tree, per-area contracts, testing strategy, phases).
- `planning/ecosystem-expansion/Rig.TUnit-Session-Handoff.md` — file-by-file relocation plan, version pins, namespace conventions, mandatory test matrix, CI updates, definition of done.
- `planning/base-library/Rig.TUnit-Library-Design.md` + `planning/fluent-builder-expansion/Rig.TUnit-Library-Design.md` — historical designs (for format reference only; not for behavior).
- `src/` directory — current source layout (six packages, 56 passing tests).
- `.claude/rules/*.md` — project-wide architecture, testing, async, observability, security, configuration, naming rules. The spec MUST honor all of them.

## Feature

### Name
`003-rig-tunit-ecosystem-expansion`

### One-line summary
Transform Rig.TUnit from six packages into a full microservice test platform (~50 packages) organized by **Base + Provider** (DRY) covering databases (SQL + NoSQL), messaging, caching, storage, observability, security, resilience, HTTP mocking, health checks, concurrency, parallelism, CI, and microservice patterns (Outbox / Inbox / Event Sourcing / Saga / Snapshots / Contracts) — delivered **strictly test-first**.

---

## Hard requirements

### R1 — TDD is non-negotiable
- No production class is committed without its failing test landing in the **same commit**.
- Every base contract is specified by a **contract test suite** (abstract TUnit class) before any provider is implemented.
- Every provider implements the contract suite; providers cannot ship if any contract test fails.
- Per-package merge gate: line coverage ≥ 90%, branch coverage ≥ 85%, contract suite 100%, parallel-isolation smoke passes.

### R2 — Base + Provider (DRY)
- Every multi-provider area has a base package: `Rig.TUnit.Databases`, `.Databases.Sql`, `.Databases.NoSql`, `.Messaging`, `.Caching`, `.Storage`, `.Observability`, `.Security`.
- Each base defines: `I{Area}Rig` contract, `{Area}FixtureBase`, `{Area}RigBuilder<TSelf>`, `{Area}Assert` static DSL, shared helpers (Wait, Listener, EventSender, BackplaneCapture, SeedBuilder, etc.).
- Provider packages implement **only** engine-specific bits (container image, wait strategy, dialect quirks) — target ≤ 200 LOC of provider-specific code.
- Every provider MUST pass the base's contract test suite.

### R3 — Hard cutover (no compat shims)
- Delete `src/Rig.TUnit.SqlServer/`, `src/Rig.TUnit.Redis/`, `src/Rig.TUnit.ServiceBus/` and their test projects.
- Delete extension files: `SqlServerContainerExtensions`, `RedisContainerExtensions`, `ServiceBusContainerExtensions`, `GrpcServiceReplacementExtensions` (merge its generic logic into `Rig.TUnit.Core.Extensions.ServiceRemovalExtensions`).
- Relocate retained sources (`SqlServerFixture`, `DbContextHelper`, `InMemoryDbExtensions`, `RedisFixture`, `ServiceBusFixture`, `ListenerHelper`, `ServiceBusEventSender`) into their new provider packages with updated namespaces.
- Split shared behavior: `ListenerHelper` → `ListenerBase` (base) + `ServiceBusListener` (provider). Same split for `ServiceBusEventSender`.
- Promote `DbContextHelper` into `Rig.TUnit.Databases.Sql` base as EF-provider-agnostic.
- **Keep `InMemoryDbExtensions`** — relocate to `Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (EF InMemory provider stays as the fastest, lowest-fidelity fast path).
- **Add `Rig.TUnit.Databases.Sql.Sqlite`** — real SQLite `:memory:` as an additional fast path (higher SQL fidelity than EF InMemory, still containerless). Both fast paths coexist; developers pick per-scenario.
- Three-way fast-path choice: EF InMemory (fastest / no SQL) vs Sqlite in-memory (fast / real SQL) vs container (full dialect fidelity).
- All 56 existing tests are rewritten/moved under the new layout; final count ≥ 56 green.

### R4 — Package tree (final)

Infrastructure:
- `Rig.TUnit.Core`, `.Mediator`, `.Grpc`, `.WebAPI` (kept).
- `Rig.TUnit.Databases` → `.Sql` (also contains `InMemoryDbExtensions` EF-InMemory fast path) → `{SqlServer, Postgresql, MySql, Oracle, Sqlite}`; `.NoSql` → `{Cosmos, Mongo, Dynamo, Cassandra, EventStore, ElasticSearch, Redis}`.
- `Rig.TUnit.Messaging` → `{ServiceBus, Kafka, RabbitMq, Sqs, Nats}`.
- `Rig.TUnit.Caching` → `{Memory, Redis, Hybrid, Fusion}` (focus on stampede, tag invalidation, coherency, fail-safe — not thin `IDistributedCache`).
- `Rig.TUnit.Storage` → `{AzureBlob, S3, MinIO, FileSystem}`.
- `Rig.TUnit.Observability` → `{Tracing, Metrics, Logging, Seq, AppInsights}`.
- `Rig.TUnit.Security` → `{Jwt, OAuth, Mtls, Policies}`.

Single-provider:
- `Rig.TUnit.Http` (WireMock-style), `.Resilience` (FakeTimeProvider + Polly), `.HealthChecks`, `.Concurrency`, `.Docker`, `.Parallelism`, `.Ci`.

Microservices (cross-cutting):
- `Rig.TUnit.Microservices.{Outbox, Inbox, EventSourcing, Saga, Snapshots, Contracts}`.

Meta:
- `Rig.TUnit` (Core + common), `.Microservices` (opinionated microservice stack), `.All` (everything).

### R5 — Rule compliance
All code must honor `.claude/rules/*.md`:
- `net10.0`, TUnit 1.34.5+, Testcontainers 4.6.0+, Mediator.Abstractions 3.0.2.
- File-scoped namespaces, `sealed` classes, records for value objects, `private set` on entities.
- No `DateTime.Now` — inject `TimeProvider`.
- No `async void`, no `.Result`, no `.Wait()`.
- `Options` pattern for every fixture config (`[Required]` + `ValidateOnStart()`).
- `CancellationToken` propagated through every async API.
- `ILogger<T>` only; no `Console.Write`.
- No circular dependencies — enforced by `NetArchTest` in `Rig.TUnit.Architecture.Tests`.

### R6 — Parallel-safety by default
- Every fixture exposes an `IsolationKey` derived from the test's execution context.
- Every fixture passes the shared `ParallelIsolationContract` test (20 parallel executions, zero cross-talk) before merge.
- Ports/schemas/topics/cache-prefixes/containers are uniquely generated per test.

### R7 — Observability is first-class
- `Rig.TUnit.Observability.Tracing` supplies in-memory OTEL exporter + `TraceAssert` DSL.
- `Rig.TUnit.Observability.Metrics` supplies `MeterListener` capture + `MetricAssert` (including tag-cardinality guard).
- `Rig.TUnit.Observability.Logging` supplies in-memory `ILoggerProvider` capture + `LogAssert` + anti-pattern detector (fails test on interpolated-template log call, `Console.Write`, or PII-shaped property names — directly enforces `observability.md`).
- `Rig.TUnit.Observability.Seq` supplies Testcontainers `datalust/seq` + Serilog sink + `SeqAssert.Query(...)` DSL + dashboard snapshot capture for CI artifacts. Seq and Logging share the same assertion surface so tests swap providers with one line.

### R8 — Caching targets real coherency, not thin `IDistributedCache`
- `CacheAssert.Stampede(key).ConcurrentMisses(100).ProducerCalledOnce()`.
- `CacheAssert.TagInvalidation(tag).Purges(keys).Keeps(otherKeys)`.
- `CacheAssert.Coherent(acrossNodes: 2).Within(timeout)`.
- `CacheAssert.FailSafe().WhenBackendThrows.ServesStaleFor(softTtl)`.
- `CacheAssert.NegativeCached(key).WithShorterTtl()`.
- `BackplaneCapture` intercepts Redis pub/sub invalidation.
- `ClockControl` integrates `FakeTimeProvider` so TTLs advance without `Task.Delay`.

### R9 — Security testing uses real middleware (no bypass)
- `JwtBuilder` produces HS256/RS256 tokens accepted by real `JwtBearerHandler`.
- `MockOAuthServer` implements `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration` — tests run against genuine `.AddJwtBearer(...)`.
- `PolicyAssert` evaluates real ASP.NET Core policies against synthetic `ClaimsPrincipal`.
- The existing `TestAuthenticationHandler` stays only as a smoke-test helper; policy/JWT tests must use the new packages.

### R10 — Microservice patterns compose other providers
- `Microservices.Outbox` depends on base `Databases` + `Messaging` (not any specific provider) — works over any configured DB + broker combo.
- `Microservices.Snapshots` uses Verify-compatible on-disk format with microservice-opinionated scrubbers (correlation/causation IDs, event IDs, timestamps, sequence numbers).
- `Microservices.EventSourcing` provides a `When(event).Then(state)` aggregate harness + event catalogue verification.

---

## Phased delivery

Every phase ships independently with its own merge gate. No phase starts until the previous phase's merge gate is met.

- **Phase A — Base contracts + hard cutover.** `Databases`, `.Sql`, `.NoSql`, `Messaging`, `Caching` bases. Relocate SqlServer/ServiceBus/Redis into providers. Keep `InMemoryDbExtensions` (relocated to `Rig.TUnit.Databases.Sql`). Add `Rig.TUnit.Databases.Sql.Sqlite` as additional fast path. Delete old package shells. Architecture tests added. 56+ tests green.
- **Phase B — Rule-mandated capabilities.** `Observability.Logging` + `.Seq` + `.Tracing`; `Security.Jwt` + `.OAuth`; `Http`; `Resilience`.
- **Phase C — Microservice patterns.** `Microservices.Outbox` + `.Inbox` + `.EventSourcing` + `.Snapshots`; `Concurrency`; `HealthChecks`.
- **Phase D — Provider expansion.** `Postgresql`, `MySql`, `Cosmos`, `Mongo`, `Kafka`, `RabbitMq`, `Caching.Hybrid`, `Caching.Fusion`, `Storage.AzureBlob`, `Storage.S3`. (Sqlite ships in Phase A alongside the InMemory fast path.)
- **Phase E — Polish.** `Docker`, `Parallelism`, `Ci`, `Observability.Metrics` + `.AppInsights`, `Security.Mtls` + `.Policies`, `Microservices.Saga` + `.Contracts`, remaining providers.

---

## Test plan (summary — spec MUST expand)

### Test projects
Every source package `Rig.TUnit.X` has matching:
- `tests/Rig.TUnit.X.Tests.Unit` — pure logic, builders, assertions.
- `tests/Rig.TUnit.X.Tests.Contract` — abstract base classes re-run by every provider.
- `tests/Rig.TUnit.X.Tests.Integration` — provider packages only; real containers / services.

Plus one new: `tests/Rig.TUnit.Architecture.Tests` — `NetArchTest` rules (no circular deps, naming, sealed, no `DateTime.Now`, no `async void`, public types must have tests).

### Mandatory per-provider contract tests
1. `Fixture_InitializeAsync_IsIdempotent`
2. `Fixture_DisposeAsync_IsSafeToCallTwice`
3. `Builder_UseContainer_ResolvesConnectionSource`
4. `Builder_UseConfig_ResolvesFromIConfiguration`
5. `Builder_UseOptions_ResolvesFromIOptions`
6. `Builder_UseValue_UsesRawConnectionString`
7. `Builder_UseAuto_SelectsContainerInCi`
8. `Builder_UseAuto_SelectsConfigLocally`
9. `Builder_ForceContainersInCi_RejectsConfigInCi`
10. `IsolationKey_PerTest_DoesNotCollide` (20 parallel)
11. `CancellationToken_Honored_ThrowsOperationCanceled`
12. `EventualConsistency_WaitHelper_DetectsStateChange`
13. At least 3 provider-specific quirk tests (documented dialect differences).

### Mandatory per-assertion-DSL tests
1. Positive (assertion holds).
2. Negative (fails with expected structured message).
3. Boundary (near-miss / just-over-threshold).
4. Async/timeout (eventual consistency).
5. Cancellation (`CancellationToken` honored).

### Area-specific coverage (non-exhaustive — spec must expand)
- **Databases.Sql fast-path parity**: the same `DbContextHelper` CRUD contract passes against all three — EF InMemory (`InMemoryDbExtensions`), SQLite `:memory:` (`Rig.TUnit.Databases.Sql.Sqlite`), and Testcontainers SqlServer. Documented behavior differences (dialect, concurrency, transactions) are explicitly asserted per path.
- **Caching**: stampede (100 concurrent misses → producer once), tag invalidation, backplane coherency, fail-safe, negative caching, eager refresh window.
- **Observability.Seq**: container boots, sink wired, query DSL returns hits, anti-pattern detector fires on interpolated template + PII property, dashboard snapshot captured.
- **Security.Jwt/OAuth**: HS256 + RS256 accepted by real middleware, expired/tampered/not-yet-valid rejected, OAuth client-credentials round-trip, OIDC discovery served.
- **Http**: matcher matrix, scenario state machine across 3 calls, delay + intermittent-failure, record/replay, DelegatingHandler variant.
- **Resilience**: FakeTimeProvider advances Polly backoff deterministically, circuit state transitions, retry count asserted, rate limit asserted.
- **HealthChecks**: live vs ready distinguished, dependency-down flips Ready to Unhealthy, startup-probe timing asserted.
- **Concurrency**: two-writer conflict across SqlServer + Postgres + Cosmos + Mongo, `If-Match` → 412, `If-None-Match` → 304, sequence-number idempotency.
- **Microservices.Outbox**: relay drains → publishes via ServiceBus + Kafka in matrix; `ExactlyOnce` under concurrent relay; DLQ branch; `OutboxReplay` backfill.
- **Microservices.Snapshots**: first-run creates `.received.*`, second-run passes, scrubbers applied, mismatch produces readable diff.
- **Parallelism**: port allocator no collisions under 100 concurrent requests, schema names unique across 20 parallel tests, shared-state detector flags static-field write.

### Coverage gate (per package)
- Line ≥ 90%, branch ≥ 85%.
- Contract suite 100%.
- Parallel-isolation smoke green.
- Benchmark regression budget met (fixture startup < 110% of baseline).
- Public types have XML docs (warning-as-error on missing).

---

## Definition of done

1. All ~50 packages build with `dotnet build` — zero warnings (warnings-as-errors).
2. Full solution `dotnet test` green; coverage gate met per package.
3. `NetArchTest` architecture suite green; no circular deps, no rule violations.
4. `BenchmarkDotNet` suite within regression budget.
5. Old packages deleted, `Rig.TUnit.slnx` clean of their references.
6. README + example test per package.
7. Every public API has XML docs.
8. CI matrix green across provider versions (Postgres 14/15/16, SqlServer 2019/2022, Mongo 6/7, Kafka 3.x).
9. All 56 pre-existing tests ported into new layout and green; new count substantially higher (expected several hundred across contract + integration suites).
10. Spec-to-task-to-code traceability: every spec requirement links to at least one test and one source class.

---

## Please produce in the generated spec

- Per-phase task breakdown with explicit RED/GREEN/REFACTOR steps per class.
- Per-package file list (source + three test projects).
- Contract test abstract-class signatures for every base area.
- Provider-specific test method lists (13 mandatory contract tests + 5 per assertion × DSL count).
- Architecture-test rule list for `NetArchTest`.
- Dependency diagram (base → provider → microservice compositions).
- Risk register — flaky tests (containers), flaky providers (Cosmos emulator), version drift, coverage drift, Docker-less CI fallback.
- Exit criteria per phase (merge gate checklist).
- Concrete acceptance tests for each hard requirement R1–R10.

The spec output will feed `/dai.plan`, `/dai.tasks`, `/dai.go` in that order, so ensure it is granular enough to generate an executable task list.
