# Rig.TUnit.Messaging

> Messaging family-base: `MessagingRigBuilder<TSelf>`, `ListenerBase<T>`, `EventSenderBase` (correlation / causation / W3C traceparent), `MessageAssert`, `DeadLetterAssert`, `OrderingAssert`, `TopicNamingConvention`.

## What this package is

The shared contract for every messaging provider (`.Kafka`, `.Nats`,
`.RabbitMq`, `.ServiceBus`, `.Sqs`). Defines the listener-lifecycle
contract (`ListenerBase<T>`), the sender side with automatic
correlation/causation ID propagation and W3C `traceparent` injection
(`EventSenderBase`), and three fluent assertion families:
`MessageAssert` (payload shape), `DeadLetterAssert` (DLQ content after
N retries), `OrderingAssert` (strict FIFO, partition-ordered, or
best-effort).

`TopicNamingConvention` enforces the `{company}-{domain}-{side}` naming
so cross-service traces line up in Seq / OpenTelemetry.

Install one of the leaves directly.

## When to use it

- Authoring a new messaging backend.
- Writing provider-agnostic messaging helpers.
- **Not for**: concrete messaging — install a leaf package.

## Prerequisites

- .NET 10 SDK

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultMessageTtl` | `TimeSpan` | `5m` | Applied when the backend supports per-message TTL |
| `MaxRetryAttempts` | `int` | `5` | Before dead-lettering |
| `CorrelationHeader` | `string` | `"X-Correlation-Id"` | HTTP-compatible header name |
| `TracePropagation` | `bool` | `true` | Inject W3C `traceparent` on send |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.IMessagingRig`
- `Rig.TUnit.Messaging.Fixtures.MessagingFixtureBase`
- `Rig.TUnit.Messaging.Builder.MessagingRigBuilder<TSelf>`
- `Rig.TUnit.Messaging.Listeners.ListenerBase<T>`
- `Rig.TUnit.Messaging.Senders.EventSenderBase`
- `Rig.TUnit.Messaging.Assertions.MessageAssert`
- `Rig.TUnit.Messaging.Assertions.DeadLetterAssert`
- `Rig.TUnit.Messaging.Assertions.OrderingAssert`
- `Rig.TUnit.Messaging.Helpers.TopicNamingConvention`

## Per-test isolation

Each leaf names topics/queues with `{IsolationKey}` so parallel tests
do not collide. Teardown deletes the per-test queue/topic.

## Parallelism + performance

## §9 — N/A: family-base; per-provider. Kafka bind-port constrains
parallelism; ServiceBus / SQS are effectively unbounded.

## Troubleshooting

- **`traceparent` header missing on received message** — the sender is
  bypassing `EventSenderBase` (using the raw SDK client). Always route
  through the Rig sender for consistent propagation.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Ordering guarantees differ: Kafka partition-ordered, ServiceBus
  session-ordered, SQS FIFO queues exact-ordered, RabbitMQ per-queue,
  NATS best-effort.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
