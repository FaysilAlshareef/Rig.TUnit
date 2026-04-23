# Data Model — 007-messaging-topology-sessions

**Generated**: 2026-04-23
**Purpose**: enumerate every new public type this feature adds, per package, with its shape, invariants, and mapping to the underlying SDK. This is the concrete form of the "new public types" column of the coverage plan.

---

## Base library — `src/Rig.TUnit.Messaging`

### `SendContext` (new record) · phase 0 · T000

```csharp
namespace Rig.TUnit.Messaging.Helpers;

/// <summary>
/// Optional per-message routing hints carried alongside <see cref="EventSenderBase.BuildHeaders"/>.
/// Each provider maps the populated fields to its native ordering primitive.
/// </summary>
/// <param name="SessionKey">Per-session ordering key — maps to ServiceBus <c>SessionId</c> /
///   SQS <c>MessageGroupId</c>. On Kafka, folds to <see cref="PartitionKey"/> when that is null.</param>
/// <param name="PartitionKey">Per-partition ordering key — maps to Kafka <c>Message.Key</c> /
///   ServiceBus <c>PartitionKey</c> / RabbitMQ routing-key. Must equal <see cref="SessionKey"/> on
///   session-enabled ServiceBus entities.</param>
/// <param name="DeduplicationKey">Broker-level duplicate-detection key — maps to SQS
///   <c>MessageDeduplicationId</c> / NATS JetStream <c>Nats-Msg-Id</c> / ServiceBus <c>MessageId</c>
///   (when duplicate-detection is enabled on the entity).</param>
public readonly record struct SendContext(
    string? SessionKey = null,
    string? PartitionKey = null,
    string? DeduplicationKey = null);
```

**Invariants**:
- All three fields default to `null`. `default(SendContext)` is the empty context.
- Immutable value type; safe to pass by value to async overloads.

### `ITopologyBuilder` (new marker) · phase 0 · T001

```csharp
namespace Rig.TUnit.Messaging.Topology;

/// <summary>
/// Marker / application hook implemented by every provider-specific topology builder.
/// Per C-003 carries no fluent methods — those live on the provider-specific sub-interface
/// (<c>IServiceBusTopologyBuilder</c>, <c>IKafkaTopologyBuilder</c>, …).
/// </summary>
public interface ITopologyBuilder
{
    /// <summary>
    /// Applies every declaration recorded on the provider-specific builder to the broker,
    /// idempotently (create-if-not-exists). Called by the rig's <c>BuildAsync</c> pipeline.
    /// </summary>
    Task ApplyAsync(CancellationToken ct);
}
```

**Invariant**: this interface MUST NOT declare any other methods. T001's RED test is the regression guard.

### `CapturedMessage<TMessage>` (modified) · phase 0 · T000

```csharp
namespace Rig.TUnit.Messaging.Helpers;

public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string Body,                     // was string? — narrowed per C-001
    string? CorrelationId,
    string? SessionKey = null);      // new trailing optional per C-001
```

**Migration**: every provider's listener that previously passed `null` for `Body` now coerces to `""` at the `Record(...)` call site. One-line change per listener.

### `MessagingRigBuilder<TSelf>` (no change) · phase 0 · T002

Per C-003, the base class **does not** declare a generic `WithTopology` method. Each `{Provider}RigBuilder` declares its own strongly-typed `WithTopology(Action<I{Provider}TopologyBuilder>)` in its own phase. T002 lands only a regression-guard unit test.

---

## ServiceBus — `src/Rig.TUnit.Messaging.ServiceBus` · Phase 1

### `IServiceBusTopologyBuilder` · T012

