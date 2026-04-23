using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;
using Rig.TUnit.Databases.Sql.SqlServer.Builder;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit;

public sealed class SqlServerBuilderTests
{
    private const string SampleConn = "Server=(local);Database=TestDb;Integrated Security=True";

    [Test]
    public async Task SqlServerRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(SqlServerRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task SqlServerRigBuilder_TypeMetadata_InheritsSqlRigBuilderCrtp()
    {
        var baseType = typeof(SqlServerRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType.GetGenericTypeDefinition()).IsEqualTo(typeof(SqlRigBuilder<>));
        await Assert.That(baseType.GenericTypeArguments[0]).IsEqualTo(typeof(SqlServerRigBuilder));
    }

    [Test]
    public async Task SqlServerRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => new SqlServerRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SqlServerRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new SqlServerRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlServer_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => ((RigBuilder)null!).UseSqlServer(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlServer_NullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseSqlServer(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlServer_NullConfigure_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => captured!.UseSqlServer(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlServer_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var returned = captured!.UseSqlServer(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseSqlServer_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;

        captured!.UseSqlServer(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseSqlServer_ValidArgs_PassesSqlServerRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        SqlServerRigBuilder? passed = null;

        captured!.UseSqlServer(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }

    [Test]
    public async Task SqlServerRigBuilder_ReplaceDbContext_RegistersContextInServices()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var builder = new SqlServerRigBuilder(captured!, source);
        builder.ReplaceDbContext<SampleDbContext>();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SampleDbContext));

        await Assert.That(descriptor).IsNotNull();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
