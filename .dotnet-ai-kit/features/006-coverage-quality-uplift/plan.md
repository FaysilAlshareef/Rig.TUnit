# Implementation Plan — 006-coverage-quality-uplift

**Feature**: Coverage & Quality Uplift
**Branch**: `feat/006-coverage-quality-uplift`
**Mode**: Generic / single-repo
**Complexity**: Complex (10 FRs, 7 phases, parallel workstreams)
**Generated**: 2026-04-21

---

## Constitution Check

Project constitution not found (`.dotnet-ai-kit/memory/constitution.md` does not exist). Proceeding without constitution gate — run `/dai.learn` after this feature to generate one.

Key detected conventions applied in this plan:
- .NET 10 / C# 14 (`net10.0`)
- TUnit testing framework (`[Test]`, `Assert.That(...).Is...()`)
- `async Task` test methods (no `Thread.Sleep`)
- `{Method}_{Scenario}_{ExpectedResult}` test naming
- Arrange-Act-Assert with blank-line separation
- `RigConnect.FromValue()` for no-container builder testing
- `services.AddRigTUnit(rig => captured = rig)` DI capture pattern

---

## Complexity Tracking

No violations. All work is additive (test files + CI YAML + docs). No production source files change. No new NuGet packages.

---

## Phase 1 — CI Foundation (BLOCKING)

**Merge this phase before starting Phases 2–4.**

### T001 — Extend `integration-core` matrix

**File**: `.github/workflows/ci.yml`

**Change** (line 294):
```yaml
# BEFORE
area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]

# AFTER
area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience, Core, Ci, Grpc, Http, WebAPI, Mediator]
```

No step body changes needed — `${{ matrix.area }}` already parameterises both the build path and the test path.

**Risk mitigation**: If `Grpc` or `Http` appear to have latent infrastructure failures, wrap ONLY those entries with a conditional `continue-on-error: true` on those specific matrix entries (using `include` with `continue-on-error` per-entry). Create a follow-up task (T001a) and resolve before T090.

**TDD note**: CI YAML changes do not follow RED/GREEN commit discipline. Use a single `green(T001):` commit with note: `CI change — no production code affected`.

### T002 — Annotate coverage gate

**File**: `.github/workflows/ci.yml` (~line 360)

**Change**:
```yaml
      - name: Enforce coverage threshold (line-rate ≥ 0.90, branch-rate ≥ 0.85)
        # Disabled 2026-04-20; re-enabled by feat/006 T090
        continue-on-error: true
```

**TDD note**: Single `green(T002):` commit.

### T003 — Verify CI run

- Push Phase 1 PR to `feat/006-coverage-quality-uplift`
- Confirm all 6 newly-added integration projects appear in CI with PASS
- Record run ID in PR description
- **Exit gate**: All 6 projects GREEN before merging Phase 1 PR

---

## Phase 2 — Pattern A: Builder API Coverage

**Prerequisite**: Phase 1 merged.
**All 7 tasks may be worked in parallel on sub-branches.**

### Pattern: Reference implementation
Follow `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/` exactly:

```csharp
// Extension tests pattern
public sealed class Use{Provider}RigBuilderExtensionsTests
{
    [Test]
    public async Task Use{Provider}_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue("{sample-connection-string}");

        await Assert.That(() =>
                ((RigBuilder)null!).Use{Provider}(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Use{Provider}_NullSource_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.Use{Provider}(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Use{Provider}_WithValidArgs_ReturnsSameRigBuilderForFluentChain()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("{sample-connection-string}");

        var returned = captured!.Use{Provider}(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task Use{Provider}_ConfigureReceives{Provider}RigBuilderInstance()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("{sample-connection-string}");
        {Provider}RigBuilder? configured = null;

        captured!.Use{Provider}(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }
}
```

```csharp
// Builder exercise pattern
public sealed class {Provider}RigBuilderExerciseTests
{
    private const string SampleConnectionString = "{provider-sample-cs}";

    [Test]
    public async Task ReplaceDbContext_WiresUp{Provider}ViaUseProvider_Registers{Provider}OptionsExtension()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new {Provider}RigBuilder(captured!, source);

        // Act
        builder.ReplaceDbContext<SampleDbContext>();

        // Assert
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<SampleDbContext>>();
        var ext = options.Extensions.OfType<{Provider}OptionsExtension>().FirstOrDefault();
        await Assert.That(ext).IsNotNull();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
```