```csharp
namespace Rig.TUnit.Messaging.ServiceBus.Topology;

public interface IServiceBusTopologyBuilder : ITopologyBuilder
{
    IServiceBusTopologyBuilder Topic(string name, Action<IServiceBusTopicConfig>? configure = null);
    IServiceBusTopologyBuilder Subscription(string topic, string name, Action<IServiceBusSubscriptionConfig>? configure = null);
    IServiceBusTopologyBuilder Queue(string name, Action<IServiceBusQueueConfig>? configure = null);
}

public interface IServiceBusTopicConfig
{
    IServiceBusTopicConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusTopicConfig WithEnablePartitioning(bool enabled = true);
    IServiceBusTopicConfig WithRequiresDuplicateDetection(bool enabled = true);
}

public interface IServiceBusSubscriptionConfig
{
    IServiceBusSubscriptionConfig WithRequiresSession(bool required = true);
    IServiceBusSubscriptionConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusSubscriptionConfig WithLockDuration(TimeSpan lockDuration);
    IServiceBusSubscriptionConfig WithMaxDeliveryCount(int count);
    IServiceBusSubscriptionConfig WithDeadLetter(string topicOrQueue);
    IServiceBusSubscriptionConfig WithRule(string name, SqlRuleFilter filter);
}

public interface IServiceBusQueueConfig
{
    IServiceBusQueueConfig WithRequiresSession(bool required = true);
    IServiceBusQueueConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusQueueConfig WithLockDuration(TimeSpan lockDuration);
    IServiceBusQueueConfig WithMaxDeliveryCount(int count);
    IServiceBusQueueConfig WithDeadLetter(string topicOrQueue);
}
```

**Compile-fence (T013 test)**: `IServiceBusQueueConfig` MUST NOT declare `.WithFifo`, `.WithQuorum`, `.WithPartitions`, `.WithSubjects` (Kafka / Rabbit / NATS / SQS-exclusive).

### Sealed impls · T012

- `ServiceBusTopologyBuilder : IServiceBusTopologyBuilder` — records declarations; applies via `ServiceBusAdministrationHelper` on `ApplyAsync`.
- `ServiceBusAdministrationHelper` — wraps `ServiceBusAdministrationClient`; idempotent `CreateTopicAsync` / `CreateSubscriptionAsync` / `CreateRuleAsync` / `CreateQueueAsync`.

### `ServiceBusSessionListener` · T011

```csharp
namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

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

    public override Task StartAsync(CancellationToken ct);
    public override Task StopAsync(CancellationToken ct);
    public ValueTask DisposeAsync();
}
```

Uses `ServiceBusClient.CreateSessionProcessor`. Populates `CapturedMessage.SessionKey = args.SessionId`.

### `ServiceBusEventSender` (extended) · T010

New overload:

```csharp
public Task SendAsync(
    string body,
    SendContext context,
    string? correlationId = null,
    string? causationId = null,
    string? traceparent = null,
    IReadOnlyDictionary<string, string>? additionalHeaders = null,
    CancellationToken ct = default);
```

Mapping:
- `context.SessionKey` → `ServiceBusMessage.SessionId`
- `context.PartitionKey` → `ServiceBusMessage.PartitionKey` (must equal `SessionId` when both set — throws `InvalidOperationException` on mismatch)
- `context.DeduplicationKey` → `ServiceBusMessage.MessageId` (requires duplicate-detection-enabled entity)

### `ServiceBusRigBuilder` (extended) · T013

```csharp
public ServiceBusRigBuilder WithTopology(Action<IServiceBusTopologyBuilder> configure);
```

Returns `this` for chain continuation. Records topology declarations against the builder for materialisation during `BuildAsync`.

---

## Kafka — `src/Rig.TUnit.Messaging.Kafka` · Phase 2

### `IKafkaTopologyBuilder` + configs · T023

```csharp
namespace Rig.TUnit.Messaging.Kafka.Topology;

public interface IKafkaTopologyBuilder : ITopologyBuilder
{
    IKafkaTopologyBuilder Topic(string name, Action<IKafkaTopicConfig>? configure = null);
    // No Queue / Exchange / Subscription — compile error to attempt (C-003)
}

public interface IKafkaTopicConfig
{
    IKafkaTopicConfig WithPartitions(int count);                  // default 1
    IKafkaTopicConfig WithReplicationFactor(short factor);        // default 1
    IKafkaTopicConfig WithConfig(string key, string value);       // retention.ms, cleanup.policy, …
}
```

**Compile-fence (T023 test)**: `IKafkaTopologyBuilder.GetMethods()` MUST NOT include `Queue` / `Exchange` / `Subscription`.

