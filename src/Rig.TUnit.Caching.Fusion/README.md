# Rig.TUnit.Caching.Fusion

[FusionCache](https://github.com/ZiggyCreatures/FusionCache) provider — L1 (in-memory) with fail-safe fallback, eager refresh, and tag invalidation. No container required.

## Install

```
dotnet add package Rig.TUnit.Caching.Fusion
```

## Example

```csharp
await using var fx = new FusionCacheFixture();
await fx.InitializeAsync();

var key = $"k-{Guid.NewGuid():N}";
var value = await fx.Cache.GetOrSetAsync<string>(key, async (_, _) =>
{
    await Task.Yield();
    return "computed";
});
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseFusionCache(RigConnect.FromValue("fusion-in-memory"), cfg => { }));
```

### Helpers for decision logic

- `FailSafeHelper.IsFailSafeApplicable(entryOptions, elapsed)` — returns `true` when fail-safe fallback should apply (enabled + within `FailSafeMaxDuration`).
- `EagerRefreshHelper.ShouldEagerRefresh(entryOptions, elapsed)` — returns `true` when the elapsed time enters the eager window (`Duration * EagerRefreshThreshold ≤ elapsed < Duration`).

## Options

`FusionCacheFixtureOptions` — configured via `appsettings.json` under section `RigTUnit:FusionCache`:

- `DefaultDurationSeconds` (default 60)
- `IsFailSafeEnabled` (default true)
- `FailSafeMaxDurationSeconds` (default 3600)
- `EagerRefreshThreshold` (default 0.8 — background refresh at 80% of TTL)

## Dependencies

`Rig.TUnit.Caching`, `ZiggyCreatures.FusionCache`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Options`
