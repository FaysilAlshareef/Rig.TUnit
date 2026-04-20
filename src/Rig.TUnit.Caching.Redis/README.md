# Rig.TUnit.Caching.Redis

> Redis-as-cache fixture (primary home for `RedisFixture`) with `RedisBackplaneCapture` for pub/sub cache-invalidation tests.

## What this package is

The Rig.TUnit Redis-as-cache provider. Primary home for `RedisFixture` —
the same container is re-used by `Rig.TUnit.Databases.NoSql.Redis` in
its KV role. Exposes `UseRedisCache(…)` (no bare `UseRedis` — ADR-007
forces the caller to declare intent cache vs KV). Ships
`RedisBackplaneCapture` which subscribes to a pub/sub channel and
records cache-invalidation events, so tests can assert
"publishing `orders:cache-invalidate` evicts the L1 on every node".

## When to use it

- Testing distributed-cache coherency with pub/sub-based invalidation.
- L2 backing for FusionCache / HybridCache distributed-tier tests.
- Asserting cache hit-rate targets against real Redis.
- **Not for**: KV-store scenarios — use `Rig.TUnit.Databases.NoSql.Redis`.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Redis image ~120 MB)
- `StackExchange.Redis` (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Redis.Fixtures;
using StackExchange.Redis;

await using var fx = new RedisFixture();
await fx.InitializeAsync();

var db = ConnectionMultiplexer.Connect(fx.ConnectionString).GetDatabase();
await db.StringSetAsync("cache:orders:42", "{}", TimeSpan.FromMinutes(5));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"redis:7-alpine"` | Image |
| `StartupTimeoutSeconds` | `int` | `30` | Redis boots fast |
| `Password` | `string?` | `null` | Off in dev mode |
| `BackplaneChannel` | `string` | `"cache-invalidate"` | Pub/sub channel |

## Fixture + helper APIs

- `Rig.TUnit.Caching.Redis.Fixtures.RedisFixture`
- `Rig.TUnit.Caching.Redis.Options.RedisCacheFixtureOptions`
- `Rig.TUnit.Caching.Redis.Builder.RedisCacheRigBuilder`
- `Rig.TUnit.Caching.Redis.Helpers.RedisBackplaneCapture`

## Per-test isolation

Per-test key prefix `cache:{IsolationKey:short}:*`. Teardown via
`SCAN + DEL` on the prefix. Pub/sub channels are per-fixture to avoid
cross-test subscriber leakage.

## Parallelism + performance

- First-run pull: ~5 s.
- Warm startup: ~1 s.
- Per-op Get/Set: ~150 µs (in-process latency dominated).
- Parallelism: 8+ concurrent tests — key-prefix isolation makes this
  trivially safe.

## Troubleshooting

- **`TimeoutException` on first op** — `ConnectionMultiplexer.Connect`
  is synchronous and blocks until the initial topology discovery
  completes; under heavy parallel startup this can exceed the default
  5-second timeout. Raise `SyncTimeout` in your config.
- **Backplane capture missed events** — `ISubscriber.Subscribe` is
  eventually-consistent; poll for the subscription to be active before
  publishing (fixture does this but a custom wire-up must too).

See [docs/troubleshooting.md#redis-cache](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Redis pub/sub is fire-and-forget — a subscriber started *after* a
  publish misses the message. Always subscribe before triggering the
  event the test expects.
- `KEYS *` is the classic production foot-gun; the fixture-internal
  teardown uses `SCAN` (O(1) per batch) instead.
- Cache-vs-KV split rationale: ADR-007. Bare `UseRedis` does not exist;
  this is deliberate.

## Benchmarks

See [`RedisCacheBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/RedisCacheBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-007 — Redis cache/KV split](../../docs/adr/ADR-007-redis-cache-kv-split.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Caching`](../Rig.TUnit.Caching/README.md)
- Sibling: [`Rig.TUnit.Databases.NoSql.Redis`](../Rig.TUnit.Databases.NoSql.Redis/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
