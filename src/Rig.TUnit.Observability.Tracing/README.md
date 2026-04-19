# Rig.TUnit.Observability.Tracing

In-memory OpenTelemetry tracing fixture. Captures `Activity` spans into an
`InMemoryExporter` so tests can assert on tags, status, parent/child, and duration
via the `TraceAssert` fluent DSL.

## Install

```xml
<PackageReference Include="Rig.TUnit.Observability.Tracing" />
```

## Example

```csharp
await using var fx = new TracingFixture(new TracingFixtureOptions { ServiceName = "my-svc" });
await fx.InitializeAsync();

using (var act = fx.ActivitySource.StartActivity("op.work"))
{
    act?.SetTag("http.method", "GET");
    act?.SetStatus(ActivityStatusCode.Ok);
}

TraceAssert.HasSpan(fx, "op.work")
           .WithTag("http.method", "GET")
           .WithStatus(ActivityStatusCode.Ok)
           .DurationLessThan(TimeSpan.FromSeconds(1));
```

## Dependencies
- `Rig.TUnit.Observability`
- `OpenTelemetry`, `OpenTelemetry.Exporter.InMemory`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:070.