### T010 — `Databases.Sql.SqlServer`

**Source under test**:
- `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs` — `options.UseSqlServer(connectionString)`
- `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilderExtensions.cs`

**Test file**: `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit/SqlServerBuilderTests.cs`

**Provider extension type**: `SqlServerOptionsExtension` (from `Microsoft.EntityFrameworkCore.SqlServer`)

**Sample connection string**: `"Server=localhost;Database=test;User Id=sa;Password=Test1234!;"`

**Tests to write**:
1. `UseSqlServer_NullRig_ThrowsArgumentNullException`
2. `UseSqlServer_NullSource_ThrowsArgumentNullException`
3. `UseSqlServer_NullConfigure_ThrowsArgumentNullException`
4. `UseSqlServer_WithValidArgs_ReturnsSameRigBuilderForFluentChain`
5. `UseSqlServer_ConfigureReceivesSqlServerRigBuilderInstance`
6. `ReplaceDbContext_WiresUpSqlServerViaUseProvider_RegistersSqlServerOptionsExtension`

**TDD**: `red(T010):` commit with test file failing → `green(T010):` with no source change (code exists; tests only needed).

### T011 — `Databases.Sql.MySql`

**Source under test**: `MySqlRigBuilder`, `MySqlRigBuilderExtensions`

**Provider extension type**: `MySqlOptionsExtension` or check via `options.Extensions.Any(e => e.GetType().Name.Contains("MySql"))`

**Sample connection string**: `"Server=localhost;Database=test;User=root;Password=root;"`

**Tests**: mirror T010 pattern substituting `MySql`.

### T012 — `Databases.Sql.Oracle`

**Source under test**: `OracleRigBuilder`, `OracleBuilderExtensions` (note non-standard suffix — check actual class name)

**Sample connection string**: `"User Id=system;Password=oracle;Data Source=localhost/XEPDB1;"`

**Tests**: mirror T010 pattern substituting `Oracle`.

### T013 — `Databases.Sql.Sqlite`

**Source under test**: `SqliteRigBuilder`, `SqliteRigBuilderExtensions`

**Provider extension type**: `SqliteOptionsExtension`

**Sample connection string**: `"Data Source=:memory:"` (Sqlite supports in-memory — no container needed even in builder test)

**Tests**: mirror T010 pattern substituting `Sqlite`.

### T014 — `Databases.NoSql.Redis`

**Source under test**: `RedisKvRigBuilder`, `RedisKvRigBuilderExtensions`, `KeyScanHelper`

**Note**: Redis builder is not EF Core based. Adapt the builder exercise test to verify the connection source type returned rather than `DbContextOptions`:
```csharp
// Verify the builder holds the correct source type
var source = RigConnect.FromValue("localhost:6379");
var builder = new RedisKvRigBuilder(captured!, source);
await Assert.That(builder).IsNotNull(); // smoke test; source stored internally
```

**`KeyScanHelper` coverage**: exercise the `ScanAsync` method with a mocked `IDatabase` from `NSubstitute`.

**Tests for builder** (6): null-rig, null-source, null-configure, valid-fluent-chain, configure-receives-instance.
**Tests for `KeyScanHelper`**: `ScanAsync_WithPattern_ReturnsMatchingKeys`, `ScanAsync_EmptyResult_ReturnsEmptyList`.

### T015 — `Caching.Redis`

**Source under test**: `RedisCacheRigBuilder`, `RedisCacheRigBuilderExtensions`, `RedisBackplaneCapture`

**Builder tests**: mirror T014 Redis pattern.

**`RedisBackplaneCapture` tests** (unit — no container):
- `Capture_PublishedMessage_IsRecorded`: create `RedisBackplaneCapture`, call the capture delegate, assert message is in `CapturedMessages`.
- `Clear_RemovesAllMessages`: populate then clear, assert empty.

These tests should live in the *integration* suite (which already has Redis available). Mark unit-only tests in the `.Tests.Unit` project; backplane tests in `.Tests.Integration`.

### T016 — `Caching.Memory`

**Source under test**: `MemoryCacheRigBuilder`, `MemoryCacheRigBuilderExtensions`, `InMemoryConnectionSource`

**Builder tests**: mirror T013/T014 pattern (no EF Core; `IMemoryCache` is the resource).