### `KafkaEventSender` (extended) · T020

Maps `Message.Key = context.PartitionKey ?? context.SessionKey ?? correlationId ?? Guid.NewGuid().ToString()`. Fixes the existing correlation-id conflation at [`KafkaEventSender.cs:34`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34).

### `KafkaFixtureOptions.DefaultPartitions` · T021

```csharp
[Range(1, 200)]
public int DefaultPartitions { get; init; } = 1;
```

### `KafkaListener.Assign(int partition)` · T024

Optional pinned-partition helper; exposes `IConsumer.Assign(new TopicPartition(topic, partition))`.

### `KafkaRigBuilder.WithTopology` · T023

```csharp
public KafkaRigBuilder WithTopology(Action<IKafkaTopologyBuilder> configure);
```

---

## SQS — `src/Rig.TUnit.Messaging.Sqs` · Phase 3

### `ISqsTopologyBuilder` + `ISqsQueueConfig` · T031

```csharp
namespace Rig.TUnit.Messaging.Sqs.Topology;

public interface ISqsTopologyBuilder : ITopologyBuilder
{
    ISqsTopologyBuilder Queue(string name, Action<ISqsQueueConfig>? configure = null);
    // No Topic (SNS out of scope) / Exchange / Stream / Subscription
}

public interface ISqsQueueConfig
{
    ISqsQueueConfig WithFifo(bool contentBasedDeduplication = false);   // appends .fifo suffix
    ISqsQueueConfig WithVisibilityTimeout(TimeSpan timeout);
    ISqsQueueConfig WithMessageRetentionPeriod(TimeSpan period);
    ISqsQueueConfig WithDeadLetter(string queue, int maxReceiveCount);
}
```

**Compile-fence (T031 test)**: no `.Topic` / `.Exchange` / `.Stream` / `.Subscription` on `ISqsTopologyBuilder`.

### `SqsEventSender` (extended) · T030

New overload maps:
- `context.SessionKey` → `SendMessageRequest.MessageGroupId` (required on FIFO)
- `context.DeduplicationKey` → `SendMessageRequest.MessageDeduplicationId`
- `context.PartitionKey` — ignored (meaningless on SQS)
- Pre-flight validation: if queue URL ends with `.fifo` and `SessionKey` is null, throws `InvalidOperationException` with hint.

### `SqsListener` (extended) · T032

`ReceiveMessageRequest.AttributeNames += ["MessageGroupId", "SequenceNumber"]`. Populates `CapturedMessage.SessionKey` from `MessageGroupId`.

### `SqsRigBuilder.WithTopology` · T031

```csharp
public SqsRigBuilder WithTopology(Action<ISqsTopologyBuilder> configure);
```

---

## RabbitMQ — `src/Rig.TUnit.Messaging.RabbitMq` · Phase 4

### `IRabbitMqTopologyBuilder` + configs · T042

```csharp
namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public interface IRabbitMqTopologyBuilder : ITopologyBuilder
{
    IRabbitMqTopologyBuilder Exchange(string name, ExchangeType type, Action<IRabbitMqExchangeConfig>? configure = null);
    IRabbitMqTopologyBuilder Queue(string name, Action<IRabbitMqQueueConfig>? configure = null);
    IRabbitMqTopologyBuilder Binding(string exchange, string queue, string routingKey);
    // No Subscription / Stream — compile error
}

public interface IRabbitMqExchangeConfig
{
    IRabbitMqExchangeConfig Durable(bool durable = true);
    IRabbitMqExchangeConfig AutoDelete(bool autoDelete = false);
}

public interface IRabbitMqQueueConfig
{
    IRabbitMqQueueConfig Durable(bool durable = true);
    IRabbitMqQueueConfig WithMessageTtl(TimeSpan ttl);
    IRabbitMqQueueConfig WithMaxLength(int count);
    IRabbitMqQueueConfig WithMaxPriority(byte max);
    IRabbitMqQueueConfig WithDeadLetterExchange(string exchange, string? routingKey = null);
    IRabbitMqQueueConfig WithQuorum();
}

public enum ExchangeType
{
    Direct,
    Topic,
    Fanout,
    Headers,
}
```

