using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.Cassandra;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Fixtures;

public sealed class CassandraFixture : DocumentFixtureBase
{
    private CassandraContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new CassandraBuilder().WithImage("cassandra:5").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _container.StartAsync(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
