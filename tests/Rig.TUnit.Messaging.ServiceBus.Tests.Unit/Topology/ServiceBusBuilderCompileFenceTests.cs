using System.Reflection;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit.Topology;

/// <summary>
/// T012-RED structural fence: IServiceBusQueueConfig must never declare broker-alien methods.
/// Fails at runtime (type not found) until T012-GREEN adds IServiceBusQueueConfig.
/// Once added, enforces that WithFifo/WithQuorum/WithPartitions/WithSubjects are absent
/// per C-003 (those fluent methods belong to Kafka/RabbitMQ/NATS/SQS providers only).
/// </summary>
public sealed class ServiceBusBuilderCompileFenceTests
{
    private static readonly Assembly SbAssembly =
        typeof(Rig.TUnit.Messaging.ServiceBus.Fixtures.ServiceBusFixture).Assembly;

    [Test]
    public async Task IServiceBusQueueConfig_TypeExists_AndDeclaresNoForbiddenMethods()
    {
        // Arrange — fails RED until T012-GREEN adds IServiceBusQueueConfig
        var type = SbAssembly.GetType(
            "Rig.TUnit.Messaging.ServiceBus.Topology.IServiceBusQueueConfig",
            throwOnError: false);

        await Assert.That(type).IsNotNull();

        var forbidden = new[] { "WithFifo", "WithQuorum", "WithPartitions", "WithSubjects" };
        foreach (var methodName in forbidden)
        {
            var method = type!.GetMethod(methodName);
            await Assert.That(method).IsNull();
        }
    }
}
