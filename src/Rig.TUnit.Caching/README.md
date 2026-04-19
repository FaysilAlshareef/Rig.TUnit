# Rig.TUnit.Caching

Caching base layer. Ships `ICacheRig`, `CacheFixtureBase`, `CacheRigBuilder<TSelf>`, `CacheAssert` (Stampede / TagInvalidation / Coherent / FailSafe / NegativeCached / HitRate / EagerRefresh), `StampedeTester`, `BackplaneCapture`, `ClockControl` (wraps `FakeTimeProvider`). Concrete providers: `.Memory`, `.Redis`, `.Hybrid`, `.Fusion`.

## Install

```
dotnet add package Rig.TUnit.Caching.Redis
```

## Dependencies

`Rig.TUnit.Core`, `Microsoft.Extensions.TimeProvider.Testing`
