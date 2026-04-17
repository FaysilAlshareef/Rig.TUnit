# Rig.TUnit.Messaging.ServiceBus

Microsoft-official ServiceBus emulator provider. Uses `mcr.microsoft.com/azure-messaging/servicebus-emulator` + SQL Edge sidecar (C-001). Requires `AcceptEula=true`.

## Install

```
dotnet add package Rig.TUnit.Messaging.ServiceBus
```

## Example

```csharp
await using var sb = new ServiceBusFixture();
await sb.InitializeAsync();

await using var sender = new ServiceBusEventSender(new ServiceBusClient(sb.ConnectionString), topic: "orders");
var listener = new ServiceBusListener(new ServiceBusClient(sb.ConnectionString), "orders", "sub");
await listener.StartAsync(CancellationToken.None);

await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
await MessageAssert.Within(listener, TimeSpan.FromSeconds(5), expectedCount: 1);
```

## Dependencies

`Rig.TUnit.Messaging`, `Testcontainers.ServiceBus`, `Azure.Messaging.ServiceBus`