**Compile-fence (T042 test)**: `IRabbitMqQueueConfig` must not declare `.WithFifo` / `.WithRequiresSession` / `.WithPartitions`.

### `RabbitMqEventSender` (extended) · T040

New overload accepts explicit `exchange` + `routingKey`. Default to `exchange = ""` / `routingKey = _queue` (legacy default-exchange behaviour). Writes `SendContext.PartitionKey` to `x-partition-key` application header so listener can recover key after broker strips routing key.

### `RabbitMqListener` (extended) · T041

- Declares exchange + binding before `BasicConsumeAsync` when configured.
- Reads `x-partition-key` header into `CapturedMessage.SessionKey`.

### `RabbitMqRigBuilder.WithTopology` · T042

```csharp
public RabbitMqRigBuilder WithTopology(Action<IRabbitMqTopologyBuilder> configure);
```

---

## NATS JetStream — `src/Rig.TUnit.Messaging.Nats` · Phase 5

### `INatsTopologyBuilder` + configs · T054

```csharp
namespace Rig.TUnit.Messaging.Nats.Topology;

public interface INatsTopologyBuilder : ITopologyBuilder
{
    INatsTopologyBuilder Stream(string name, Action<INatsStreamConfig>? configure = null);
    INatsTopologyBuilder Consumer(string stream, string name, Action<INatsConsumerConfig>? configure = null);
    // No Queue / Topic / Exchange / Subscription
}

public interface INatsStreamConfig
{
    INatsStreamConfig WithSubjects(params string[] subjects);
    INatsStreamConfig WithRetention(RetentionPolicy policy);
    INatsStreamConfig WithMaxMessages(long max);
    INatsStreamConfig WithStorage(StorageType storage);
}

public interface INatsConsumerConfig
{
    INatsConsumerConfig WithFilterSubjects(params string[] subjects);
    INatsConsumerConfig WithDeliverPolicy(DeliverPolicy policy);
    INatsConsumerConfig WithReplayPolicy(ReplayPolicy policy);
    INatsConsumerConfig WithOrderedConsumer();   // shorthand for DeliverPolicy.All + ReplayPolicy.Instant + FlowControl
}
```

Enums (`RetentionPolicy`, `StorageType`, `DeliverPolicy`, `ReplayPolicy`) re-exported from `NATS.Client.JetStream` or mirrored if the package uses obscure names.

**Compile-fence (T054 test)**: no `.Queue` / `.Topic` / `.Exchange` / `.Subscription` on `INatsTopologyBuilder`.

### `NatsJetStreamFixture` · T051

```csharp
namespace Rig.TUnit.Messaging.Nats.Fixtures;

public sealed class NatsJetStreamFixture : FixtureBase, IAsyncDisposable
{
    public string ConnectionString { get; }
    public INatsJSContext JetStream { get; }
    // Fixture lifecycle identical to NatsFixture; core NATS fixture stays untouched.
}
```

### `NatsJetStreamEventSender` · T052

```csharp
namespace Rig.TUnit.Messaging.Nats.Helpers;

public sealed class NatsJetStreamEventSender : EventSenderBase, IAsyncDisposable
{
    public Task SendAsync(string body, SendContext context, /* headers params */ CancellationToken ct = default);
}
```

Mapping:
- `context.SessionKey` — appended to subject as trailing segment (`orders.{SessionKey}`) OR mapped via `FilterSubjects` at the consumer.
- `context.DeduplicationKey` → `Nats-Msg-Id` header — triggers JetStream server-side dedup.
- Publish via `INatsJSContext.PublishAsync(subject, payload, headers)`.

### `NatsJetStreamListener` · T053

Ordered consumer backed by `INatsJSContext.CreateConsumerAsync` with `ConsumerConfig { DeliverPolicy = All, ReplayPolicy = Instant, FlowControl = true }`. Populates `CapturedMessage.SessionKey` from the subject segment configured by the fixture's `.WithFilterSubjects`.

### `NatsRigBuilder.WithTopology` · T054

```csharp
public NatsRigBuilder WithTopology(Action<INatsTopologyBuilder> configure);
```

