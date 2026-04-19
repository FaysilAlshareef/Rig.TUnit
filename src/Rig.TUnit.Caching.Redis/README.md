# Rig.TUnit.Caching.Redis

Redis in its cache role (primary home for `RedisFixture`). Exposes `RedisCacheRigBuilder` + `UseRedisCache(source, cache => ...)`. A bare `UseRedis` method is intentionally NOT exposed — use `UseRedisKv` (in `Rig.TUnit.Databases.NoSql.Redis`) for the KV role. Ships `RedisBackplaneCapture` for pub/sub-based cache-invalidation tests.

## Install

```
dotnet add package Rig.TUnit.Caching.Redis
```

## Example

```csharp
var rig = new RigBuilder(services)
    .UseRedisCache(RigConnect.FromContainer(redisFixture), cache => { /* ... */ })
    .Build();
```

## Dependencies

`Rig.TUnit.Caching`, `Testcontainers.Redis`, `StackExchange.Redis`
