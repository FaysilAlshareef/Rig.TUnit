# CI Postgres Flake — Root Cause Analysis

**Date:** 2026-04-19
**Branch:** `master` (commit `9d3369f` — merge of Feature 004 PR #3)
**Failing job:** `Integration — SQL matrix (Postgresql)` on run [`24624135692`](https://github.com/FaysilAlshareef/Rig.TUnit/actions/runs/24624135692/job/72000002793)

## Symptom

One test fails on master CI:

```
UsePostgresFluentTests.UsePostgres_DbContext_PerformsInsertSelectRoundTrip
  TUnit.Engine.Exceptions.TestFailedException:
  [Test Failure] DbUpdateException: An error occurred while saving the entity changes.
  ---> Npgsql.PostgresException: 42P01: relation "Samples" does not exist
```

The failure occurs at `UsePostgresFluentTests.cs:31` (`await ctx.SaveChangesAsync();`).
All other 48 jobs in the run are green (SqlServer, Sqlite, MySql, Oracle, Mongo, Cosmos, Kafka, Redis, etc.).

## User's hypothesis vs reality

**Hypothesis reported by user:** "CI tests were running very well before merge and now fail — this is a CI issue or test issue introduced by the merge."

**Reality from `gh run list`:**

| Time | Branch | Commit | Conclusion |
|---|---|---|---|
| 2026-04-19T07:47:53Z | master | 9d3369f | **failure** |
| 2026-04-19T07:41:39Z | feat/provider-consistency-remediation | 3b936df | success |
| 2026-04-19T07:35:12Z | feat/provider-consistency-remediation | b024346 | **failure** |
| 2026-04-19T07:16:30Z | feat/provider-consistency-remediation | 55b63a2 | **failure** |
| 2026-04-19T00:45:54Z | feat/provider-consistency-remediation | fc961c1 | **failure** |
| 2026-04-19T00:45:40Z | feat/provider-consistency-remediation | a0039ca | **failure** |

`git log 3b936df..9d3369f` returns only the merge commit — zero source changes between the last green feat-branch run and the failing master run.

**Conclusion:** The bug existed on the feat branch. The branch failed CI five times in a row on this exact spot, then passed once (probably by luck), then the PR was merged to master and the same race re-triggered. This is not a regression introduced by the merge — it is a **pre-existing flaky test merged through a weak gate**.

## Root cause

### Test design

[`tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`](../../tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs):

```csharp
public sealed class UsePostgresFluentTests
{
    [Test]
    public async Task UsePostgres_DbContext_PerformsInsertSelectRoundTrip()
    {
        var fx = await SharedPostgresFixture.GetAsync();        // <-- shared container
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UsePostgres(fx.ConnectionString).Options;
        await using var ctx = new SampleDbContext(options);
        await ctx.Database.EnsureCreatedAsync();                 // creates "Samples" table
        var entity = new SampleEntity { Name = $"round-trip-{Guid.NewGuid():N}" };
        ctx.Samples.Add(entity);
        await ctx.SaveChangesAsync();                            // <-- 42P01 here
        ...
    }
    ...
    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
    {
        public DbSet<SampleEntity> Samples => Set<SampleEntity>();
    }
    private sealed class SampleEntity { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
}
```

### Shared fixture

[`SharedPostgresFixture.cs`](../../tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/SharedPostgresFixture.cs):

```csharp
internal static class SharedPostgresFixture
{
    private static readonly Lazy<Task<PostgresFixture>> Instance = new(async () =>
    {
        var fx = new PostgresFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<PostgresFixture> GetAsync() => Instance.Value;
}
```

### Sibling tests in the same project

- `PostgresContract.cs` — inherits from SQL contract suite
- `PostgresDbContextHelperTests.cs`
- `PostgresParallelIsolationTests.cs` — inherits `ParallelIsolationContract`
- `PostgresQuirkTests.cs`
- `UsePostgresFluentTests.cs` (the failing file — **the only file that uses `Samples`**)

### Race condition

1. `SharedPostgresFixture` hands every test the **same** connection string → all tests share ONE physical Postgres database.
2. TUnit runs tests in parallel by default.
3. `UsePostgres_DbContext_PerformsInsertSelectRoundTrip` calls `EnsureCreatedAsync()` which creates the `Samples` table, then `Add` + `SaveChangesAsync` against it.
4. Concurrently, other tests (`PostgresContract`, `PostgresDbContextHelperTests`, `PostgresQuirkTests`) may drop/recreate schema via their own `EnsureCreated` / `EnsureDeleted` / helper-driven DB resets against the same DB.
5. If a sibling test's teardown drops the schema between our test's `EnsureCreated` (table exists) and `SaveChanges` (writes to table), Postgres returns `42P01: relation "Samples" does not exist`.

The 22-second test duration (vs sub-second on the green attempt) confirms this is a timing-sensitive race, not a deterministic bug.

### Rule violations

1. [`.claude/rules/testing.md`](../../.claude/rules/testing.md) — **"Never share mutable state between tests"** and **"EF Core / Database Tests: Never share database state between tests"**.
2. [`.claude/rules/architecture-profile.md`](../../.claude/rules/architecture-profile.md) — **"NEVER share mutable state between tests"**.

## Why the gate let this through

1. CI on feat branch failed 5×, passed 1×. The PR was merged after the one green run. There is no configured run-the-CI-N-times-for-flake-detection step.
2. The "Build + Unit + Arch" job enumerates every non-integration/contract/benchmark project individually — it never runs the failing test (which is in `Integration`).
3. `fail-fast: false` on the matrix means one green SqlServer run doesn't block merge; the PR reviewer looked at the successful run only.
4. No coverage or mutation-test gate catches this class of race.

## Broader exposure

Every other family has the same pattern — one shared container fixture per test project with parallel test execution. These have not flaked yet, but the design risk is identical. Files to audit:

- `tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Databases.Sql.MySql.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Databases.NoSql.*.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Messaging.*.Tests.Integration/Shared*Fixture.cs`
- `tests/Rig.TUnit.Storage.*.Tests.Integration/Shared*Fixture.cs`

## Fix direction (not applied — planning only)

Three options, preference order:

### Option A — Per-test database via existing helper (preferred)

`Rig.TUnit.Databases.Sql.Postgresql` already ships `PostgresDbContextHelper` per Feature 004 FR-005. Refactor the test to request a per-test database name from the shared container:

```csharp
var fx = await SharedPostgresFixture.GetAsync();
await using var db = await fx.CreateEphemeralDatabaseAsync();  // unique DB per test
var options = new DbContextOptionsBuilder<SampleDbContext>()
    .UsePostgres(db.ConnectionString).Options;
```

Pros: parallelism preserved, isolation restored, matches `DatabasePerTestHelper` pattern.
Cons: requires a small helper addition if `CreateEphemeralDatabaseAsync` doesn't exist today.

### Option B — Unique schema per test

Scope `SampleDbContext` to a test-generated schema (`CREATE SCHEMA t_<guid>`). Drop on teardown.

Pros: no fixture changes.
Cons: EF Core model needs `modelBuilder.HasDefaultSchema(...)` injection; invasive.

### Option C — Serialize the offending test

Mark `UsePostgresFluentTests` with `[NotInParallel]` (TUnit attribute). Lowest effort.

Pros: one-line fix.
Cons: halves the test-parallelism benefit; doesn't fix sibling-race risk across the family.

## CI gate hardening

In addition to the test fix:

1. Add a **flake-detection step** on PR branches: re-run failed jobs up to 3× and treat any flake as a PR review flag (don't auto-merge).
2. Add coverage collection to each matrix job (cobertura output + artifact upload).
3. Consider mirroring the test infrastructure audit as a dedicated `architecture-test` job that runs `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` without the `SkipUntilFixed` category.
4. Upload the TUnit HTML report as an artifact so triage doesn't require raw run logs.
