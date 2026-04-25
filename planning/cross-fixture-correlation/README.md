# Planning — Cross-fixture correlation (F-012)

**Feature ID**: F-012
**Family**: Cross-cutting
**Status**: planned
**Depends on**: —
**Target release**: v0.10
**Estimated tasks**: ~17 (Phase 0: 12 · 5 docs)

---

## Why this feature exists

A real distributed-tracing test asserts: "a request hit the HTTP front door, propagated through Kafka, was processed by a saga, wrote to Postgres, and emitted a metric — all under one trace." The rig captures each piece in its own fixture today, but **nothing joins the captures**. There is no `traceId` correlation across the HTTP, Grpc, Messaging, EFCore, Tracing, and Microservices fixtures.

W3C `traceparent` already flows through every transport for free if the SDKs are wired correctly. The rig just needs an assertion API that joins captures by trace.

## What we deliver

A new base-library cross-fixture assertion surface that joins captures from every fixture by W3C `traceparent`:

```csharp
public static class CorrelationAssert
{
    public static CorrelationScope Trace(string traceId);
}

public sealed class CorrelationScope
{
    public CorrelationScope Spans(TracingFixture fixture);
    public CorrelationScope Logs(LoggingFixture fixture);
    public CorrelationScope Metrics(MetricsFixture fixture);
    public CorrelationScope HttpCalls(HttpFixture fixture);
    public CorrelationScope Messages(MessagingFixture fixture);
    public CorrelationScope DbCalls(DatabaseFixture fixture);

    public CorrelationScope ContainsExactly(int count);
    public CorrelationScope InOrder(params Func<CapturedEvent, bool>[] expectations);
    public CorrelationScope NoOrphans();
    public CorrelationScope ContinuousChain();
}
```

The fixtures themselves do not change shape — they already capture. F-012 only adds the **join**.

## Gaps closed (from CC-5 in the gap analysis)

- Cross-boundary trace assertions impossible today.
- Saga / outbox / inbox tests can't assert their messages share a parent trace.
- Distributed-tracing regressions ship silently because no rig test catches them.

## Providers in scope

This is **base-library only** — the existing capture fixtures are already there. Wiring touches:

| Package | What changes |
|---------|--------------|
| `src/Rig.TUnit.Observability.Tracing` | exposes `IReadOnlyList<Activity>` keyed by traceId |
| `src/Rig.TUnit.Observability.Logging` | exposes captured log scopes with traceId |
| `src/Rig.TUnit.Observability.Metrics` | exposes recorded measurements with exemplar traceId |
| `src/Rig.TUnit.Http` | every captured `HttpRequestMessage` records traceparent |
| `src/Rig.TUnit.Grpc` | every captured call records traceparent metadata |
| `src/Rig.TUnit.Messaging` (base) | `CapturedMessage<T>` adds optional traceparent header (already on the wire — only surface the field) |

## Exit criteria

- `CorrelationAssert.Trace(...)` ships in `Rig.TUnit` base library, 100 % line coverage.
- Each capture fixture exposes a `ByTrace(string traceId)` accessor.
- One end-to-end RED-leading scenario test that exercises HTTP → Kafka → Saga → Postgres and asserts `ContinuousChain()`.
- `docs/ordering-assertions.md` augmented with a "Cross-fixture correlation" section.

## Dependencies on other planned features

- Upstream: none.
- Downstream: F-013 (multi-tenant scope — joins by tenant header on top of trace), F-034 (OTel propagation depth uses CorrelationAssert as its outer surface), F-038 (outbox/inbox correctness asserts message + db row share trace), F-039 (saga timeout asserts compensations chain on the original trace).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 012-cross-fixture-correlation

Read first:
- planning/cross-fixture-correlation/README.md
- W3C Trace Context spec (traceparent / tracestate)
- src/Rig.TUnit.Observability.Tracing/* (existing Activity capture)
- src/Rig.TUnit.Messaging/Helpers/CapturedMessage*.cs (Feature 007 SessionKey extension)

Generate a feature spec that:
1. Introduces CorrelationAssert + CorrelationScope as a base-library surface only (no new fixtures).
2. Each capture fixture exposes ByTrace(string traceId) yielding its own captured rows for that trace.
3. Phase 0 lands the contract + a 6-fixture end-to-end RED scenario that fails until every capture surfaces traceparent.
4. Each capture fixture's GREEN commit flips part of the scenario.
5. Phase 6 documents the cross-fixture pattern in docs/ordering-assertions.md.

Constraints:
- Zero new dependencies (rely on System.Diagnostics.Activity).
- traceparent format strictly W3C; do not invent a new header.
- ContinuousChain() asserts no orphan spans / messages / log scopes.
- Pre-release library — change CapturedMessage<T> shape if needed (no [Obsolete]).

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
