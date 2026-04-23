# Feature 006 — Coverage & Quality Uplift: Roadmap

**Status**: Planning  
**Branch**: `feat/006-coverage-quality-uplift`  
**Baseline scan**: `ci/coverage-scan` run `24712477011` (2026-04-21)  
**Current line coverage**: 80.4 %  
**Target line coverage**: ≥ 90 % per package (68 / 68 assemblies)  
**Current branch coverage**: 66.4 %  
**Target branch coverage**: ≥ 85 % per package

---

## Mission

Raise every failing source package to its coverage gate, close the six missing integration-test
project gaps in production CI, restore functional benchmark regression detection, and ship a
production-quality root README — without touching any production source code that is unrelated to
the coverage fixes.

## Non-goals

- New provider integrations (those belong in Feature 007+).
- Shared-fixture isolation (Phase 3 T066 of the post-005 plan — tracked separately).
- Per-provider README files.
- Changes to public API surface or behaviour.

## Delivery mode

RED → GREEN commit discipline applies to all new tests.  Every task that adds tests gets a RED
commit (test only, must fail build) followed by a GREEN commit (implementation change if any, tests
pass).  Tasks that add only tests against existing untested code use a single GREEN commit (the
code already exists; the test is the fix).

---

## Phase 1 — CI foundation (unblocks everything)

**Goal**: Repair the CI pipeline before writing tests, so every subsequent PR is measured
accurately.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T001 | Extend `integration-core` matrix in `ci.yml` to include `Core, Ci, Grpc, Http, WebAPI, Mediator` | `.github/workflows/ci.yml` line 294 | < 1 h |
| T002 | Add `re-enable by feat/006 T090` comment to coverage gate `continue-on-error` line | `.github/workflows/ci.yml` line 363 | < 15 min |
| T003 | Verify all 6 newly-added integration projects build and pass in CI (PR gate) | CI | 0 h (automated) |

**Phase 1 exit gate**: All 6 previously-missing integration projects appear in the CI run log
with a PASS result.

---

## Phase 2 — Pattern A: Builder API coverage (7 packages)

**Goal**: Reach ≥ 90 % line on each builder-bypass package by adding unit tests that exercise
the `{Provider}RigBuilder` / `{Provider}RigBuilderExtensions` / `ConnectionSource` classes.

Reference implementation: `Rig.TUnit.Databases.Sql.Postgresql.Builder.PostgresRigBuilder` (100 %).
Pattern: call each `RigConnect.*` factory method, assert the returned `ConnectionSource` type and
that the fixture can be constructed from it.

### T010 — SqlServer builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Databases.Sql.SqlServer` (51.4 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `SqlServerRigBuilder` (0 %), `SqlServerRigBuilderExtensions` (0 %) |
| Approach | Unit tests; no container required. Assert `FromConfig` returns `ConfigConnectionSource`, `FromOptions` returns `OptionsConnectionSource<SqlServerFixtureOptions>`, `FromContainer` returns `AutoConnectionSource`, `FromValue` returns `ValueConnectionSource` |

### T011 — MySql builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Databases.Sql.MySql` (72.9 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `MySqlRigBuilder` (20 %) |
| Approach | Same as T010; `MySqlRigBuilder` has partial coverage so only missing branches |

### T012 — Oracle builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Databases.Sql.Oracle` (62.5 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `OracleRigBuilder` (33.3 %), `OracleBuilderExtensions` (0 %) |
| Approach | Unit tests for builder + extensions |

### T013 — Sqlite builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Databases.Sql.Sqlite` (74.3 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `SqliteRigBuilder` (0 %), `SqliteRigBuilderExtensions` (0 %) |
| Approach | Unit tests; Sqlite needs no container |

