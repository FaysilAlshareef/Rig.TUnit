# Rig.TUnit.Mediator

> Pipeline-inspection helpers and an in-memory `IMediator` fake for Martin Othamar's `Mediator` library.

## What this package is

A thin layer over the `Mediator` / `Mediator.Abstractions` packages that lets
you (a) assert the order and composition of pipeline behaviours executed for a
given request, (b) replace individual handlers with NSubstitute-backed fakes
without rewiring DI, and (c) capture every handled request/response pair for
after-the-fact assertions.

It does **not** wrap MediatR — the two libraries have incompatible source
generators. See [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
for why contract tests live at the family base.

## When to use it

- Testing a CQRS slice where a command/query goes through MediatR pipeline
  behaviours (validation, logging, retry).
- Asserting the behaviour-chain composition is what you expect.
- Stubbing a single handler while leaving the rest real.
- **Not for**: testing the generated source code of `Mediator.SourceGenerator`
  — that is the library's job.

## Prerequisites

- .NET 10 SDK
- Project under test references `Mediator` + `Mediator.SourceGenerator`.

## Quick start

```csharp
using Rig.TUnit.Mediator.Helpers;
using Mediator;

var probe = new MediatorPipelineProbe();
var mediator = probe.BuildMediator(services =>
{
    services.AddSingleton<IRequestHandler<Ping, Pong>>(
        _ => new PingHandler());
});

var pong = await mediator.Send(new Ping("hi"));
await Assert.That(probe.RequestsHandled).HasCount(1);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `CaptureResponses` | `bool` | `true` | Record every handler's return value on the probe. |
| `FailOnUnhandled` | `bool` | `true` | Throw if a request reaches Send without a registered handler. |

## Fixture + helper APIs

- `Rig.TUnit.Mediator.Helpers.MediatorPipelineProbe` — inspection
- `Rig.TUnit.Mediator.Helpers.RequestSpy<TRequest, TResponse>` — record calls
- `Rig.TUnit.Mediator.Helpers.BehaviourOrderAssert` — pipeline-order assertion

## Per-test isolation

`MediatorPipelineProbe` is scoped — each test instantiates its own; handlers
and behaviours register per-probe. No static state.

## Parallelism + performance

- Probe construction: ~5 ms (one DI container per probe).
- Handler dispatch: zero overhead vs bare Mediator — the probe intercepts
  via a pipeline behaviour registered at position 0.
- Safe under full test parallelism.

## Troubleshooting

- **`NoHandlerForRequest`** — the source generator did not discover your
  handler. Ensure it is `public` (or `internal` with `[InternalsVisibleTo]`
  for the test project).
- **Pipeline behaviour skipped** — `Mediator` composes behaviours in DI
  registration order; check `services.AddTransient` order.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `Mediator.SourceGenerator` writes the dispatcher at *compile* time —
  a handler added at runtime without source-gen re-run will not be found.
- The probe's order assertion ignores framework-internal behaviours
  (exception interception, logging).

## Benchmarks

See [`MediatorPipelineBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MediatorPipelineBenchmarks.cs);
tracked in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
