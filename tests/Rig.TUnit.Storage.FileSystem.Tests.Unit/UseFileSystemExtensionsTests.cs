using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.FileSystem.Builder;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

public sealed class UseFileSystemExtensionsTests
{
    private const string SampleConnectionString = "/tmp/rigtunit-fs";

    [Test]
    public async Task UseFileSystem_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => ((RigBuilder)null!).UseFileSystem(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseFileSystem_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseFileSystem(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseFileSystem_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => captured!.UseFileSystem(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseFileSystem_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var returned = captured!.UseFileSystem(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseFileSystem_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;
        captured!.UseFileSystem(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseFileSystem_ValidArgs_PassesFileSystemRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        FileSystemRigBuilder? passed = null;
        captured!.UseFileSystem(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
