# Rig.TUnit.Messaging.RabbitMq

Testcontainers-backed RabbitMQ provider (`rabbitmq:3-management`) with a sealed `RabbitMqRigBuilder`, fluent `UseRabbitMq` extension, and `RabbitMqListener` / `RabbitMqEventSender` helpers built on `RabbitMQ.Client` 7.x async APIs.

## Install

```
dotnet add package Rig.TUnit.Messaging.RabbitMq
```

## Example

```csharp
await using var rmq = new RabbitMqFixture();
await rmq.InitializeAsync();

await using var sender = new RabbitMqEventSender(rmq.ConnectionString, queue: "orders");
var listener = new RabbitMqListener(rmq.ConnectionString, "orders");
await listener.StartAsync(CancellationToken.None);

await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
await MessageAssert.Within(listener, TimeSpan.FromSeconds(5), expectedCount: 1);
```

## Dependencies

`Rig.TUnit.Messaging`, `Testcontainers.RabbitMq`, `RabbitMQ.Client` 7.x
