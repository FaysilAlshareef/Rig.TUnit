# Planning — NoSQL throughput / RU / cost assertions (F-023)

**Feature ID**: F-023
**Family**: NoSQL
**Status**: planned
**Depends on**: F-020 (collection/index topology)
**Target release**: v0.14
**Estimated tasks**: ~34 (Phase 0: 5 · 3 providers × 8 tasks · 5 docs)

---

## Why this feature exists

The most expensive production-NoSQL incidents are silent cost regressions: a poorly-indexed Cosmos query that suddenly costs 50 RUs instead of 5, a Dynamo scan replacing a query, a Mongo Atlas IOPS spike. None of them are catchable in CI today.

Real-world tests this enables:
- "This query MUST stay under 5 RUs."
- "After this index was added, that query's RU dropped."
- "Hot partition: sustained writes to one PK trigger a `429`."
- "Dynamo capacity throttling: bursts above provisioned capacity hit `ProvisionedThroughputExceededException`."

## What we deliver

A cost-assertion API and hot-partition simulator:

```csharp
public static class CostAssert
{
    public static CostAssertion LastRequest();
    public static CostAssertion Query(string queryName);
}

public sealed class CostAssertion
{
    public CostAssertion Cost(double maxRu);
    public CostAssertion Region(string expected);
    public CostAssertion ConsumedFromIndex(string indexName);
    public CostAssertion ScanCount(int max);
}

public sealed class HotPartitionSimulator
{
    public Task<HotPartitionResult> SaturateAsync(string pk, int writesPerSecond, TimeSpan duration);
}

public sealed record HotPartitionResult(int Throttled, double Throughput, IReadOnlyList<Exception> Failures);
```

## Gaps closed (from NOSQL-4 in the gap analysis)

- RU / capacity / IOPS-per-query assertions.
- Hot-partition / throttling simulation.
- Index-usage assertions ("query used IDX_ORDERS_USERID, not full scan").

## Providers in scope

3: Cosmos (RU charge from `x-ms-request-charge`), Dynamo (capacity-units from response), Mongo Atlas (IOPS via free-tier monitoring proxy).

## Exit criteria

- `CostAssert.LastRequest()` and `Query(name)` ship with 100 % line coverage.
- `HotPartitionSimulator` ships with deterministic concurrency control.
- Each in-scope provider has ≥ 3 RED scenarios (RU bound, hot-partition throttling, index-usage assertion).
- `docs/providers/*.md` updated with cost-assertion section per provider.

## Dependencies on other planned features

- Upstream: F-020.
- Downstream: F-024 (provider quirks deepen with engine-specific cost knobs).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 023-nosql-throughput-and-cost

Read first:
- planning/nosql-throughput-and-cost/README.md
- planning/nosql-collection-and-index-topology/README.md (F-020 must be shipped)
- Cosmos x-ms-request-charge docs, DynamoDB ConsumedCapacity docs, Mongo Atlas profiler

Generate a feature spec that:
1. Introduces CostAssert.LastRequest / Query(name) and HotPartitionSimulator.
2. Each in-scope provider phase delivers ≥ 3 RED scenarios.
3. Phase 6 documents per-provider cost units and the assertion idioms.

Constraints:
- HotPartitionSimulator deterministic — bounded concurrency, fixed seed.
- Cost numbers normalised to a typed RuCharge / CapacityUnits record per engine.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
