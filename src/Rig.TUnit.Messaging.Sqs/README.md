# Rig.TUnit.Messaging.Sqs

LocalStack-backed Amazon SQS provider (`localstack/localstack:3`) with a sealed `SqsRigBuilder`, fluent `UseSqs` extension, and `SqsListener` / `SqsEventSender` helpers built on the AWSSDK.SQS client.

## Install

```
dotnet add package Rig.TUnit.Messaging.Sqs
```

## Example

```csharp
await using var sqs = new SqsFixture();
await sqs.InitializeAsync();

var queue = await sqs.Client.CreateQueueAsync("orders");
await using var sender = new SqsEventSender(sqs.Client, queue.QueueUrl);
var listener = new SqsListener(sqs.Client, queue.QueueUrl);
await listener.StartAsync(CancellationToken.None);

await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 1);
```

## Dependencies

`Rig.TUnit.Messaging`, `Testcontainers.LocalStack`, `AWSSDK.SQS`
