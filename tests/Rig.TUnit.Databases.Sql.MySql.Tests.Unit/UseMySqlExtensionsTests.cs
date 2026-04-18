using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.MySql.Builder;

namespace Rig.TUnit.Databases.Sql.MySql.Tests.Unit;

public sealed class UseMySqlExtensionsTests
{
    private const string SampleConn = "Server=localhost;Database=x;Uid=u;Pwd=p;";

    [Test]
    public async Task UseMySql_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => ((RigBuilder)null!).UseMySql(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMySql_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseMySql(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMySql_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => captured!.UseMySql(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMySql_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var returned = captured!.UseMySql(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseMySql_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;
        captured!.UseMySql(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }
}
