# Rig.TUnit.Observability.Logging

In-memory logging fixture with `ILogger<T>` capture + structured scope stack.
Ships `LogAssert` (fluent entry assertions) and `AntiPatternDetector` (runtime PII
and interpolated-template detection).

## Install

```xml
<PackageReference Include="Rig.TUnit.Observability.Logging" />
<PackageReference Include="Rig.TUnit.Observability.Logging.Analyzers" />
```

## Example

```csharp
await using var fx = new LoggingFixture();
await fx.InitializeAsync();

fx.CreateLogger("Orders").LogInformation("Processing {OrderId}", 42);

LogAssert.Logged(fx, LogLevel.Information)
         .WithProperty("OrderId", 42)
         .Once();

new AntiPatternDetector().AssertClean(fx);
```

## Dependencies
- `Rig.TUnit.Observability`
- `Microsoft.Extensions.Logging`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:072, C-005, C-006.
