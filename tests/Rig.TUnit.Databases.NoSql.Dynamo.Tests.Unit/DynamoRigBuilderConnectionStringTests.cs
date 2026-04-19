using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Dynamo.Builder;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

public sealed class DynamoRigBuilderConnectionStringTests
{
    private const string SampleConnectionString = "http://unit-test:4566";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new DynamoRigBuilder(captured!, source);

        var connectionString = builder.ConnectionString;

        await Assert.That(connectionString).IsEqualTo(SampleConnectionString);
    }

    [Test]
    public async Task ConnectionString_DifferentSources_ReturnDistinctValues()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);

        var a = new DynamoRigBuilder(captured!, RigConnect.FromValue("http://a:4566"));
        var b = new DynamoRigBuilder(captured!, RigConnect.FromValue("http://b:4566"));

        await Assert.That(a.ConnectionString).IsNotEqualTo(b.ConnectionString);
    }
}
