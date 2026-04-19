using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Cosmos.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Fixtures;

/// <summary>
/// Testcontainers-backed Cosmos emulator fixture. Uses the vnext-preview Linux
/// emulator image via the generic Testcontainers API — the dedicated
/// Testcontainers.CosmosDb module targets the legacy Windows emulator, which
/// doesn't run under Linux containers (testcontainers-dotnet#1306).
///
/// Windows runners cannot host the Linux emulator; Integration tests gate with
/// <c>[Category("cosmos")]</c> + runtime <c>OperatingSystem.IsWindows()</c> skip.
/// </summary>
public sealed class CosmosFixture : DocumentFixtureBase
{
    private readonly CosmosFixtureOptions _options;
    private IContainer? _container;

    public CosmosFixture() : this(new CosmosFixtureOptions()) { }

    public CosmosFixture(IOptions<CosmosFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public CosmosFixture(CosmosFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container is null
        ? throw new InvalidOperationException("InitializeAsync must run before ConnectionString is available")
        : BuildConnectionString(_container);

    public override string DatabaseName => _options.DatabaseName;

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new ContainerBuilder(_options.Image)
            .WithPortBinding(_options.HttpsGatewayPort, assignRandomHostPort: true)
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

    private string BuildConnectionString(IContainer container)
    {
        var port = container.GetMappedPublicPort(_options.HttpsGatewayPort);
        // Emulator well-known primary key (public, published by MS docs).
        const string wellKnownKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        return $"AccountEndpoint=https://{container.Hostname}:{port}/;AccountKey={wellKnownKey};";
    }
}
