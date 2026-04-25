using System.Reflection;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.Tests.Unit.Builder;

/// <summary>
/// C-003 regression guard: <see cref="MessagingRigBuilder{TSelf}"/> MUST NOT declare a
/// generic <c>WithTopology</c> method. Per the provider-scoped topology design, every
/// provider-specific <c>{Provider}RigBuilder</c> declares its own strongly-typed
/// <c>WithTopology(Action&lt;I{Provider}TopologyBuilder&gt;)</c>. A generic base-class
/// overload would re-introduce the runtime-unsupported method trap this design was
/// created to prevent.
/// </summary>
public sealed class MessagingRigBuilderNoGenericWithTopologyTests
{
    [Test]
    public async Task MessagingRigBuilder_Base_DoesNotDeclareWithTopology()
    {
        var baseType = typeof(MessagingRigBuilder<>);

        var withTopologyMethods = baseType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => string.Equals(m.Name, "WithTopology", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(withTopologyMethods)
            .IsEmpty()
            .Because(
                "C-003 forbids a generic WithTopology on the base class. Each "
                + "{Provider}RigBuilder must declare its own strongly-typed overload "
                + "against its provider-specific I{Provider}TopologyBuilder.");
    }

    [Test]
    public async Task MessagingRigBuilder_Base_DeclaresAndOnly()
    {
        var baseType = typeof(MessagingRigBuilder<>);

        var declaredMethods = baseType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        await Assert.That(declaredMethods).Contains("And");
    }
}
