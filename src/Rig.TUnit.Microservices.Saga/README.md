# Rig.TUnit.Microservices.Saga

> `SagaHarness` — multi-step orchestration asserter that drives a saga through its state machine and asserts each compensating action fires on failure.

## What this package is

A test harness for orchestration sagas (the long-running, eventually-
consistent cousin of a transaction). `SagaHarness` replays a sequence of
events and commands against the saga-under-test, lets you inject failures
at any step, and asserts that the correct compensating commands are issued.
Typical tests verify the happy path, each failure branch, and the
idempotency of compensations.

Works with any mediator-dispatched saga (`Mediator` or MediatR); you plug
the saga instance in and drive it via the harness' public methods.

## When to use it

- Testing a checkout-style saga with N steps (reserve inventory, charge
  card, book shipping, notify customer).
- Verifying the compensation graph is complete and symmetric.
- Fuzzing failure timing — "if step 3 fails after step 4 commits, are we
  still consistent?"
- **Not for**: choreography-style sagas (each service handles its own
  events); those do not have a central orchestrator to test.

## Prerequisites

- .NET 10 SDK
- Your saga is a class the harness can instantiate (parameterless or via
  provided factory delegate).

## Quick start

```csharp
using Rig.TUnit.Microservices.Saga;

var harness = new SagaHarness<CheckoutSaga>(
    factory: () => new CheckoutSaga());

await harness.Advance(new InventoryReserved());
await harness.Advance(new PaymentCaptured());

await Assert.That(harness.PublishedCommands)
    .Contains(c => c is BookShipping);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultTimeout` | `TimeSpan` | `5s` | Per-`Advance` deadline. |
| `ThrowOnUnexpectedPublish` | `bool` | `true` | Fail fast when the saga emits a command the test never expected. |
| `RecordCompensations` | `bool` | `true` | Capture compensating commands for assertions. |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.Saga.SagaHarness<TSaga>`
- `Rig.TUnit.Microservices.Saga.Assertions.CompensationAssert`
- `Rig.TUnit.Microservices.Saga.Helpers.FailureInjector`

## Per-test isolation

Each `SagaHarness` owns its own saga instance and state machine. Harness
state is disposed at the end of the test; nothing bleeds across tests.

## Parallelism + performance

- Harness construction: ~1 ms.
- Per-step cost: one saga method invocation + state snapshot — sub-millisecond
  for typical 5–10 step sagas.
- Safe under full parallelism.

## Troubleshooting

- **`UnexpectedPublishException`** — the saga emitted a command the test did
  not anticipate. Either add the expectation or assert the saga is correct.
- **Compensation missing** — `harness.PublishedCommands` includes both
  forward and compensating commands; filter with `OfType<>` to isolate.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- The harness does not persist saga state — if your saga uses
  `SnapshotStore` for durable state, stub it or the test will try to touch
  a real store.
- Timer-triggered saga timeouts are simulated via `TimeProvider`; advance
  virtual time with `harness.AdvanceTime(TimeSpan)`.

## Benchmarks

See [`SagaBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SagaBenchmarks.cs);
tracked in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)

## License

MIT. See [LICENSE](../../LICENSE).
