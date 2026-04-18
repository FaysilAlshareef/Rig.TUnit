using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.KurrentDb;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;

public sealed class KurrentDbFixture : DocumentFixtureBase
{
    private KurrentDbContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new KurrentDbBuilder("kurrentplatform/kurrentdb:25.1").Build();
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
