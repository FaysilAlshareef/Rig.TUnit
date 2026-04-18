using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.S3.Builder;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

public sealed class UseS3ExtensionsTests
{
    private const string SampleConnectionString = "http://localhost:4566";

    [Test]
    public async Task UseS3_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => ((RigBuilder)null!).UseS3(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseS3_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseS3(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseS3_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => captured!.UseS3(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseS3_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var returned = captured!.UseS3(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseS3_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;
        captured!.UseS3(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseS3_ValidArgs_PassesS3RigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        S3RigBuilder? passed = null;
        captured!.UseS3(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
