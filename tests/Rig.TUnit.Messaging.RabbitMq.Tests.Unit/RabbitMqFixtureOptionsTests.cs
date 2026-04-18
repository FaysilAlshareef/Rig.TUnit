using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Messaging.RabbitMq.Options;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class RabbitMqFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = RabbitMqFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:RabbitMq");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new RabbitMqFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("3-management");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(180);
        await Assert.That(options.Username).IsEqualTo("guest");
        await Assert.That(options.Password).IsEqualTo("guest");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new RabbitMqFixtureOptions
        {
            ImageTag = "3.13-management",
            StartupTimeoutSeconds = 120,
            Username = "admin",
            Password = "secret",
        };

        await Assert.That(options.ImageTag).IsEqualTo("3.13-management");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
        await Assert.That(options.Username).IsEqualTo("admin");
        await Assert.That(options.Password).IsEqualTo("secret");
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new RabbitMqFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new RabbitMqFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task StartupTimeoutSeconds_AboveRange_FailsValidation()
    {
        var options = new RabbitMqFixtureOptions { StartupTimeoutSeconds = 601 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
