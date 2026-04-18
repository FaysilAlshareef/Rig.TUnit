using Rig.TUnit.Databases.NoSql.Dynamo.Options;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

/// <summary>
/// T030-RED coverage-lifting test (FR-035) for <see cref="DynamoFixtureOptions"/>.
/// </summary>
public sealed class DynamoFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = DynamoFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Dynamo");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new DynamoFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("3");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(180);
        await Assert.That(options.Region).IsEqualTo("us-east-1");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new DynamoFixtureOptions
        {
            ImageTag = "4.0",
            StartupTimeoutSeconds = 60,
            Region = "eu-west-1",
        };

        await Assert.That(options.ImageTag).IsEqualTo("4.0");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
        await Assert.That(options.Region).IsEqualTo("eu-west-1");
    }
}
