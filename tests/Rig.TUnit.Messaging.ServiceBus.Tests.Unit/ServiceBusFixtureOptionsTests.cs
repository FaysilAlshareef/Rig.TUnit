using Rig.TUnit.Messaging.ServiceBus.Options;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 coverage-lifting tests for <see cref="ServiceBusFixtureOptions"/>. Init-only
/// autoprops do not register as line-covered when exercised only through DI binding
/// (coverlet/MTP measurement quirk). This suite constructs the options record explicitly
/// — with defaults and with every property overridden — so every init-only line is hit.
/// </summary>
public sealed class ServiceBusFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = ServiceBusFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:ServiceBus");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new ServiceBusFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("1.1.2");
        await Assert.That(options.SqlEdgeImageTag).IsEqualTo("1.0.7");
        await Assert.That(options.ConfigFilePath).IsEqualTo("TestInfrastructure/service-bus-config.json");
        await Assert.That(options.AcceptEula).IsTrue();
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new ServiceBusFixtureOptions
        {
            ImageTag = "1.1.3",
            SqlEdgeImageTag = "1.0.8",
            ConfigFilePath = "custom/path/config.json",
            AcceptEula = false,
            StartupTimeoutSeconds = 45,
        };

        await Assert.That(options.ImageTag).IsEqualTo("1.1.3");
        await Assert.That(options.SqlEdgeImageTag).IsEqualTo("1.0.8");
        await Assert.That(options.ConfigFilePath).IsEqualTo("custom/path/config.json");
        await Assert.That(options.AcceptEula).IsFalse();
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(45);
    }
}
