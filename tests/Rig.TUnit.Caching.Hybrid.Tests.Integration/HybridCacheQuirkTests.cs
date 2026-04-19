using Microsoft.Extensions.Caching.Hybrid;
using Rig.TUnit.Caching.Hybrid.Fixtures;

namespace Rig.TUnit.Caching.Hybrid.Tests.Integration;

/// <summary>HybridCache quirks: GetOrCreateAsync factory, tag invalidation, stampede coalescing.</summary>
public sealed class HybridCacheQuirkTests
{
    [Test]
    public async Task GetOrCreateAsync_WhenKeyMissing_InvokesFactoryOnce()
    {
        await using var fx = new HybridCacheFixture();
        await fx.InitializeAsync();
        var count = 0;

        var a = await fx.Cache.GetOrCreateAsync($"k-{Guid.NewGuid():N}", async _ =>
        {
            Interlocked.Increment(ref count);
            await Task.Yield();
            return "value";
        });

        await Assert.That(a).IsEqualTo("value");
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveByTagAsync_InvalidatesTaggedEntries()
    {
        await using var fx = new HybridCacheFixture();
        await fx.InitializeAsync();
        var key = $"k-{Guid.NewGuid():N}";
        var producerCalls = 0;

        Task<string> Produce()
        {
            Interlocked.Increment(ref producerCalls);
            return Task.FromResult("v");
        }

        await fx.Cache.GetOrCreateAsync(key, async _ => await Produce(), tags: new[] { "tag-A" });
        await fx.Cache.RemoveByTagAsync("tag-A");
        await fx.Cache.GetOrCreateAsync(key, async _ => await Produce(), tags: new[] { "tag-A" });

        await Assert.That(producerCalls).IsEqualTo(2);
    }

    [Test]
    public async Task GetOrCreateAsync_UnderConcurrentMisses_CoalescesProducer()
    {
        await using var fx = new HybridCacheFixture();
        await fx.InitializeAsync();
        var key = $"k-{Guid.NewGuid():N}";
        var producerCalls = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => fx.Cache.GetOrCreateAsync(key, async _ =>
        {
            Interlocked.Increment(ref producerCalls);
            await Task.Delay(50);
            return "v";
        }).AsTask()).ToArray();
        await Task.WhenAll(tasks);

        // HybridCache typically coalesces concurrent misses — producer called 1 time.
        await Assert.That(producerCalls).IsEqualTo(1);
    }
}
