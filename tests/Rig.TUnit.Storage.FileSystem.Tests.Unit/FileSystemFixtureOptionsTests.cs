using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Storage.FileSystem.Options;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

public sealed class FileSystemFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = FileSystemFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:FileSystem");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new FileSystemFixtureOptions();
        await Assert.That(options.RootPathPrefix).IsEqualTo("rigtunit-fs");
        await Assert.That(options.CleanupOnDispose).IsTrue();
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new FileSystemFixtureOptions
        {
            RootPathPrefix = "custom-prefix",
            CleanupOnDispose = false,
        };
        await Assert.That(options.RootPathPrefix).IsEqualTo("custom-prefix");
        await Assert.That(options.CleanupOnDispose).IsFalse();
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new FileSystemFixtureOptions();
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task RootPathPrefix_Empty_FailsRequiredValidation()
    {
        var options = new FileSystemFixtureOptions { RootPathPrefix = "" };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
