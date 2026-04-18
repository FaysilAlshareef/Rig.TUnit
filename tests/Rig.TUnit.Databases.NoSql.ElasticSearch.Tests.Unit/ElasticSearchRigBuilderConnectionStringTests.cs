using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Builder;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class ElasticSearchRigBuilderConnectionStringTests
{
    private const string Sample = "http://unit-test:9200";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var builder = new ElasticSearchRigBuilder(captured!, RigConnect.FromValue(Sample));
        await Assert.That(builder.ConnectionString).IsEqualTo(Sample);
    }

    [Test]
    public async Task ConnectionString_DifferentSources_ReturnDistinctValues()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var a = new ElasticSearchRigBuilder(captured!, RigConnect.FromValue("http://a:9200"));
        var b = new ElasticSearchRigBuilder(captured!, RigConnect.FromValue("http://b:9200"));
        await Assert.That(a.ConnectionString).IsNotEqualTo(b.ConnectionString);
    }
}
