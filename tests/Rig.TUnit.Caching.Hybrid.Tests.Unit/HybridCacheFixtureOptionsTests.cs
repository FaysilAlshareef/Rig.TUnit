using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Caching.Hybrid.Options;

namespace Rig.TUnit.Caching.Hybrid.Tests.Unit;

public sealed class HybridCacheFixtureOptionsTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        var actual = HybridCacheFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:HybridCache");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        var options = new HybridCacheFixtureOptions();

        await Assert.That(options.DefaultExpirationSeconds).IsEqualTo(60);
        await Assert.That(options.LocalCacheExpirationSeconds).IsEqualTo(30);
        await Assert.That(options.MaximumPayloadBytes).IsEqualTo(1024 * 1024);
        await Assert.That(options.MaximumKeyLength).IsEqualTo(1024);
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        var options = new HybridCacheFixtureOptions
        {
            DefaultExpirationSeconds = 120,
            LocalCacheExpirationSeconds = 15,
            MaximumPayloadBytes = 4096,
            MaximumKeyLength = 256,
        };

        await Assert.That(options.DefaultExpirationSeconds).IsEqualTo(120);
        await Assert.That(options.LocalCacheExpirationSeconds).IsEqualTo(15);
        await Assert.That(options.MaximumPayloadBytes).IsEqualTo(4096);
        await Assert.That(options.MaximumKeyLength).IsEqualTo(256);
    }

    [Test]
    public async Task Defaults_PassValidation()
    {
        var options = new HybridCacheFixtureOptions();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task DefaultExpirationSeconds_BelowRange_FailsValidation()
    {
        var options = new HybridCacheFixtureOptions { DefaultExpirationSeconds = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task MaximumPayloadBytes_BelowRange_FailsValidation()
    {
        var options = new HybridCacheFixtureOptions { MaximumPayloadBytes = 0 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }
}
