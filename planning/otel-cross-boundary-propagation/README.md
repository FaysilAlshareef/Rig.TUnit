# Planning — OTel cross-boundary propagation (F-034)

**Feature ID**: F-034
**Family**: Observability
**Status**: planned
**Depends on**: F-012 (cross-fixture correlation)
**Target release**: v0.12
**Estimated tasks**: ~52 (Phase 0: 5 · 7 boundaries × 6 tasks · 5 docs)

---

## Why this feature exists

F-012 introduces `CorrelationAssert.Trace(...)` as the cross-fixture join. F-034 is the deepening that makes the join work in real distributed scenarios:

- HTTP `traceparent` header in / out.
- gRPC metadata `traceparent`.
- Kafka header `traceparent` (binary header).
- ServiceBus message property.
- RabbitMQ headers.
- SQS message attribute.
- NATS JetStream header.
- EFCore activity around `DbCommand`.
- StackExchange.Redis activity.

Without each transport doing the right thing on inject + extract, `CorrelationAssert.ContinuousChain()` returns false negatives.

## What we deliver

- Each fixture's outbound transport emits W3C `traceparent` (verified via wire-level capture).
- Each fixture's inbound transport extracts it into the local `Activity.Current`.
- A scoped `WithPropagation(PropagationMode mode)` to test no-propagation / parent-only / sample-decision-only.
- Explicit assertion API:

```csharp
public static class PropagationAssert
{
    public static PropagationScope Trace(string traceId);
}

public sealed class PropagationScope
{
    public PropagationScope CrossedHttpBoundary();
    public PropagationScope CrossedKafkaBoundary();
    public PropagationScope CrossedServiceBusBoundary();
    public PropagationScope CrossedDbBoundary();
    public PropagationScope NoOrphanSpans();
    public PropagationScope SampledThroughout(SamplingDecision expected);
}
```

## Gaps closed (from OBS-1 in the gap analysis)

- W3C `traceparent` propagation across all transports.
- Sampling decisions consistent across boundaries.
- No-orphan-spans assertion.

## Providers in scope

7: HTTP, gRPC, Messaging × 5 (already partially handled by Feature 007), EFCore (StackExchange.Redis).

## Exit criteria

- Each transport's send/receive path verified via wire-level capture (e.g. ToxiProxy + tcpdump-equivalent recorder).
- `PropagationAssert.Trace(...)` ships with 100 % line coverage.
- ≥ 7 RED scenarios — one per boundary.
- ADR-012 (planned) finalised: "W3C traceparent as the rig's correlation key".

## Dependencies on other planned features

- Upstream: F-012.
- Downstream: F-036 (sampling decisions), F-037 (Seq artefacts), F-038 (outbox correctness asserts trace continues into relay).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 034-otel-cross-boundary-propagation

Read first:
- planning/otel-cross-boundary-propagation/README.md
- planning/cross-fixture-correlation/README.md (F-012 must be shipped)
- W3C Trace Context spec
- OpenTelemetry .NET propagators docs

Generate a feature spec that:
1. Introduces PropagationAssert.Trace(...) plus boundary-specific operators.
2. Each transport phase wires inject/extract via OTel propagators.
3. ≥ 7 RED scenarios, one per boundary.
4. ADR-012 finalised in Phase 6.

Constraints:
- Use System.Diagnostics.DistributedContextPropagator where possible.
- No new dependencies beyond OpenTelemetry.* already on the rig.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, ADR-012 finalisation.
```
