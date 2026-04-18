using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;
using Rig.TUnit.Security.Policies.Builder;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class PolicyRigBuilderTests
{
    [Test]
    public async Task PolicyRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(PolicyRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task PolicyRigBuilder_TypeMetadata_InheritsSecurityRigBuilderCrtp()
    {
        var baseType = typeof(PolicyRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SecurityRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(PolicyRigBuilder));
    }

    [Test]
    public async Task PolicyRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("Test");
        await Assert.That(() => new PolicyRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task PolicyRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new PolicyRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task PolicyRigBuilder_Scheme_PassesThroughFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Bearer");
        var built = new PolicyRigBuilder(captured!, source);
        await Assert.That(built.Scheme).IsEqualTo("Bearer");
    }
}
