# Rig.TUnit.Messaging.RabbitMq

> Testcontainers-backed RabbitMQ fixture (`rabbitmq:3-management`) with async `RabbitMqListener` / `RabbitMqEventSender` on `RabbitMQ.Client` 7.x.

## What this package is

The Rig.TUnit RabbitMQ provider. `RabbitMqFixture` spins the management-
plugin-enabled image (so the HTTP admin API is available at `:15672` for
debugging), exposes the AMQP connection string, and ships async
listener/sender helpers on the new `RabbitMQ.Client` 7.x API.
`DeadLetterAssert` knows the DLX/DLQ convention the sender uses so
tests can assert the message landed in the dead-letter queue after N
failures.

## When to use it

- Integration tests for RabbitMQ queues, exchanges, bindings.
- Asserting dead-letter behaviour under retry policies.
- Verifying routing-key patterns (direct/topic/fanout/headers).
- **Not for**: pure unit tests of message-handler logic.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (RabbitMQ image ~220 MB)
- `RabbitMQ.Client` 7.x (transitive — async-only API)

## Quick start

```csharp
using Rig.TUnit.Messaging.RabbitMq.Fixtures;
using Rig.TUnit.Messaging.RabbitMq.Senders;

await using var fx = new RabbitMqFixture();
await fx.InitializeAsync();

await using var sender = new RabbitMqEventSender(fx.ConnectionString, queue: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
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
- `Rig.TUnit.Messaging.RabbitMq.Builder.RabbitMqRigBuilder`
- `Rig.TUnit.Messaging.RabbitMq.Listeners.RabbitMqListener`
- `Rig.TUnit.Messaging.RabbitMq.Senders.RabbitMqEventSender`

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
  `RabbitMqEventSender` wires this automatically.

## Benchmarks

See [`RabbitMqMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/RabbitMqMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
