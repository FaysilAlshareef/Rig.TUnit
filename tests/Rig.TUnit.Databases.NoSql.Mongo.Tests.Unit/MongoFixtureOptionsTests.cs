using Rig.TUnit.Databases.NoSql.Mongo.Options;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T025a coverage-lifting test (FR-035) for <see cref="MongoFixtureOptions"/>. Init-only
/// autoprops do not register as line-covered when exercised only through DI binding
/// (coverlet/MTP measurement quirk). This suite constructs the options record explicitly
/// — with defaults and with every property overridden — so every init-only line is hit.
/// </summary>
public sealed class MongoFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        // Bind the const through a local so TUnit's analyzer doesn't treat the whole call as constant.
        var actual = MongoFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Mongo");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        // Act
        var options = new MongoFixtureOptions();

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("7");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(360);
        await Assert.That(options.Username).IsEqualTo("root");
        await Assert.That(options.Password).IsEqualTo("mongo");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        // Act
        var options = new MongoFixtureOptions
        {
            ImageTag = "6",
            StartupTimeoutSeconds = 45,
            Username = "alice",
            Password = "hunter2",
        };

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("6");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(45);
        await Assert.That(options.Username).IsEqualTo("alice");
        await Assert.That(options.Password).IsEqualTo("hunter2");
    }
}
