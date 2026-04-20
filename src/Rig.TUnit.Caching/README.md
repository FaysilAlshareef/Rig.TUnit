# Rig.TUnit.Caching

> Caching family-base: `CacheRigBuilder<TSelf>` + `CacheAssert` (Stampede, TagInvalidation, Coherent, FailSafe, NegativeCached, HitRate, EagerRefresh) + `StampedeTester` + `BackplaneCapture` + `ClockControl`.

## What this package is

The shared contract for the Caching family. Defines `ICacheRig`,
`CacheFixtureBase`, and the novel `CacheAssert` fluent API with
assertions for the seven cache-quality dimensions that matter in
production: stampede prevention, tag-based invalidation coherence,
distributed-cluster coherency, fail-safe read-through, negative-caching,
hit-rate targets, and eager-refresh semantics. `ClockControl` wraps
`FakeTimeProvider` so TTL tests run instantly.

Concrete providers: `.Memory`, `.Redis`, `.Hybrid`, `.Fusion`.

## When to use it

- Authoring a new cache provider (MemoryCache with custom eviction, etc).
- Writing cache-semantic tests that run against every provider.
- **Not for**: concrete caching — install one of the four leaves.

## Prerequisites

- .NET 10 SDK
- `Microsoft.Extensions.TimeProvider.Testing` (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Helpers;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

var clock = new ClockControl();
await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultTtl` | `TimeSpan` | `5m` | Starting TTL for cache entries |
| `EnableStatistics` | `bool` | `true` | Track hit/miss counts |
| `NegativeCacheTtl` | `TimeSpan` | `30s` | TTL for null / not-found entries |
| `StampedePreventionWindow` | `TimeSpan` | `500ms` | Coalesce concurrent loaders |

## Fixture + helper APIs

- `Rig.TUnit.Caching.ICacheRig`
- `Rig.TUnit.Caching.Fixtures.CacheFixtureBase`
- `Rig.TUnit.Caching.Builder.CacheRigBuilder<TSelf>`
- `Rig.TUnit.Caching.Assertions.CacheAssert`
- `Rig.TUnit.Caching.Helpers.StampedeTester`
- `Rig.TUnit.Caching.Helpers.BackplaneCapture`
- `Rig.TUnit.Caching.Helpers.ClockControl`

## Per-test isolation

Each provider isolates differently — memory caches per-fixture, Redis
via key prefix, hybrid via L1 reset + L2 prefix. The base contract's
`CacheAssert` assumes isolation is guaranteed by the provider.

## Parallelism + performance

## §9 — N/A: family-base; per-provider. Memory is fastest; Fusion is
slowest (backplane + L1 + L2 coordination).

## Troubleshooting

- **`StampedeTester` reports > 1 loader call** — the provider's stampede
  prevention window is narrower than the test's concurrent-call window.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Stampede prevention semantics differ — some providers coalesce per-key,
  others per-process. `CacheAssert.Stampede(…)` tests per-key coalescing.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*CacheBenchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-007 — Redis cache/KV split](../../docs/adr/ADR-007-redis-cache-kv-split.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
