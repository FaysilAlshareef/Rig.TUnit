# Rig.TUnit.Caching.Memory

> `IMemoryCache`-backed `CacheFixtureBase` — single-node in-memory cache. Zero-container speed-of-light tier.

## What this package is

The simplest cache provider: a standard `Microsoft.Extensions.Caching.
Memory.IMemoryCache` wrapped in a Rig.TUnit fixture. No container, no
distributed coordination, no backplane — just a dictionary with TTL +
size-limit eviction and the `CacheRigContract` subset that applies to
single-node caches.

## When to use it

- Tests that need a trivially-fast cache for sanity-check assertions.
- In-process caching scenarios where L2 distribution is deliberately absent.
- Running the `CacheRigContract` in its fastest mode.
- **Not for**: distributed coherency testing (nothing to coordinate); use
  `Rig.TUnit.Caching.Fusion` with an L2 Redis backplane.

## Prerequisites

- .NET 10 SDK
- `Microsoft.Extensions.Caching.Memory` (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Memory.Fixtures;

await using var fx = new MemoryCacheFixture();
await fx.InitializeAsync();

fx.Cache.Set("k", "v", TimeSpan.FromSeconds(30));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `SizeLimit` | `long?` | `null` | `IMemoryCache.SizeLimit` |
| `CompactionPercentage` | `double` | `0.25` | % evicted on size-pressure |
| `DefaultTtl` | `TimeSpan` | `5m` | Applied when entry has no explicit TTL |

## Fixture + helper APIs

- `Rig.TUnit.Caching.Memory.Fixtures.MemoryCacheFixture`
- `Rig.TUnit.Caching.Memory.Options.MemoryCacheFixtureOptions`
- `Rig.TUnit.Caching.Memory.Builder.MemoryCacheRigBuilder`

## Per-test isolation

Per-fixture `IMemoryCache` instance. Each test owns its own cache;
nothing is shared. Safe under full parallelism without any extra work.

## Parallelism + performance

- Zero container startup.
- `Set`/`Get`: ~200 ns.
- Safe under full parallelism.

## Troubleshooting

- **Coherency assertions N/A** — `CacheAssert.Coherent(…)` is a no-op
  against a single-node cache. The contract suite's skipped entries are
  documented; do not treat the skip as a regression.
- **Size-limit evictions not seen** — `SizeLimit` must be combined with
  per-entry `Size`; entries without a Size are never size-evicted.

See [docs/troubleshooting.md#memory-cache](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `IMemoryCache.Remove(object)` takes `object`, not `string` — typed
  wrappers are your friend.
- `AbsoluteExpirationRelativeToNow` on `Set` measures from *enqueue*
  time, not eviction-check time. A 5 s expiry set at T=0 evicts at T=5.

## Benchmarks

See [`MemoryCacheBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MemoryCacheBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. Speed-of-light reference.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Caching`](../Rig.TUnit.Caching/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
