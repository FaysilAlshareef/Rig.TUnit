using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Docker.Builder;

namespace Rig.TUnit.Docker.Tests.Unit;

public sealed class UseDockerExtensionsTests
{
    private const string SampleImage = "alpine:3";

    [Test]
    public async Task UseDocker_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleImage);
        await Assert.That(() => ((RigBuilder)null!).UseDocker(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDocker_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseDocker(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDocker_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleImage);
        await Assert.That(() => captured!.UseDocker(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDocker_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleImage);
        var returned = captured!.UseDocker(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseDocker_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleImage);
        var calls = 0;
        captured!.UseDocker(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }
}
