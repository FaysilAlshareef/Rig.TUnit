using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Messaging.Sqs.Options;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

public sealed class SqsFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = SqsFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Sqs");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new SqsFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("3");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(240);
        await Assert.That(options.Region).IsEqualTo("us-east-1");
        await Assert.That(options.AccessKeyId).IsEqualTo("test");
        await Assert.That(options.SecretAccessKey).IsEqualTo("test");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new SqsFixtureOptions
        {
            ImageTag = "3.4",
            StartupTimeoutSeconds = 120,
            Region = "eu-west-1",
            AccessKeyId = "custom",
            SecretAccessKey = "secret",
        };

        await Assert.That(options.ImageTag).IsEqualTo("3.4");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
        await Assert.That(options.Region).IsEqualTo("eu-west-1");
        await Assert.That(options.AccessKeyId).IsEqualTo("custom");
        await Assert.That(options.SecretAccessKey).IsEqualTo("secret");
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new SqsFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new SqsFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task Region_Empty_FailsRequiredValidation()
    {
        var options = new SqsFixtureOptions { Region = "" };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
