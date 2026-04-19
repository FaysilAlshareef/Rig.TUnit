using Rig.TUnit.Databases.NoSql.KurrentDb.Options;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

public sealed class KurrentDbFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = KurrentDbFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:KurrentDb");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new KurrentDbFixtureOptions();
        await Assert.That(options.ImageTag).IsEqualTo("25.1");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(300);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new KurrentDbFixtureOptions
        {
            ImageTag = "25.2",
            StartupTimeoutSeconds = 120,
        };
        await Assert.That(options.ImageTag).IsEqualTo("25.2");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }
}
