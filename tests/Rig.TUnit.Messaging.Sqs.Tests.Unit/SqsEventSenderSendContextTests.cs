using Amazon.SQS;
using Amazon.SQS.Model;
using NSubstitute;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

// T030-RED: compile-fail until T030-GREEN adds SqsEventSender.SendAsync(SendContext, ...).
public sealed class SqsEventSenderSendContextTests
{
    private const string StandardQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/my-queue";
    private const string FifoQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/my-queue.fifo";

    [Test]
    public async Task SendAsync_FifoQueueMissingSessionKey_ThrowsInvalidOperationException(CancellationToken ct)
    {
        // Arrange — FIFO queue requires MessageGroupId (SessionKey)
        var client = Substitute.For<IAmazonSQS>();
        var sender = new SqsEventSender(client, FifoQueueUrl);
        var context = new SendContext();

        // Act & Assert — must throw before issuing a network call
        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task SendAsync_SessionKey_CallsApiWithMessageGroupId(CancellationToken ct)
    {
        // Arrange
        var client = Substitute.For<IAmazonSQS>();
        client.SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(new SendMessageResponse()));
        var sender = new SqsEventSender(client, FifoQueueUrl);
        var context = new SendContext(SessionKey: "group-1");

        // Act
        await sender.SendAsync("body", context: context, ct: ct);

        // Assert — MessageGroupId must match SessionKey
        await client.Received(1).SendMessageAsync(
            Arg.Is<SendMessageRequest>(r => r.MessageGroupId == "group-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_DeduplicationKey_CallsApiWithMessageDeduplicationId(CancellationToken ct)
    {
        // Arrange
        var client = Substitute.For<IAmazonSQS>();
        client.SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(new SendMessageResponse()));
        var sender = new SqsEventSender(client, FifoQueueUrl);
        var context = new SendContext(SessionKey: "group-1", DeduplicationKey: "dedup-42");

        // Act
        await sender.SendAsync("body", context: context, ct: ct);

        // Assert — MessageDeduplicationId must match DeduplicationKey
        await client.Received(1).SendMessageAsync(
            Arg.Is<SendMessageRequest>(r => r.MessageDeduplicationId == "dedup-42"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_StandardQueue_DoesNotSetFifoFields(CancellationToken ct)
    {
        // Arrange — standard queue with SendContext; no FIFO fields should be set
        var client = Substitute.For<IAmazonSQS>();
        client.SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(new SendMessageResponse()));
        var sender = new SqsEventSender(client, StandardQueueUrl);
        var context = new SendContext(SessionKey: "grp");

        // Act — should not throw; SessionKey is silently ignored on standard queues
        await sender.SendAsync("body", context: context, ct: ct);

        // Assert — request went through without FIFO-specific fields
        await client.Received(1).SendMessageAsync(
            Arg.Is<SendMessageRequest>(r => r.MessageGroupId == null),
            Arg.Any<CancellationToken>());
    }
}
