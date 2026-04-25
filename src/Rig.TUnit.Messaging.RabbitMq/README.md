# Rig.TUnit.Messaging.RabbitMq

> Testcontainers-backed RabbitMQ fixture (`rabbitmq:3-management`) with async listener/sender on `RabbitMQ.Client` 7.x and a fluent topology builder for exchanges, bindings, queue arguments, and DLX.

## What this package is

The Rig.TUnit RabbitMQ provider. `RabbitMqFixture` spins the management-
plugin-enabled image (HTTP admin UI on `:15672` for debugging), exposes
the AMQP connection string, and ships async listener / sender helpers
on the new `RabbitMQ.Client` 7.x async-only API. Ships:

- **`RabbitMqEventSender`** — async sender with a `SendContext`
  overload mapping `PartitionKey` → routing key and writing the
  `x-partition-key` AMQP header. Explicit `exchange` + `routingKey`
  parameters; legacy default-exchange behaviour preserved when omitted.
- **`RabbitMqListener`** — declares its queue + (optional) exchange +
  binding before `BasicConsumeAsync`; reads `x-partition-key` into
  `CapturedMessage.SessionKey`. Decode failures are returned as a typed
  error via the `Errors` collection (the consumer runs `autoAck:true`
  so a throw would silently drop the payload).
- **`RabbitMqTopologyBuilder`** + provider-scoped interfaces
  (`IRabbitMqTopologyBuilder`, `IRabbitMqExchangeConfig`,
  `IRabbitMqQueueConfig`, `ExchangeType` enum) wired via
  `RabbitMqRigBuilder.WithTopology(…)`.
- **Queue-argument plumbing** — `IRabbitMqQueueConfig.WithMessageTtl`,
  `WithMaxLength`, `WithMaxPriority`, `WithDeadLetterExchange`,
  `WithQuorum`. Each maps to the corresponding AMQP `x-…` header.
- **`DeadLetterAssert`** — DLX/DLQ inspection that knows the
  convention the topology builder wires up.

## When to use it

- Integration tests for RabbitMQ queues, exchanges, bindings.
- Asserting topic-exchange fan-out across multiple bound queues.
- Asserting dead-letter behaviour under retry policies (DLX on nack).
- Verifying routing-key patterns (direct / topic / fanout / headers).
- Priority-queue ordering and quorum-queue durability.
- **Not for**: pure unit tests of message-handler logic.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (RabbitMQ image ~220 MB)
- `RabbitMQ.Client` 7.x (transitive — async-only API)

## Quick start

```csharp
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Fixtures;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

await using var fx = new RabbitMqFixture();
await fx.InitializeAsync();

// Default-exchange path (legacy — unchanged).
await using var sender = new RabbitMqEventSender(fx.ConnectionString, queue: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");

// Topic-exchange + routing-key path with SendContext.PartitionKey on the new overload.
await using var topicSender = new RabbitMqEventSender(
    fx.ConnectionString, exchange: "events", routingKey: "order.created");
await topicSender.SendAsync(
    "{\"orderId\":1}",
    context: new SendContext(PartitionKey: "order.created"),
    ct: ct);
```

Topic-exchange + DLX topology via the `WithTopology` rig hook:

```csharp
services.AddRigTUnit(rig =>
    rig.UseRabbitMq(RigConnect.FromValue(fx.ConnectionString), r =>
        r.WithTopology(t =>
            t.Exchange("events", ExchangeType.Topic)
             .Exchange("events-dlx", ExchangeType.Fanout)
             .Queue("eu-orders", c => c
                 .WithMessageTtl(TimeSpan.FromMinutes(5))
                 .WithDeadLetterExchange("events-dlx"))
             .Queue("dlq-store")
             .Binding("events", "eu-orders", "order.eu.*")
             .Binding("events-dlx", "dlq-store", ""))));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"rabbitmq:3-management"` | Image with admin UI |
| `StartupTimeoutSeconds` | `int` | `60` | RabbitMQ boot |
| `VHost` | `string` | `"/"` | Virtual host |
| `Username` | `string` | `"rigtunit"` | Default user |
| `Password` | `string` | `"rigtunit"` | Default password |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.RabbitMq.Fixtures.RabbitMqFixture`
- `Rig.TUnit.Messaging.RabbitMq.Options.RabbitMqFixtureOptions`
- `Rig.TUnit.Messaging.RabbitMq.Builder.RabbitMqRigBuilder` — ships
  `WithTopology(Action<IRabbitMqTopologyBuilder>)`.
- `Rig.TUnit.Messaging.RabbitMq.Helpers.RabbitMqListener` — exposes the
  typed `Errors` collection for decode-failure assertion.
- `Rig.TUnit.Messaging.RabbitMq.Helpers.RabbitMqEventSender` — ships
  `SendAsync(string, SendContext, …)` overload + explicit
  `exchange` / `routingKey` parameters.
- `Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqTopologyBuilder`
- `Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqExchangeConfig`
- `Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqQueueConfig` —
  `WithMessageTtl`, `WithMaxLength`, `WithMaxPriority`,
  `WithDeadLetterExchange`, `WithQuorum`.
- `Rig.TUnit.Messaging.RabbitMq.Topology.ExchangeType` — enum.
- `Rig.TUnit.Messaging.RabbitMq.Topology.RabbitMqTopologyBuilder`

## Per-test isolation

Per-test queue and DLQ: `orders_{IsolationKey:short}` +
`orders_{IsolationKey:short}.dlq`. Teardown deletes both queues.

## Parallelism + performance

- First-run pull: ~20 s.
- Warm startup: ~8 s (plugins take time).
- Per-op publish: ~2 ms.
- Parallelism: 8+ concurrent tests; queue-level isolation is trivial.

## Troubleshooting

- **`PRECONDITION_FAILED - inequivalent arg`** — queue redeclared with
  different args between runs; teardown must delete, or declare with
  identical args each time.
- **Consumer hangs on `Received`** — the listener's ack-policy is
  manual by default; call `listener.AckAsync(deliveryTag)` after each
  message, or use the auto-ack option.

See [docs/troubleshooting.md#rabbitmq](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- RabbitMQ 7.x async-only `RabbitMQ.Client` broke the sync `IModel`
  contract — all listener/sender code must be `async`.
- Durable queues survive broker restart; ephemeral queues do not. Test
  assertion style must match.
- DLX routing requires `x-dead-letter-exchange` on queue declaration —
  declare via `IRabbitMqQueueConfig.WithDeadLetterExchange(name)` so
  the topology builder writes the argument.
- Idempotent topology re-apply only succeeds when the second declaration
  uses **identical** arguments. Re-declaring with conflicting args
  throws `OperationInterruptedException` (`PRECONDITION_FAILED`); see
  `WithTopology_CalledTwice_IsIdempotent` test for the contract.
- Quorum queues (`IRabbitMqQueueConfig.WithQuorum()`) survive broker
  restart and require RabbitMQ ≥ 3.8; the management image bundled
  ships with 3.x.
- Listener runs `autoAck:true`, so decode failures are returned via
  `RabbitMqListener.Errors` rather than re-thrown — the message has
  already been removed from the queue by the time the callback runs.

## Benchmarks

See [`RabbitMqMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/RabbitMqMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Provider deep-dive: [`docs/providers/rabbitmq.md`](../../docs/providers/rabbitmq.md)
  (exchange + binding + DLX example, queue-args reference table).
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)
- Feature design: [Sessions & Partitions](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
