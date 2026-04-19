using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Mongo.Builder;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T025a coverage-lifting test (FR-035) for <see cref="MongoRigBuilder.ConnectionString"/>.
/// The basic metadata assertions in <see cref="MongoRigBuilderTests"/> cover the sealed +
/// CRTP shape but never execute the <c>ConnectionString</c> getter. This suite constructs
/// the builder through the DI pipeline and drives the property.
/// </summary>
public sealed class MongoRigBuilderConnectionStringTests
{
    private const string SampleConnectionString = "mongodb://unit-test:27017";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new MongoRigBuilder(captured!, source);

        // Act
        var connectionString = builder.ConnectionString;

        // Assert
        await Assert.That(connectionString).IsEqualTo(SampleConnectionString);
    }

    [Test]
    public async Task ConnectionString_DifferentSources_ReturnDistinctValues()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);

        var a = new MongoRigBuilder(captured!, RigConnect.FromValue("mongodb://a:27017"));
        var b = new MongoRigBuilder(captured!, RigConnect.FromValue("mongodb://b:27017"));

        // Act
        var aStr = a.ConnectionString;
        var bStr = b.ConnectionString;

        // Assert
        await Assert.That(aStr).IsNotEqualTo(bStr);
    }
}