---

## Architecture tests — `tests/Rig.TUnit.Architecture.Tests`

### `.parity-coverage.txt` · T003

Format: newline-separated assembly names, no blank lines, no comments.

Phase-0 content: empty file.

Appended (one line per provider phase GREEN commit):

```
Rig.TUnit.Messaging.ServiceBus     ← appended in T013 GREEN
Rig.TUnit.Messaging.Kafka           ← appended in T023 GREEN
Rig.TUnit.Messaging.Sqs             ← appended in T031 GREEN
Rig.TUnit.Messaging.RabbitMq        ← appended in T042 GREEN
Rig.TUnit.Messaging.Nats            ← appended in T054 GREEN
```

### `ProviderCompletenessTests` extension · T003

New assertions (for every assembly listed in `.parity-coverage.txt`):

1. `{Provider}RigBuilder.GetMethods()` contains at least one method named `WithTopology` with exactly one parameter whose type is `Action<T>` where `T` implements `ITopologyBuilder`.
2. `{Provider}EventSender.GetMethods()` contains a `SendAsync` overload whose parameter list includes a `SendContext`.
3. If the provider is in the session-capable set (`ServiceBus`, `Kafka`, `Nats`, `Sqs`), a concrete type named matching `{Provider}(Session|JetStream)Listener` exists and inherits `ListenerBase<>`.

### `DependencyDirectionTests` extension · T050

Asserts `NATS.Client.JetStream` package reference appears **only** in `src/Rig.TUnit.Messaging.Nats/Rig.TUnit.Messaging.Nats.csproj`.

---

## Summary table — new public type count per package

| Package | New public types | New public methods on existing types |
|---------|------------------|---------------------------------------|
| `Rig.TUnit.Messaging` | 2 (`SendContext`, `ITopologyBuilder`) | 1 (`EventSenderBase.BuildHeaders(SendContext,…)`); 1 (`CapturedMessage<TMessage>.SessionKey` property) |
| `Rig.TUnit.Messaging.ServiceBus` | 6 (`ServiceBusSessionListener`, `IServiceBusTopologyBuilder`, `IServiceBusTopicConfig`, `IServiceBusSubscriptionConfig`, `IServiceBusQueueConfig`, `ServiceBusTopologyBuilder` impl) + `ServiceBusAdministrationHelper` | 2 (`ServiceBusEventSender.SendAsync(SendContext,…)`, `ServiceBusRigBuilder.WithTopology`) |
| `Rig.TUnit.Messaging.Kafka` | 3 (`IKafkaTopologyBuilder`, `IKafkaTopicConfig`, `KafkaTopologyBuilder`) | 3 (`KafkaEventSender.SendAsync(SendContext,…)`, `KafkaFixtureOptions.DefaultPartitions`, `KafkaListener.Assign`, `KafkaRigBuilder.WithTopology`) |
| `Rig.TUnit.Messaging.Sqs` | 3 (`ISqsTopologyBuilder`, `ISqsQueueConfig`, `SqsTopologyBuilder`) | 2 (`SqsEventSender.SendAsync(SendContext,…)`, `SqsRigBuilder.WithTopology`) |
| `Rig.TUnit.Messaging.RabbitMq` | 4 (`IRabbitMqTopologyBuilder`, `IRabbitMqExchangeConfig`, `IRabbitMqQueueConfig`, `RabbitMqTopologyBuilder`) + `ExchangeType` enum | 2 (`RabbitMqEventSender.SendAsync(SendContext,…)`, `RabbitMqRigBuilder.WithTopology`) |
| `Rig.TUnit.Messaging.Nats` | 7 (`NatsJetStreamFixture`, `NatsJetStreamEventSender`, `NatsJetStreamListener`, `INatsTopologyBuilder`, `INatsStreamConfig`, `INatsConsumerConfig`, `NatsTopologyBuilder`) | 1 (`NatsRigBuilder.WithTopology`) |
| **Total new public types** | **25 interfaces + impls + fixtures** | 12 new methods / properties |

Every type above ships at 100 % line coverage in its introducing PR (NFR-C1 reviewer rule).
