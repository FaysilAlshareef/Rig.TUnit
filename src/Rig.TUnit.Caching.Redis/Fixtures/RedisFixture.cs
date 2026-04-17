using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Fixtures;
using Rig.TUnit.Caching.Redis.Options;
using Testcontainers.Redis;

namespace Rig.TUnit.Caching.Redis.Fixtures;

/// <summary>
/// Primary home for the Redis fixture. Both <c>Rig.TUnit.Caching.Redis</c> (cache role)
/// and <c>Rig.TUnit.Databases.NoSql.Redis</c> (KV role) share this instance.
/// </summary>
public sealed class RedisFixture : CacheFixtureBase
{
    private readonly RedisFixtureOptions _options;
    private RedisContainer? _container;

    public RedisFixture() : this(new RedisFixtureOptions())
    {
    }

    public RedisFixture(IOptions<RedisFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public RedisFixture(RedisFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first");

    public override async Task InitializeAsync()
    {
        if (_container is not null)
        {
            return;
        }

        _container = new RedisBuilder()
            .WithImage($"redis:{_options.ImageTag}")
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
