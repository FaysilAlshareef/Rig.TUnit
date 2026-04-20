# Rig.TUnit.Observability.Logging

> In-memory `ILogger<T>` capture with structured-scope stack, fluent `LogAssert`, and runtime `AntiPatternDetector` (PII + interpolated templates).

## What this package is

A logging integration-test fixture. `LoggingFixture` provides a
`CreateLogger(category)` factory backed by an in-memory `ILoggerProvider`
that captures every log entry plus the full `BeginScope` stack.
`LogAssert` gives fluent assertions over the captured entries:
`LogAssert.Logged(fx, LogLevel.Information).WithProperty("OrderId", 42).Once()`.
`AntiPatternDetector` runs at assertion time and catches two of the
most common logging foot-guns: PII in structured properties and
string-interpolated log templates.

## When to use it

- Asserting log messages emitted under specific conditions.
- Regression-testing log shape after a refactor.
- Catching interpolated templates (`$"..."`) before they land in prod.
- **Not for**: volumetric load testing — the in-memory capture is
  unbounded and will OOM on millions of entries.

## Prerequisites

- .NET 10 SDK
- `Microsoft.Extensions.Logging` (transitive)

## Quick start

```csharp
using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Fixtures;

await using var fx = new LoggingFixture();
await fx.InitializeAsync();

fx.CreateLogger("Orders").LogInformation("Processing {OrderId}", 42);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `MinLevel` | `LogLevel` | `Trace` | Below this, entries are dropped |
| `CaptureScopes` | `bool` | `true` | Record `BeginScope` stack |
| `ThrowOnAntiPattern` | `bool` | `false` | If `true`, the fixture fails fast on detection |

## Fixture + helper APIs

- `Rig.TUnit.Observability.Logging.Fixtures.LoggingFixture`
- `Rig.TUnit.Observability.Logging.Options.LoggingFixtureOptions`
- `Rig.TUnit.Observability.Logging.Builder.LoggingRigBuilder`
- `Rig.TUnit.Observability.Logging.Assertions.LogAssert`
- `Rig.TUnit.Observability.Logging.Helpers.AntiPatternDetector`

## Per-test isolation

Each `LoggingFixture` owns its entry buffer. No shared state. Safe
under full parallelism.

## Parallelism + performance

- Fixture construction: ~1 ms.
- Per-log overhead: ~5 µs (capture + scope clone).
- Safe under full parallelism.

## Troubleshooting

- **`LogAssert.Logged(…).Once()` fails with 0** — the `MinLevel`
  filtered the entry out. Check `fx.MinLevel` matches the level you
  expect.
- **`AntiPatternDetector` flags a non-PII property** — tune the
  `AntiPatternDetector.IgnoreProperties` list for the false positive.

See [docs/troubleshooting.md#logging](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `BeginScope` state is captured as an opaque object — the harness
  deep-clones it via `JsonSerializer` so later mutations do not corrupt
  the captured scope.
- Structured properties under `{Name}` placeholders are what gets
  captured; positional `{0}`-style logs capture only the formatted
  string. `AntiPatternDetector` flags the positional form.

## Benchmarks

See [`LoggingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/LoggingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Observability`](../Rig.TUnit.Observability/README.md)
- Sibling: [`Rig.TUnit.Observability.Logging.Analyzers`](../Rig.TUnit.Observability.Logging.Analyzers/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
