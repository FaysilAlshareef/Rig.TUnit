using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration.Fifo;

// T033c-RED: compile-fail until T030-GREEN adds SqsEventSender.SendAsync(SendContext).
public sealed class ContentBasedDedupTests
{
    [Test]
    public async Task SendAsync_DuplicateBodyWithinWindow_ReceivedOnce(CancellationToken ct)
    {
        // Arrange — FIFO queue with content-based deduplication enabled
        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"dedup-{Guid.NewGuid():N}.fifo";

        var createResp = await fx.Client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true",
            },
        }, ct);
        var queueUrl = createResp.QueueUrl;

        var sender = new SqsEventSender(fx.Client, queueUrl);
        var listener = new SqsListener(fx.Client, queueUrl);
        await listener.StartAsync(ct);

        // Act — send the same body twice in the same group (should be deduped to one)
        const string body = "duplicate-payload";
        await sender.SendAsync(body, context: new SendContext(SessionKey: "dedup-group"), ct: ct);
        await sender.SendAsync(body, context: new SendContext(SessionKey: "dedup-group"), ct: ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(400));
        while (listener.Count < 1)
        {
            try { if (!await timer.WaitForNextTickAsync(timeoutCts.Token)) break; }
            catch (OperationCanceledException) { break; }
        }

        await listener.StopAsync(ct);

        // Assert — content-based dedup means only one message received within the 5-minute window
        await Assert.That(listener.Count).IsEqualTo(1);
        await Assert.That(listener.Captured.First().Body).IsEqualTo(body);

        await listener.DisposeAsync();
    }
}
