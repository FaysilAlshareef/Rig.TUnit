# Rig.TUnit.Core

> The heart of Rig.TUnit — `RigBuilder` CRTP, `IsolationKey`, and the `FixtureOptions` base contract every provider builds on.

## What this package is

The foundation layer. Every provider (Postgres, Redis, Kafka, Cosmos, …)
derives from `RigBuilder<TSelf>` via the curiously-recurring template
pattern so `Use{Provider}(…)` extension chains return strongly-typed builders
without erasing state. `IsolationKey.FromExecutionContext()` reads the
ambient `TUnit` test-context and produces a deterministic
`feature/class/method/iteration` identifier that provider fixtures thread
into container names, schema names, queue names — anywhere per-test naming
matters.

If you are reading one README in this repo, read this one — everything else
specialises what happens here.

## When to use it

- Authoring a new `Rig.TUnit.*` provider — derive your builder from
  `RigBuilder<TSelf>`.
- Writing a domain-only test that needs `IsolationKey` but no container.
- Consuming a fixture's `Options` class — every one of them has a
  `public const string SectionName` convention inherited from this package.
- **Not for**: production application code. This is a test-only library.

## Prerequisites

- .NET 10 SDK. No runtime dependencies beyond
  `Microsoft.Extensions.DependencyInjection.Abstractions` and
  `Microsoft.Extensions.Options`.

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var isolation = IsolationKey.FromExecutionContext();
var rig = new RigBuilder()
    .WithIsolation(isolation)
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `TimeProvider` | `TimeProvider` | `TimeProvider.System` | Injected clock; override in tests to freeze time. |
| `DefaultCancellationTimeout` | `TimeSpan` | `30s` | Auto-cancel token used by async helpers. |

## Fixture + helper APIs

- `Rig.TUnit.Core.Builder.RigBuilder` / `RigBuilder<TSelf>` — CRTP root
- `Rig.TUnit.Core.Helpers.IsolationKey` — per-test naming
- `Rig.TUnit.Core.Configuration.TestConfigurationBuilder` — options binding
- `Rig.TUnit.Core.Fixtures.IRigFixture` — the disposable fixture contract
- `Rig.TUnit.Core.Fakers.*` — Bogus `Faker<T>` presets

## Per-test isolation

`IsolationKey.FromExecutionContext()` returns a short, filesystem-safe
identifier of the form `fixture_ABCD1234` derived from the TUnit
`TestContext.TestDetails` (class + method + iteration). Every provider
fixture appends this to container / schema / queue names.

## Parallelism + performance

- Zero containers, zero I/O at `Core` level — `new RigBuilder()` is O(µs).
- `IsolationKey.FromExecutionContext()` is a single `SHA256` over the
  test-context tuple, memoised per `AsyncLocal` flow.
- Safe under full test parallelism.

## Troubleshooting

- **`IsolationKey.FromExecutionContext()` returns same value in two tests** —
  confirm you are inside a TUnit test method, not a static constructor or
  module initialiser.
- **`FixtureOptions.ValidateOnStart()` fails** — the bound configuration
  section is missing a `[Required]` property; the error message names it.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `IsolationKey` includes the *test iteration index* when you use TUnit's
  `[Repeat]` / `[Arguments]` attributes — critical when the same method runs
  N times against parallel fixtures.
- `RigBuilder` uses CRTP, not interface-default-methods, because TUnit's
  source-generator-driven discovery historically struggled with
  interface-default method disambiguation (ADR-002).

## Benchmarks

See [`CoreBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/CoreBenchmarks.cs)
and [`CoreBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/CoreBenchmarks.cs);
baseline numbers are tracked in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-002 — CRTP RigBuilder](../../docs/adr/ADR-002-crtp-rigbuilder.md)
- [ADR-006 — IsolationKey](../../docs/adr/ADR-006-isolationkey.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
