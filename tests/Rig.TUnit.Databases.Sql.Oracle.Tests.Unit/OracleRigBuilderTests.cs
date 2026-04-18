using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;
using Rig.TUnit.Databases.Sql.Oracle.Builder;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Unit;

public sealed class OracleRigBuilderTests
{
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
        var source = RigConnect.FromValue("User Id=rigtunit;Password=rigtunit;Data Source=localhost:1521/FREEPDB1");
        await Assert.That(() => new OracleRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OracleRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new OracleRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
