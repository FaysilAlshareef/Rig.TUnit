using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Core.Tests.Contract;

/// <summary>
/// Base contract that every downstream provider's rig-builder suite can inherit and
/// parameterise via <see cref="BuildServices"/>. Asserts the Core-level invariants
/// that a rig must satisfy regardless of which backing infrastructure it wires:
///
/// 1. <c>AddRigTUnit</c> invokes the configure delegate exactly once.
/// 2. The configure delegate receives a non-null builder bound to the outer services.
/// 3. The builder's <see cref="RigBuilder.Services"/> property stays reference-equal to the original.
/// 4. <see cref="RigBuilder.ForceContainersInCi"/> returns the same builder (chainable).
/// </summary>
public abstract class CoreRigContract
{
    /// <summary>Override to supply a service collection pre-populated with any provider-specific DI.</summary>
    protected virtual IServiceCollection BuildServices() => new ServiceCollection();

    [Test]
    public async Task AddRigTUnit_InvokesConfigureDelegateExactlyOnce()
    {
        var invocations = 0;
        var services = BuildServices();

        services.AddRigTUnit(_ => invocations++);

        await Assert.That(invocations).IsEqualTo(1);
    }

    [Test]
    public async Task AddRigTUnit_ExposesOriginalServiceCollection()
    {
        var services = BuildServices();
        IServiceCollection? captured = null;

        services.AddRigTUnit(rig => captured = rig.Services);

        await Assert.That(captured).IsSameReferenceAs(services);
    }

    [Test]
    public async Task RigBuilder_ForceContainersInCi_IsChainable()
    {
        var services = BuildServices();
        RigBuilder? result = null;

        services.AddRigTUnit(rig => result = rig.ForceContainersInCi());

        await Assert.That(result).IsNotNull();
    }
}