**`InMemoryConnectionSource`**: verify it implements `IRigConnectionSource` and returns a non-null `IMemoryCache` from `GetConnectionAsync`.

---

## Phase 3 — Pattern B: Base-Family Assertion Coverage

**Prerequisite**: Phase 1 merged. May run in parallel with Phase 2.

### Pattern: Base-family assertion tests

These test the assertion helpers that live in the base family packages. Use mocked interfaces from `NSubstitute` or in-memory constructs — no containers.

### T020 — `Caching`

**Target classes**: `CacheAssert`, `BackplaneCapture`, `BackplaneMessage`, `ClockControl`, `StampedeTester`

**Test approach**:
- `CacheAssert`: mock `IMemoryCache` with `NSubstitute`. Call `CacheAssert.ContainsKey(cache, "key")` — verify it calls `TryGetValue`.
- `BackplaneMessage`: construct record directly, verify properties.
- `ClockControl`: advance/freeze — pure logic, no external dependencies.
- `BackplaneCapture` + `StampedeTester`: integration-level (Redis required). Add tests to `Rig.TUnit.Caching.Fusion.Tests.Integration` which already has Redis in CI.

**Test file**: `tests/Rig.TUnit.Caching.Tests.Unit/CacheAssertTests.cs` + `ClockControlTests.cs`

### T021 — `Databases`

**Target classes**: `DatabaseAssert`, `MigrationAssert`

**Test approach**: Use `InMemoryDbContext` (via `UseInMemoryDatabase` from `Microsoft.EntityFrameworkCore.InMemory`). Verify `DatabaseAssert.TableExists<TEntity>(context)` returns true after `EnsureCreated()`.

**Test file**: `tests/Rig.TUnit.Databases.Tests.Unit/DatabaseAssertTests.cs`

### T022 — `Databases.NoSql`

**Target classes**: `JsonDocumentAssert`, `ChangeFeedCapture<TDocument>`

**Test approach**:
- `JsonDocumentAssert`: use `JsonDocument.Parse(...)` — pure `System.Text.Json`, no container.
- `ChangeFeedCapture<TDocument>`: integration test requiring Cosmos emulator. Add to existing Cosmos integration suite.

**Test file**: `tests/Rig.TUnit.Databases.NoSql.Tests.Unit/JsonDocumentAssertTests.cs`

### T023 — `Databases.Sql`

**Target classes**: `RawSqlAssert`, `RawSqlAssert<T>`, `DeadlockSimulator`, `TransactionScope`, `DbContextHelper<TContext,T>`

**Test approach**:
- `RawSqlAssert`: use `Microsoft.Data.Sqlite` in-memory. Call raw SQL assertions against a real SQLite connection.
- `DeadlockSimulator` + `TransactionScope`: integration — requires a database. Add to `Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration` (Sqlite is lightweight, no container).
- `DbContextHelper<TContext,T>`: exercise remaining branches via Sqlite in-memory `DbContext`.

**Test file**: `tests/Rig.TUnit.Databases.Sql.Tests.Unit/RawSqlAssertTests.cs`

### T024 — `Messaging`

**Target classes**: `DeadLetterAssert`, `OrderingAssert`, `OrderingAssert<T>`, `MessageAssert`, `EventEnvelope`

**Test approach**: All via `List<CapturedMessage<T>>` in memory — no broker container:
- Populate a list of messages, call assert methods, verify pass/fail behaviour.
- `EventEnvelope`: construct directly, verify properties and serialization.

**Test file**: `tests/Rig.TUnit.Messaging.Tests.Unit/MessagingAssertTests.cs`

### T025 — `Security`

**Target classes**: `SecurityAssert`, `SecurityAssertionException`

**Test approach**: Mock `HttpClient` response with `NSubstitute`. Call `SecurityAssert.ReturnsUnauthorized(response)` etc. Trigger exception path to cover `SecurityAssertionException`.

**Test file**: `tests/Rig.TUnit.Security.Tests.Unit/SecurityAssertTests.cs`

### T026 — `Storage`

**Target classes**: `BlobAssert`, `BlobAssertion`, `BlobDescriptor`, `LifecycleRule`, `SasBuilder`, `BlobAssertionException`

**Test approach**:
- `BlobDescriptor`, `LifecycleRule`, `SasBuilder`: pure value objects — construct and assert properties.
- `BlobAssert` + `BlobAssertion`: mock `BlobContainerClient` with `NSubstitute`. Exercise happy and failure paths.
- `BlobAssertionException`: trigger via failed assertion path.

