# Amazon SQS provider

> Docs in progress — populated as Feature 007 phases land.

## Quick-start

```csharp
await using var fixture = new SqsFixture();
await fixture.InitializeAsync();

var sender = new SqsEventSender(fixture.Client, queueUrl);
var listener = new SqsListener(fixture.Client, queueUrl);

await listener.StartAsync(ct);
await sender.SendAsync("{ \"orderId\": 1 }", ct: ct);
// ...wait, then assert listener.Captured
await listener.StopAsync(ct);
```

## FIFO ordering per group

> Available after Feature 007 Phase 3 (T030-GREEN).

Use `SendContext.SessionKey` to set the `MessageGroupId`. SQS FIFO queues process
messages within the same group in strict FIFO order.

```csharp
var sender = new SqsEventSender(fixture.Client, fifoQueueUrl);
var listener = new SqsListener(fixture.Client, fifoQueueUrl);

await listener.StartAsync(ct);

for (var i = 0; i < 10; i++)
{
    await sender.SendAsync($"msg-{i}", context: new SendContext(SessionKey: "customer-42"), ct: ct);
}

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener, m => m.Attributes["MessageGroupId"], m => long.Parse(m.Attributes["SequenceNumber"]));
```

## Topology via `WithTopology`

> Available after Feature 007 Phase 3 (T031-GREEN).

```csharp
services.AddRigTUnit(rig =>
    rig.UseSqs(RigConnect.FromValue(serviceUrl), sqs =>
        sqs.WithTopology(t =>
            t.Queue("orders.fifo", cfg => cfg
                .WithFifo(contentBasedDeduplication: true)
                .WithMessageRetentionPeriod(TimeSpan.FromDays(4))))));
```

## Isolation key convention

When running parallel tests against the same LocalStack instance, always include a unique
test-run prefix in queue names to avoid cross-test contamination:

```csharp
var queueName = $"orders-{Guid.NewGuid():N}.fifo";
```
