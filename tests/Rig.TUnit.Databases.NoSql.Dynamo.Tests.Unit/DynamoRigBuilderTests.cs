using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.Dynamo.Builder;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

/// <summary>
/// T030-RED unit tests for <see cref="DynamoRigBuilder"/> — CRTP shape, seal,
/// constructor null-guards. No container, no infrastructure.
/// </summary>
public sealed class DynamoRigBuilderTests
{
    [Test]
    public async Task DynamoRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(DynamoRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task DynamoRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(DynamoRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(DynamoRigBuilder));
    }

    [Test]
    public async Task DynamoRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("http://localhost:4566");

        await Assert.That(() => new DynamoRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DynamoRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new DynamoRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
