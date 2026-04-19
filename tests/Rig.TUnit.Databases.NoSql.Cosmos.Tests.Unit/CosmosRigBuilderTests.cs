using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.Cosmos.Builder;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Unit;

public sealed class CosmosRigBuilderTests
{
    [Test]
    public async Task CosmosRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(CosmosRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task CosmosRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(CosmosRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(CosmosRigBuilder));
    }

    [Test]
    public async Task CosmosRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("AccountEndpoint=https://localhost:8081/;AccountKey=xxx;");
        await Assert.That(() => new CosmosRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task CosmosRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new CosmosRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
