using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.Oracle.Options;
using Testcontainers.Oracle;

namespace Rig.TUnit.Databases.Sql.Oracle.Fixtures;

/// <summary>
/// Testcontainers-backed Oracle fixture. Uses the Oracle Free image via the
/// <see cref="OracleBuilder"/> — boots in ~60-90s on a warm Docker daemon.
/// </summary>
public sealed class OracleFixture : SqlFixtureBase
{
    private readonly OracleFixtureOptions _options;
    private OracleContainer? _container;

    public OracleFixture() : this(new OracleFixtureOptions()) { }

    public OracleFixture(IOptions<OracleFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public OracleFixture(OracleFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run before ConnectionString is available");

    public override string DatabaseName => _options.Username;

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new OracleBuilder(_options.Image)
            .WithUsername(_options.Username)
            .WithPassword(_options.Password)
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
