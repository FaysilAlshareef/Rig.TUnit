# Rig.TUnit.Observability.AppInsights

In-process Application Insights test fixture. Wires a `TelemetryClient` to a
`CapturingTelemetryChannel` so tests emit via the real `TelemetryClient` API
while the channel records every item into a thread-safe queue — zero network
egress, deterministic assertions.

## Install

```bash
dotnet add package Rig.TUnit.Observability.AppInsights
```

## Quick start

```csharp
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;

await using var fx = new AppInsightsFixture();
await fx.InitializeAsync();

fx.Client.TrackEvent("order.placed");
fx.Client.TrackDependency("SQL", "Orders", "INSERT", DateTime.UtcNow, TimeSpan.FromMilliseconds(4), success: true);

AppInsightsAssert.Event(fx.Channel, "order.placed").Exactly(1);
AppInsightsAssert.Dependency(fx.Channel, "SQL").AtLeast(1);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseAppInsights(RigConnect.FromValue("instrumentation-key"), cfg => { })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `InstrumentationKey` | `00000000-…` | Placeholder AI key (no egress) |
| `RoleName` | `rigtunit-tests` | Cloud role name on every telemetry item |

## Assertion surface

- `AppInsightsAssert.Event(channel, name).Exactly(N)` — custom events
- `AppInsightsAssert.Dependency(channel, type).AtLeast(N)` — outgoing dep calls
- `AppInsightsAssert.Exception<TException>(channel).Exactly(N)` — tracked exceptions

Mirrors `TraceAssert` / `MetricAssert` in other Rig.TUnit Observability
packages for a uniform experience across tracing, metrics, and AI telemetry.
