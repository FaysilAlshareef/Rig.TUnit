# Rig.TUnit.Messaging.Kafka

Testcontainers-backed Kafka provider (`confluentinc/cp-kafka`) with a sealed `KafkaRigBuilder`, fluent `UseKafka` extension, and `KafkaListener` / `KafkaEventSender` helpers that capture every message delivered to a test subscription.

## Install

```
dotnet add package Rig.TUnit.Messaging.Kafka
```

## Example

```csharp
await using var kafka = new KafkaFixture();
await kafka.InitializeAsync();

await using var sender = new KafkaEventSender(kafka.ConnectionString, topic: "orders");
var listener = new KafkaListener(kafka.ConnectionString, "orders", groupId: "test");
await listener.StartAsync(CancellationToken.None);

await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1);
```

## Dependencies

`Rig.TUnit.Messaging`, `Testcontainers.Kafka`, `Confluent.Kafka`
