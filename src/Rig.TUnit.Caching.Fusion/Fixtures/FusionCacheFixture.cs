using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Fixtures;
using Rig.TUnit.Caching.Fusion.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Fixtures;

/// <summary>FusionCache fixture — exposes fail-safe, eager refresh, and tag semantics.</summary>
public sealed class FusionCacheFixture : CacheFixtureBase
{
    private readonly FusionCacheFixtureOptions _options;
    private FusionCache? _cache;
    private MemoryCache? _memory;

    public FusionCacheFixture() : this(new FusionCacheFixtureOptions())
    {
    }

    public FusionCacheFixture(IOptions<FusionCacheFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public FusionCacheFixture(FusionCacheFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

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
                Duration = TimeSpan.FromSeconds(_options.DefaultDurationSeconds),
                IsFailSafeEnabled = _options.IsFailSafeEnabled,
                FailSafeMaxDuration = TimeSpan.FromSeconds(_options.FailSafeMaxDurationSeconds),
                EagerRefreshThreshold = _options.EagerRefreshThreshold,
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
