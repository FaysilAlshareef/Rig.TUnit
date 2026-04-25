using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.ServiceBus.Options;
using Testcontainers.ServiceBus;

namespace Rig.TUnit.Messaging.ServiceBus.Fixtures;

/// <summary>
/// Microsoft-official ServiceBus emulator fixture (<c>mcr.microsoft.com/azure-messaging/servicebus-emulator</c>)
/// paired with the required SQL Edge sidecar. Sets <c>ACCEPT_EULA=Y</c> via options.
/// </summary>
public sealed class ServiceBusFixture : MessagingFixtureBase
{
    private readonly ServiceBusFixtureOptions _options;
    private ServiceBusContainer? _container;

    public ServiceBusFixture() : this(new ServiceBusFixtureOptions())
    {
    }

    public ServiceBusFixture(IOptions<ServiceBusFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public ServiceBusFixture(ServiceBusFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (!_options.AcceptEula)
        {
            throw new InvalidOperationException("ServiceBusFixtureOptions.AcceptEula must be true");
        }
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first");

    /// <summary>
    /// Admin/management connection string for use with
    /// <c>ServiceBusAdministrationClient</c>. The ServiceBus emulator exposes
    /// management operations on a separate HTTP port (5300 by default), distinct
    /// from the AMQP messaging port (5672). The standard <see cref="ConnectionString"/>
    /// only carries the AMQP endpoint — admin-client requests against it produce
    /// <c>HttpIOException: The response ended prematurely</c> because the AMQP
    /// port doesn't speak HTTP. Use this property when constructing
    /// <c>ServiceBusAdministrationClient</c> instances.
    /// See <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator#choosing-the-right-connection-string">
    /// official docs</see> and
    /// <see href="https://github.com/testcontainers/testcontainers-dotnet/blob/develop/src/Testcontainers.ServiceBus/ServiceBusContainer.cs">
    /// Testcontainers source</see>.
    /// </summary>
    public string AdminConnectionString => _container?.GetHttpConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first");

    public override async Task InitializeAsync()
    {
        if (_container is not null)
        {
            return;
        }

        // Testcontainers' WithConfig() wraps the path in `new FileInfo(path)`,
        // which resolves relative paths against `Environment.CurrentDirectory`
        // — that's the dotnet-test invocation directory (often the repo root),
        // NOT the test-exe's bin folder where the JSON is copied. Resolve to an
        // absolute path against AppContext.BaseDirectory so the mount is always
        // pointed at the file copied into the test output. Without this, the
        // Testcontainers volume mount silently picks up a non-existent file,
        // the emulator starts with empty config, and admin operations later
        // fail with MessagingEntityNotFound.
        var configPath = Path.IsPathRooted(_options.ConfigFilePath)
            ? _options.ConfigFilePath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.ConfigFilePath));

        _container = new ServiceBusBuilder($"mcr.microsoft.com/azure-messaging/servicebus-emulator:{_options.ImageTag}")
            .WithAcceptLicenseAgreement(_options.AcceptEula)
            .WithConfig(configPath)
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
