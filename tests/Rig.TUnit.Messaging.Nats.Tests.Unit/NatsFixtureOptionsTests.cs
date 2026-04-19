using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Messaging.Nats.Options;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class NatsFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = NatsFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Nats");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new NatsFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("2.10-alpine");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(180);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new NatsFixtureOptions
        {
            ImageTag = "2.11-alpine",
            StartupTimeoutSeconds = 60,
        };

        await Assert.That(options.ImageTag).IsEqualTo("2.11-alpine");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new NatsFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new NatsFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task ImageTag_Empty_FailsRequiredValidation()
    {
        var options = new NatsFixtureOptions { ImageTag = "" };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
