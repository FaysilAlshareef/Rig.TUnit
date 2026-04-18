using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.Mongo.Builder;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T022-RED unit tests for <see cref="MongoRigBuilder"/> — CRTP shape, seal, constructor
/// null-guards. No container, no infrastructure.
/// </summary>
public sealed class MongoRigBuilderTests
{
    [Test]
    public async Task MongoRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(MongoRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task MongoRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(MongoRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(MongoRigBuilder));
    }

    [Test]
    public async Task MongoRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("mongodb://localhost");

        await Assert.That(() => new MongoRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MongoRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new MongoRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
