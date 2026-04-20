# Rig.TUnit.Caching.Fusion

> FusionCache provider — L1 in-memory + optional L2 distributed + backplane, with fail-safe, eager refresh, and tag invalidation.

## What this package is

The Rig.TUnit adapter for [FusionCache](https://github.com/ZiggyCreatures/FusionCache).
`FusionCacheFixture` configures an `IFusionCache` with production-shape
defaults (fail-safe enabled, eager-refresh at 80 % TTL, 60 s default
duration) and exposes `FailSafeHelper` + `EagerRefreshHelper` — pure
decision-logic predicates that tests use to assert the cache should
apply fail-safe or trigger an eager refresh, without racing the
scheduler.

No container required — FusionCache is in-process. For the L2 distributed
tier, pair with `Rig.TUnit.Caching.Redis`.

## When to use it

- Testing services that use FusionCache in production.
- Verifying fail-safe fallback triggers when the underlying loader throws.
- Asserting eager-refresh enters the right window near TTL expiry.
- **Not for**: plain `IMemoryCache` — use `Rig.TUnit.Caching.Memory`.

## Prerequisites

- .NET 10 SDK
- `ZiggyCreatures.FusionCache` 2.x (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Fusion.Fixtures;

await using var fx = new FusionCacheFixture();
await fx.InitializeAsync();

var key = $"k-{Guid.NewGuid():N}";
var value = await fx.Cache.GetOrSetAsync<string>(key, async (_, _) =>
{
    await Task.Yield();
    return "computed";
});
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultDurationSeconds` | `int` | `60` | TTL for `GetOrSet` entries |
| `IsFailSafeEnabled` | `bool` | `true` | Serve stale on loader failure |
| `FailSafeMaxDurationSeconds` | `int` | `3600` | Fail-safe window upper bound |
| `EagerRefreshThreshold` | `double` | `0.8` | Background refresh at 80 % of TTL |

Section name: `RigTUnit:FusionCache`.

## Fixture + helper APIs

- `Rig.TUnit.Caching.Fusion.Fixtures.FusionCacheFixture`
- `Rig.TUnit.Caching.Fusion.Options.FusionCacheFixtureOptions`
- `Rig.TUnit.Caching.Fusion.Builder.FusionCacheRigBuilder`
- `Rig.TUnit.Caching.Fusion.Helpers.FailSafeHelper`
- `Rig.TUnit.Caching.Fusion.Helpers.EagerRefreshHelper`

## Per-test isolation

Per-fixture `IFusionCache` instance — each test owns its own cache
graph. No state is shared across tests by default; pairing with the
Redis L2 backplane adds the Redis key-prefix isolation pattern.

## Parallelism + performance

- Zero container startup.
- `GetOrSetAsync` warm hit: ~500 ns.
- Stampede coalescing: ~50 µs for first-loader dogpile test.
- Safe under full parallelism.

## Troubleshooting

- **Fail-safe does not fire** — check `IsFailSafeEnabled = true` AND
  `FailSafeMaxDuration > 0` AND the loader threw (a `null` return is
  not a failure).
- **Eager refresh fires too often** — test's synthetic clock is running
  real wall time; wrap with `ClockControl` from `Rig.TUnit.Caching` to
  freeze.

See [docs/troubleshooting.md#fusion](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- FusionCache's adaptive caching can return a stale value even when the
  loader succeeded — by design (for fail-safe). Tests asserting "always
  fresh" must disable it.
- Eager refresh is fire-and-forget; the current caller still gets the
  cached value while a background refresh runs. Tests racing this
  boundary must poll, not assume.

## Benchmarks

See [`FusionCacheBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/FusionCacheBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-007 — Redis cache/KV split](../../docs/adr/ADR-007-redis-cache-kv-split.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Caching`](../Rig.TUnit.Caching/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