### T014 — RedisKv builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Databases.NoSql.Redis` (23.5 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `RedisKvRigBuilder` (0 %), `RedisKvRigBuilderExtensions` (0 %), `KeyScanHelper` (50 %) |
| Approach | Builder: unit tests (no container). `KeyScanHelper`: unit tests using a mock `IConnectionMultiplexer` |

### T015 — Redis cache builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Caching.Redis` (38 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Caching.Redis.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `RedisCacheRigBuilder` (0 %), `RedisCacheRigBuilderExtensions` (0 %), `RedisBackplaneCapture` (15.7 %) |
| Approach | Builder: unit tests. `RedisBackplaneCapture`: add calls in the existing `Rig.TUnit.Caching.Redis.Tests.Integration` suite |

### T016 — Memory cache builder tests

| Item | Detail |
|------|--------|
| Package | `Rig.TUnit.Caching.Memory` (63.1 % → target ≥ 90 %) |
| Test file | `tests/Rig.TUnit.Caching.Memory.Tests.Unit/BuilderTests.cs` |
| Classes to cover | `MemoryCacheRigBuilder` (0 %), `MemoryCacheRigBuilderExtensions` (0 %), `InMemoryConnectionSource` (0 %) |
| Approach | Unit tests; `InMemoryConnectionSource` needs no container |

**Phase 2 exit gate**: All 7 Pattern A packages report ≥ 90 % line in CI.

---

## Phase 3 — Pattern B: Base-family assertion coverage (7 packages)

**Goal**: Exercise the base-family assertion helpers and utility classes that provider integration
tests bypass.

### T020 — `Rig.TUnit.Caching` base (18 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `CacheAssert` (0 %), `BackplaneCapture` (20 %), `BackplaneMessage` (0 %), `ClockControl` (0 %), `StampedeTester` (0 %) |
| Approach | Add a `CacheBaseAssertTests.cs` in `Rig.TUnit.Caching.Tests.Unit` (create if not exists). `CacheAssert` can be unit-tested with a mock `IMemoryCache`. `StampedeTester` / `BackplaneCapture` require a Redis container — add from the Fusion integration suite which already has Redis |

### T021 — `Rig.TUnit.Databases` base (46.9 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `DatabaseAssert` (0 %), `MigrationAssert` (0 %) |
| Approach | Add `DatabaseAssertTests.cs` in `Rig.TUnit.Databases.Tests.Unit`. Both classes work against an `IDbConnection` / `DbContext` — use `InMemoryDbContext` (already used by Sqlite tests) |

### T022 — `Rig.TUnit.Databases.NoSql` base (12.5 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `JsonDocumentAssert` (8 %), `ChangeFeedCapture<TDocument>` (0 %) |
| Approach | `JsonDocumentAssert`: unit test with `System.Text.Json.JsonDocument`. `ChangeFeedCapture<TDocument>`: add calls in `Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration` (the only provider that uses change feed) |

### T023 — `Rig.TUnit.Databases.Sql` base (43.5 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `RawSqlAssert` (0 %), `RawSqlAssert<T>` (0 %), `DeadlockSimulator` (0 %), `TransactionScope` (0 %), `DbContextHelper<TContext,T>` (55.8 %) |
| Approach | `RawSqlAssert`: unit test with Sqlite in-memory. `DeadlockSimulator` / `TransactionScope`: integration tests against `Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration` (no extra container). `DbContextHelper<TContext,T>` branch gaps: add missing overload tests |

### T024 — `Rig.TUnit.Messaging` base (30.9 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `DeadLetterAssert` (0 %), `OrderingAssert` (0 %), `OrderingAssert<T>` (0 %), `MessageAssert` (20 %), `EventEnvelope` (0 %) |
| Approach | All four classes can be unit-tested with `List<CapturedMessage<T>>` — no container. Add `MessagingBaseAssertTests.cs` in `Rig.TUnit.Messaging.Tests.Unit` |

