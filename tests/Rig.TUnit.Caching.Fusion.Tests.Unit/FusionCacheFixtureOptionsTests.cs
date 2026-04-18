using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Caching.Fusion.Options;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

public sealed class FusionCacheFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = FusionCacheFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:FusionCache");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new FusionCacheFixtureOptions();

        await Assert.That(options.DefaultDurationSeconds).IsEqualTo(60);
        await Assert.That(options.IsFailSafeEnabled).IsTrue();
        await Assert.That(options.FailSafeMaxDurationSeconds).IsEqualTo(3600);
        await Assert.That(options.EagerRefreshThreshold).IsEqualTo(0.8f);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new FusionCacheFixtureOptions
        {
            DefaultDurationSeconds = 120,
            IsFailSafeEnabled = false,
            FailSafeMaxDurationSeconds = 7200,
            EagerRefreshThreshold = 0.5f,
        };

        await Assert.That(options.DefaultDurationSeconds).IsEqualTo(120);
        await Assert.That(options.IsFailSafeEnabled).IsFalse();
        await Assert.That(options.FailSafeMaxDurationSeconds).IsEqualTo(7200);
        await Assert.That(options.EagerRefreshThreshold).IsEqualTo(0.5f);
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new FusionCacheFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task DefaultDurationSeconds_BelowRange_FailsValidation()
    {
        var options = new FusionCacheFixtureOptions { DefaultDurationSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task EagerRefreshThreshold_BelowRange_FailsValidation()
    {
        var options = new FusionCacheFixtureOptions { EagerRefreshThreshold = 0.0f };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task EagerRefreshThreshold_AboveRange_FailsValidation()
    {
        var options = new FusionCacheFixtureOptions { EagerRefreshThreshold = 1.1f };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
