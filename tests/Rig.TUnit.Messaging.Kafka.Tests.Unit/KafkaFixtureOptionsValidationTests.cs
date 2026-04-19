using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Messaging.Kafka.Options;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 data-annotation validation coverage for <see cref="KafkaFixtureOptions"/>.
/// </summary>
public sealed class KafkaFixtureOptionsValidationTests
{
    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new KafkaFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new KafkaFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task StartupTimeoutSeconds_AboveRange_FailsValidation()
    {
        var options = new KafkaFixtureOptions { StartupTimeoutSeconds = 601 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task ImageTag_Empty_FailsRequiredValidation()
    {
        var options = new KafkaFixtureOptions { ImageTag = "" };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
