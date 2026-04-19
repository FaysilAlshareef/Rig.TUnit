using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.Cassandra.Builder;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED unit tests for <see cref="CassandraRigBuilder"/> — CRTP shape, seal,
/// constructor null-guards. No container, no infrastructure.
/// </summary>
public sealed class CassandraRigBuilderTests
{
    [Test]
    public async Task CassandraRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(CassandraRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task CassandraRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(CassandraRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(CassandraRigBuilder));
    }

    [Test]
    public async Task CassandraRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("cassandra://localhost:9042");

        await Assert.That(() => new CassandraRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task CassandraRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new CassandraRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
