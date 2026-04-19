using Rig.TUnit.Caching.Fusion.Helpers;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

/// <summary>
/// Pure-function unit tests for <see cref="EagerRefreshHelper.ShouldEagerRefresh"/>.
/// Deterministic decision logic only — no cache, no container. Uses TUnit's awaited
/// assertion API (the project-wide convention) so signatures are async Task despite
/// the function under test being synchronous.
/// </summary>
public sealed class EagerRefreshHelperTests
{
    [Test]
    public async Task ShouldEagerRefresh_ThresholdNotSet_ReturnsFalse()
    {
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = null,
        };

        var actual = EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(9));

        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task ShouldEagerRefresh_ElapsedBelowEagerWindow_ReturnsFalse()
    {
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = 0.8f,
        };

        var actual = EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(5));

        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task ShouldEagerRefresh_ElapsedInsideEagerWindow_ReturnsTrue()
    {
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = 0.8f,
        };

        var actual = EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(9));

        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task ShouldEagerRefresh_ElapsedAtExactThreshold_ReturnsTrue()
    {
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = 0.8f,
        };

        var actual = EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(8));

        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task ShouldEagerRefresh_ElapsedBeyondDuration_ReturnsFalse()
    {
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = 0.8f,
        };

        var actual = EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(11));

        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task ShouldEagerRefresh_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => EagerRefreshHelper.ShouldEagerRefresh(null!, TimeSpan.FromMinutes(1)))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ShouldEagerRefresh_NegativeElapsed_ThrowsArgumentOutOfRangeException()
    {
        var options = new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(10), EagerRefreshThreshold = 0.8f };

        await Assert.That(() => EagerRefreshHelper.ShouldEagerRefresh(options, TimeSpan.FromMinutes(-1)))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }
}
