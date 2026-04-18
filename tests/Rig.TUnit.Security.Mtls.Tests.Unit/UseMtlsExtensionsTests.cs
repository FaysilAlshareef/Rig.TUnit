using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Mtls.Builder;

namespace Rig.TUnit.Security.Mtls.Tests.Unit;

public sealed class UseMtlsExtensionsTests
{
    private const string SampleThumbprint = "ABC123";

    [Test]
    public async Task UseMtls_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleThumbprint);
        await Assert.That(() => ((RigBuilder)null!).UseMtls(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMtls_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseMtls(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMtls_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleThumbprint);
        await Assert.That(() => captured!.UseMtls(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMtls_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleThumbprint);
        var returned = captured!.UseMtls(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseMtls_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleThumbprint);
        var calls = 0;
        captured!.UseMtls(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseMtls_ValidArgs_PassesMtlsRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleThumbprint);
        MtlsRigBuilder? passed = null;
        captured!.UseMtls(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
