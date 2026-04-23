using Rig.TUnit.Messaging.Sqs.Topology;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit.Topology;

// T031-RED: compile-fail until T031-GREEN adds ISqsTopologyBuilder/ISqsQueueConfig/SqsTopologyBuilder.
public sealed class SqsTopologyBuilderTests
{
    [Test]
    public async Task Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new SqsTopologyBuilder(null!, "https://sqs.us-east-1.amazonaws.com/"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Queue_WithNoConfig_ReturnsBuilderForChaining()
    {
        // Arrange
        ISqsTopologyBuilder builder = new SqsTopologyBuilder(null!, "https://sqs.us-east-1.amazonaws.com/");

        // Act
        var result = builder.Queue("my-queue");

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Queue_WithFifo_ReturnsBuilderForChaining()
    {
        // Arrange
        ISqsTopologyBuilder builder = new SqsTopologyBuilder(null!, "https://sqs.us-east-1.amazonaws.com/");

        // Act
        var result = builder.Queue("orders.fifo", cfg => cfg.WithFifo());

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Queue_WithFifoAndContentBasedDedup_ReturnsBuilderForChaining()
    {
        // Arrange
        ISqsTopologyBuilder builder = new SqsTopologyBuilder(null!, "https://sqs.us-east-1.amazonaws.com/");

        // Act
        var result = builder.Queue("orders.fifo", cfg =>
            cfg.WithFifo(contentBasedDeduplication: true)
               .WithMessageRetentionPeriod(TimeSpan.FromDays(4)));

        // Assert
        await Assert.That(result).IsNotNull();
    }
}
