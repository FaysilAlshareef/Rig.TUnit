# Planning — Distributed lock + serializer poisoning (F-026)

**Feature ID**: F-026
**Family**: Caching
**Status**: planned
**Depends on**: F-008 (deterministic clock — lock leases)
**Target release**: v0.14
**Estimated tasks**: ~42 (Phase 0: 5 · 4 providers × 8 tasks · 5 docs)

---

## Why this feature exists

Two cache-adjacent correctness scenarios remain after F-025:

1. **Distributed mutex**: Redlock, FusionCache `WithSimpleLockingProvider`, HybridCache distributed locks. Real bugs: lease expiry under partition, lost mutex during failover, fencing-token drift. No rig surface today.
2. **Serializer poisoning**: a JSON-deserialization throw after a schema change turns the cache into a denial-of-service — every read fails until the key is evicted. Real-world bug; the rig must let users assert "poisoned key was skipped, origin called".

## What we deliver

```csharp
public interface IDistributedLock
{
    Task<IDisposable> AcquireAsync(string key, TimeSpan lease, CancellationToken ct);
    Task<long?> GetFencingTokenAsync(string key, CancellationToken ct);
}

public static class LockAssert
{
    public static DistributedLockAssertion Distributed(string key);
}

public sealed class DistributedLockAssertion
{
    public DistributedLockAssertion HeldByExactly(int holders).UnderConcurrency(int n);
    public DistributedLockAssertion ReleasedAfter(TimeSpan span);
    public DistributedLockAssertion FencingTokenMonotonic();
}

public sealed class SerializerPoisonInjector
{
    public IDisposable PoisonKey(string key, Exception toThrow);
    public IDisposable PoisonRandom(double rate);
}

public static class CachePoisonAssert
{
    public static PoisonAssertion PoisonedKey(string key);
}
```

## Gaps closed (from CACHE-4 + CACHE-6 in the gap analysis)

- Distributed lock correctness under concurrency / partition.
- Fencing-token monotonicity.
- Serializer-poisoning detection and skip behaviour.

## Providers in scope

4: Memory (no-op for distributed lock), Redis, Hybrid, Fusion.

## Exit criteria

- `IDistributedLock`, `LockAssert.Distributed`, `SerializerPoisonInjector`, `CachePoisonAssert` ship with 100 % line coverage.
- Each non-Memory provider has ≥ 3 RED scenarios (concurrency-50, lease expiry under fake clock, poisoned-key skip).
- `docs/providers/caching.md` updated.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-039 (saga uses distributed lock for compensator-once semantics).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 026-caching-locks-and-poisoning

Read first:
- planning/caching-locks-and-poisoning/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- planning/caching-stampede-and-tags/README.md (sibling — share fixture shape)
- Redlock-net, FusionCache locking, HybridCache locking docs

Generate a feature spec that:
1. Introduces IDistributedLock + LockAssert + SerializerPoisonInjector + CachePoisonAssert.
2. Each non-Memory provider phase ships ≥ 3 RED scenarios.
3. Fencing-token monotonicity asserted under concurrent acquire/release.

Constraints:
- Memory provider explicitly NoOp for distributed-lock — documented, not faked.
- F-008 IFakeClock advanced for lease expiry tests.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
