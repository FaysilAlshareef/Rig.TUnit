# Rig.TUnit.Docker

Generic Testcontainers-backed container fixture for Rig.TUnit. Use when the
provider-specific fixtures (SqlServer, Redis, Kafka, ...) are not a fit —
e.g., for custom test harness containers, third-party services without a
Rig.TUnit package, or exploratory testing.

## Install

```bash
dotnet add package Rig.TUnit.Docker
```

## Quick start

```csharp
using Rig.TUnit.Docker.Fixtures;

await using var fx = new ContainerFixture(
    image: "alpine:3",
    env: new Dictionary<string, string> { ["FOO"] = "bar" },
    exposedPorts: new[] { 8080 });
await fx.InitializeAsync();

// fx.Container exposes the raw Testcontainers IContainer for bespoke control.
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseDocker(RigConnect.FromValue("alpine:3"), cfg => { /* future extensions */ })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `DefaultImage` | `alpine:3` | Fallback image for generic containers |
| `IsolatePerTestNetwork` | `true` | Each fixture runs in its own Docker network |
| `ReuseImageCache` | `true` | Cache pulled images across fixtures |
| `DefaultStartupTimeoutSeconds` | `300` | Max time to wait for container readiness |

## Compose backend

`ContainerFixture` uses Testcontainers' native compose support. A fallback to
`Ductus.FluentDocker` is kept as an escape hatch if the native backend
regresses (activation criteria tracked in this README when needed).
