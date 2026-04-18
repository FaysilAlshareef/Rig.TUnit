using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Fixtures;
using Rig.TUnit.Caching.Hybrid.Options;

namespace Rig.TUnit.Caching.Hybrid.Fixtures;

/// <summary>
/// In-process <see cref="HybridCache"/>-backed fixture. Supports tag invalidation
/// via <c>RemoveByTagAsync</c>. L1 (in-memory) only — add a Redis L2 via
/// <see cref="IServiceCollection"/> if coherency-across-nodes testing is required.
/// </summary>
public sealed class HybridCacheFixture : CacheFixtureBase
{
    private readonly HybridCacheFixtureOptions _options;
    private ServiceProvider? _provider;
    private HybridCache? _cache;

    public HybridCacheFixture() : this(new HybridCacheFixtureOptions())
    {
    }

    public HybridCacheFixture(IOptions<HybridCacheFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public HybridCacheFixture(HybridCacheFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public HybridCache Cache => _cache
        ?? throw new InvalidOperationException("HybridCacheFixture not initialised — call InitializeAsync.");

    public override string ConnectionString => "hybrid-in-memory";

    public override Task InitializeAsync()
    {
        if (_cache is not null) return Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddHybridCache(o =>
        {
            o.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(_options.DefaultExpirationSeconds),
                LocalCacheExpiration = TimeSpan.FromSeconds(_options.LocalCacheExpirationSeconds),
            };
            o.MaximumPayloadBytes = _options.MaximumPayloadBytes;
            o.MaximumKeyLength = _options.MaximumKeyLength;
        });
        _provider = services.BuildServiceProvider();
        _cache = _provider.GetRequiredService<HybridCache>();
        return Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        _cache = null;
        if (_provider is not null)
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
            _provider = null;
        }
    }
}
