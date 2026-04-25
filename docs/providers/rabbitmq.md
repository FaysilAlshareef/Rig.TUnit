# RabbitMQ Provider

> Rig.TUnit.Messaging.RabbitMq — topic exchange fan-out, DLX routing, priority ordering, quorum queues.

## Quick-start

```csharp
var listener = new RabbitMqListener(fx.ConnectionString, "my-queue");
await listener.StartAsync(ct);

await using var sender = new RabbitMqEventSender(fx.ConnectionString, "my-queue");
await sender.SendAsync("hello world");

await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1, ct);
await listener.DisposeAsync();
```

## Topic Exchange Fan-out

> Full example — T044a

Declare a topic exchange and bind queues to routing patterns using `WithTopology`:

```csharp
builder.WithTopology(t =>
{
    t.Exchange("events", ExchangeType.Topic)
     .BindQueue("user-queue",  "user.*")
     .BindQueue("order-queue", "order.*")
     .BindQueue("stock-queue", "stock.*");
});
await captured.ApplyTopologyAsync(ct);

await sender.SendAsync("body", context: new SendContext(PartitionKey: "user.created"), ct: ct);
```

## DLX on Nack

> Full example — T044b

Declare a dead-letter exchange for rejected messages:

```csharp
builder.WithTopology(t =>
{
    t.Exchange("dlx", ExchangeType.Direct).BindQueue("dlq", "dead");
    t.Queue("main-queue", cfg => cfg.WithDeadLetterExchange("dlx", "dead"));
});
```

## Priority Queue Ordering

> Full example — T044c

Declare a priority queue:

```csharp
builder.WithTopology(t =>
    t.Queue("pri-queue", cfg => cfg.WithMaxPriority(10)));
```

## Quorum Queues

> Full example — T044d

Declare a quorum queue for stronger durability guarantees:

```csharp
builder.WithTopology(t =>
    t.Queue("quorum-queue", cfg => cfg.WithQuorum()));
```
