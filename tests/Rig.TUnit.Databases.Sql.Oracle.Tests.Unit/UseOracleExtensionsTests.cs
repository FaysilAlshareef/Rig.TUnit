using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Oracle.Builder;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Unit;

public sealed class UseOracleExtensionsTests
{
    private const string SampleConn = "User Id=u;Password=p;Data Source=localhost:1521/FREEPDB1";

    [Test]
    public async Task UseOracle_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => ((RigBuilder)null!).UseOracle(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOracle_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseOracle(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOracle_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => captured!.UseOracle(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseOracle_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var returned = captured!.UseOracle(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseOracle_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;
        captured!.UseOracle(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }
}
