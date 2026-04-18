using Rig.TUnit.Messaging.Kafka.Options;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 coverage-lifting tests for <see cref="KafkaFixtureOptions"/>.
/// </summary>
public sealed class KafkaFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = KafkaFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Kafka");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new KafkaFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("7.6.0");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(300);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new KafkaFixtureOptions
        {
            ImageTag = "7.7.0",
            StartupTimeoutSeconds = 120,
        };

        await Assert.That(options.ImageTag).IsEqualTo("7.7.0");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }
}
