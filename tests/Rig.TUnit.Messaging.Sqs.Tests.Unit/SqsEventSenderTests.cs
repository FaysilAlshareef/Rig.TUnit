using Amazon.SQS;
using NSubstitute;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

public sealed class SqsEventSenderTests
{
    [Test]
    public async Task Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        await Assert.That(() => new SqsEventSender(null!, "https://sqs.example.com/queue"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithNullQueueUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new SqsEventSender(Substitute.For<IAmazonSQS>(), null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WithWhitespaceQueueUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new SqsEventSender(Substitute.For<IAmazonSQS>(), "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WithValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new SqsEventSender(Substitute.For<IAmazonSQS>(), "https://sqs.example.com/queue"))
            .ThrowsNothing();
    }
}
