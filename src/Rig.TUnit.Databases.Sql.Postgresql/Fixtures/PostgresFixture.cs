using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.Postgresql.Options;
using Testcontainers.PostgreSql;

namespace Rig.TUnit.Databases.Sql.Postgresql.Fixtures;

/// <summary>
/// Testcontainers-backed PostgreSQL fixture. Uses the official <c>postgres</c> image.
/// </summary>
public sealed class PostgresFixture : SqlFixtureBase
{
    private readonly PostgresFixtureOptions _options;
    private PostgreSqlContainer? _container;

    public PostgresFixture() : this(new PostgresFixtureOptions()) { }

    public PostgresFixture(IOptions<PostgresFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public PostgresFixture(PostgresFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run before ConnectionString is available");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new PostgreSqlBuilder($"postgres:{_options.ImageTag}")
            .WithUsername(_options.Username)
            .WithPassword(_options.Password)
            .WithDatabase(_options.Database)
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
