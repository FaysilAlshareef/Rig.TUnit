using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Hybrid.Builder;
using Rig.TUnit.Caching.Hybrid.Fixtures;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Hybrid.Tests.Integration;

/// <summary>
/// Integration test: wires HybridCache via the fluent <c>UseHybridCache</c> pipeline
/// and exercises the fixture end-to-end (in-process; no Docker required — Hybrid is
/// an in-memory L1 cache). Verifies that the Builder + Fixture integrate cleanly.
/// </summary>
public sealed class UseHybridCacheFluentTests
{
    [Test]
    public async Task UseHybridCache_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("hybrid-in-memory");

        HybridCacheRigBuilder? configured = null;
        captured!.UseHybridCache(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
        await Assert.That(configured!.ConnectionString).IsEqualTo("hybrid-in-memory");
    }

    [Test]
    public async Task Fixture_Initialize_ThenGetSetRoundTrip_Succeeds()
    {
        await using var fx = new HybridCacheFixture();
        await fx.InitializeAsync();

        var key = $"integ-{Guid.NewGuid():N}";
        var factoryCalls = 0;

        var a = await fx.Cache.GetOrCreateAsync(key, async _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Yield();
            return "hello";
        });
        var b = await fx.Cache.GetOrCreateAsync(key, async _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Yield();
            return "hello-again";
        });

        await Assert.That(a).IsEqualTo("hello");
        await Assert.That(b).IsEqualTo("hello");
        await Assert.That(factoryCalls).IsEqualTo(1);
    }
}
