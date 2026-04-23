# Research — 007-messaging-topology-sessions

**Generated**: 2026-04-23
**Purpose**: capture the code-base reconnaissance, SDK surface review, and conventions that inform the plan. These are *findings* — not decisions. Decisions live in [spec.md](spec.md) / [plan.md](plan.md).

---

## Codebase scan

### .NET / language

- `global.json`: `sdk.version: 10.0.100`, `rollForward: latestFeature`, `test.runner: Microsoft.Testing.Platform`.
- Therefore C# 14 is available; file-scoped namespaces, primary constructors, collection expressions, required members, `field` keyword are all in-profile.
- `Directory.Packages.props` is the single source of truth for package versions (Central Package Management).

### Test framework

- TUnit is the runner (`[Test]`, `Assert.That(x).IsEqualTo(y)`, `async Task` test methods).
- No xUnit / NUnit / MSTest anywhere in the messaging tree — confirmed by searching for `[Fact]`, `[TestMethod]`, `ClassData`: zero hits.
- All existing messaging tests use `{Method}_{Scenario}_{ExpectedResult}` naming.

### Existing messaging shape (base library)

```
src/Rig.TUnit.Messaging/
├── Assertions/
│   ├── DeadLetterAssert.cs
│   ├── MessageAssert.cs
│   └── OrderingAssert.cs            ← PerKeyMonotonic exists; no signature change needed
├── Builder/
│   └── MessagingRigBuilder.cs       ← abstract base — does NOT currently have WithTopology
├── Contracts/
│   └── IMessagingRig.cs
├── Conventions/
├── EventEnvelope.cs
├── Fixtures/
│   └── MessagingFixtureBase.cs      ← IsolationKey source
├── Helpers/
│   ├── EventSenderBase.cs           ← BuildHeaders extension point (T000 additive)
│   └── ListenerBase.cs              ← CapturedMessage<TMessage> (T000 modifies)
└── README.md
```

### Existing `CapturedMessage<TMessage>` (current shape)

Quoted from [`src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs:27`](../../../src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs:27):

```csharp
public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    string? CorrelationId);
```

Per C-001 the post-feature shape is:

```csharp
public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string Body,                    // was string?
    string? CorrelationId,
    string? SessionKey = null);     // new
```

Every existing listener call site (5 providers) emits `result.Message.Value`, `msg.Body.ToString()`, etc. — coercion to `""` on null is a one-line change each.

### Existing provider shapes (pre-feature)

| Provider | Sender sets | Listener captures | Notable gap |
|----------|------------|-------------------|-------------|
| ServiceBus | `ServiceBusMessage.CorrelationId`, `MessageId`, `ApplicationProperties` | `ServiceBusProcessor` (not session-aware) | No `SessionId` / `PartitionKey` on sender; no session listener. |
| Kafka | `Message.Key = correlationId ?? Guid` ([KafkaEventSender.cs:34](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34)) | `ConsumeResult<string, string>` | Key conflated with correlation ID; topic hardcoded to 1 partition ([KafkaListener.cs:80](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:80)). |
| RabbitMQ | `BasicProperties.CorrelationId`, `MessageId`, `Headers` | Queue-level consumer | `BasicPublishAsync` uses default exchange + queue name ([RabbitMqEventSender.cs:48](../../../src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs:48)); only `QueueDeclareAsync`, no exchange/binding. |
| NATS | Core pub/sub only | Core subscription | No JetStream fixture / sender / listener. |
| SQS | Standard queue attributes + headers via `MessageAttributes` | Standard receive | No `MessageGroupId` / `MessageDeduplicationId`; LocalStack FIFO not exercised. |

### Current Directory.Packages.props messaging lines

```
Azure.Messaging.ServiceBus  7.18.2    ← bump to ≥ 7.20.1 (T014)
Confluent.Kafka             2.6.0     ← stays
RabbitMQ.Client             7.0.0     ← stays
AWSSDK.SQS                  3.7.400   ← stays
NATS.Client.Core            2.5.0     ← stays
                                      ← NATS.Client.JetStream NEW (T050)
```

### Existing provider-parity architecture test

[`tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`](../../../tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs) — 182 lines. Phase 0 T003 extends it:
- reads `.parity-coverage.txt`;
- for every assembly listed there, asserts `{Provider}RigBuilder` has a `WithTopology(Action<TBuilder>)` method where `TBuilder : ITopologyBuilder`;
- sender has a `SendAsync(string, SendContext, …)` overload;
- session-capable providers (ServiceBus, Kafka, NATS JetStream, SQS) declare a session-aware listener.

