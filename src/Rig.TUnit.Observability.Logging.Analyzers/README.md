# Rig.TUnit.Observability.Logging.Analyzers

Roslyn analyzer that complements `Rig.TUnit.Observability.Logging`. Catches at
compile time what the runtime detector cannot.

## Diagnostics

| ID      | Severity | Description                                                    |
|---------|----------|----------------------------------------------------------------|
| RTU001  | Warning  | `$"..."` argument passed as message template to `ILogger.Log*` |
| RTU002  | Warning  | `Console.Write*`/`Console.WriteLine*` in a non-test assembly   |
| RTU003  | Warning  | PII-shaped property name in a log call                         |

## Install

```xml
<PackageReference Include="Rig.TUnit.Observability.Logging.Analyzers">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

## Dependencies
- Targets `netstandard2.0`
- `Microsoft.CodeAnalysis.CSharp`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:072, C-006.