### T025 — `Rig.TUnit.Security` base (25.9 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `SecurityAssert` (0 %), `SecurityAssertionException` (0 %) |
| Approach | Unit tests with a mock `HttpClient` / `IHttpClientFactory`. Add `SecurityAssertTests.cs` in `Rig.TUnit.Security.Tests.Unit` |

### T026 — `Rig.TUnit.Storage` base (16.6 % → ≥ 90 %)

| Item | Detail |
|------|--------|
| Classes to cover | `BlobAssert` (0 %), `BlobAssertion` (0 %), `BlobAssertionException` (0 %), `BlobDescriptor` (0 %), `LifecycleRule` (0 %), `SasBuilder` (0 %) |
| Approach | `BlobDescriptor`, `LifecycleRule`, `SasBuilder`: pure value-object unit tests. `BlobAssert` / `BlobAssertion`: unit tests with a mock `BlobContainerClient`. Add `StorageBaseAssertTests.cs` in `Rig.TUnit.Storage.Tests.Unit` |

**Phase 3 exit gate**: All 7 Pattern B packages report ≥ 90 % line in CI.

---

## Phase 4 — Pattern C: Targeted helper coverage (miscellaneous)

**Goal**: Close the remaining class-level zeros and partial coverage in packages that don't fit
cleanly into Pattern A or B.

| Task | Package (current → target) | Classes to cover | Approach |
|------|---------------------------|-----------------|---------|
| T030 | `Rig.TUnit.Grpc` (40.4 % → ≥ 90 %) | `GrpcClientHelper<TClient,TProgram,TResult>` (0 %), `GrpcClientHelper<TClient,TProgram>` (0 %), `EndpointMappingStartupFilter` (0 %), `WebApplicationFactoryExtensions` (26.6 %) | Add Grpc integration tests in `Rig.TUnit.Grpc.Tests.Integration` (now in CI after T001). These tests already exist but don't call all code paths — add missing call patterns |
| T031 | `Rig.TUnit.Observability.Seq` (25.5 % → ≥ 90 %) | `SeqAssert` (0 %), `SeqAssertionException` (0 %), `SeqQueryAssertion` (0 %), `SeqFixture` (40.8 %) | Add assertion API calls in `Rig.TUnit.Observability.Seq.Tests.Integration`. `SeqFixture` already starts a container; extend existing tests |
| T032 | `Rig.TUnit.Microservices.Contracts` (35 % → ≥ 90 %) | `PactBrokerClientStub` (0 %), `ProviderVerificationHarness` (0 %), `ProviderVerificationReport` (0 %) | Unit tests with a `WireMock.Net` stub for the Pact Broker HTTP calls. These helpers are pure HTTP client wrappers |
| T033 | `Rig.TUnit.Messaging.ServiceBus` (59.7 % → ≥ 90 %) | `ServiceBusEventSender` (35 %), `ServiceBusListener` (26.6 %) | Add message-flow tests in `Rig.TUnit.Messaging.ServiceBus.Tests.Integration`; exercise ACK, NACK, dead-letter, retry paths |
| T034 | `Rig.TUnit.Http` (85.1 % → ≥ 90 %) | `CapturedRequest` (0 %), `NoopHandler` (0 %), `HttpMockVerifier` (64.7 %) | Add unit tests for `CapturedRequest` (value object), `NoopHandler` (passthrough), and missing `HttpMockVerifier` branches |
| T035 | `Rig.TUnit.HealthChecks` (83.7 % → ≥ 90 %) | `HealthAssertionException` (0 %) | Add a test that triggers the exception path in `HealthAssert`; one test suffices |
| T036 | `Rig.TUnit.Resilience` (81.7 % → ≥ 90 %) | `BulkheadAssert` (0 %) | Add bulkhead policy test in `Rig.TUnit.Resilience.Tests.Unit` |
| T037 | `Rig.TUnit.Microservices.Saga` (77.8 % → ≥ 90 %) | `SagaAssert` (50 %), `SagaHarness` (69.2 %), `CompensationFailure` (0 %), `SagaAssertionException` (0 %) | Add compensation-failure path test; exercise `SagaAssert` error overload |
| T038 | `Rig.TUnit.Microservices.Outbox` (82.7 % → ≥ 90 %) | `OutboxEntryAssertion<T>` (48.2 %), `CustomOutboxStore<TRow>` (33.3 %), `OutboxAssertionException` (0 %) | Add exception-path tests; exercise `CustomOutboxStore` overloads |
| T039 | `Rig.TUnit.Observability.AppInsights` (71.7 % → ≥ 90 %) | `AppInsightsDependencyAssertion` (0 %), `AppInsightsEventAssertion` (33.3 %), `AppInsightsExceptionAssertion` (33.3 %), `AppInsightsRigBuilder` (50 %) | Add missing assertion-type tests in integration suite; builder: add `FromConfig` / `FromOptions` path tests |
| T039b | `Rig.TUnit.Microservices.EventSourcing` (88.7 % → ≥ 90 %) | `AggregateAssert` (66.6 %), `EventCatalogueAssert` (62.5 %), `RaisedAssertion<T>` (66.6 %) | Add missing overload calls in existing unit tests |
| T039c | `Rig.TUnit.Security.Jwt` (87.6 % → ≥ 90 %) | `JwtRigBuilder` (66.6 %) | Add `FromConfig` / `FromValue` builder path tests |
| T039d | `Rig.TUnit.Security.Policies` (88.8 % → ≥ 90 %) | `PolicyAssertionException` (0 %), `PolicyAssert` (76.1 %) | Add exception-path test; exercise missing `PolicyAssert` overloads |
| T039e | `Rig.TUnit.Messaging.Tests.Contract` (78.4 % → ≥ 90 %) | `MessagingRigContract` (78.4 %) | Add missing contract scenario in the contract test base |

