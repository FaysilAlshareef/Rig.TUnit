using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Storage.S3.Options;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

public sealed class S3FixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = S3FixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:S3");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new S3FixtureOptions();
        await Assert.That(options.ImageTag).IsEqualTo("3");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(180);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new S3FixtureOptions { ImageTag = "3.4", StartupTimeoutSeconds = 120 };
        await Assert.That(options.ImageTag).IsEqualTo("3.4");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new S3FixtureOptions();
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new S3FixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task ImageTag_Empty_FailsRequiredValidation()
    {
        var options = new S3FixtureOptions { ImageTag = "" };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
