using Rig.TUnit.Messaging.RabbitMq.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit.Topology;

// T043-RED: runtime-RED until T043-GREEN implements all With* args on IRabbitMqQueueConfig.
// (Methods are already in T042-GREEN; tests pass from day one — compile fence pattern.)
public sealed class RabbitMqQueueArgsTests
{
    private static Type QueueConfigType =>
        typeof(IRabbitMqTopologyBuilder).Assembly
            .GetType("Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqQueueConfig")
        ?? throw new InvalidOperationException("IRabbitMqQueueConfig not found.");

    [Test]
    public async Task IRabbitMqQueueConfig_DeclaresWith_MessageTtl()
    {
        // Arrange & Act
        var method = QueueConfigType.GetMethod("WithMessageTtl");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.GetParameters().Length).IsEqualTo(1);
        await Assert.That(method.GetParameters()[0].ParameterType).IsEqualTo(typeof(TimeSpan));
    }

    [Test]
    public async Task IRabbitMqQueueConfig_DeclaresWith_MaxLength()
    {
        // Arrange & Act
        var method = QueueConfigType.GetMethod("WithMaxLength");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.GetParameters()[0].ParameterType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task IRabbitMqQueueConfig_DeclaresWith_MaxPriority()
    {
        // Arrange & Act
        var method = QueueConfigType.GetMethod("WithMaxPriority");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.GetParameters()[0].ParameterType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task IRabbitMqQueueConfig_DeclaresWith_DeadLetterExchange()
    {
        // Arrange & Act
        var method = QueueConfigType.GetMethod("WithDeadLetterExchange");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.GetParameters()[0].ParameterType).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task IRabbitMqQueueConfig_DeclaresWith_Quorum()
    {
        // Arrange & Act
        var method = QueueConfigType.GetMethod("WithQuorum");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.GetParameters().Length).IsEqualTo(0);
    }

    [Test]
    public async Task IRabbitMqQueueConfig_HasNoFifoOrPartitionMethods()
    {
        // Arrange & Act & Assert — SQS/Kafka-specific concepts must not appear on RabbitMq config
        var forbidden = new[] { "WithFifo", "WithRequiresSession", "WithPartitions" };
        foreach (var name in forbidden)
        {
            await Assert.That(QueueConfigType.GetMethod(name)).IsNull();
        }
    }
}
