# Rig.TUnit.Messaging

> Messaging family-base: `MessagingRigBuilder<TSelf>`, `ListenerBase<T>`, `EventSenderBase` (correlation / causation / W3C traceparent), `MessageAssert`, `DeadLetterAssert`, `OrderingAssert`, `TopicNamingConvention`.

## What this package is

The shared contract for every messaging provider (`.Kafka`, `.Nats`,
`.RabbitMq`, `.ServiceBus`, `.Sqs`). Defines:

- **Listener lifecycle** — `ListenerBase<T>` (start/stop, capture).
- **Sender side** — `EventSenderBase` with automatic
  correlation/causation/W3C `traceparent` injection.
- **Cross-provider routing keys** — `SendContext(SessionKey, PartitionKey,
  DeduplicationKey)` carries the per-message ordering / partitioning /
  idempotency hints that each provider maps to its native primitive
  (Service Bus `SessionId`, Kafka `Message.Key`, SQS `MessageGroupId`,
  RabbitMQ routing key, NATS JetStream subject).
- **Topology builder marker** — `ITopologyBuilder` is the application hook
  every provider's `WithTopology(…)` lambda returns. The base interface
  carries no fluent methods; provider packages own their own typed
  surface so calling `.Queue()` on Kafka or `.WithFifo()` on RabbitMQ is
  a compile error, not a runtime no-op.
- **Captured envelope** — `CapturedMessage<TMessage>` records the raw
  message, headers, body, correlation ID, and (for session-aware
  listeners) the per-session ordering key.
- **Assertion families** — `MessageAssert` (payload shape),
  `DeadLetterAssert` (DLQ content after N retries), `OrderingAssert`
  (strict FIFO, partition-ordered, or best-effort).

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
using Rig.TUnit.Messaging.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

await using var _ = rig;

// SendContext carries cross-provider routing keys to any *EventSender.SendAsync overload.
var ctx = new SendContext(
    SessionKey: "order-42",        // Service Bus SessionId · SQS MessageGroupId · NATS x-session-key
    PartitionKey: "order-42",      // Kafka Message.Key · RabbitMQ routing key
    DeduplicationKey: "evt-123");  // Service Bus MessageId · SQS MessageDeduplicationId
```

Provider-specific `WithTopology(…)` hooks (returning a typed
`ITopologyBuilder` subtype) live on each provider's `RigBuilder` —
`ServiceBusRigBuilder.WithTopology(Action<IServiceBusTopologyBuilder>)`,
`KafkaRigBuilder.WithTopology(Action<IKafkaTopologyBuilder>)`, etc.

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
- `Rig.TUnit.Messaging.Builder.MessagingRigBuilder<TSelf>` — note: by
  design (per ADR / NFR-C3) the base does **not** declare `WithTopology`;
  each provider's `RigBuilder` adds the strongly-typed hook.
- `Rig.TUnit.Messaging.Helpers.ListenerBase<T>` + `CapturedMessage<T>`
- `Rig.TUnit.Messaging.Helpers.EventSenderBase`
- `Rig.TUnit.Messaging.Helpers.SendContext` — record carrying
  `SessionKey` / `PartitionKey` / `DeduplicationKey`.
- `Rig.TUnit.Messaging.Topology.ITopologyBuilder` — marker; declares only
  `Task ApplyAsync(CancellationToken)`.
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
  NATS best-effort (core) / per-stream-subject (JetStream).
- `SendContext` does not invent semantics on providers that lack them.
  `DeduplicationKey` is honoured only by Service Bus (with duplicate
  detection enabled on the entity) and SQS FIFO; Kafka / RabbitMQ /
  NATS ignore it.
- Topology builders are **provider-scoped, compile-time-enforced**:
  there is no shared `ITopologyBuilder.Queue(...)` to throw on. The
  presence test (`ProviderCompletenessTests`) only asserts that every
  provider listed in `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt`
  ships a `WithTopology` hook returning some `ITopologyBuilder`; the
  exact surface is each provider's choice.
- Idempotent apply: every provider's `ApplyAsync` is create-or-update,
  so running the same topology twice (or against an already-provisioned
  broker) succeeds without throwing.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Glossary](../../docs/glossary.md)
- [Ordering assertions — capability matrix](../../docs/ordering-assertions.md)
- Per-provider topology + sessions docs: [`docs/providers/`](../../docs/providers/)
  ([service-bus.md](../../docs/providers/service-bus.md) ·
  [kafka.md](../../docs/providers/kafka.md) ·
  [rabbitmq.md](../../docs/providers/rabbitmq.md) ·
  [sqs.md](../../docs/providers/sqs.md) ·
  [nats.md](../../docs/providers/nats.md))
- Feature design: [Sessions & Partitions Design](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder Design](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
