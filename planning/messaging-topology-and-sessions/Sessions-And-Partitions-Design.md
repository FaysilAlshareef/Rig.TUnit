# Sessions & Partitions — Technical Design

**Feature**: 007
**Owner**: Messaging base library + 5 provider packages
**Status**: Draft

---

## Problem

Every broker has a "messages with the same key go to the same consumer, in order"
primitive, but they call it something different and expose it through different APIs:

| Broker | Primitive | SDK type |
|--------|-----------|----------|
| Azure Service Bus | Session | `ServiceBusMessage.SessionId`, `ServiceBusSessionProcessor` |
| Azure Service Bus (partitioned entity) | Partition key | `ServiceBusMessage.PartitionKey` |
| Kafka | Partition key (hashed to partition) | `Message<TKey,TValue>.Key` |
| RabbitMQ | Routing key on exchange | `BasicPublishAsync(exchange, routingKey, …)` |
| NATS JetStream | Subject hierarchy | `ConsumerConfig.FilterSubject(s)` |
| Amazon SQS FIFO | Message group | `SendMessageRequest.MessageGroupId` |

Today, `Rig.TUnit.Messaging` treats all of these as opaque. `OrderingAssert.PerKeyMonotonic`
exists, but no sender lets you **set** the key; the Kafka sender even overloads
`correlationId` as the partition key, which is semantically wrong.

---

## Goal

One concept in the public API — a "partition key" or "session key" — that maps cleanly to
each provider's native primitive, so `OrderingAssert.PerKeyMonotonic` can validate real
per-key ordering end-to-end on every provider.

---

## Design

### Base library

Extend `EventSenderBase` with an optional `SendContext`:

```csharp
public readonly record struct SendContext(
    string? SessionKey = null,         // Service Bus SessionId / SQS MessageGroupId
    string? PartitionKey = null,       // Kafka key / Service Bus PartitionKey / Rabbit routingKey
    string? DeduplicationKey = null);  // SQS MessageDeduplicationId (FIFO), Rabbit message-id
```

- `SessionKey` and `PartitionKey` are semantically close but not identical.
  - Service Bus enforces `PartitionKey == SessionId` when both are set on session-aware
    entities — base sender must surface a precondition error before the broker does.
  - Kafka has no session concept; `SessionKey` is mapped to `PartitionKey` if
    `PartitionKey` is null.
  - SQS FIFO uses `SessionKey` → `MessageGroupId`; `PartitionKey` is meaningless (ignore).
- `DeduplicationKey` is provider-optional; only SQS FIFO and Rabbit (message-id)
  consume it.

The existing `SendAsync(string body, …)` overload stays; a new overload accepts
`SendContext`. The old overload forwards an empty context. **No breaking change.**

### Listener side

Add a `SessionKey` property to `CapturedMessage<T>` (nullable, populated when the provider
surfaces it), so `OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey, m => m.Sequence)`
works uniformly.

---

## Per-provider mapping

### ServiceBus

| SendContext | Service Bus field |
|---|---|
| `SessionKey` | `ServiceBusMessage.SessionId` |
| `PartitionKey` | `ServiceBusMessage.PartitionKey` (must equal `SessionId` if both set) |
| `DeduplicationKey` | `ServiceBusMessage.MessageId` (requires duplicate detection enabled on entity) |

**Listener**: new `ServiceBusSessionListener` uses `ServiceBusClient.CreateSessionProcessor`.
Requires the subscription to be declared `RequiresSession = true` (see Topology design).
Processor handler gets `ProcessSessionMessageEventArgs` which exposes `SessionId` —
populate `CapturedMessage.SessionKey`.

**Validation**: `ServiceBusEventSender.SendAsync` throws `InvalidOperationException` when
both `SessionKey` and `PartitionKey` are set and unequal. Emulate before broker round-trip.

### Kafka

| SendContext | Kafka field |
|---|---|
| `SessionKey` | folds into `PartitionKey` if `PartitionKey` is null |
| `PartitionKey` | `Message<string,string>.Key` |
| `DeduplicationKey` | ignored (Kafka exactly-once uses transactions, out of scope) |

**Today**: `KafkaEventSender.SendAsync` sets `Key = correlationId ?? Guid.NewGuid().ToString()`
([KafkaEventSender.cs:34](../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34)).

**Fix**: `Key = context.PartitionKey ?? context.SessionKey ?? correlationId ?? Guid…`.
The correlation-id fallback stays as a last resort for back-compat but is documented as
"test-only — real applications should set `PartitionKey` explicitly".

**Listener**: `CapturedMessage.SessionKey = result.Message.Key`. No new listener type
required; a multi-partition topic already delivers in-order per key.

### RabbitMQ

| SendContext | RabbitMQ field |
|---|---|
| `SessionKey` | `routingKey` (when using a topic exchange with `key.*` wildcards) |
| `PartitionKey` | same — aliased |
| `DeduplicationKey` | `BasicProperties.MessageId` (native de-dup is not built in; document as advisory) |

