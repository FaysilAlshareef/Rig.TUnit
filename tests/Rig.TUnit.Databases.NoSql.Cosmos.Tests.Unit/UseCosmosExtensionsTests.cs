using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Cosmos.Builder;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Unit;

public sealed class UseCosmosExtensionsTests
{
    private const string SampleConn = "AccountEndpoint=https://localhost:8081/;AccountKey=xxx;";

    [Test]
    public async Task UseCosmos_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => ((RigBuilder)null!).UseCosmos(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseCosmos_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseCosmos(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseCosmos_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => captured!.UseCosmos(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseCosmos_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var returned = captured!.UseCosmos(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseCosmos_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;
        captured!.UseCosmos(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }
}
