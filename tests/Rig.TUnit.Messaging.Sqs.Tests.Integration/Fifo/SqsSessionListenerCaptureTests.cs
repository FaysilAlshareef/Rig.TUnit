using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration.Fifo;

// T032-RED: runtime-fail until T032-GREEN requests AttributeNames + populates SessionKey.
public sealed class SqsSessionListenerCaptureTests
{
    [Test]
    public async Task ReceiveMessage_WithMessageGroupId_PopulatesCapturedMessageSessionKey(CancellationToken ct)
    {
        // Arrange
        const string sessionKey = "capture-group-1";
        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"session-capture-{Guid.NewGuid():N}.fifo";

        var queueResp = await fx.Client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true",
            },
        }, ct);

        var sender = new SqsEventSender(fx.Client, queueResp.QueueUrl);
        await using var listener = new SqsListener(fx.Client, queueResp.QueueUrl);
        await listener.StartAsync(ct);

        // Act
        await sender.SendAsync("hello", new SendContext(SessionKey: sessionKey), ct: ct);

        // Assert
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 1, ct);
        var captured = listener.Captured.First();
        await Assert.That(listener.Count).IsEqualTo(1);
        await Assert.That(captured.SessionKey).IsEqualTo(sessionKey);
    }
}