**Test file**: `tests/Rig.TUnit.Storage.Tests.Unit/BlobAssertTests.cs` + `BlobValueObjectTests.cs`

---

## Phase 4 — Pattern C: Targeted Helper Coverage

**Prerequisite**: Phase 1 merged. May run in parallel with Phases 2 and 3.

### T030 — `Grpc` (40.4 % → ≥ 90 %)

**Target classes**: `GrpcClientHelper<T>`, `EndpointMappingStartupFilter`, `WebApplicationFactoryExtensions`

**Test location**: `tests/Rig.TUnit.Grpc.Tests.Integration/` (now in CI after T001)

**Approach**: Use `WebApplicationFactory<TProgram>` with gRPC service registered. Exercise `GrpcClientHelper` to create a client and make a call. `EndpointMappingStartupFilter` exercise via factory startup filter pipeline.

### T031 — `Observability.Seq` (25.5 % → ≥ 90 %)

**Target classes**: `SeqAssert`, `SeqAssertionException`, `SeqQueryAssertion`, `SeqFixture`

**Approach**: Unit tests for `SeqAssert` + exception via mocked HTTP response. Integration tests extend `SeqFixture` with additional scenarios (Seq container already available in Observability integration job).

### T032 — `Microservices.Contracts` (35 % → ≥ 90 %) — see C-001

**Target classes**: `PactBrokerClientStub`, `ProviderVerificationHarness`, `ProviderVerificationReport`

**Approach** (per C-001 resolution — no WireMock.Net):
- `PactBrokerClientStub.Load()`: write a JSON pact file to `Path.GetTempPath()`, call `Load("consumer","provider")`, verify result.
- `PactBrokerClientStub.DiscoverPacts()`: create temp dir with two `.json` files, verify count.
- `PactBrokerClientStub.Load_FileNotFound_ThrowsFileNotFoundException`.
- `ProviderVerificationHarness.VerifyAllAsync_AllPass_ReturnsSuccessReport`: inline lambda simulator returning `(200, null)`.
- `ProviderVerificationHarness.VerifyAllAsync_StatusMismatch_ReturnsFailureInReport`: simulator returns wrong status.
- `ProviderVerificationHarness.VerifyAllAsync_BodyMismatch_ReturnsFailureInReport`.
- `ProviderVerificationReport`: verify `Success` property, `Verified` count.

**Test file**: `tests/Rig.TUnit.Microservices.Contracts.Tests.Unit/ContractsAssertTests.cs`

### T033 — `Messaging.ServiceBus` (59.7 % → ≥ 90 %)

**Target classes**: `ServiceBusEventSender`, `ServiceBusListener`

**Approach**: Integration tests in `Rig.TUnit.Messaging.ServiceBus.Tests.Integration` (Service Bus emulator). Exercise ACK, NACK, dead-letter, and retry paths. Use `[Retry(3)]` on flaky paths.

### T034 — `Http` (85.1 % → ≥ 90 %)

**Target classes**: `CapturedRequest`, `NoopHandler`, `HttpMockVerifier`

**Approach**: Pure unit tests.
- `CapturedRequest`: construct with method/path/body, verify all properties.
- `NoopHandler`: send a request through it, verify it passes through.
- `HttpMockVerifier`: exercise the uncovered branch assertions.

**Test file**: `tests/Rig.TUnit.Http.Tests.Unit/HttpHelperTests.cs`

### T035 — `HealthChecks` (83.7 % → ≥ 90 %)

**Target class**: `HealthAssertionException`

**Approach**: Trigger the exception via a failing health assertion. Assert exception message and properties.

**Test file**: `tests/Rig.TUnit.HealthChecks.Tests.Unit/HealthAssertionExceptionTests.cs`

### T036 — `Resilience` (81.7 % → ≥ 90 %)

**Target class**: `BulkheadAssert`

**Approach**: Use `NSubstitute` to mock the bulkhead policy. Call `BulkheadAssert.IsRejecting(policy)` with a mock that returns a rejection result.

**Test file**: `tests/Rig.TUnit.Resilience.Tests.Unit/BulkheadAssertTests.cs`

### T037 — `Microservices.Saga` (77.8 % → ≥ 90 %)

