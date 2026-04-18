using Cassandra;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Cassandra.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.Cassandra;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Fixtures;

public sealed class CassandraFixture : DocumentFixtureBase
{
    private readonly CassandraFixtureOptions _options;
    private CassandraContainer? _container;
    private Cluster? _cluster;
    private ISession? _session;

    public CassandraFixture() : this(new CassandraFixtureOptions()) { }

    public CassandraFixture(IOptions<CassandraFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public CassandraFixture(CassandraFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public ISession Session => _session
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new CassandraBuilder($"cassandra:{_options.ImageTag}").Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        var contactPoint = _container.Hostname;
        const int CassandraNativePort = 9042;
        var port = _container.GetMappedPublicPort(CassandraNativePort);
        _cluster = Cluster.Builder()
            .AddContactPoint(contactPoint)
            .WithPort(port)
            .Build();
        _session = await _cluster.ConnectAsync().ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.ShutdownAsync().ConfigureAwait(false);
            _session = null;
        }
        if (_cluster is not null)
        {
            await _cluster.ShutdownAsync().ConfigureAwait(false);
            _cluster = null;
        }
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
