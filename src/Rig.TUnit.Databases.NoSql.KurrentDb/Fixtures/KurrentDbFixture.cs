using KurrentDB.Client;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Rig.TUnit.Databases.NoSql.KurrentDb.Options;
using Testcontainers.KurrentDb;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;

public sealed class KurrentDbFixture : DocumentFixtureBase
{
    private readonly KurrentDbFixtureOptions _options;
    private KurrentDbContainer? _container;
    private KurrentDBClient? _client;

    public KurrentDbFixture() : this(new KurrentDbFixtureOptions()) { }

    public KurrentDbFixture(IOptions<KurrentDbFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public KurrentDbFixture(KurrentDbFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public KurrentDBClient Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new KurrentDbBuilder($"kurrentplatform/kurrentdb:{_options.ImageTag}").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        var settings = KurrentDBClientSettings.Create(_container.GetConnectionString());
        _client = new KurrentDBClient(settings);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync().ConfigureAwait(false);
            _client = null;
        }
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