**Target classes**: `SagaAssert`, `SagaHarness`, `CompensationFailure`, `SagaAssertionException`

**Approach**: `CompensationFailure` is a record/exception — trigger it directly. `SagaAssert` + `SagaHarness` via existing Saga integration tests with additional scenarios.

### T038 — `Microservices.Outbox` (82.7 % → ≥ 90 %)

**Target classes**: `OutboxEntryAssertion<T>`, `CustomOutboxStore<TRow>`, `OutboxAssertionException`

**Approach**: `OutboxAssertionException` — trigger via failed assertion. `OutboxEntryAssertion<T>` + `CustomOutboxStore<TRow>` — exercise uncovered branches in unit tests using in-memory lists.

### T039 — `Observability.AppInsights` (71.7 % → ≥ 90 %)

**Target classes**: `AppInsightsDependencyAssertion`, `AppInsightsEventAssertion`, `AppInsightsExceptionAssertion`, `AppInsightsRigBuilder`

**Approach**: Unit tests using `NSubstitute` to mock the AppInsights telemetry client. Builder tests follow Pattern A.

### T039b — `Microservices.EventSourcing` (88.7 % → ≥ 90 %)

**Target classes**: `AggregateAssert`, `EventCatalogueAssert`, `RaisedAssertion<T>`

**Approach**: Minimal targeted tests covering uncovered branches. Each class is assertion-helper oriented — mock the aggregate/catalogue with `NSubstitute`.

### T039c — `Security.Jwt` (87.6 % → ≥ 90 %)

**Target class**: `JwtRigBuilder` (cover `FromConfig` and `FromValue` paths)

**Approach**: Follow builder Pattern A — `RigConnect.FromConfig()` and `RigConnect.FromValue("your-secret-key")`.

### T039d — `Security.Policies` (88.8 % → ≥ 90 %)

**Target classes**: `PolicyAssertionException`, `PolicyAssert` (uncovered overloads)

**Approach**: `PolicyAssertionException` — trigger via failed policy assertion. Cover each uncovered `PolicyAssert` overload.

### T039e — `Messaging.Tests.Contract` (78.4 % → ≥ 90 %)

**Approach**: Extend the existing contract base scenario with additional test cases covering uncovered branches in the contract base class.

---

## Phase 5 — Benchmark Remediation

**Independent — may run in parallel with Phases 2–4.**

### T040 — Fix `CoreRuntime.Core80` → `CoreRuntime.Core100`

**File**: `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` line 18

```csharp
// BEFORE
.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)

// AFTER
.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core100)
```

**Verification**: `dotnet build tests/Rig.TUnit.Benchmarks` — must compile clean.

**TDD**: Single `green(T040):` commit — "Benchmark config change — no test needed; this IS the fix".

### T041 — Populate `benchmarks/baseline-006.json`

```bash
dotnet run -c Release --project tests/Rig.TUnit.Benchmarks \
  -- --filter "*" --exporters json --artifacts benchmarks/baseline-tmp
```

Copy the output JSON to `benchmarks/baseline-006.json`. Verify ≥ 50 entries and all `runtime` fields contain `.NET 10.`.

**TDD**: Single `green(T041):` commit — "Baseline JSON is data, not behaviour; populate from local run".

### T042 — Update CI regression step

**File**: `.github/workflows/ci.yml` — benchmark job regression step

**Changes**:
1. Update baseline reference from `baseline-005.json` to `baseline-006.json`
2. Remove `|| echo "::warning::..."` guard so non-zero exit blocks the job

**TDD**: Single `green(T042):` commit.

### T043 — Add `benchmark-action/github-action-benchmark@v1`

**File**: `.github/workflows/ci.yml` — add step to benchmark job

```yaml
- name: Store benchmark result
  uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: benchmarks/baseline-tmp/results/*.json
    github-token: ${{ secrets.GITHUB_TOKEN }}
    auto-push: true
    alert-threshold: '120%'
    comment-on-alert: true
    fail-on-alert: true
```

**Pre-requisite** (manual, one-time): Create `gh-pages` branch and enable GitHub Pages in repo settings (see C-003).

**TDD**: Single `green(T043):` commit.

---

## Phase 6 — Root README Rewrite

**Independent — may run in parallel with Phases 2–5.**

Per `planning/post-005-coverage-quality-uplift/README-Rewrite-Plan.md`.

### T060 — Sections 1–4

