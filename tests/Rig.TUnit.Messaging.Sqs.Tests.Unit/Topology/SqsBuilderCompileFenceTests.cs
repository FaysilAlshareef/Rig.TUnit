using System.Reflection;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit.Topology;

/// <summary>
/// T031-RED structural fence: ISqsTopologyBuilder must never declare broker-alien methods.
/// Fails at runtime (type not found) until T031-GREEN adds ISqsTopologyBuilder.
/// Once added, enforces that Topic/Exchange/Stream/Subscription are absent per C-003.
/// </summary>
public sealed class SqsBuilderCompileFenceTests
{
    private static readonly Assembly SqsAssembly =
        typeof(Rig.TUnit.Messaging.Sqs.Helpers.SqsEventSender).Assembly;

    [Test]
    public async Task ISqsTopologyBuilder_TypeExists_AndDeclaresNoForbiddenMethods()
    {
        // Arrange — fails RED at runtime until T031-GREEN adds ISqsTopologyBuilder
        var type = SqsAssembly.GetType(
            "Rig.TUnit.Messaging.Sqs.Topology.ISqsTopologyBuilder",
            throwOnError: false);

        await Assert.That(type).IsNotNull();

        var forbidden = new[] { "Topic", "Exchange", "Stream", "Subscription" };
        foreach (var methodName in forbidden)
        {
            var method = type!.GetMethod(methodName);
            await Assert.That(method).IsNull();
        }
    }
}
