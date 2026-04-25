# Ordering assertions

`OrderingAssert.PerKeyMonotonic` asserts that messages captured by a listener arrived in
monotonically non-decreasing order within each key group. All five messaging providers support
this assertion via their respective ordering primitives.

## API

```csharp
OrderingAssert.PerKeyMonotonic<T>(
    ListenerBase<T> listener,
    Func<T, string>  keyExtractor,
    Func<T, long>    sequenceExtractor);
```

- `keyExtractor` — groups messages by their routing key (session ID, partition key, etc.)
- `sequenceExtractor` — extracts a broker-assigned sequence number that must be non-decreasing within each group

## Provider capability matrix

| Provider | Ordering primitive | `SendContext` field | `keyExtractor` | `sequenceExtractor` |
|----------|--------------------|---------------------|----------------|---------------------|
| **Azure Service Bus** | Native session processor — FIFO per session | `SessionKey` | `m => m.SessionId` | `m => m.SequenceNumber` |
| **Apache Kafka** | Deterministic partition via murmur2 key hash | `PartitionKey` (falls back to `SessionKey`) | `m => m.Message.Key` | `m => m.Offset.Value` |
| **Amazon SQS** | FIFO queue `MessageGroupId` | `SessionKey` → `MessageGroupId` | `m => m.Attributes["MessageGroupId"]` | `m => long.Parse(m.Attributes["SequenceNumber"])` |
| **RabbitMQ** | Topic-exchange routing key; single-queue FIFO per binding | `PartitionKey` → routing key | `m => m.RoutingKey` | `m => (long)m.DeliveryTag` |
| **NATS JetStream** | Ordered consumer (deliver-all, replay-instant, explicit ack) | `SessionKey` → `x-session-key` header | `m => m.Headers?["x-session-key"] ?? string.Empty` | `m => m.CapturedMessage.ReceivedAt.Ticks` |

## Usage examples

### Azure Service Bus

```csharp
await using var sender   = new ServiceBusEventSender(client, "orders");
await using var listener = new ServiceBusSessionListener(client, "orders", "shipping-sessions");
await listener.StartAsync(ct);

for (var i = 0; i < 20; i++)
    await sender.SendAsync($"msg-{i}", context: new SendContext(SessionKey: "cust-1"), ct: ct);

// wait for all messages...

OrderingAssert.PerKeyMonotonic(listener,
    m => m.SessionId,
    m => m.SequenceNumber);
```

### Apache Kafka

```csharp
await using var sender = new KafkaEventSender(fixture.ConnectionString, "orders");
var listener = new KafkaListener(fixture.ConnectionString, "orders", "grp");
await listener.StartAsync(ct);

for (var i = 0; i < 20; i++)
    await sender.SendAsync($"msg-{i}", context: new SendContext(PartitionKey: "cust-1"), ct: ct);

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener,
    m => m.Message.Key,
    m => m.Offset.Value);
```

### Amazon SQS

```csharp
var sender   = new SqsEventSender(fixture.Client, fifoQueueUrl);
var listener = new SqsListener(fixture.Client, fifoQueueUrl);
await listener.StartAsync(ct);

for (var i = 0; i < 10; i++)
    await sender.SendAsync($"msg-{i}", context: new SendContext(SessionKey: "cust-1"), ct: ct);

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener,
    m => m.Attributes["MessageGroupId"],
    m => long.Parse(m.Attributes["SequenceNumber"]));
```

### RabbitMQ

```csharp
builder.WithTopology(t =>
    t.Exchange("events", ExchangeType.Topic).BindQueue("order-q", "order.*"));
await captured.ApplyTopologyAsync(ct);

await using var sender = new RabbitMqEventSender(fx.ConnectionString, "order-q");
var listener = new RabbitMqListener(fx.ConnectionString, "order-q");
await listener.StartAsync(ct);

for (var i = 0; i < 10; i++)
    await sender.SendAsync($"msg-{i}", context: new SendContext(PartitionKey: "order.created"), ct: ct);

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener,
    m => m.RoutingKey,
    m => (long)m.DeliveryTag);
```

### NATS JetStream

```csharp
builder.WithTopology(t => t.Stream("events", cfg => cfg.WithSubjects("events.>")));
await captured.ApplyTopologyAsync(ct);

await using var sender   = new NatsJetStreamEventSender(fx.JetStream, "events.orders");
await using var listener = new NatsJetStreamListener(fx.JetStream, "events", "events.orders");
await listener.StartAsync(ct);

for (var i = 0; i < 10; i++)
    await sender.SendAsync($"msg-{i}", context: new SendContext(SessionKey: "cust-1"), ct: ct);

// wait for messages...

// Use ReceivedAt ticks as a proxy for stream sequence within an ordered consumer
OrderingAssert.PerKeyMonotonic(listener,
    m => m.SessionKey ?? string.Empty,
    m => listener.Captured.IndexOf(listener.Captured.First(x => x == m)));
```

> **Note:** For NATS JetStream the stream sequence is available via `NatsJSMsg.Metadata.Sequence.Stream`
> when using the raw consumer API. The `NatsJetStreamListener` surfaces `CapturedMessage.SessionKey`
> from the `x-session-key` header. Use `ReceivedAt.Ticks` as a lightweight ordering proxy when the
> stream sequence is not directly accessible through the listener abstraction.

## Provider notes

| Provider | FIFO guarantee scope | Parallel-key support | Notes |
|----------|--------------------|---------------------|-------|
| ServiceBus | Per session ID, within a single session handler | Yes — sessions processed concurrently across handlers | Requires session-enabled entity (`RequiresSession = true`) |
| Kafka | Per partition — all messages with the same key land on the same partition | Yes — different keys route to different partitions | Partition count is fixed at topic creation; set via `WithTopology` |
| SQS | Per `MessageGroupId`, within a FIFO queue | Yes — different groups processed concurrently | Requires `.fifo` queue suffix; deduplication ID set via `DeduplicationKey` |
| RabbitMQ | Per queue — single consumer guarantees FIFO; fan-out breaks ordering | Only with quorum queues + single consumer per queue | `WithQuorumQueue()` for HA; ordering breaks with `x-max-priority` |
| NATS JetStream | Per stream — ordered consumer delivers in stream sequence order | Single ordered consumer per stream; use filter subjects for multi-key | `FilterSubjects` on `NatsJSOrderedConsumerOpts`; set `MaxMsgs` for retention |
