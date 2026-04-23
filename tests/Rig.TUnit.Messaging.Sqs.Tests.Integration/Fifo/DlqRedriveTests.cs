using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration.Fifo;

// T033b-RED: compile-fail until T030-GREEN adds SqsEventSender.SendAsync(SendContext).
public sealed class DlqRedriveTests
{
    [Test]
    public async Task SendAsync_MessageExceedsMaxReceiveCount_MovesToDlq(CancellationToken ct)
    {
        // Arrange — FIFO source queue with maxReceiveCount=1, backed by a FIFO DLQ
        var fx = await SharedSqsFixture.GetAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var dlqName = $"dlq-redrive-{suffix}.fifo";
        var srcName = $"src-redrive-{suffix}.fifo";

        var dlqResp = await fx.Client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = dlqName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true",
            },
        }, ct);
        var dlqArn = (await fx.Client.GetQueueAttributesAsync(
            dlqResp.QueueUrl, ["QueueArn"], ct)).Attributes["QueueArn"];

        var srcResp = await fx.Client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = srcName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true",
                ["RedrivePolicy"] = $"{{\"maxReceiveCount\":\"1\",\"deadLetterTargetArn\":\"{dlqArn}\"}}",
            },
        }, ct);
        var srcUrl = srcResp.QueueUrl;
        var dlqUrl = dlqResp.QueueUrl;

        var sender = new SqsEventSender(fx.Client, srcUrl);
        var dlqListener = new SqsListener(fx.Client, dlqUrl);

        // Act — send one message and receive without deleting (to trigger max-receive-count)
        await sender.SendAsync(
            "fail-me",
            context: new SendContext(SessionKey: "redrive-group"),
            ct: ct);

        // Receive the message but do NOT delete it — simulating a processing failure
        var receiveResp = await fx.Client.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = srcUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5,
            VisibilityTimeout = 0,
        }, ct);

        // After MaxReceiveCount exhausted, the message moves to DLQ on next receive
        await dlqListener.StartAsync(ct);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (dlqListener.Count < 1)
        {
            try { if (!await timer.WaitForNextTickAsync(timeoutCts.Token)) break; }
            catch (OperationCanceledException) { break; }
        }

        await dlqListener.StopAsync(ct);

        // Assert — message should have ended up on the DLQ
        await Assert.That(dlqListener.Count).IsGreaterThanOrEqualTo(1);

        await dlqListener.DisposeAsync();
    }
}
