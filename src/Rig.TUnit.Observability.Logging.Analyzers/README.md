# Rig.TUnit.Observability.Logging.Analyzers

> Roslyn analyzer catching three common logging foot-guns at compile time: interpolated templates (`$"..."`), stray `Console.Write*`, and PII-shaped property names.

## What this package is

A compile-time complement to `Rig.TUnit.Observability.Logging`'s runtime
`AntiPatternDetector`. Three Roslyn diagnostics fire at build time so
bad logging never lands in a PR:

| ID | Severity | Description |
|----|----------|-------------|
| RTU001 | Warning | `$"..."` argument passed as message template to `ILogger.Log*` |
| RTU002 | Warning | `Console.Write*` / `Console.WriteLine*` in a non-test assembly |
| RTU003 | Warning | PII-shaped property name in a log call (`email`, `ssn`, `password`, …) |

## When to use it

- Any project that uses `ILogger` and wants the anti-patterns caught
  before the log lands.
- Teams enforcing structured logging conventions.
- **Not for**: test projects where `Console.WriteLine` is acceptable —
  RTU002 auto-skips for assemblies tagged with `[assembly: InternalsVisibleTo]`
  or named `*.Tests.*`.

## Prerequisites

- .NET 10 SDK (analyser targets `netstandard2.0` for broad compatibility).
- `Microsoft.CodeAnalysis.CSharp` 4.12+.

## Quick start

```csharp
// Build error at compile time, not runtime:
logger.LogInformation($"Processing {orderId}");  // RTU001
Console.WriteLine("hello");                       // RTU002
logger.LogInformation("User {Email}", email);     // RTU003
```

## Options

## §6 — N/A: analyser package has no `FixtureOptions`. Severity is tuned
via the standard `.editorconfig` mechanism:
`dotnet_diagnostic.RTU001.severity = error`.

## Fixture + helper APIs

- `Rig.TUnit.Observability.Logging.Analyzers.InterpolatedTemplateAnalyzer` (RTU001)
- `Rig.TUnit.Observability.Logging.Analyzers.ConsoleWriteAnalyzer` (RTU002)
- `Rig.TUnit.Observability.Logging.Analyzers.PiiPropertyAnalyzer` (RTU003)

## Per-test isolation

## §8 — N/A: compile-time analyser — no runtime state to isolate.

## Parallelism + performance

- Per-file analysis: sub-millisecond for typical source sizes.
- Roslyn runs analysers in parallel per file; the IDE pays the cost
  incrementally.
- Safe under full parallel compilation.

## Troubleshooting

- **RTU002 fires on test code** — confirm the test project matches the
  default `*.Tests.*` name pattern, or set `dotnet_diagnostic.RTU002.severity
  = none` in the test project's `.editorconfig`.
- **RTU003 false positive on a domain-term property** — add an
  `dotnet_diagnostic.RTU003.severity = none` suppression in the
  specific file / line.

See [docs/troubleshooting.md#logging-analyzers](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Analysers target `netstandard2.0` to load in any SDK version; do not
  raise the target without checking the MSBuild Roslyn version matrix.
- PII heuristic is name-based (`email`, `phone`, `ssn`, `dob`,
  `password`, `creditcard`, …). The list is intentionally conservative
  and tunable via `.editorconfig` suppressions.

## Benchmarks

## §12 — N/A: compile-time cost is folded into Roslyn; no dedicated
benchmark run. CI tracks total build time as a proxy.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Observability.Logging`](../Rig.TUnit.Observability.Logging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
