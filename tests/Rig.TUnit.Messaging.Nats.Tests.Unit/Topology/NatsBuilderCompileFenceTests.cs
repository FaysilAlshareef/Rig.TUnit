using System.Reflection;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit.Topology;

// T054-RED: runtime-RED until T054-GREEN ships INatsTopologyBuilder with correct shape.
public sealed class NatsBuilderCompileFenceTests
{
    private static Type TopologyBuilderType =>
        typeof(Rig.TUnit.Messaging.Nats.Builder.NatsRigBuilder).Assembly
            .GetType("Rig.TUnit.Messaging.Nats.Topology.INatsTopologyBuilder")
        ?? throw new InvalidOperationException("INatsTopologyBuilder not found.");

    [Test]
    public async Task INatsTopologyBuilder_HasNoQueueMethod()
    {
        await Assert.That(TopologyBuilderType.GetMethod("Queue")).IsNull();
    }

    [Test]
    public async Task INatsTopologyBuilder_HasNoTopicMethod()
    {
        await Assert.That(TopologyBuilderType.GetMethod("Topic")).IsNull();
    }

    [Test]
    public async Task INatsTopologyBuilder_HasNoExchangeMethod()
    {
        await Assert.That(TopologyBuilderType.GetMethod("Exchange")).IsNull();
    }

    [Test]
    public async Task INatsTopologyBuilder_HasNoSubscriptionMethod()
    {
        await Assert.That(TopologyBuilderType.GetMethod("Subscription")).IsNull();
    }

    [Test]
    public async Task INatsTopologyBuilder_DeclareStreamMethod()
    {
        var method = TopologyBuilderType.GetMethod("Stream");
        await Assert.That(method).IsNotNull();
    }
}
