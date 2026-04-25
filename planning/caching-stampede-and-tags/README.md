# Planning — Cache stampede + tag invalidation + tier coherence (F-025)

**Feature ID**: F-025
**Family**: Caching
**Status**: planned
**Depends on**: F-008 (deterministic clock — TTL / staleness windows)
**Target release**: v0.13
**Estimated tasks**: ~52 (Phase 0: 7 · 4 cache providers × 10 tasks · 5 docs)

---

## Why this feature exists

Cache correctness has three production bugs that the rig cannot reproduce today:

1. **Stampede**: 10 k requests miss simultaneously — the origin gets DDoS'd by its own cache miss. FusionCache's `FactoryTimeout` + single-flight protects against this; the rig has no surface to assert single-flight worked.
2. **Tier coherence (Hybrid / Fusion)**: L1 (in-process) drifts from L2 (Redis) when the backplane drops a message. Real bugs ship because no integration test catches the drift.
3. **Tag-based invalidation** (HybridCache .NET 9, FusionCache tagging): "invalidate every key with tag `user:42`" — there is no rig assertion API for "after Tag.Invalidate(t), all tagged keys were re-resolved".

Plus eviction-policy, fail-safe (return stale on origin failure), and serializer-poisoning regressions are all real-world.

## What we deliver

- `WithCachePolicy(Action<ICachePolicyConfig>)` builder method (key prefix, TTL defaults, eviction, tier-config).
- `CacheAssert` family: `Stampede`, `Tier`, `Tag`, `Returned`, `Eviction`.
- A backplane-message capture for Hybrid / Fusion tiers.

```csharp
public interface ICachePolicyConfig
{
    ICachePolicyConfig WithKeyPrefix(string prefix);
    ICachePolicyConfig WithDefaultTtl(TimeSpan span);
    ICachePolicyConfig WithEviction(EvictionPolicy policy);
    ICachePolicyConfig WithFailSafe(TimeSpan maxStaleness);
    ICachePolicyConfig WithSingleFlight(bool enabled);
}

public static class CacheAssert
{
    public static StampedeAssertion Stampede();
    public static TierAssertion Tier(CacheTier tier);
    public static TagAssertion Tag(string tag);
    public static EvictionAssertion Eviction(string key);
    public static StalenessAssertion Returned(object value);
}

public sealed class StampedeAssertion
{
    public StampedeAssertion PreventedBySingleFlight();
    public StampedeAssertion OriginCalls(int expected);
}

public sealed class TierAssertion
{
    public TierAssertion Diverged(bool expected).From(CacheTier other).Within(TimeSpan span);
}
```

## Gaps closed (from CACHE-1, CACHE-2, CACHE-3, CACHE-5 in the gap analysis)

- Stampede assertions.
- Eviction / staleness window assertions.
- Two-tier coherence (Hybrid / Fusion).
- Tag-based invalidation assertions.

## Providers in scope

4: Memory, Redis (cache role), Hybrid, Fusion.

## Exit criteria

- `WithCachePolicy` and `CacheAssert.*` ship with 100 % line coverage.
- Each cache provider has ≥ 4 RED scenarios (stampede prevented, TTL eviction, fail-safe staleness, tag invalidation).
- `docs/providers/caching.md` (new or extended) covers each provider's policy support matrix.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-026 (distributed locks deepen cache correctness).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 025-caching-stampede-and-tags

Read first:
- planning/caching-stampede-and-tags/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- FusionCache + HybridCache + StackExchange.Redis docs

Generate a feature spec that:
1. Introduces WithCachePolicy + CacheAssert (Stampede / Tier / Tag / Eviction / Returned).
2. Each cache provider phase ships ≥ 4 RED scenarios.
3. Hybrid / Fusion phases include backplane-capture for tier-coherence assertions.

Constraints:
- F-008 IFakeClock advanced for all TTL / staleness assertions.
- Single-flight assertion uses semaphore-counted origin calls, no real Task.Delay.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
