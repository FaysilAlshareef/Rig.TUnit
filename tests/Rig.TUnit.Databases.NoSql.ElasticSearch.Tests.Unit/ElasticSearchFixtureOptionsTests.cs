using Rig.TUnit.Databases.NoSql.ElasticSearch.Options;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class ElasticSearchFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = ElasticSearchFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:ElasticSearch");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new ElasticSearchFixtureOptions();
        await Assert.That(options.ImageTag).IsEqualTo("8.15.3");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(360);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new ElasticSearchFixtureOptions
        {
            ImageTag = "9.0.0",
            StartupTimeoutSeconds = 240,
        };
        await Assert.That(options.ImageTag).IsEqualTo("9.0.0");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(240);
    }
}
