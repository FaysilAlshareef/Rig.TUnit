# Topology Builder — Technical Design

**Feature**: 007
**Owner**: Messaging base library + 5 provider packages
**Status**: Draft

---

## Problem

Each provider creates topology differently today:

| Provider | Today | Limitation |
|----------|-------|------------|
| ServiceBus | Declarative JSON (`service-bus-config.json`) at container boot | Static. Cannot create topics per test. No `RequiresSession`, no filters, no DLQ. |
| Kafka | `AdminClient.CreateTopicsAsync` inline in listener | Hardcoded `NumPartitions = 1`, no configs, no ACLs. |
| RabbitMQ | `QueueDeclareAsync` inline in sender | Queue only. No exchange, no binding, no DLX. |
| NATS | Nothing — core pub/sub auto-routes | No JetStream streams exist. No durability. |
| SQS | Nothing in the fixture — relies on LocalStack pre-state | No FIFO queue creation, no DLQ, no attributes. |

Consequence: tests that want a realistic topology (filtered subscriptions, DLQs, bound
exchanges, compacted topics, FIFO queues) cannot be written in the rig.

---

## Goal

A single fluent API — `builder.WithTopology(t => …)` — that every provider exposes on its
`RigBuilder`. Provider mapping is natural, not lowest-common-denominator: the interface
offers a superset and each provider implements the subset that makes sense for it.

---

## Public API — provider-scoped (resolved C-003, 2026-04-23)

**Rule**: compile-time safety over runtime fall-through. If a provider doesn't support a
concept, its interface must not expose it — trying to call `.WithFifo()` on RabbitMQ or
`.Queue()` on Kafka must be a **compiler error**, not a runtime `NotSupportedException`,
and never a silent no-op.

### Base library — marker interface only

```csharp
/// <summary>
/// Marker / application hook implemented by every provider-specific topology builder.
/// Carries no fluent methods — those live on the provider-specific sub-interface.
/// </summary>
public interface ITopologyBuilder
{
    Task ApplyAsync(CancellationToken ct);
}
```

### Provider-specific builder surfaces

Each provider package owns one builder interface and one config interface per concept it
supports. Unsupported concepts are **absent** — no stubs, no throws, no no-ops.

```csharp
// src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusTopologyBuilder.cs
public interface IServiceBusTopologyBuilder : ITopologyBuilder
{
    IServiceBusTopologyBuilder Topic(string name, Action<IServiceBusTopicConfig>? configure = null);
    IServiceBusTopologyBuilder Subscription(string topic, string name, Action<IServiceBusSubscriptionConfig>? configure = null);
    IServiceBusTopologyBuilder Queue(string name, Action<IServiceBusQueueConfig>? configure = null);
}

public interface IServiceBusQueueConfig
{
    IServiceBusQueueConfig WithRequiresSession(bool required = true);
    IServiceBusQueueConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusQueueConfig WithMaxDeliveryCount(int count);
    IServiceBusQueueConfig WithDeadLetter(string topicOrQueue);
}

// src/Rig.TUnit.Messaging.Kafka/Topology/IKafkaTopologyBuilder.cs
public interface IKafkaTopologyBuilder : ITopologyBuilder
{
    IKafkaTopologyBuilder Topic(string name, Action<IKafkaTopicConfig>? configure = null);
    // No Queue/Exchange/Subscription — compile error to attempt
}

public interface IKafkaTopicConfig
{
    IKafkaTopicConfig WithPartitions(int count);
    IKafkaTopicConfig WithReplicationFactor(short factor);
    IKafkaTopicConfig WithConfig(string key, string value);   // cleanup.policy, retention.ms, etc.
}

// src/Rig.TUnit.Messaging.RabbitMq/Topology/IRabbitMqTopologyBuilder.cs
public interface IRabbitMqTopologyBuilder : ITopologyBuilder
{
    IRabbitMqTopologyBuilder Exchange(string name, ExchangeType type, Action<IRabbitMqExchangeConfig>? configure = null);
    IRabbitMqTopologyBuilder Queue(string name, Action<IRabbitMqQueueConfig>? configure = null);
    IRabbitMqTopologyBuilder Binding(string exchange, string queue, string routingKey);
}

public interface IRabbitMqQueueConfig
{
    IRabbitMqQueueConfig WithMessageTtl(TimeSpan ttl);
    IRabbitMqQueueConfig WithMaxLength(int count);
    IRabbitMqQueueConfig WithMaxPriority(byte max);
    IRabbitMqQueueConfig WithDeadLetterExchange(string exchange, string? routingKey = null);
    IRabbitMqQueueConfig WithQuorum();
}

// src/Rig.TUnit.Messaging.Nats/Topology/INatsTopologyBuilder.cs
public interface INatsTopologyBuilder : ITopologyBuilder
{
    INatsTopologyBuilder Stream(string name, Action<INatsStreamConfig>? configure = null);
    INatsTopologyBuilder Consumer(string stream, string name, Action<INatsConsumerConfig>? configure = null);
}

public interface INatsStreamConfig
{
    INatsStreamConfig WithSubjects(params string[] subjects);
    INatsStreamConfig WithRetention(RetentionPolicy policy);
    INatsStreamConfig WithMaxMessages(long max);
}

// src/Rig.TUnit.Messaging.Sqs/Topology/ISqsTopologyBuilder.cs
public interface ISqsTopologyBuilder : ITopologyBuilder
{
    ISqsTopologyBuilder Queue(string name, Action<ISqsQueueConfig>? configure = null);
}

public interface ISqsQueueConfig
{
    ISqsQueueConfig WithFifo(bool contentBasedDeduplication = false);
    ISqsQueueConfig WithVisibilityTimeout(TimeSpan timeout);
    ISqsQueueConfig WithDeadLetter(string queue, int maxReceiveCount);
    ISqsQueueConfig WithMessageRetentionPeriod(TimeSpan period);
}
```

