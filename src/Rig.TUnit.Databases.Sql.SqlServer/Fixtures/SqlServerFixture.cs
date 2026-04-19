using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.SqlServer.Options;
using Testcontainers.MsSql;

namespace Rig.TUnit.Databases.Sql.SqlServer.Fixtures;

/// <summary>
/// Testcontainers-backed SQL Server fixture. Managed lifecycle via TUnit's
/// <c>IAsyncInitializer</c>/<c>IAsyncDisposable</c>.
/// </summary>
public sealed class SqlServerFixture : SqlFixtureBase
{
    private readonly SqlServerFixtureOptions _options;
    private MsSqlContainer? _container;

    /// <summary>Parameterless ctor — uses defaults; most tests bind options via DI.</summary>
    public SqlServerFixture() : this(new SqlServerFixtureOptions())
    {
    }

    public SqlServerFixture(IOptions<SqlServerFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public SqlServerFixture(SqlServerFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run before ConnectionString is available");

    public override string DatabaseName => IsolationKey.ForSqlServerDatabase();

    public override async Task InitializeAsync()
    {
        if (_container is not null)
        {
            return;
        }

        _container = new MsSqlBuilder($"mcr.microsoft.com/mssql/server:{_options.ImageTag}")
            .WithPassword(_options.SaPassword)
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
