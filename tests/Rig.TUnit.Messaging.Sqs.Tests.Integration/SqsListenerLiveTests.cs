using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration;

/// <summary>
/// FR-035 live container round-trip for <see cref="SqsEventSender"/> → <see cref="SqsListener"/>
/// against LocalStack. Requires Docker.
/// </summary>
public sealed class SqsListenerLiveTests
{
    [Test]
    public async Task Roundtrip_SendOneMessage_ListenerCapturesIt()
    {
        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"rig-test-{Guid.NewGuid():N}";

        var createResp = await fx.Client.CreateQueueAsync(new CreateQueueRequest { QueueName = queueName });
        var queueUrl = createResp.QueueUrl;

        var listener = new SqsListener(fx.Client, queueUrl);
        await listener.StartAsync(CancellationToken.None);

        var sender = new SqsEventSender(fx.Client, queueUrl);
        await sender.SendAsync("{\"orderId\":42}", correlationId: "abc-123");

        await MessageAssert.Within(listener, TimeSpan.FromSeconds(30), expectedCount: 1);

        await listener.DisposeAsync();

        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(1);
    }
}
