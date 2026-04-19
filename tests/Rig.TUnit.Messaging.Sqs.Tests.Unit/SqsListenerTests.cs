using Amazon.SQS;
using NSubstitute;
using Rig.TUnit.Messaging.Sqs.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

/// <summary>
/// FR-035 guard-only tests for <see cref="SqsListener"/>. Uses an NSubstitute <see cref="IAmazonSQS"/>
/// — the listener's receive-loop only runs when <c>StartAsync</c> is called, so constructor null-guards
/// fire without any AWS call.
/// </summary>
public sealed class SqsListenerTests
{
    [Test]
    public async Task Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        await Assert.That(() => new SqsListener(null!, "https://sqs.example.com/queue"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithNullQueueUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new SqsListener(Substitute.For<IAmazonSQS>(), null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WithWhitespaceQueueUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new SqsListener(Substitute.For<IAmazonSQS>(), "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WithValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new SqsListener(Substitute.For<IAmazonSQS>(), "https://sqs.example.com/queue"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Count_AfterConstruction_IsZero()
    {
        var listener = new SqsListener(Substitute.For<IAmazonSQS>(), "https://sqs.example.com/queue");

        await Assert.That(listener.Count).IsEqualTo(0);
    }
}
