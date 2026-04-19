using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;
using Rig.TUnit.Databases.Sql.MySql.Builder;

namespace Rig.TUnit.Databases.Sql.MySql.Tests.Unit;

public sealed class MySqlRigBuilderTests
{
    [Test]
    public async Task MySqlRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(MySqlRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task MySqlRigBuilder_TypeMetadata_InheritsSqlRigBuilderCrtp()
    {
        var baseType = typeof(MySqlRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(SqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(MySqlRigBuilder));
    }

    [Test]
    public async Task MySqlRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("Server=localhost;Database=x;Uid=u;Pwd=p;");
        await Assert.That(() => new MySqlRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MySqlRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new MySqlRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
