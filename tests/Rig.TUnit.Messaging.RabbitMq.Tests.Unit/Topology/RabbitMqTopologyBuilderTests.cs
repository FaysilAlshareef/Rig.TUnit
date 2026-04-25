using Rig.TUnit.Messaging.RabbitMq.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit.Topology;

// T042-RED: CS0234/CS0246 compile-fail until T042-GREEN adds IRabbitMqTopologyBuilder/RabbitMqTopologyBuilder.
public sealed class RabbitMqTopologyBuilderTests
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Test]
    public async Task Exchange_WithTopicType_ReturnsBuilderForChaining()
    {
        // Arrange
        IRabbitMqTopologyBuilder builder = new RabbitMqTopologyBuilder(OfflineUri);

        // Act
        var result = builder.Exchange("events", ExchangeType.Topic);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Queue_WithNoConfig_ReturnsBuilderForChaining()
    {
        // Arrange
        IRabbitMqTopologyBuilder builder = new RabbitMqTopologyBuilder(OfflineUri);

        // Act
        var result = builder.Queue("my-queue");

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Exchange_NullName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new RabbitMqTopologyBuilder(OfflineUri).Exchange(null!, ExchangeType.Direct))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Queue_NullName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new RabbitMqTopologyBuilder(OfflineUri).Queue(null!))
            .Throws<ArgumentException>();
    }
}
