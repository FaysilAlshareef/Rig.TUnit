using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Storage.AzureBlob.Options;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

public sealed class AzureBlobFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = AzureBlobFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:AzureBlob");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new AzureBlobFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("latest");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new AzureBlobFixtureOptions
        {
            ImageTag = "3.30.0",
            StartupTimeoutSeconds = 60,
        };
        await Assert.That(options.ImageTag).IsEqualTo("3.30.0");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new AzureBlobFixtureOptions();
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new AzureBlobFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task ImageTag_Empty_FailsRequiredValidation()
    {
        var options = new AzureBlobFixtureOptions { ImageTag = "" };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
