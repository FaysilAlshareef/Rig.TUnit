using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Fixtures;

namespace Rig.TUnit.Caching.Hybrid.Fixtures;

/// <summary>
/// In-process <see cref="HybridCache"/>-backed fixture. Supports tag invalidation
/// via <c>RemoveByTagAsync</c>. L1 (in-memory) only — add a Redis L2 via
/// <see cref="IServiceCollection"/> if coherency-across-nodes testing is required.
/// </summary>
public sealed class HybridCacheFixture : CacheFixtureBase
{
    private ServiceProvider? _provider;
    private HybridCache? _cache;

    public HybridCache Cache => _cache
        ?? throw new InvalidOperationException("HybridCacheFixture not initialised — call InitializeAsync.");

    public override string ConnectionString => "hybrid-in-memory";

    public override Task InitializeAsync()
    {
        if (_cache is not null) return Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddHybridCache();
        _provider = services.BuildServiceProvider();
        _cache = _provider.GetRequiredService<HybridCache>();
        return Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        _cache = null;
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
            _provider = null;
        }
    }
}