**Phase 4 exit gate**: All 15 Pattern C packages report ≥ 90 % line in CI.

---

## Phase 5 — Benchmark remediation

**Goal**: Fix the .NET 8→10 runtime bug, populate the baseline, and add benchmark visualisation.

| Task | Description | Depends on | Effort |
|------|-------------|-----------|--------|
| T040 | Fix `InProcessEmitBenchmarkConfig.WithRuntime` to `CoreRuntime.Core100` | — | 30 min |
| T041 | Run full benchmark suite locally; commit `benchmarks/baseline-006.json` | T040 | 2 h |
| T042 | Update `ci.yml` baseline path; remove `\|\| echo` guard | T041 | 15 min |
| T043 | Add `benchmark-action/github-action-benchmark` to CI; enable GitHub Pages | T042 | 4 h |

See `Benchmark-Remediation-Plan.md` for full detail.

**Phase 5 exit gate**: CI benchmark job fails on a simulated regression (test by temporarily
lowering threshold to 101 %); GitHub Pages shows a trend chart.

---

## Phase 6 — README rewrite

**Goal**: Replace the placeholder root `README.md` with the 14-section production version.

| Task | Description | Effort |
|------|-------------|--------|
| T060 | Draft Sections 1–4 | 2 h |
| T061 | Draft Sections 5–7 | 3 h |
| T062 | Draft Sections 8–11 | 2 h |
| T063 | Draft Sections 12–14 | 2 h |
| T064 | Review pass — compile snippets, verify NuGet names | 2 h |
| T065 | Merge after Phase 6 exit gate | — |

See `README-Rewrite-Plan.md` for full section-by-section guidance.

**Phase 6 exit gate**: Link-checker CI job passes on the new README (all badge URLs resolve,
all internal file references exist).

---

## Phase 7 — Gate hardening

**Goal**: Re-enable the coverage gate as a hard block.

| Task | Description | Depends on |
|------|-------------|-----------|
| T090 | Remove `continue-on-error: true` from coverage gate step | All Phase 2–4 tasks complete in CI |
| T091 | Verify gate blocks a deliberate regression PR | T090 |

