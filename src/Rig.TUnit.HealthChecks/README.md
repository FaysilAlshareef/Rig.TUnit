# Rig.TUnit.HealthChecks

`HealthAssert` fluent assertions against ASP.NET Core health endpoints.
Ships a `DependencyDownSimulator` so tests can flip a specific dependency unhealthy
and verify live/ready probes respond correctly.

## Install

```xml
<PackageReference Include="Rig.TUnit.HealthChecks" />
```

## Example

```csharp
await HealthAssert.On(client, "/health/ready")
                  .Contains("database")
                  .IsHealthy()
                  .InTime(TimeSpan.FromSeconds(1));
```

Probe kinds distinguished via `ProbeKind.Live | Ready | Startup`.

Spec: `003-rig-tunit-ecosystem-expansion` — US9.
