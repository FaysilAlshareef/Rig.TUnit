# Rig.TUnit.Messaging.Nats

Testcontainers-backed NATS provider (`nats:2.10-alpine`) with a sealed `NatsRigBuilder`, fluent `UseNats` extension, and `NatsListener` / `NatsEventSender` helpers built on `NATS.Client.Core`.

## Install

```
dotnet add package Rig.TUnit.Messaging.Nats
```

## Example

```csharp
await using var nats = new NatsFixture();
await nats.InitializeAsync();

await using var sender = new NatsEventSender(nats.ConnectionString, subject: "orders");
var listener = new NatsListener(nats.ConnectionString, "orders");
await listener.StartAsync(CancellationToken.None);

await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
await MessageAssert.Within(listener, TimeSpan.FromSeconds(5), expectedCount: 1);
```

## Dependencies

`Rig.TUnit.Messaging`, `Testcontainers.Nats`, `NATS.Client.Core`
