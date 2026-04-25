using System.Reflection;
using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.Tests.Unit.Topology;

/// <summary>
/// C-003 regression guard: <see cref="ITopologyBuilder"/> is a marker interface. Per the
/// provider-scoped design, it MUST declare exactly one method (<c>ApplyAsync</c>) — fluent
/// builder verbs live on provider-specific sub-interfaces (e.g., <c>IServiceBusTopologyBuilder</c>).
/// If this test fails, a generic builder method has crept back onto the base contract.
/// </summary>
public sealed class ITopologyBuilderContractTests
{
    [Test]
    public async Task ITopologyBuilder_DeclaresOnlyApplyAsync()
    {
        var declaredMethods = typeof(ITopologyBuilder)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        await Assert.That(declaredMethods.Length).IsEqualTo(1);
        await Assert.That(declaredMethods[0]).IsEqualTo("ApplyAsync");
    }

    [Test]
    public async Task ITopologyBuilder_ApplyAsync_AcceptsCancellationToken()
    {
        var applyAsync = typeof(ITopologyBuilder).GetMethod(
            "ApplyAsync",
            BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(applyAsync).IsNotNull();
        var parameters = applyAsync!.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(1);
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task ITopologyBuilder_ApplyAsync_ReturnsTask()
    {
        var applyAsync = typeof(ITopologyBuilder).GetMethod(
            "ApplyAsync",
            BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(applyAsync).IsNotNull();
        await Assert.That(applyAsync!.ReturnType).IsEqualTo(typeof(Task));
    }
}
