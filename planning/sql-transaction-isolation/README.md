# Planning — SQL transaction & isolation matrix (F-016)

**Feature ID**: F-016
**Family**: SQL
**Status**: planned
**Depends on**: F-015 (schema topology — declare tables with concurrency tokens)
**Target release**: v0.12
**Estimated tasks**: ~72 (Phase 0: 7 · 5 providers × 12 tasks · 5 docs)

---

## Why this feature exists

Concurrency-correctness in SQL is the source of most production bugs that integration tests should catch but don't. The rig today has no surface for:

- Forcing a specific `IsolationLevel` per transaction.
- Asserting that a known operation **is** the deadlock victim.
- Reproducing optimistic-concurrency conflicts deterministically.
- Asserting `READ COMMITTED SNAPSHOT` vs `SERIALIZABLE` semantic differences.
- Driving two concurrent transactions to a known race-window.

Real-world bugs this catches:
- `Repository.UpdateAsync` not re-reading row version after concurrency exception → silent overwrite.
- A migration that adds a NOT NULL column without `WITH ONLINE` → blocking lock under load.
- A reporting query holding `SH` locks longer than the SLA → cascade blocking.

## What we deliver

Two cooperating surfaces.

**1. Isolation control:**

```csharp
public abstract partial class SqlFixture
{
    public Task<DbTransaction> BeginAsync(IsolationLevel level, CancellationToken ct = default);
    public Task<T> WithIsolationAsync<T>(IsolationLevel level, Func<DbTransaction, Task<T>> action, CancellationToken ct = default);
}
```

**2. Concurrency assertions:**

```csharp
public static class TransactionAssert
{
    public static DeadlockAssertion Deadlock();
    public static ConcurrencyAssertion OptimisticConcurrencyConflict<T>();
    public static IsolationAssertion Isolation(IsolationLevel level);
}

public sealed class DeadlockAssertion
{
    public DeadlockAssertion VictimWas(string queryTag);
    public DeadlockAssertion DetectedWithinMs(int threshold);
}
```

Plus a **race-window helper**:

```csharp
public static class Race
{
    public static Task<RaceResult> RunAsync(Func<Task> a, Func<Task> b, RaceBarrier barrier, CancellationToken ct);
}

public sealed class RaceBarrier
{
    public IDisposable HoldAt(string label);
    public Task ReleaseAsync(string label);
}
```

`RaceBarrier` lets a test suspend `tx_a` at a labelled point and resume it after `tx_b` reaches its own labelled point — deterministic interleaving.

## Gaps closed (from SQL-1 in the gap analysis)

- Deadlock-victim assertions.
- Isolation-level matrix coverage.
- Optimistic concurrency reproduction.
- Race-window deterministic interleaving.

## Providers in scope

5: SqlServer, Postgresql, MySql, Oracle, Sqlite (Sqlite has reduced isolation surface — `SERIALIZABLE` only — that's documented, not faked).

## Exit criteria

- `WithIsolationAsync`, `TransactionAssert`, `Race` ship with 100 % line coverage.
- Each provider has ≥ 4 RED scenarios: dirty-read demonstration, lost-update prevention, deadlock injection, optimistic concurrency.
- `ProviderCompletenessTests` extended with `SqlProviders_Declare_TransactionAssertions`.
- `docs/providers/*.md` adds an "Isolation matrix" table per provider listing which IsolationLevels the engine actually honours.

## Dependencies on other planned features

- Upstream: F-015 (need WithSchema to declare row-version columns).
- Downstream: F-018 (CDC tests assert read-only isolation), F-038 (outbox correctness uses TransactionAssert), F-047 (resilience policies under deadlock retry).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 016-sql-transaction-isolation

Read first:
- planning/sql-transaction-isolation/README.md
- planning/sql-schema-and-migrations/README.md (F-015 must be shipped)
- SqlServer / Postgres / MySql / Oracle / Sqlite isolation-level docs
- src/Rig.TUnit.Databases.Sql/* (existing fixture shape)

Generate a feature spec that:
1. Introduces WithIsolationAsync, TransactionAssert (Deadlock / OptimisticConcurrencyConflict / Isolation), and Race + RaceBarrier.
2. Phase 0 lands the contract + ProviderCompletenessTests parity.
3. Phases 1..5 per provider deliver 4 RED scenarios each (dirty read, lost update, deadlock, optimistic concurrency).
4. Document each provider's actual honoured IsolationLevels (Sqlite is SERIALIZABLE only).

Constraints:
- Race / RaceBarrier MUST be deterministic — labelled holds, no Thread.Sleep.
- TransactionAssert.Deadlock detects via SqlException error codes, not timeouts.
- Pre-release library — no [Obsolete] aliases.
- File-scoped namespaces, sealed concrete types, TUnit AAA.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
