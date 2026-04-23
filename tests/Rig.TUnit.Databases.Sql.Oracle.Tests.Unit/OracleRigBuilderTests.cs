using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;
using Rig.TUnit.Databases.Sql.Oracle.Builder;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Unit;

public sealed class OracleRigBuilderTests
{
    private const string SampleConn = "User Id=rigtunit;Password=rigtunit;Data Source=localhost:1521/FREEPDB1";

    [Test]
    public async Task OracleRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(OracleRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task OracleRigBuilder_TypeMetadata_InheritsSqlRigBuilderCrtp()
    {
        var baseType = typeof(OracleRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(OracleRigBuilder));
    }

    [Test]
    public async Task OracleRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);
        await Assert.That(() => new OracleRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OracleRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new OracleRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OracleRigBuilder_ReplaceDbContext_RegistersContextInServices()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var builder = new OracleRigBuilder(captured!, source);
        builder.ReplaceDbContext<SampleDbContext>();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SampleDbContext));

        await Assert.That(descriptor).IsNotNull();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
