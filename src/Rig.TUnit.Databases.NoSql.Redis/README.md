# Rig.TUnit.Databases.NoSql.Redis

> Redis-as-KV-store fixture, sibling to `Rig.TUnit.Caching.Redis`. Same container, different semantic contract.

## What this package is

Redis wears two hats in real systems: a best-effort cache (with eviction,
TTLs, dogpile prevention) and a durable key-value store (primary of
record for session tokens, counters, distributed locks). The Rig.TUnit
split keeps those roles in separate packages so consumers declare
intent — `UseRedisCache(…)` vs `UseRedisKv(…)`. Shares the underlying
`RedisFixture` with `Rig.TUnit.Caching.Redis` to avoid a duplicate
container per test process.

Ships `KeyScanHelper` — a `SCAN`-based enumeration wrapper with sensible
batch size + match-pattern support, because `KEYS *` is forbidden in
anything touching production-shape data.

## When to use it

- Integration tests where Redis is the *primary* store (session state,
  idempotency keys, leaderboards).
- Tests that need to enumerate keys safely during assertions.
- **Not for**: caching scenarios — use `Rig.TUnit.Caching.Redis`.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Redis image ~120 MB)
- `StackExchange.Redis` (transitive)

## Quick start

```csharp
using Rig.TUnit.Caching.Redis.Fixtures;
using Rig.TUnit.Databases.NoSql.Redis.Helpers;
using StackExchange.Redis;

await using var fx = new RedisFixture();
await fx.InitializeAsync();

var db = ConnectionMultiplexer.Connect(fx.ConnectionString).GetDatabase();
await db.StringSetAsync("session:abc", "user-42");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"redis:7-alpine"` | Image |
| `StartupTimeoutSeconds` | `int` | `30` | Redis boots in ~1 s |
| `Password` | `string?` | `null` | Off in dev mode |
| `AppendOnly` | `bool` | `false` | AOF persistence; disabled for test speed |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.Redis.Builder.RedisKvRigBuilder`
- `Rig.TUnit.Databases.NoSql.Redis.Helpers.KeyScanHelper`
- Shared: `Rig.TUnit.Caching.Redis.Fixtures.RedisFixture`

## Per-test isolation

Per-test key prefix: `session:{IsolationKey:short}:*`. `KeyScanHelper`
teardown issues `SCAN + DEL` against the prefix. Cheaper than running a
container per test.

## Parallelism + performance

- First-run pull: ~5 s.
- Warm startup: ~1 s.
- Per-test prefix scrub: ~5–10 ms per 100 keys.
- Parallelism: 8+ concurrent tests; key-prefix isolation is effectively
  free.

## Troubleshooting

- **Cross-test data bleed** — a test forgot the `{IsolationKey}` prefix.
  The `KeyScanHelper` only scrubs inside the prefix; keys outside linger.
- **`ERR SCAN iteration terminated by timeout`** — increase `count`
  batch size (default 100) or reduce the number of keys per test.

See [docs/troubleshooting.md#redis](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Redis is single-threaded per shard; `MULTI`/`EXEC` is atomic but a
  slow script blocks everyone else. Avoid Lua scripts in tests unless
  testing them.
- Keyspace notifications (`CONFIG SET notify-keyspace-events "KEA"`) are
  off by default; enable explicitly if testing pub/sub on key expiry.

## Benchmarks

See [`RedisKvBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/RedisKvBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-007 — Redis cache/KV split](../../docs/adr/ADR-007-redis-cache-kv-split.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)
- Sibling: [`Rig.TUnit.Caching.Redis`](../Rig.TUnit.Caching.Redis/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
