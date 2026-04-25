using System.Reflection;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit.Topology;

/// <summary>
/// T023-RED structural fence: IKafkaTopologyBuilder must never declare broker-alien methods.
/// Fails at runtime (type not found) until T023-GREEN adds IKafkaTopologyBuilder.
/// Once added, enforces that Queue/Exchange/Subscription are absent per C-003.
/// </summary>
public sealed class KafkaBuilderCompileFenceTests
{
    private static readonly Assembly KafkaAssembly =
        typeof(Rig.TUnit.Messaging.Kafka.Helpers.KafkaEventSender).Assembly;

    [Test]
    public async Task IKafkaTopologyBuilder_TypeExists_AndDeclaresNoForbiddenMethods()
    {
        // Arrange — fails RED at runtime until T023-GREEN adds IKafkaTopologyBuilder
        var type = KafkaAssembly.GetType(
            "Rig.TUnit.Messaging.Kafka.Topology.IKafkaTopologyBuilder",
            throwOnError: false);

        await Assert.That(type).IsNotNull();

        var forbidden = new[] { "Queue", "Exchange", "Subscription" };
        foreach (var methodName in forbidden)
        {
            var method = type!.GetMethod(methodName);
            await Assert.That(method).IsNull();
        }
    }
}
