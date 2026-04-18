using Microsoft.Extensions.Caching.Memory;
using Rig.TUnit.Caching.Fixtures;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Fixtures;

/// <summary>FusionCache fixture — exposes fail-safe, eager refresh, and tag semantics.</summary>
public sealed class FusionCacheFixture : CacheFixtureBase
{
    private FusionCache? _cache;
    private MemoryCache? _memory;

    public FusionCache Cache => _cache
        ?? throw new InvalidOperationException("FusionCacheFixture not initialised — call InitializeAsync.");

    public override string ConnectionString => "fusion-in-memory";

    public override Task InitializeAsync()
    {
        if (_cache is not null) return Task.CompletedTask;
        _memory = new MemoryCache(new MemoryCacheOptions());
        _cache = new FusionCache(new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(1),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(1),
            },
        }, _memory);
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _cache?.Dispose();
        _memory?.Dispose();
        _cache = null;
        _memory = null;
        return ValueTask.CompletedTask;
    }
}
