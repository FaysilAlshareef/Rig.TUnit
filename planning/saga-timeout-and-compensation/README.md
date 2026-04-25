# Planning — Saga timeout & compensation (F-039)

**Feature ID**: F-039
**Family**: Microservices
**Status**: planned
**Depends on**: F-008 (clock for timeouts), F-038 (outbox dispatch for compensators)
**Target release**: v0.14
**Estimated tasks**: ~26 (Phase 0: 7 · 1 package × 14 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Microservices.Saga` provides fixtures but no fluent state-machine builder or saga-instance assertions. Production saga bugs the rig must catch:

- A step times out — does compensation fan out correctly?
- One compensator throws — what happens to the rest?
- Saga restarted from event store mid-flight — same outcome as the original run?
- Schema upgrade mid-saga — older instances complete on the new code path.
- Concurrent commands hit the same saga instance — locking / version semantics.

## What we deliver

```csharp
public interface ISagaStateBuilder
{
    ISagaStateBuilder Saga(string name);
    ISagaStateBuilder WithTimeout(TimeSpan timeout);
    ISagaStateBuilder WithCompensationOrder(CompensationOrder order);
    ISagaStateBuilder WithRetryPolicy(int maxRetries, TimeSpan backoff);
}

public enum CompensationOrder { ReverseOrderOfExecution, ParallelFanOut }

public abstract partial class SagaFixture
{
    public SagaFixture WithStateStore(Action<ISagaStateBuilder> configure);
    public Task<SagaInstance> StartAsync(string sagaId, object payload, CancellationToken ct);
    public Task ReplayAsync(string sagaId, CancellationToken ct);
}

public static class SagaAssert
{
    public static SagaInstanceAssertion Instance(SagaFixture fixture, string sagaId);
}

public sealed class SagaInstanceAssertion
{
    public SagaInstanceAssertion IsInState(string state);
    public SagaInstanceAssertion TransitionedVia(params string[] states);
    public SagaInstanceAssertion TimedOut().CompensatedSteps(params string[] steps);
    public SagaInstanceAssertion Replayable();
    public SagaInstanceAssertion CompensatorFailed(string step).RolledBackTo(string state);
}
```

## Gaps closed (from MS-3 in the gap analysis)

- Saga timeout / compensation order.
- Partial-compensation-failure semantics.
- Replay-from-event-store determinism.
- Concurrent-command lock semantics.

## Providers in scope

1: `src/Rig.TUnit.Microservices.Saga`.

## Exit criteria

- `ISagaStateBuilder`, `SagaAssert.Instance` ship with 100 % line coverage.
- ≥ 5 RED-leading scenarios (timeout → compensate, partial compensation, replay-from-event-store, concurrent command lock, compensator failure).
- F-008 fake-clock used for timeout assertions.
- `docs/providers/saga.md` (new) covers compensation patterns.

## Dependencies on other planned features

- Upstream: F-008, F-038.
- Downstream: F-040 (event sourcing pairs with saga for replay).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 039-saga-timeout-and-compensation

Read first:
- planning/saga-timeout-and-compensation/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- planning/outbox-inbox-correctness/README.md (F-038 must be shipped)
- src/Rig.TUnit.Microservices.Saga/* (current state)

Generate a feature spec that:
1. Introduces ISagaStateBuilder + WithStateStore on SagaFixture.
2. SagaAssert.Instance with TimedOut / Replayable / TransitionedVia operators.
3. ≥ 5 RED-leading scenarios.

Constraints:
- F-008 IFakeClock for timeout advances.
- Replayability tested via deterministic event-store snapshot (depends on F-011 long-term).
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
