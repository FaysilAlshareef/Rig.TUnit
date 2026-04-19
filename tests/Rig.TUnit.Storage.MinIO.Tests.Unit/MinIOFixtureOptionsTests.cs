using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Storage.MinIO.Options;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

public sealed class MinIOFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = MinIOFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:MinIO");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new MinIOFixtureOptions();
        await Assert.That(options.ImageTag).IsEqualTo("latest");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(180);
        await Assert.That(options.Username).IsEqualTo("minioadmin");
        await Assert.That(options.Password).IsEqualTo("minioadmin");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new MinIOFixtureOptions
        {
            ImageTag = "RELEASE.2025-01-20T14-49-07Z",
            StartupTimeoutSeconds = 120,
            Username = "admin",
            Password = "secret",
        };
        await Assert.That(options.ImageTag).IsEqualTo("RELEASE.2025-01-20T14-49-07Z");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
        await Assert.That(options.Username).IsEqualTo("admin");
        await Assert.That(options.Password).IsEqualTo("secret");
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new MinIOFixtureOptions();
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task StartupTimeoutSeconds_BelowRange_FailsValidation()
    {
        var options = new MinIOFixtureOptions { StartupTimeoutSeconds = 0 };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task ImageTag_Empty_FailsRequiredValidation()
    {
        var options = new MinIOFixtureOptions { ImageTag = "" };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
