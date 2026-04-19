using Rig.TUnit.Caching.Fusion.Fixtures;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Tests.Integration;

/// <summary>FusionCache quirks: fail-safe fallback and eager refresh.</summary>
public sealed class FusionCacheQuirkTests
{
    [Test]
    public async Task GetOrSetAsync_WhenFactoryThrowsAndFailSafeStaleExists_ReturnsStale()
    {
        await using var fx = new FusionCacheFixture();
        await fx.InitializeAsync();
        var key = $"k-{Guid.NewGuid():N}";

        // First call populates.
        await fx.Cache.SetAsync(key, "v1", TimeSpan.FromMilliseconds(100));
        await Task.Delay(150); // expire

        // Second call — factory throws. With fail-safe enabled, stale value is returned.
        var result = await fx.Cache.GetOrSetAsync<string>(
            key,
            (_, _) => throw new InvalidOperationException("producer down"),
            options: new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(1),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(1),
            });

        await Assert.That(result).IsEqualTo("v1");
    }

    [Test]
    public async Task GetOrSetAsync_WhenValueFresh_DoesNotInvokeFactory()
    {
        await using var fx = new FusionCacheFixture();
        await fx.InitializeAsync();
        var key = $"k-{Guid.NewGuid():N}";
        var calls = 0;

        await fx.Cache.SetAsync(key, "fresh", TimeSpan.FromMinutes(5));
        var result = await fx.Cache.GetOrSetAsync<string>(
            key,
            async (_, _) => { Interlocked.Increment(ref calls); await Task.Yield(); return "should-not-see"; });

        await Assert.That(result).IsEqualTo("fresh");
        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    public async Task SetAsync_ThenGetAsync_Roundtrips()
    {
        await using var fx = new FusionCacheFixture();
        await fx.InitializeAsync();
        var key = $"k-{Guid.NewGuid():N}";

        await fx.Cache.SetAsync(key, 42);
        var value = await fx.Cache.GetOrDefaultAsync<int>(key);

        await Assert.That(value).IsEqualTo(42);
    }
}
