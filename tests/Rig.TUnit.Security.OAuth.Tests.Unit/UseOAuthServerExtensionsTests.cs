using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.OAuth.Builder;

namespace Rig.TUnit.Security.OAuth.Tests.Unit;

public sealed class UseOAuthServerExtensionsTests
{
    private const string SampleIssuer = "https://rigtunit-oauth/";

    [Test]
    public async Task UseOAuthServer_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleIssuer);
        await Assert.That(() => ((RigBuilder)null!).UseOAuthServer(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOAuthServer_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseOAuthServer(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOAuthServer_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        await Assert.That(() => captured!.UseOAuthServer(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOAuthServer_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        var returned = captured!.UseOAuthServer(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseOAuthServer_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        var calls = 0;
        captured!.UseOAuthServer(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseOAuthServer_ValidArgs_PassesOAuthRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        OAuthRigBuilder? passed = null;
        captured!.UseOAuthServer(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
