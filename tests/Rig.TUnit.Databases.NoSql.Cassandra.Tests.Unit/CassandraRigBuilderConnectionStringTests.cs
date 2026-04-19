using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Cassandra.Builder;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED coverage-lifting test (FR-035) for <see cref="CassandraRigBuilder.ConnectionString"/>.
/// The basic metadata assertions in <see cref="CassandraRigBuilderTests"/> cover the sealed +
/// CRTP shape but never execute the <c>ConnectionString</c> getter. This suite constructs
/// the builder through the DI pipeline and drives the property.
/// </summary>
public sealed class CassandraRigBuilderConnectionStringTests
{
    private const string SampleConnectionString = "cassandra://unit-test:9042";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new CassandraRigBuilder(captured!, source);

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

        var a = new CassandraRigBuilder(captured!, RigConnect.FromValue("cassandra://a:9042"));
        var b = new CassandraRigBuilder(captured!, RigConnect.FromValue("cassandra://b:9042"));

        // Act
        var aStr = a.ConnectionString;
        var bStr = b.ConnectionString;

        // Assert
        await Assert.That(aStr).IsNotEqualTo(bStr);
    }
}
