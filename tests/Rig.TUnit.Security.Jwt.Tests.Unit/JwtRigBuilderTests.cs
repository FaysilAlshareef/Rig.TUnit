using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;
using Rig.TUnit.Security.Jwt.Builder;

namespace Rig.TUnit.Security.Jwt.Tests.Unit;

public sealed class JwtRigBuilderTests
{
    [Test]
    public async Task JwtRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(JwtRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task JwtRigBuilder_TypeMetadata_InheritsSecurityRigBuilderCrtp()
    {
        var baseType = typeof(JwtRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SecurityRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(JwtRigBuilder));
    }

    [Test]
    public async Task JwtRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("rigtunit-jwt-issuer");
        await Assert.That(() => new JwtRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task JwtRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new JwtRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
