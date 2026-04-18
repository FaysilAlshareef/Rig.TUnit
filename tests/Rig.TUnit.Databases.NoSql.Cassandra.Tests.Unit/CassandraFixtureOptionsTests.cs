using Rig.TUnit.Databases.NoSql.Cassandra.Options;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED coverage-lifting test (FR-035) for <see cref="CassandraFixtureOptions"/>. Init-only
/// autoprops do not register as line-covered when exercised only through DI binding (coverlet/MTP
/// measurement quirk). This suite constructs the options record explicitly — with defaults and
/// with every property overridden — so every init-only line is hit.
/// </summary>
public sealed class CassandraFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = CassandraFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Cassandra");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        // Act
        var options = new CassandraFixtureOptions();

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("5");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(360);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        // Act
        var options = new CassandraFixtureOptions
        {
            ImageTag = "4.1",
            StartupTimeoutSeconds = 90,
        };

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("4.1");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(90);
    }
}
