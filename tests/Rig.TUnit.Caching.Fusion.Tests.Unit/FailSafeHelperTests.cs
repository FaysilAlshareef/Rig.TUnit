using Rig.TUnit.Caching.Fusion.Helpers;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

/// <summary>
/// Pure-function unit tests for <see cref="FailSafeHelper.IsFailSafeApplicable"/>.
/// No cache, no container — deterministic decision logic only.
/// </summary>
public sealed class FailSafeHelperTests
{
    [Test]
    public async Task IsFailSafeApplicable_WhenFailSafeDisabled_ReturnsFalse()
    {
        var options = new FusionCacheEntryOptions
        {
            IsFailSafeEnabled = false,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
        };

        var actual = FailSafeHelper.IsFailSafeApplicable(options, TimeSpan.FromMinutes(5));

        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task IsFailSafeApplicable_WhenEnabledAndWithinMaxDuration_ReturnsTrue()
    {
        var options = new FusionCacheEntryOptions
        {
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
        };

        var actual = FailSafeHelper.IsFailSafeApplicable(options, TimeSpan.FromMinutes(30));

        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task IsFailSafeApplicable_WhenEnabledAndExactlyAtMaxDuration_ReturnsTrue()
    {
        var options = new FusionCacheEntryOptions
        {
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
        };

        var actual = FailSafeHelper.IsFailSafeApplicable(options, TimeSpan.FromHours(1));

        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task IsFailSafeApplicable_WhenEnabledAndBeyondMaxDuration_ReturnsFalse()
    {
        var options = new FusionCacheEntryOptions
        {
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
        };

        var actual = FailSafeHelper.IsFailSafeApplicable(options, TimeSpan.FromHours(2));

        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task IsFailSafeApplicable_WhenNullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => FailSafeHelper.IsFailSafeApplicable(null!, TimeSpan.FromMinutes(1)))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IsFailSafeApplicable_WhenNegativeElapsed_ThrowsArgumentOutOfRangeException()
    {
        var options = new FusionCacheEntryOptions { IsFailSafeEnabled = true, FailSafeMaxDuration = TimeSpan.FromHours(1) };

        await Assert.That(() => FailSafeHelper.IsFailSafeApplicable(options, TimeSpan.FromMinutes(-1)))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }
}
