using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.KurrentDb.Builder;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

public sealed class UseKurrentDbExtensionsTests
{
    private const string Sample = "esdb://localhost:2113?tls=false";

    [Test]
    public async Task UseKurrentDb_NullRig_Throws()
    {
        var source = RigConnect.FromValue(Sample);
        await Assert.That(() => ((RigBuilder)null!).UseKurrentDb(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKurrentDb_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseKurrentDb(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKurrentDb_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        await Assert.That(() => captured!.UseKurrentDb(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKurrentDb_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        var returned = captured!.UseKurrentDb(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseKurrentDb_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        var calls = 0;
        captured!.UseKurrentDb(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseKurrentDb_ValidArgs_PassesBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        KurrentDbRigBuilder? passed = null;
        captured!.UseKurrentDb(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
