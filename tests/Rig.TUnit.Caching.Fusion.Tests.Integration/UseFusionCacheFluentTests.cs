using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Fusion.Builder;
using Rig.TUnit.Caching.Fusion.Fixtures;
using Rig.TUnit.Caching.Fusion.Helpers;
using Rig.TUnit.Core.Builder;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Tests.Integration;

/// <summary>
/// Integration: fluent wiring via <c>UseFusionCache</c> + end-to-end fixture round-trip
/// exercising fail-safe fallback and eager-refresh decisions via the helpers.
/// In-process, no Docker.
/// </summary>
public sealed class UseFusionCacheFluentTests
{
    [Test]
    public async Task UseFusionCache_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("fusion-in-memory");

        FusionCacheRigBuilder? configured = null;
        captured!.UseFusionCache(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
        await Assert.That(configured!.ConnectionString).IsEqualTo("fusion-in-memory");
    }

    [Test]
    public async Task Fixture_Initialize_ThenGetOrSetRoundTrip_Succeeds()
    {
        await using var fx = new FusionCacheFixture();
        await fx.InitializeAsync();

        var key = $"integ-{Guid.NewGuid():N}";
        var value = await fx.Cache.GetOrSetAsync<string>(key, async (_, _) =>
        {
            await Task.Yield();
            return "computed";
        });

        await Assert.That(value).IsEqualTo("computed");
    }

    [Test]
    public async Task FailSafeHelper_AndEagerRefreshHelper_AgreeWithFixtureConfig()
    {
        await using var fx = new FusionCacheFixture();
        await fx.InitializeAsync();

        var opts = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
            EagerRefreshThreshold = 0.8f,
        };

        await Assert.That(FailSafeHelper.IsFailSafeApplicable(opts, TimeSpan.FromMinutes(30))).IsTrue();
        await Assert.That(FailSafeHelper.IsFailSafeApplicable(opts, TimeSpan.FromHours(2))).IsFalse();
        await Assert.That(EagerRefreshHelper.ShouldEagerRefresh(opts, TimeSpan.FromMinutes(9))).IsTrue();
        await Assert.That(EagerRefreshHelper.ShouldEagerRefresh(opts, TimeSpan.FromMinutes(5))).IsFalse();
    }
}
