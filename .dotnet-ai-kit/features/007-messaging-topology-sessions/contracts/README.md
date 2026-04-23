# Contracts — 007-messaging-topology-sessions

C# contracts that this feature introduces to the public API surface. Each file below is a snapshot of the **shape only** — the actual implementation lives in the matching `src/` path. Use these as the source-of-truth signature for PR review and for the `ProviderCompletenessTests` assertion set.

| File | Scope | Ships in |
|------|-------|----------|
| [`base.cs`](base.cs) | Base-library marker and record types (`SendContext`, `ITopologyBuilder`, modified `CapturedMessage<TMessage>`) | Phase 0 |
| [`servicebus.cs`](servicebus.cs) | `IServiceBusTopologyBuilder` + config interfaces + `ServiceBusSessionListener` + extended sender + `RigBuilder` hook | Phase 1 |
| [`kafka.cs`](kafka.cs) | `IKafkaTopologyBuilder` + `IKafkaTopicConfig` + extended sender + `RigBuilder` hook | Phase 2 |
| [`sqs.cs`](sqs.cs) | `ISqsTopologyBuilder` + `ISqsQueueConfig` + extended sender + `RigBuilder` hook | Phase 3 |
| [`rabbitmq.cs`](rabbitmq.cs) | `IRabbitMqTopologyBuilder` + config interfaces + `ExchangeType` enum + extended sender + `RigBuilder` hook | Phase 4 |
| [`nats.cs`](nats.cs) | `INatsTopologyBuilder` + config interfaces + JetStream fixture/sender/listener + `RigBuilder` hook | Phase 5 |

These files are **reference contracts** — they do not compile as a unit (they reference types from Azure / Confluent / RabbitMQ / NATS / AWS SDKs that aren't pulled into this folder). Every signature must match the eventual production code exactly; the implementer uses these as the target shape during RED test authoring.
