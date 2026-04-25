# Planning — Histogram / sampling assertions (F-036)

**Feature ID**: F-036
**Family**: Observability
**Status**: planned
**Depends on**: F-034 (cross-boundary propagation — sampling decisions flow with traces)
**Target release**: v0.14
**Estimated tasks**: ~32 (Phase 0: 7 · 2 packages × 10 tasks · 5 docs)

---

## Why this feature exists

The rig has counter assertions but no histogram, distribution, or sampling-decision assertions. Real-world tests this enables:

- "p99 of `http.duration` MUST be < 150 ms under 100 RPS for 30 s."
- "No span sampled more than once."
- "Error spans are always sampled, regardless of head-based sampler decision."
- "Tail-based sampler routes any trace with status=ERROR to the always-on exporter."

## What we deliver

```csharp
public static class HistogramAssert
{
    public static HistogramAssertion Meter(string meterName);
}

public sealed class HistogramAssertion
{
    public HistogramAssertion Percentile(int p).LessThan(TimeSpan span);
    public HistogramAssertion Percentile(int p).GreaterThan(TimeSpan span);
    public HistogramAssertion BucketCount(int min);
}

public static class SamplerAssert
{
    public static SamplingAssertion Decision(string traceId);
}

public sealed class SamplingAssertion
{
    public SamplingAssertion Sampled(bool expected);
    public SamplingAssertion Because(SamplingReason reason);
}

public enum SamplingReason { ErrorParent, RootDecision, ParentSampled, AlwaysOn, AlwaysOff }
```

## Gaps closed (from OBS-4 + OBS-5 in the gap analysis)

- Histogram percentile assertions.
- Sampling-decision capture and assertion.
- Tail-based sampler verification.

## Providers in scope

2: `src/Rig.TUnit.Observability.Metrics`, `src/Rig.TUnit.Observability.Tracing`.

## Exit criteria

- `HistogramAssert.Meter`, `SamplerAssert.Decision` ship with 100 % line coverage.
- ≥ 5 RED scenarios (p99 latency bound, percentile lower-bound, sampler decision capture, error-span always sampled, sampler-not-sampled).
- `docs/providers/observability.md` updated.

## Dependencies on other planned features

- Upstream: F-034.
- Downstream: F-047 (resilience tests assert on backoff distribution shape via HistogramAssert).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 036-histogram-and-sampling-assertions

Read first:
- planning/histogram-and-sampling-assertions/README.md
- planning/otel-cross-boundary-propagation/README.md (F-034 must be shipped)
- OpenTelemetry .NET histogram + sampler docs

Generate a feature spec that:
1. Introduces HistogramAssert.Meter + percentile operators.
2. SamplerAssert.Decision + reason capture.
3. ≥ 5 RED scenarios.

Constraints:
- Histogram assertions read from a captured snapshot, no live exporter dependency.
- Sampling reason attribution requires a custom Sampler that records its decision; documented.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
