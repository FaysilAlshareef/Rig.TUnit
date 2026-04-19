using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;
using Rig.TUnit.Security.Mtls.Builder;

namespace Rig.TUnit.Security.Mtls.Tests.Unit;

public sealed class MtlsRigBuilderTests
{
    [Test]
    public async Task MtlsRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(MtlsRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task MtlsRigBuilder_TypeMetadata_InheritsSecurityRigBuilderCrtp()
    {
        var baseType = typeof(MtlsRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SecurityRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(MtlsRigBuilder));
    }

    [Test]
    public async Task MtlsRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("thumbprint-1234");
        await Assert.That(() => new MtlsRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MtlsRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new MtlsRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MtlsRigBuilder_Thumbprint_PassesThroughFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("ABC123");
        var built = new MtlsRigBuilder(captured!, source);
        await Assert.That(built.Thumbprint).IsEqualTo("ABC123");
    }
}