---

## SDK surface research (per provider)

Each row links the fluent API this feature exposes to the concrete SDK call. Quoted paths refer to the SDK type — verify during implementation against the installed package version.

### ServiceBus — `Azure.Messaging.ServiceBus` 7.20.1

- `ServiceBusAdministrationClient.CreateTopicAsync(CreateTopicOptions)` — supports `DefaultMessageTimeToLive`, `EnablePartitioning`, `RequiresDuplicateDetection`.
- `.CreateSubscriptionAsync(CreateSubscriptionOptions)` — supports `RequiresSession`, `DefaultMessageTimeToLive`, `DeadLetteringOnMessageExpiration`, `LockDuration`, `MaxDeliveryCount`, `ForwardDeadLetteredMessagesTo`.
- `.CreateRuleAsync(topic, subscription, CreateRuleOptions)` — supports `SqlRuleFilter`, `CorrelationRuleFilter`.
- `ServiceBusClient.CreateSessionProcessor(topic, subscription, ServiceBusSessionProcessorOptions)` — required for session-aware consumption.
- Message side: `ServiceBusMessage.SessionId`, `.PartitionKey`, `.MessageId` (for duplicate detection).
- Constraint: on session-enabled entities, `PartitionKey` MUST equal `SessionId` when both set — broker rejects mismatches. T010 pre-flight-validates.

### Kafka — `Confluent.Kafka` 2.6.0

- `AdminClient.CreateTopicsAsync([new TopicSpecification { Name, NumPartitions, ReplicationFactor, Configs }])` — configs include `cleanup.policy`, `retention.ms`, `min.insync.replicas`.
- `AdminClient.DescribeConfigsAsync` — used in T022 test to verify applied configs.
- `Message<string, string>.Key` — the partition key. Distinct from any application-level correlation ID.
- `IConsumer.Assign(TopicPartition)` — pinned-partition consumption (T024).

### RabbitMQ — `RabbitMQ.Client` 7.0.0

- `IChannel.ExchangeDeclareAsync(name, type, durable, autoDelete, arguments)`.
- `IChannel.QueueDeclareAsync(name, durable, exclusive, autoDelete, arguments)` — arguments dictionary carries `x-dead-letter-exchange`, `x-dead-letter-routing-key`, `x-message-ttl`, `x-max-length`, `x-max-priority`, `x-queue-type=quorum`.
- `IChannel.QueueBindAsync(queue, exchange, routingKey, arguments)`.
- `BasicProperties.MessageId` (ad-hoc dedup hint — no broker enforcement).
- Limitation: routing key is stripped from `BasicProperties` at delivery. Listener uses a custom header `x-partition-key` set by sender to recover key (T040/T041).

### NATS JetStream — `NATS.Client.JetStream` (new, ≥ 2.5.0)

- `INatsJSContext.CreateStreamAsync(StreamConfig { Name, Subjects, Retention, MaxMsgs, Storage })`.
- `INatsJSContext.CreateConsumerAsync(stream, ConsumerConfig { Name, FilterSubjects, DeliverPolicy, ReplayPolicy, FlowControl })`.
- `INatsJSContext.PublishAsync(subject, payload, headers)` — `Nats-Msg-Id` header gates server-side dedup within the stream's dedup window.
- Ordered consumer pattern: `ConsumerConfig { DeliverPolicy = All, ReplayPolicy = Instant, FlowControl = true, AckPolicy = Explicit }`.

### SQS — `AWSSDK.SQS` 3.7.400

- `IAmazonSQS.CreateQueueAsync(CreateQueueRequest { QueueName, Attributes })` — attributes include `FifoQueue=true`, `ContentBasedDeduplication=true`, `VisibilityTimeout`, `MessageRetentionPeriod`, `RedrivePolicy` (JSON).
- `SendMessageRequest.MessageGroupId`, `.MessageDeduplicationId` — required on FIFO queue unless `ContentBasedDeduplication=true`.
- Listener: `ReceiveMessageRequest.AttributeNames = new List<string> { "MessageGroupId", "SequenceNumber" }` to recover group + sequence.
- FIFO queue name must end with `.fifo` — builder appends automatically per [Topology-Builder-Design.md §SQS](../../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md).

---