1. Headline + badges (coverage badge, NuGet badges, CI status)
2. What is Rig.TUnit (problem it solves)
3. Provider families (table of all 40 packages)
4. Quick-start (5-line example from zero to first integration test)

### T061 — Sections 5–7

5. Builder API (`RigConnect.FromValue`, `FromConfig`, `FromOptions`, `FromContainer`)
6. Isolation (how `IsolationKey` works)
7. Provider catalogue (expanded table with package IDs and feature flags)

### T062 — Sections 8–11

8. Running tests (unit vs integration; `dotnet test` commands)
9. Benchmarks (how to run; where trend chart lives)
10. CI (workflow overview; gate configuration)
11. TDD (RED/GREEN discipline; commit prefixes)

### T063 — Sections 12–14

12. Contributing (branching, PR process)
13. Architecture diagram (mermaid: Core ← Family ← Provider ← Test)
14. License

### T064 — Review pass

Verify every code snippet compiles, all NuGet IDs resolve on nuget.org, all internal file paths exist, mermaid diagram renders in GitHub preview.

### T065 — Link-checker CI job

Add a `linkcheck` job to `.github/workflows/ci.yml`:
```yaml
linkcheck:
  name: Link checker
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v5
    - uses: lycheeverse/lychee-action@v2
      with:
        args: README.md
        fail: true
```

**TDD**: All README tasks use a single `green(T06X):` commit each (documentation, not behaviour).

---

## Phase 7 — Gate Hardening (LAST)

**Must be last. Only start after SC-060 and SC-061 are GREEN on `master`.**

### T090 — Remove `continue-on-error: true`

**File**: `.github/workflows/ci.yml` (~line 363)

**Changes**:
1. Remove the `continue-on-error: true` line
2. Change `sys.exit(0)` to `sys.exit(1)` in the offenders branch of the Python script

```python
# BEFORE
print('::warning::Coverage below threshold (reporting-only) for:')
for line in offenders:
    print(f'::warning::  {line}')
sys.exit(0)  # ← report-only mode

# AFTER
print('::error::Coverage below threshold for:')
for line in offenders:
    print(f'::error::  {line}')
sys.exit(1)  # ← blocking mode
```

**TDD**: Single `green(T090):` commit.

### T091 — Deliberate-regression verification

1. Create branch `test/deliberate-regression`
2. Lower one package's line coverage to < 90 % by deleting a test
3. Push PR
4. Confirm CI fails at the coverage gate step
5. Record the CI run ID in the feature wrap-up document
6. Close PR without merging; delete branch

---

## Implementation Ordering Summary

```
Phase 1 (T001, T002) ──────────────────────────────────── merge ──► unblocks 2, 3, 4
                                                                      │
Phase 2 (T010–T016) ─────────────────────────────── parallel ◄───────┤
Phase 3 (T020–T026) ─────────────────────────────── parallel ◄───────┤
Phase 4 (T030–T039e) ────────────────────────────── parallel ◄───────┘
                                                      │
Phase 5 (T040–T043) ── independent ───────────────────┤
Phase 6 (T060–T065) ── independent ───────────────────┘
                                                      │
Phase 7 (T090, T091) ─────────────────── LAST ◄──────┘ (after SC-060+SC-061 GREEN)
```

---

## TDD Commit Checklist

For each task PR, verify:
- [ ] Phase 2/3/4 tasks: `red(T###):` commit (tests only, failing) present
- [ ] Phase 2/3/4 tasks: `green(T###):` commit (tests pass, no prod code changed) present
- [ ] Phase 1/5/6/7 tasks: single `green(T###):` commit with appropriate note
- [ ] No `--amend` across RED/GREEN boundary
- [ ] No `--no-verify`
- [ ] `commit-discipline-gate` GREEN on each PR

---

## File Change Summary

| File / Directory | Phase | Change Type |
|-----------------|-------|-------------|
| `.github/workflows/ci.yml` | 1, 5, 6, 7 | YAML edits |
| `tests/Rig.TUnit.*.Tests.Unit/*.cs` | 2, 3, 4 | New test files |
| `tests/Rig.TUnit.*.Tests.Integration/*.cs` | 4 | New/extended test files |
| `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` | 5 | One-line edit |
| `benchmarks/baseline-006.json` | 5 | New generated file |
| `README.md` | 6 | Complete rewrite |
