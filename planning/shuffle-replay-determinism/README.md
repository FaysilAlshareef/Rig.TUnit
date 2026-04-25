# Planning — Shuffle-replay determinism (F-014)

**Feature ID**: F-014
**Family**: Cross-cutting
**Status**: planned
**Depends on**: F-008 (deterministic clock — paired with deterministic scheduling)
**Target release**: v0.11
**Estimated tasks**: ~20 (Phase 0: 7 · Concurrency package × 8 · 5 docs)

---

## Why this feature exists

Race-condition tests need **reproducibility** — the same seed must always produce the same interleaving. Today `Rig.TUnit.Concurrency` and `Rig.TUnit.Parallelism` provide raw primitives but no schedule recording. A flaky test that fails 1 in 1000 runs cannot be debugged because the next run takes a different scheduling path.

Real-world test patterns this enables:
- "Run this test 1000 times; if any schedule produces a deadlock, dump that schedule and replay it."
- "Property-test: any concurrent interleaving must yield total = sum of writes."
- "Asserting fairness: under high concurrency, no thread starves > 100 ms."

`Microsoft.Coyote` is the industry-standard primitive for systematic concurrency testing in .NET; the rig should integrate it.

## What we deliver

A `Concurrency.Fuzz(action, schedules: int)` static surface that runs an action under N different deterministic schedules, recording each as a replayable seed. On failure, the seed is logged so the test reruns the exact same interleaving.

```csharp
public static class Concurrency
{
    public static FuzzReport Fuzz(Func<Task> action, int schedules = 1000, int? seed = null);
    public static FuzzReport Replay(Func<Task> action, int seed);
}

public sealed record FuzzReport(int SchedulesRun, int Failures, IReadOnlyList<int> FailingSeeds);
```

Plus a `[Fuzz(Schedules = 1000)]` TUnit attribute for declarative use.

## Gaps closed (from CC-7 in the gap analysis)

- Race-condition tests have no reproducibility primitive.
- Flaky tests cannot be replayed.
- No fairness / starvation assertions.
- No deadlock-detector that captures the wait-graph at the failure point.

## Providers in scope

This is **single-package** — `src/Rig.TUnit.Concurrency` and `src/Rig.TUnit.Parallelism`. No provider rollout.

## Public API surface (sketch)

```csharp
public sealed class FuzzScope
{
    public IDisposable RecordSchedule();
    public void DumpScheduleOnFailure(string testName);
    public WaitGraph CaptureWaitGraph();
}

public sealed record WaitGraph(IReadOnlyList<ThreadEdge> Edges)
{
    public bool HasCycle();
}
```

## Exit criteria

- `Concurrency.Fuzz` and `[Fuzz]` attribute ship; 100 % line coverage.
- Coyote integration documented under `docs/providers/concurrency.md`.
- Self-test: a known-flaky producer-consumer test is added under `tests/Rig.TUnit.Concurrency.Tests.Unit`, fuzzed at 1 000 schedules, with a failing seed dumped.
- Failing-seed replay verified — replay with the same seed reproduces the same failure.

## Dependencies on other planned features

- Upstream: F-008 — deterministic clock so scheduled `Task.Delay` advances under fuzz, not real time.
- Downstream: F-048 (concurrency fuzzing + AsyncLocal flow — deepens with deadlock detection and wait-graph snapshots).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 014-shuffle-replay-determinism

Read first:
- planning/shuffle-replay-determinism/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- Microsoft.Coyote .NET docs and samples
- src/Rig.TUnit.Concurrency/* (existing concurrency primitives)

Generate a feature spec that:
1. Introduces Concurrency.Fuzz / Replay + [Fuzz] TUnit attribute.
2. Wires Coyote as the scheduling backend; abstract behind the rig's surface so users don't import Coyote directly.
3. Phase 0 lands the contract + a known-flaky self-test that fails under N schedules.
4. Phase 6 documents the failing-seed-replay workflow.

Constraints:
- Coyote dependency lives only inside Rig.TUnit.Concurrency; architecture test guards.
- F-008 IFakeClock advanced by the scheduler, not real time.
- Failing seeds dumped to test output AND to a deterministic file under TestResults/.
- File-scoped namespaces, sealed concrete types.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
