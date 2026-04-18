# Rig.TUnit.Observability

Base package for telemetry fixtures (tracing, logging, Seq). Provides `ITelemetryRig`,
`TelemetryFixtureBase`, and `TelemetryRigBuilder<TSelf>` so every provider shares
a consistent shape with deterministic `IsolationKey` / `ServiceName`.

## Install

```xml
<PackageReference Include="Rig.TUnit.Observability" />
```

## Example

```csharp
public sealed class MyFixture : TelemetryFixtureBase
{
    public override string ConnectionString => string.Empty;
    public override Task InitializeAsync() => Task.CompletedTask;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

Concrete providers: `Rig.TUnit.Observability.Tracing`, `.Logging`, `.Seq`.

## Dependencies
- `Rig.TUnit.Core`
- `TUnit.Core`
- `Microsoft.Extensions.Options`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:070-074.