### Hooks on each RigBuilder

```csharp
public sealed class ServiceBusRigBuilder : MessagingRigBuilder<ServiceBusRigBuilder>
{
    public ServiceBusRigBuilder WithTopology(Action<IServiceBusTopologyBuilder> configure) { … }
}

public sealed class KafkaRigBuilder : MessagingRigBuilder<KafkaRigBuilder>
{
    public KafkaRigBuilder WithTopology(Action<IKafkaTopologyBuilder> configure) { … }
}
// …analogous Rabbit, NATS, SQS.
```

The parity test (NFR-C4) asserts **presence** of a `WithTopology` method on every
`{Provider}RigBuilder` whose single parameter implements `ITopologyBuilder`. It does not
constrain the specific parameter type — that is intentional, because each provider's
surface is legitimately different.

### What this rules out

- No `IQueueConfig` shared interface with every provider's method stapled on.
- No `ITopologyBuilder.Queue(...)` method on the base interface that Kafka / NATS would
  have to throw or no-op on.
- No runtime `NotSupportedException` for "this provider doesn't have queues / exchanges
  / sessions". Unsupported = compiler error.
- No `Log.Debug("…ignored on this provider")` — if the code reached that line, the type
  system should have stopped the developer five keystrokes earlier.

---

## Per-provider mapping

### ServiceBus

```csharp
// Maps to Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient
t.Topic("orders", c => c.WithDefaultMessageTimeToLive(TimeSpan.FromHours(1)))
 .Subscription("orders", "shipping", s => s
     .WithRequiresSession()
     .WithDeadLetter("orders-dlq")
     .WithRule(new SqlRuleFilter("Region='EU'")))
 .Queue("payments-dlq");
```

| Fluent call | SDK call |
|---|---|
| `.Topic(name, cfg)` | `CreateTopicAsync(CreateTopicOptions)` |
| `.Subscription(topic, name, cfg)` | `CreateSubscriptionAsync(CreateSubscriptionOptions)` — honours `RequiresSession`, `DefaultMessageTimeToLive`, `DeadLetteringOnMessageExpiration`, `LockDuration`, `MaxDeliveryCount` |
| `.WithRule(filter)` | `CreateRuleAsync(CreateRuleOptions)` |
| `.Queue(name, cfg)` | `CreateQueueAsync(CreateQueueOptions)` |

Requires `Azure.Messaging.ServiceBus` ≥ 7.20.1 for emulator admin-client support.

### Kafka

```csharp
t.Topic("orders", c => c
    .WithPartitions(6)
    .WithReplicationFactor(1)
    .WithConfig("retention.ms", "86400000")
    .WithConfig("cleanup.policy", "compact"));
```

| Fluent call | SDK call |
|---|---|
| `.Topic(name, cfg)` | `AdminClient.CreateTopicsAsync([new TopicSpecification { NumPartitions, ReplicationFactor, Configs }])` |
| `.Queue(...)` | **throws** `NotSupportedException` — Kafka has no queues |
| `.Exchange(...)` | **throws** |

### RabbitMQ

```csharp
t.Exchange("orders", ExchangeType.Topic)
 .Queue("eu-orders", c => c
     .WithDeadLetter("orders-dlq")
     .WithMessageTtl(TimeSpan.FromMinutes(5)))
 .Binding("orders", "eu-orders", "orders.eu.*")
 .Exchange("orders-dlq", ExchangeType.Fanout)
 .Queue("dlq-store")
 .Binding("orders-dlq", "dlq-store", "");
```

