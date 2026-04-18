using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Jwt.Builder;

namespace Rig.TUnit.Security.Jwt.Tests.Unit;

public sealed class UseJwtExtensionsTests
{
    private const string SampleIssuer = "rigtunit-test-issuer";

    [Test]
    public async Task UseJwt_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleIssuer);
        await Assert.That(() => ((RigBuilder)null!).UseJwt(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseJwt_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseJwt(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseJwt_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        await Assert.That(() => captured!.UseJwt(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseJwt_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        var returned = captured!.UseJwt(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseJwt_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        var calls = 0;
        captured!.UseJwt(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseJwt_ValidArgs_PassesJwtRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleIssuer);
        JwtRigBuilder? passed = null;
        captured!.UseJwt(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
