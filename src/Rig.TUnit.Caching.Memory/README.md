# Rig.TUnit.Caching.Memory

`IMemoryCache`-backed `CacheFixtureBase`. Coherency / backplane assertions are
N/A (single-node); other `CacheRigContract` tests apply unchanged.

## Install

```xml
<PackageReference Include="Rig.TUnit.Caching.Memory" />
```

## Example

```csharp
await using var fx = new MemoryCacheFixture();
await fx.InitializeAsync();
fx.Cache.Set("k", "v", TimeSpan.FromSeconds(30));
```

## Dependencies
- `Rig.TUnit.Caching`
- `Microsoft.Extensions.Caching.Memory`

Spec: `003-rig-tunit-ecosystem-expansion` — US10.
