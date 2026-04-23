# Research — 006-coverage-quality-uplift

Performed: 2026-04-21

---

## 1. CI Pipeline (`.github/workflows/ci.yml`)

### Integration-core job (line 294)
```yaml
integration-core:
  name: Integration — Core (${{ matrix.area }})
  strategy:
    fail-fast: false
    matrix:
      area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]
  steps:
    - name: Restore + Build
      run: |
        dotnet restore Rig.TUnit.slnx
        dotnet build tests/Rig.TUnit.${{ matrix.area }}.Tests.Integration/... --no-restore -c Release
    - name: Run integration
      run: dotnet test --project tests/Rig.TUnit.${{ matrix.area }}.Tests.Integration/... --no-build -c Release -- --coverage ...
```
**T001 change**: add `Core, Ci, Grpc, Http, WebAPI, Mediator` to the `area` array. No step body changes required — `${{ matrix.area }}` already parameterises paths.

### Coverage gate step (line 363)
```yaml
- name: Enforce coverage threshold (line-rate ≥ 0.90, branch-rate ≥ 0.85)
  continue-on-error: true    # ← T002: annotate; T090: remove this line
  shell: bash
  run: |
    ...
    sys.exit(0)   # ← currently exits 0 even on failure (report-only mode)
```
**T002 change**: add annotation comment above `continue-on-error: true`.
**T090 change**: remove `continue-on-error: true` and change `sys.exit(0)` in the offenders branch to `sys.exit(1)`.

### Coverage summary `needs` section (line 326)
The `coverage-summary` job already lists `integration-core` in its `needs`. Adding new matrix entries to `integration-core` is sufficient — no `needs` change required.

---

## 2. Builder Pattern (Pattern A)

### Reference: `PostgresRigBuilder` (100 % covered)
```csharp
public sealed class PostgresRigBuilder : SqlRigBuilder<PostgresRigBuilder>
{
    public PostgresRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }
    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
        => options.UseNpgsql(connectionString);
}
```
All provider builders follow the same 3-line pattern — constructor + `UseProvider` override. The test surface is:
- Null-guard testing on constructor args (via extension tests)
- `UseProvider` exercises the correct `Use{Provider}()` EF Core call
- Extension `Use{Provider}()` wires the builder and returns `RigBuilder` for fluent chaining

### Reference test pattern (`UsePostgresRigBuilderExtensionsTests.cs`)
```csharp
[Test]
public async Task UsePostgres_NullRig_ThrowsArgumentNullException() { ... }

[Test]
public async Task UsePostgres_WithValidArgs_ReturnsSameRigBuilderForFluentChain() { ... }

[Test]
public async Task UsePostgres_ConfigureReceivesPostgresRigBuilderInstance() { ... }
```
Pattern: `services.AddRigTUnit(rig => captured = rig)` to get a `RigBuilder` without starting a container.

### Reference test pattern (`PostgresRigBuilderExerciseTests.cs`)
```csharp
var source = RigConnect.FromValue("Host=localhost;...");
var builder = new PostgresRigBuilder(captured!, source);
builder.ReplaceDbContext<SampleDbContext>();
await using var provider = services.BuildServiceProvider();
var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<SampleDbContext>>();
// Assert the provider-specific extension is present
```

### SqlServer target files
- `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs` — `options.UseSqlServer(connectionString)` — mirrors Postgres exactly
- `src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilderExtensions.cs` — extension entry point

---

## 3. Contract Test Pattern

### Reference: `CacheRigContract.cs`
```csharp
[InheritsTests]
public abstract class CacheRigContract
{
    protected abstract ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct);

    [Test] public virtual async Task Fixture_InitializeAsync_IsIdempotent() { ... }
    [Test] public virtual async Task Builder_UseContainer_ResolvesConnectionSource() => await Task.CompletedTask;
    // etc.
}
```
Concrete provider test classes inherit this and override `CreateCacheRigAsync`. The abstract base is the only source of tests — `[InheritsTests]` propagates them to all concrete subclasses.

---

## 4. Benchmark Project

### `InProcessEmitBenchmarkConfig.cs` (key line)
```csharp
AddJob(Job.Dry
    .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)  // line 18 — fix to Core100
    .WithToolchain(InProcessEmitToolchain.Instance)
    ...
```
BDN version in `Directory.Packages.props`: `0.14.0`. `CoreRuntime.Core100` confirmed available in 0.14.0.

---

## 5. Coverage Scan Findings

From `planning/post-005-coverage-quality-uplift/Real-Coverage-Gap-Matrix.md` (primary source):

| Pattern | Root Cause | Packages |
|---------|-----------|---------|
| A | Builder classes have 0 % — test projects exist but no builder test files | SqlServer, MySql, Oracle, Sqlite, NoSql.Redis, Caching.Redis, Caching.Memory |
| B | Base-family assertion helpers have 0 % — families test at provider level only | Caching, Databases, Databases.NoSql, Databases.Sql, Messaging, Security, Storage |
| C | Individual helper classes missed by existing integration suites | Grpc, Observability.Seq, Microservices.Contracts, Messaging.ServiceBus, Http, HealthChecks, Resilience, Microservices.Saga, Microservices.Outbox, Observability.AppInsights, Microservices.EventSourcing, Security.Jwt, Security.Policies, Messaging.Tests.Contract |

---

## 6. Existing Test Unit Projects (Pattern A)

Each target package already has a `*.Tests.Unit` project. Tests need to be ADDED, not new projects created:
- `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit/` — exists, needs `BuilderTests.cs`
- `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Unit/` — exists
- `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Unit/` — exists
- `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit/` — exists
- `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Unit/` — exists
- `tests/Rig.TUnit.Caching.Redis.Tests.Unit/` — exists
- `tests/Rig.TUnit.Caching.Memory.Tests.Unit/` — exists

---

## 7. Dependency Key Facts

- All builder test files use `Microsoft.Extensions.DependencyInjection` (already a transitive dependency through `Rig.TUnit.Core`).
- `RigConnect.FromValue()`, `RigConnect.FromConfig()`, `RigConnect.FromOptions<T>()`, `RigConnect.FromContainer()` are the four connection source factories to exercise in every Pattern-A test.
- `NSubstitute` v5.3.0 is available project-wide.
- `WireMock.Net` is NOT in the project — do not add it (C-001 resolution).
- `BenchmarkDotNet` 0.14.0 ships `CoreRuntime.Core100`.