**Phase 7 exit gate**: A PR that drops one package below 90 % is blocked by CI.

---

## Functional requirements

| FR | Description |
|----|-------------|
| FR-060 | Every source package reports ≥ 90 % line coverage in CI |
| FR-061 | Every source package reports ≥ 85 % branch coverage in CI |
| FR-062 | All integration-test projects run on every push/PR to `master` |
| FR-063 | Coverage gate blocks merges (no `continue-on-error`) |
| FR-064 | Benchmark baseline is populated with real .NET 10 numbers |
| FR-065 | Benchmark regression detection blocks merges on ≥ 20 % regression |
| FR-066 | Root README passes link-checker CI job |

---

## Success criteria

| SC | Criterion |
|----|-----------|
| SC-060 | `coverage-scan-results/summary.csv` shows 0 packages below 90 % line gate |
| SC-061 | `coverage-scan-results/summary.csv` shows 0 packages below 85 % branch gate |
| SC-062 | `ci.yml` `integration-core` matrix includes `Core, Ci, Grpc, Http, WebAPI, Mediator` |
| SC-063 | `ci.yml` coverage gate step has no `continue-on-error` |
| SC-064 | `benchmarks/baseline-006.json` contains at least 50 benchmark entries |
| SC-065 | GitHub Pages benchmark trend chart is publicly accessible |
| SC-066 | `README.md` contains all 14 required sections; link-checker passes |

---

## Effort estimates

| Phase | Tasks | Estimated hours |
|-------|-------|----------------|
| 1 — CI foundation | T001–T003 | 1 h |
| 2 — Builder coverage | T010–T016 | 14 h |
| 3 — Base assertions | T020–T026 | 18 h |
| 4 — Targeted helpers | T030–T039e | 20 h |
| 5 — Benchmarks | T040–T043 | 7 h |
| 6 — README | T060–T065 | 11 h |
| 7 — Gate hardening | T090–T091 | 1 h |
| **Total** | 35 tasks | **~72 h** |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Grpc / Http integration tests have real failures | Medium | High | Add with `continue-on-error: true` initially (T001); fix failures before removing |
| Azure Service Bus emulator flaky in CI | Medium | Medium | `[Retry(3)]` attribute; mark flaky with `FlakyQuarantine` if persistent |
| `ChangeFeedCapture` requires Cosmos emulator (slow) | Low | Low | Only needed for T022; Cosmos job already exists in CI |
| `benchmark-action/github-action-benchmark` requires `gh-pages` branch | Low | Low | Create branch before T043 |
| Pattern A builder tests find API bugs | Low | High | Treat as RED commit; fix source before GREEN |

---

## Branch strategy

```
master
  └── feat/006-coverage-quality-uplift
        ├── T001-T003   (Phase 1 — CI)
        ├── T010-T016   (Phase 2 — Builder)
        ├── T020-T026   (Phase 3 — Base assertions)
        ├── T030-T039e  (Phase 4 — Targeted)
        ├── T040-T043   (Phase 5 — Benchmarks)
        ├── T060-T065   (Phase 6 — README)
        └── T090-T091   (Phase 7 — Gate)
```

Phases 2, 3, and 4 can be PRed in parallel after Phase 1 merges.
Phases 5 and 6 are independent of each other and of Phases 2–4.
Phase 7 must be last.

---

## Open questions

1. Should `baseline-005.json` be replaced in-place or should a new `baseline-006.json` be created?
   (Preference: rename to avoid a confusing empty file in history.)
2. Does the `benchmark-action/github-action-benchmark` dashboard need to be gated behind
   authentication or is public read acceptable for this repository?
3. Should `Rig.TUnit.Messaging.Tests.Contract` (78.4 %) be fixed by extending the base contract
   or by adding a separate unit test for the missing scenario?