## Coverage baseline (Feature 006 scan run `24712477011`)

These are the **F006 starting-state** numbers (pre-uplift). F006's exit gate closed every row to ≥ 90 line / ≥ 85 branch. By the time Feature 007 Phase 0 starts, **every package below is expected to be at the gate**; the numbers are retained here only to document the size of the gap F006 closed.

- `Rig.TUnit.Messaging` — 30.9 % → ≥ 90 % (F006 T024).
- `Rig.TUnit.Messaging.ServiceBus` — 59.7 % → ≥ 90 % (F006 T033).
- `Rig.TUnit.Messaging.Tests.Contract` — 78.4 % → ≥ 90 % (F006 T039e).
- `Rig.TUnit.Messaging.{Kafka, RabbitMq, Nats, Sqs}` — not listed as below-gate in F006 spec, so already ≥ 90 line at F006 entry and preserved by the exit gate.

Feature 007 assumes Feature 006 exit gates are green on `master` before this branch cuts. If Feature 006 has not landed when Phase 0 starts, Phase 0 cannot merge.

---

## Conventions observed

1. **Namespaces**: `Rig.TUnit.{Family}.{Provider}.{Area}` (e.g. `Rig.TUnit.Messaging.ServiceBus.Helpers`). New `Topology` folders in each provider match this: `Rig.TUnit.Messaging.{Provider}.Topology`.
2. **Class sealing**: all public concrete types in the messaging packages are `sealed`. Applies to every topology builder, listener, fixture this feature adds.
3. **Fixture pattern**: `{Provider}Fixture : FixtureBase, IAsyncDisposable` — `NatsJetStreamFixture` follows the same pattern.
4. **Options pattern**: `{Provider}FixtureOptions` with `public const string SectionName = "{Provider}"` (enforced by existing `ProviderCompletenessTests`). `KafkaFixtureOptions.DefaultPartitions` (T021) extends existing options class.
5. **Cancellation**: every async public method takes `CancellationToken ct = default` as the last parameter.
6. **Parameterisation**: no hardcoded constants in the public surface — FIFO dedup window, lock durations, TTLs all pass through config.
7. **Benchmark project location**: `tests/Rig.TUnit.Benchmarks/` (single shared project). Roadmap path `benchmarks/Rig.TUnit.Messaging.Benchmarks/*` was aspirational — resolved to "extend existing project" during spec drafting (Q-4).

---

## Unresolved technical unknowns

| # | Question | How it surfaces |
|---|----------|-----------------|
| U-1 | Exact emulator capability set on `servicebus-emulator:1.1.2` at the time Phase 1 runs. | T014 capability probe answers this empirically. If any Phase 1 scenario depends on an unsupported op, apply C-004 (skip + document + upstream issue). |
| U-2 | Does `RabbitMQ.Client` 7.0.0 expose `QueueDeclareAsync` arguments dictionary with enough type precision to set `x-queue-type=quorum` without boxing issues? | T043 integration test is the empirical answer. Research confirms the argument accepts `IDictionary<string, object>` — fine. |
| U-3 | Does `NATS.Client.JetStream` package name resolve to a version `≥ 2.5.0` compatible with `NATS.Client.Core 2.5.0`? | Verified via NuGet — JetStream 2.5.x matches Core 2.5.x. T050 pins exact version. |
| U-4 | Does LocalStack FIFO queue support full `ContentBasedDeduplication=true` semantics? | Empirically yes per LocalStack docs; T033c proves it. |

None are blockers — all resolved during their respective phase.

---

## Anti-patterns explicitly rejected

Based on the clarifications (C-000 … C-005) and memory feedback entries:

1. **No `[Obsolete]` aliases** for renamed / narrowed public surface (C-000). The pre-release packages can rename cleanly.
2. **No shared `IQueueConfig` with provider-specific `With…` methods** (C-003). Each provider's config interface declares only what that broker supports.
3. **No runtime `NotSupportedException` for unsupported operations** (C-003). Absence from the interface shape == compile error.
4. **No silent no-op / debug-log for config method non-support** (C-003). Same as above.
5. **No RED-on-master window from T003** (C-005). Progressive enforcement via `.parity-coverage.txt`.
6. **No dual spec files** — spec lives only in `.dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md` (memory: `feedback_spec_home_is_sdd_feature_folder.md`).
7. **No duplicated "additive only / no breaking change" language** in future clauses — C-000 supersedes for pre-release packages.