Rabbit has no first-class session concept; ordering is **per queue** rather than per key.
The idiomatic pattern is: one topic exchange, N routing keys, N queues bound on
`key.#`, one consumer per queue. `OrderingAssert.PerKeyMonotonic` in Rabbit mode
inspects headers, not routing keys, because the broker strips the routing key before
delivery — so the sender also writes the key to a custom header `x-partition-key`.

### NATS

Core NATS has **no ordering guarantees across subjects**. The JetStream path does:

| SendContext | JetStream behaviour |
|---|---|
| `SessionKey` | appended as a subject segment (`orders.{sessionKey}`) — subject is the partition |
| `PartitionKey` | same — aliased |
| `DeduplicationKey` | `Nats-Msg-Id` header, picked up by JetStream's built-in de-dup window |

**Listener**: new `NatsJetStreamListener` with ordered consumer
(`ConsumerConfig.DeliverPolicy = All, ReplayPolicy = Instant, FlowControl = true`).
`FilterSubjects` is populated from the builder topology definition.

### SQS FIFO

| SendContext | SQS field |
|---|---|
| `SessionKey` | `SendMessageRequest.MessageGroupId` (required on FIFO) |
| `PartitionKey` | ignored |
| `DeduplicationKey` | `SendMessageRequest.MessageDeduplicationId` (required on FIFO unless content-based dedup enabled) |

**Test isolation gotcha**: FIFO dedup window is **5 minutes**. A test that re-uses a
`DeduplicationKey` across runs on the same CI agent silently drops messages. Mitigation:
always prefix `DeduplicationKey` with `IsolationKey` (the TUnit per-test namespace).

**Listener**: standard receive; request `AttributeNames = ["MessageGroupId", "SequenceNumber"]`
to populate `CapturedMessage.SessionKey` and enable sequence-based ordering assertions.

---

## Public API changes

### Sender (all 5 providers)

```csharp
// Before
public Task SendAsync(
    string body,
    string? correlationId = null,
    string? causationId = null,
    string? traceparent = null,
    IReadOnlyDictionary<string, string>? additionalHeaders = null,
    CancellationToken ct = default);

// After — new overload, old one kept
public Task SendAsync(
    string body,
    SendContext context,
    string? correlationId = null,
    string? causationId = null,
    string? traceparent = null,
    IReadOnlyDictionary<string, string>? additionalHeaders = null,
    CancellationToken ct = default);
```

### Listener (base library)

```csharp
// CapturedMessage: tighten Body to non-null, add SessionKey.
// `Message` is the broker-native received object (ServiceBusReceivedMessage,
// ConsumeResult<,>, etc.); the type parameter stays `TMessage` to match that semantic.
public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    string? CorrelationId,
    string? SessionKey = null);   // NEW — nullable, populated when provider surfaces it
```

**Resolution C-001 (2026-04-23)**: keep the existing `TMessage Message` naming from
[`ListenerBase.cs`](../../src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs); tighten
`string? Body` → `string Body` (listeners coerce null → empty string at capture time);
add the trailing `string? SessionKey = null`. Packages are pre-release, so the
`Body` narrowing is shipped clean — no obsolete shim.

### Service Bus — new listener type

```csharp
public sealed class ServiceBusSessionListener
    : ListenerBase<ServiceBusReceivedMessage>, IAsyncDisposable
{
    public ServiceBusSessionListener(
        ServiceBusClient client,
        string topic,
        string subscription,
        ServiceBusSessionProcessorOptions? options = null,
        TimeProvider? clock = null);

    public IReadOnlyCollection<string> ObservedSessions { get; }
}
```

---

## Testing strategy

| Provider | Scenario | Assertion |
|---|---|---|
| ServiceBus | Send 100 messages, 10 distinct `SessionId`s, 10 per session | `OrderingAssert.PerKeyMonotonic` passes; each session handler saw exactly 10 in sequence |
| ServiceBus | Send with `SessionId = A`, `PartitionKey = B` | Sender throws `InvalidOperationException` before hitting broker |
| Kafka | 3-partition topic, 5 keys, 20 messages per key | Messages for each key land on one partition; `OrderingAssert` green per key |
| RabbitMQ | Topic exchange + 3 queues bound on `user.*`, `order.*`, `stock.*` | Each queue receives only its subject; header `x-partition-key` matches |
| NATS JetStream | Stream on `events.>` subject, send 3 subjects, ordered consumer | Consumer sees all 3 subjects in global sequence; reconnect mid-stream does not duplicate |
| SQS FIFO | 5 `MessageGroupId`s, 10 messages each, one consumer per group | Per-group in order; dedup window test re-sends dup ID, expects 1 delivery |

---

## Non-goals

- Cross-provider abstract "session" type. Applications that need to swap providers still
  use the provider-specific fixture; we unify only the key parameter name, not the
  semantics.
- Kafka transactions / exactly-once producer.
- Service Bus auto-renewing session locks — tests set lock duration explicitly.
- RabbitMQ stream queues (Rabbit 3.9+ `rabbitmq_stream`) — can be added later under
  `x-queue-type=stream`; out of scope here.