| Fluent call | SDK call |
|---|---|
| `.Exchange(name, type)` | `IChannel.ExchangeDeclareAsync(name, type, durable, autoDelete, args)` |
| `.Queue(name, cfg)` | `IChannel.QueueDeclareAsync(name, durable, exclusive, autoDelete, args)` — args carries DLX, TTL, priority, quorum |
| `.Binding(exchange, queue, routingKey)` | `IChannel.QueueBindAsync(queue, exchange, routingKey)` |
| `.Topic(...)` | alias for `.Exchange(name, ExchangeType.Topic)` |
| `.Subscription(...)` | **throws** |

### NATS

```csharp
t.Stream("ORDERS", c => c
    .WithSubjects("orders.>")
    .WithRetention(RetentionPolicy.Limits)
    .WithMaxMessages(10_000))
 .Consumer("ORDERS", "shipping", c => c
    .WithFilterSubjects("orders.eu.*", "orders.us.*")
    .WithDeliverPolicy(DeliverPolicy.All));
```

| Fluent call | SDK call |
|---|---|
| `.Stream(name, cfg)` | `INatsJSContext.CreateStreamAsync(StreamConfig)` |
| `.Consumer(stream, name, cfg)` | `INatsJSContext.CreateConsumerAsync(stream, ConsumerConfig)` |
| `.Queue(...) / .Topic(...) / .Exchange(...)` | **throws** — use `Stream` / `Consumer` |

Requires the `NATS.Client.JetStream` NuGet (new dependency, Nats package only).

### SQS

```csharp
t.Queue("orders", c => c
    .WithFifo(contentBasedDeduplication: true)
    .WithMessageTtl(TimeSpan.FromDays(4))
    .WithDeadLetter("orders-dlq", maxReceiveCount: 5))
 .Queue("orders-dlq");
```

| Fluent call | SDK call |
|---|---|
| `.Queue(name, cfg)` | `IAmazonSQS.CreateQueueAsync(CreateQueueRequest { QueueName, Attributes })` where `Attributes` carries `FifoQueue`, `ContentBasedDeduplication`, `MessageRetentionPeriod`, `RedrivePolicy` |
| `.Topic(...)` | **throws** — SNS integration is out of scope |

`.WithFifo()` appends `.fifo` suffix to the queue name if missing, per AWS rule.

---

## Execution model

When `builder.WithTopology(…)` is called, the builder records the declarations. On
`BuildAsync` (after the container is up and a connection string is available), the
provider's `ITopologyApplier` materialises every declaration via the SDK admin surface.

```
RigBuilder.WithTopology(Action<ITopologyBuilder>)
    → stores List<TopologyOp>
    → BuildAsync()
        → container start
        → ITopologyApplier.ApplyAsync(ops, connectionString, ct)
        → fixture ready
```

Topology application is **idempotent** (every declaration is "create if not exists"), so
tests re-running on the same shared container behave correctly.

---

## Isolation & per-test topology

Topology declared at the fixture level is shared across tests. For per-test topology:

```csharp
[Test]
public async Task T1(ServiceBusFixture fx)
{
    await fx.Topology.Subscription("orders", $"sub-{fx.IsolationKey}",
        s => s.WithRequiresSession()).ApplyAsync();
    // ...
}
```

`fx.Topology` exposes the applier at runtime; topology ops go through the same idempotent
path. `IsolationKey` prefix prevents bleed between parallel tests (already the pattern in
the rig — see [MessagingFixtureBase](../../src/Rig.TUnit.Messaging/Fixtures/MessagingFixtureBase.cs)).

---

## Relationship to existing JSON config (ServiceBus)

The `service-bus-config.json` file is **not removed**. The emulator still needs a
namespace at startup. But:

- Current JSON declares topics + subscriptions for every test.
- After Feature 007: JSON declares only the namespace (`sbemulatorns`), nothing else.
  All topics, subscriptions, rules, and DLQs are created via `ServiceBusAdministrationClient`
  at test setup.
- Benefit: tests no longer compete for fixed subscription names — per-test subscriptions
  via `IsolationKey` become practical.

---

## Testing strategy

- **Unit (per provider)**: mock the SDK admin client, verify the correct `Create…` call
  is made with the expected options for every fluent combination.
- **Integration (per provider)**: run against the real container, declare a non-trivial
  topology (e.g., topic + subscription + DLQ + session-required + SQL filter for
  ServiceBus), send and assert.
- **Architecture test**: every `{Provider}RigBuilder` must expose `WithTopology` and return
  the correct concrete `ITopologyBuilder`. Reuses the provider-completeness test pattern
  from `planning/provider-consistency-remediation`.

---

## Non-goals

- A cross-provider topology DSL (like Terraform for queues). Each provider gets its own
  `ITopologyBuilder` implementation; the *interface* is shared, the *fluent chain* is
  provider-native.
- Infrastructure-as-code replacement. This is a **test-rig** feature; it does not target
  production topology management.
- Migration tools to convert `service-bus-config.json` to code. Manual, one-time.
