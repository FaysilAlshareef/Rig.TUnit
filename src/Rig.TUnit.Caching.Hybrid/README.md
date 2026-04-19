# Rig.TUnit.Caching.Hybrid

In-process [`HybridCache`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.hybrid.hybridcache) provider — L1 (in-memory) only, with stampede coalescing and tag invalidation via `RemoveByTagAsync`. No container required.

## Install

```
dotnet add package Rig.TUnit.Caching.Hybrid
```

## Example

```csharp
await using var fx = new HybridCacheFixture();
await fx.InitializeAsync();

var key = $"k-{Guid.NewGuid():N}";
var value = await fx.Cache.GetOrCreateAsync(key, async _ =>
{
    await Task.Yield();
    return "computed-value";
});
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseHybridCache(RigConnect.FromValue("hybrid-in-memory"), cfg => { }));
```

## Options

`HybridCacheFixtureOptions` — configured via `appsettings.json` under section `RigTUnit:HybridCache`:

- `DefaultExpirationSeconds` (default 60)
- `LocalCacheExpirationSeconds` (default 30)
- `MaximumPayloadBytes` (default 1 MiB)
- `MaximumKeyLength` (default 1024)

## Dependencies

`Rig.TUnit.Caching`, `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Options`
