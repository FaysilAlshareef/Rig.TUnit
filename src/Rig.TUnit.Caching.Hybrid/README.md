# Rig.TUnit.Caching.Hybrid

> Microsoft `HybridCache` provider — L1 in-memory with stampede coalescing and tag invalidation. No container required.

## What this package is

The Rig.TUnit adapter for Microsoft's
[`HybridCache`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.hybrid.hybridcache).
`HybridCacheFixture` configures an `HybridCache` instance with sensible
test defaults (60 s default TTL, 1 MiB max payload, 1024-char max key,
local-cache TTL 30 s) and exposes the raw `HybridCache` plus a stampede
coalescing test surface. L2 distributed is off by default — pair with
`Rig.TUnit.Caching.Redis` if you want it.

## When to use it

- Testing services using Microsoft's HybridCache.
- Verifying stampede coalescing for `GetOrCreateAsync`.
- Asserting tag-invalidation via `RemoveByTagAsync`.
- **Not for**: FusionCache (different semantics, use `.Fusion`) or plain
  `IMemoryCache` (use `.Memory`).

## Prerequisites

- .NET 10 SDK
- `Microsoft.Extensions.Caching.Hybrid` 10.x (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Hybrid.Fixtures;

await using var fx = new HybridCacheFixture();
await fx.InitializeAsync();

var key = $"k-{Guid.NewGuid():N}";
var value = await fx.Cache.GetOrCreateAsync(key, async _ =>
{
    await Task.Yield();
    return "computed-value";
});
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultExpirationSeconds` | `int` | `60` | Distributed-tier TTL |
| `LocalCacheExpirationSeconds` | `int` | `30` | L1 (in-memory) TTL |
| `MaximumPayloadBytes` | `int` | `1_048_576` | 1 MiB guard |
| `MaximumKeyLength` | `int` | `1024` | Key length guard |

Section name: `RigTUnit:HybridCache`.

## Fixture + helper APIs

- `Rig.TUnit.Caching.Hybrid.Fixtures.HybridCacheFixture`
- `Rig.TUnit.Caching.Hybrid.Options.HybridCacheFixtureOptions`
- `Rig.TUnit.Caching.Hybrid.Builder.HybridCacheRigBuilder`

## Per-test isolation

Per-fixture `HybridCache` instance. Each test owns its own cache; no
shared state unless you explicitly wire an L2 distributed tier.

## Parallelism + performance

- Zero container startup.
- `GetOrCreateAsync` warm hit: ~700 ns.
- Stampede coalescing guarantees a single loader call per key within the
  coalescing window (~500 ms default).
- Safe under full parallelism.

## Troubleshooting

- **`GetOrCreateAsync` returns stale value after invalidation** — check
  `LocalCacheExpirationSeconds` > 0 combined with the L2 clear;
  `HybridCache` does not broadcast invalidation events to the L1 unless
  an L2 is wired.
- **Value not invalidated by tag** — confirm you tagged on insert:
  `.WithTags(["orders"])` inside `GetOrCreateAsync`'s options.

See [docs/troubleshooting.md#hybrid](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `HybridCache` is a singleton in the DI container; pin it as
  `AddSingleton` if you are composing your own service graph.
- Tag invalidation is per-instance for L1 only; L2 tag invalidation
  requires a compatible backplane (not shipped here).
- Serializer is `System.Text.Json` by default; register custom if your
  values contain polymorphic types.

## Benchmarks

See [`HybridCacheBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/HybridCacheBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Caching`](../Rig.TUnit.Caching/README.md)
- Sibling: [`Rig.TUnit.Caching.Fusion`](../Rig.TUnit.Caching.Fusion/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
