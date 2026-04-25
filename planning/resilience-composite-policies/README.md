# Planning — Resilience composite policies + state machine (F-047)

**Feature ID**: F-047
**Family**: Resilience
**Status**: planned
**Depends on**: F-008 (clock for backoff), F-009 (chaos to drive state transitions)
**Target release**: v0.14
**Estimated tasks**: ~28 (Phase 0: 7 · 1 package × 16 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Resilience` ships per-policy assertions (retry, circuit-breaker, rate-limit, bulkhead) but no first-class **composite-pipeline** assertions. Real-world correctness questions:

- Did the pipeline traverse `[Retry → CircuitBreaker → Bulkhead → Fallback]` in the expected order?
- Did the circuit-breaker enter **half-open** correctly, and did exactly **one** probe go through?
- Is the retry budget (Polly v8 `RetryBudget`) exhausted at the expected request count?
- Does the jitter backoff distribution match `Uniform(0, max)` shape?

## What we deliver

```csharp
public abstract partial class ResilienceRigBuilder
{
    public ResilienceRigBuilder WithResilience(Action<IResiliencePipelineBuilder> configure);
}

public static class ResilienceAssert
{
    public static PipelineAssertion Pipeline(string pipelineName);
    public static RetryAssertion Retry(string pipelineName);
    public static CircuitBreakerAssertion CircuitBreaker(string pipelineName);
    public static BulkheadAssertion Bulkhead(string pipelineName);
    public static RateLimiterAssertion RateLimiter(string pipelineName);
}

public sealed class PipelineAssertion
{
    public PipelineAssertion Traversed(params PolicyKind[] order);
    public PipelineAssertion Outcome(OutcomeKind outcome);
}

public sealed class CircuitBreakerAssertion
{
    public CircuitBreakerAssertion TransitionedTo(CircuitState state).After(int attempts);
    public CircuitBreakerAssertion HalfOpenProbes(int exactly);
}

public sealed class RetryAssertion
{
    public RetryAssertion Attempts(int count);
    public RetryAssertion Backoffs().DistributionMatches(BackoffShape shape);
    public RetryAssertion BudgetExhausted().AtRequest(int n);
}
```

## Gaps closed (from RESIL-1 in the gap analysis)

- Composite-pipeline state-machine traversal.
- Circuit-breaker half-open exact-one probe under concurrency.
- Retry-budget exhaustion (Polly v8).
- Jitter backoff distribution shape.

## Providers in scope

1: `src/Rig.TUnit.Resilience`.

## Exit criteria

- `WithResilience`, `ResilienceAssert.Pipeline / Retry / CircuitBreaker / Bulkhead / RateLimiter` ship with 100 % line coverage.
- ≥ 6 RED scenarios.
- F-008 fake-clock for backoff timing; F-009 fault injection drives state transitions.
- `docs/providers/resilience.md` updated.

## Dependencies on other planned features

- Upstream: F-008, F-009.
- Downstream: none.

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 047-resilience-composite-policies

Read first:
- planning/resilience-composite-policies/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- planning/fault-and-chaos-injection/README.md (F-009 must be shipped)
- src/Rig.TUnit.Resilience/* (current state)
- Polly v8 ResiliencePipelineBuilder + RetryBudget docs

Generate a feature spec that:
1. Introduces WithResilience + ResilienceAssert.* surface.
2. ≥ 6 RED scenarios covering composite traversal, half-open probe, retry budget, distribution shape.

Constraints:
- F-008 IFakeClock for backoff.
- F-009 IFaultBuilder drives state transitions.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
