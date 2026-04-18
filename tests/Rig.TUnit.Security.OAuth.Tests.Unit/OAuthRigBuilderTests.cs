using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;
using Rig.TUnit.Security.OAuth.Builder;

namespace Rig.TUnit.Security.OAuth.Tests.Unit;

public sealed class OAuthRigBuilderTests
{
    [Test]
    public async Task OAuthRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(OAuthRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task OAuthRigBuilder_TypeMetadata_InheritsSecurityRigBuilderCrtp()
    {
        var baseType = typeof(OAuthRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SecurityRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(OAuthRigBuilder));
    }

    [Test]
    public async Task OAuthRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("https://rigtunit-oauth/");
        await Assert.That(() => new OAuthRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OAuthRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new OAuthRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OAuthRigBuilder_Issuer_PassesThroughFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("https://rigtunit-oauth/");
        var built = new OAuthRigBuilder(captured!, source);
        await Assert.That(built.Issuer).IsEqualTo("https://rigtunit-oauth/");
    }
}
