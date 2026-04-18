using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.MySql.Options;
using Testcontainers.MySql;

namespace Rig.TUnit.Databases.Sql.MySql.Fixtures;

/// <summary>
/// Testcontainers-backed MySQL fixture. Uses the official <c>mysql</c> image.
/// Pomelo 9 consumes the connection string directly — no server-version auto-detect
/// required at fixture level (the caller's DbContext can call
/// <c>ServerVersion.AutoDetect(...)</c> when configuring the provider).
/// </summary>
public sealed class MySqlFixture : SqlFixtureBase
{
    private readonly MySqlFixtureOptions _options;
    private MySqlContainer? _container;

    public MySqlFixture() : this(new MySqlFixtureOptions()) { }

    public MySqlFixture(IOptions<MySqlFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public MySqlFixture(MySqlFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run before ConnectionString is available");

    public override string DatabaseName => _options.Database;

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new MySqlBuilder($"mysql:{_options.ImageTag}")
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
