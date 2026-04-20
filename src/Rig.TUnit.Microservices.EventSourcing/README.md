# Rig.TUnit.Microservices.EventSourcing

> Given/When/Then harness for event-sourced aggregates with `AggregateAssert.Raised<T>().WithData(…)` and `EventCatalogueAssert` for schema-evolution verification.

## What this package is

The testing harness for event-sourced aggregates. `EventSourcingHarness
<TAggregate>` lets you rehydrate an aggregate from a list of past events,
execute a command, and assert on the events it raises.
`EventCatalogueAssert` takes a declared catalogue (version → event-type
list) and verifies the aggregate's rehydration path still accepts every
historical event shape — the load-bearing check for additive schema
evolution.

## When to use it

- Unit-testing an event-sourced aggregate.
- Verifying schema-evolution compatibility after a new event version lands.
- **Not for**: integration tests that require a real event store — layer
  with `Rig.TUnit.Databases.NoSql.KurrentDb`.

## Prerequisites

- .NET 10 SDK
- Your aggregate exposes a `Rehydrate(events)` factory and a
  `Pending` / `ClearPending` raised-event pattern (or equivalent).

## Quick start

```csharp
using Rig.TUnit.Microservices.EventSourcing;

var harness = new EventSourcingHarness<Order>(
    rehydrate:   events => Order.Rehydrate(events),
    getRaised:   o => o.Pending,
    clearRaised: o => o.ClearPending());

harness.Given(new OrderCreated("O-1", 100m))
       .When(o => o.Approve())
       .Then(new OrderApproved("O-1"));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `RequireExactEventOrder` | `bool` | `true` | Fail if raised event order does not match `Then(…)` |
| `IgnoreEventMetadata` | `bool` | `true` | Scrub `EventId` / `Timestamp` in comparisons |
| `CatalogueSourceType` | `Type?` | `null` | Optional `EventCatalogue` type for schema checks |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.EventSourcing.EventSourcingHarness<TAggregate>`
- `Rig.TUnit.Microservices.EventSourcing.Assertions.AggregateAssert`
- `Rig.TUnit.Microservices.EventSourcing.Assertions.EventCatalogueAssert`

## Per-test isolation

Each harness instance owns its own aggregate state — no shared statics,
no cross-test bleed. Safe under full parallelism.

## Parallelism + performance

- Harness construction: ~1 µs.
- Rehydrate + apply: dominated by aggregate's own logic, typically
  sub-millisecond.
- Safe under full parallelism.

## Troubleshooting

- **`Then(expected)` fails with length mismatch** — aggregate raised
  more or fewer events than expected. Strict mode is the default; switch
  to `RequireExactEventOrder=false` if the contract allows extras.
- **`EventCatalogueAssert` fails after version bump** — a new event
  type was added to the code but not to the catalogue declaration;
  update both in lockstep.

See [docs/troubleshooting.md#event-sourcing](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- The harness does not persist events between `When` calls — each call
  reapplies the full event stream. Aggregates that cache state across
  calls break this assumption; see `WithCarriedState` opt-in.
- Event IDs are typically scrubbed in comparisons because they are
  generated at raise-time; the `IgnoreEventMetadata` flag controls this.

## Benchmarks

See [`EventSourcingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/EventSourcingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Microservices.Snapshots`](../Rig.TUnit.Microservices.Snapshots/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
