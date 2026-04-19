# Rig.TUnit.Observability.Seq

Boots a `datalust/seq` Testcontainer and wires a Serilog Seq sink so log entries
emitted through the fixture's `ILoggerFactory` land in a real Seq instance. Ships
`SeqAssert` with the same shape as `LogAssert` for a one-line swap.

## Install

```xml
<PackageReference Include="Rig.TUnit.Observability.Seq" />
```

Requires Docker on the test host.

## Example

```csharp
await using var fx = new SeqFixture(new SeqFixtureOptions { ImageTag = "latest" });
await fx.InitializeAsync();

fx.Factory.CreateLogger("T").LogWarning("Disk low {Free}", 100);

await SeqAssert.Query(fx, "Level=@Warning").Count(1).Within(TimeSpan.FromSeconds(10));
```

## Dashboard snapshot

`SeqFixture.CaptureDashboardSnapshotAsync(testName)` writes a `.txt` artifact to
`TestResults/seq-dashboards/` containing the Seq URL + service metadata.

## Dependencies
- `Rig.TUnit.Observability`
- `Testcontainers`, `Serilog`, `Serilog.Sinks.Seq`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:073, FR:074.
