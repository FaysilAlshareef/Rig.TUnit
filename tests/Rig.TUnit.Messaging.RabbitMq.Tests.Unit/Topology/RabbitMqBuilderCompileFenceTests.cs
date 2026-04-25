using System.Reflection;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit.Topology;

// T042-RED: runtime-RED until T042-GREEN adds IRabbitMqTopologyBuilder to the assembly.
public sealed class RabbitMqBuilderCompileFenceTests
{
    private static readonly Assembly Assembly =
        typeof(Rig.TUnit.Messaging.RabbitMq.Helpers.RabbitMqListener).Assembly;

    [Test]
    public async Task IRabbitMqTopologyBuilder_ExistsInAssembly()
    {
        // Arrange
        var type = Assembly.GetType("Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqTopologyBuilder");

        // Assert — RED until T042-GREEN
        await Assert.That(type).IsNotNull();
    }

    [Test]
    public async Task IRabbitMqTopologyBuilder_HasNoSubscriptionMethod()
    {
        // Arrange
        var type = Assembly.GetType("Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqTopologyBuilder");
        if (type is null) return;

        // Assert — provider boundary: no Subscription, Stream on RabbitMq builder
        var forbidden = new[] { "Subscription", "Stream", "Topic", "Session" };
        foreach (var name in forbidden)
        {
            var method = type.GetMethod(name);
            await Assert.That(method).IsNull();
        }
    }

    [Test]
    public async Task IRabbitMqQueueConfig_HasNoFifoOrPartitionMethods()
    {
        // Arrange
        var type = Assembly.GetType("Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqQueueConfig");
        if (type is null) return;

        // Assert — SQS/Kafka-specific concepts must not leak onto RabbitMq config
        var forbidden = new[] { "WithFifo", "WithRequiresSession", "WithPartitions" };
        foreach (var name in forbidden)
        {
            var method = type.GetMethod(name);
            await Assert.That(method).IsNull();
        }
    }
}
