# Rig.TUnit.Databases.NoSql.Redis

Redis in its key-value store role. Shares the `RedisFixture` with `Rig.TUnit.Caching.Redis`, but exposes `RedisKvRigBuilder` + `UseRedisKv(source, kv => ...)` so the consumer can disambiguate "cache" vs "KV store". Ships `KeyScanHelper` for safe `SCAN`-based enumeration.

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.Redis
```

## Example

```csharp
var rig = new RigBuilder(services)
    .UseRedisKv(RigConnect.FromContainer(redisFixture), kv => { /* ... */ })
    .Build();
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Rig.TUnit.Caching.Redis`
