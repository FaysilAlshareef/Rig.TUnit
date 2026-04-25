using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration.Fifo;

// T033a-RED: compile-fail until T030-GREEN adds SqsEventSender.SendAsync(SendContext).
public sealed class FifoOrderingTests
{
    [Test]
    public async Task SendAsync_5GroupsAcrossFifoQueue_PerGroupMonotonicOrderingPreserved(CancellationToken ct)
    {
        // Arrange — FIFO queue, 5 groups × 10 messages
        const int groupCount = 5;
        const int messagesPerGroup = 10;
        var groups = Enumerable.Range(0, groupCount).Select(i => $"group-{i}").ToArray();

        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"fifo-order-{Guid.NewGuid():N}.fifo";

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

        // Act — interleaved sends across all groups
        for (var i = 0; i < messagesPerGroup; i++)
        {
            foreach (var group in groups)
            {
                await sender.SendAsync(
                    $"msg-{group}-{i}",
                    context: new SendContext(SessionKey: group),
                    ct: ct);
            }
        }

        var totalExpected = groupCount * messagesPerGroup;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (listener.Count < totalExpected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(300, ct);
        }

        await listener.StopAsync(ct);

        // Assert — per-group FIFO ordering preserved; SequenceNumber is monotonic
        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(totalExpected);
        OrderingAssert.PerKeyMonotonic(
            listener,
            m => m.Attributes.TryGetValue("MessageGroupId", out var g) ? g : "",
            m => long.TryParse(m.Attributes.GetValueOrDefault("SequenceNumber"), out var s) ? s : 0);

        await listener.DisposeAsync();
    }
}
