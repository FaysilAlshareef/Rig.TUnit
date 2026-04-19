using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Builder;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class ElasticSearchRigBuilderTests
{
    [Test]
    public async Task ElasticSearchRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(ElasticSearchRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task ElasticSearchRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(ElasticSearchRigBuilder).BaseType;
        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(ElasticSearchRigBuilder));
    }

    [Test]
    public async Task ElasticSearchRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("http://localhost:9200");
        await Assert.That(() => new ElasticSearchRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ElasticSearchRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new ElasticSearchRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
