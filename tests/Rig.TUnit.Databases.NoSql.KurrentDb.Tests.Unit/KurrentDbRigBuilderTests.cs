using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.KurrentDb.Builder;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

public sealed class KurrentDbRigBuilderTests
{
    [Test]
    public async Task KurrentDbRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(KurrentDbRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task KurrentDbRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(KurrentDbRigBuilder).BaseType;
        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(KurrentDbRigBuilder));
    }

    [Test]
    public async Task KurrentDbRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("esdb://localhost:2113?tls=false");
        await Assert.That(() => new KurrentDbRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task KurrentDbRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new KurrentDbRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
