using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.MinIO.Builder;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

public sealed class UseMinIOExtensionsTests
{
    private const string SampleConnectionString = "http://localhost:9000";

    [Test]
    public async Task UseMinIO_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => ((RigBuilder)null!).UseMinIO(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMinIO_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseMinIO(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMinIO_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => captured!.UseMinIO(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMinIO_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var returned = captured!.UseMinIO(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseMinIO_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;
        captured!.UseMinIO(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseMinIO_ValidArgs_PassesMinIORigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        MinIORigBuilder? passed = null;
        captured!.UseMinIO(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
