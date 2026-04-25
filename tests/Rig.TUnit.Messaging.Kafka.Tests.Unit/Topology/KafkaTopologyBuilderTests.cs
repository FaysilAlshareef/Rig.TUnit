using Rig.TUnit.Messaging.Kafka.Topology;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit.Topology;

// T023-RED: compile-fail until T023-GREEN adds IKafkaTopologyBuilder/IKafkaTopicConfig/KafkaTopologyBuilder.
public sealed class KafkaTopologyBuilderTests
{
    [Test]
    public async Task KafkaTopologyBuilder_NullBootstrap_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.That(() => new KafkaTopologyBuilder(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Topic_WithNoConfig_ReturnsBuilderForChaining()
    {
        // Arrange
        IKafkaTopologyBuilder builder = new KafkaTopologyBuilder("192.0.2.1:9092");

        // Act
        var result = builder.Topic("my-topic");

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Topic_WithPartitions_ReturnsBuilderForChaining()
    {
        // Arrange
        IKafkaTopologyBuilder builder = new KafkaTopologyBuilder("192.0.2.1:9092");

        // Act
        var result = builder.Topic("my-topic", cfg => cfg.WithPartitions(6));

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Topic_WithConfig_ReturnsBuilderForChaining()
    {
        // Arrange
        IKafkaTopologyBuilder builder = new KafkaTopologyBuilder("192.0.2.1:9092");

        // Act
        var result = builder.Topic("my-topic", cfg =>
            cfg.WithPartitions(1).WithConfig("cleanup.policy", "compact"));

        // Assert
        await Assert.That(result).IsNotNull();
    }
}
