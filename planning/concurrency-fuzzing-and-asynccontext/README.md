# Planning — Concurrency fuzzing + AsyncLocal flow (F-048)

**Feature ID**: F-048
**Family**: Concurrency
**Status**: planned
**Depends on**: F-014 (shuffle-replay determinism)
**Target release**: v0.15
**Estimated tasks**: ~32 (Phase 0: 7 · 2 packages × 10 tasks · 5 docs)

---

## Why this feature exists

F-014 introduces the foundational `Concurrency.Fuzz` primitive. F-048 deepens it with:

- **Deadlock detection** via wait-graph snapshot at the failure point.
- **`AsyncLocal` / `ExecutionContext` flow** assertions through `Task.Run`, `Parallel.ForEachAsync`, channels, `IAsyncEnumerable`.
- **Memory-fence / volatile-write ordering** assertions on benchmarks.
- **Thread-pool starvation** simulation (work-item flood).
- **Fairness / starvation** thresholds.

Real-world bugs:
- A `lock (state) { … await … }` deadlock that only manifests under specific scheduling.
- An ambient `IServiceProvider` lost via `Task.Run` — silently uses the wrong DI scope.
- Memory ordering bug where reader sees pre-write state on weak architectures.

## What we deliver

```csharp
public sealed class WaitGraph
{
    public IReadOnlyList<ThreadEdge> Edges { get; }
    public bool HasCycle();
    public IReadOnlyList<long> CycleThreadIds { get; }
}

public static class DeadlockAssert
{
    public static DeadlockAssertion FromFuzzReport(FuzzReport report);
}

public sealed class DeadlockAssertion
{
    public DeadlockAssertion None();
    public DeadlockAssertion Cycle().BetweenThreads(int count);
    public DeadlockAssertion WaitGraphCaptured();
}

public static class AsyncContextAssert
{
    public static AsyncFlowAssertion Flowed(string scopeKey).Through(AsyncBoundary boundary);
}

public sealed class AsyncFlowAssertion
{
    public AsyncFlowAssertion Preserved();
    public AsyncFlowAssertion Lost();
    public AsyncFlowAssertion DiagnosticHint(string boundary);
}

public static class ConcurrencyStarvationAssert
{
    public static StarvationAssertion ThreadPool();
}
```

## Gaps closed (from CONC-1 in the gap analysis)

- Deadlock detection with wait-graph capture.
- AsyncLocal flow through async boundaries.
- Thread-pool starvation simulation.
- Fairness / starvation thresholds.

## Providers in scope

2: `src/Rig.TUnit.Concurrency`, `src/Rig.TUnit.Parallelism`.

## Exit criteria

- `WaitGraph`, `DeadlockAssert`, `AsyncContextAssert.Flowed`, `ConcurrencyStarvationAssert` ship with 100 % line coverage.
- ≥ 5 RED scenarios.
- `docs/providers/concurrency.md` updated.

## Dependencies on other planned features

- Upstream: F-014.
- Downstream: none.

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 048-concurrency-fuzzing-and-asynccontext

Read first:
- planning/concurrency-fuzzing-and-asynccontext/README.md
- planning/shuffle-replay-determinism/README.md (F-014 must be shipped)
- src/Rig.TUnit.Concurrency/* and Parallelism/* (current state)

Generate a feature spec that:
1. Introduces WaitGraph + DeadlockAssert + AsyncContextAssert.Flowed + ConcurrencyStarvationAssert.
2. ≥ 5 RED scenarios.

Constraints:
- WaitGraph capture deterministic (no race on snapshot).
- AsyncContextAssert covers Task.Run, Parallel.ForEachAsync, ChannelReader, IAsyncEnumerable, ConfigureAwait(false).
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
