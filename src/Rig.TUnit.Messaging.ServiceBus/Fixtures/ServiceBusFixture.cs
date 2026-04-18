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

    public override async Task InitializeAsync()
    {
        if (_container is not null)
        {
            return;
        }

        _container = new ServiceBusBuilder($"mcr.microsoft.com/azure-messaging/servicebus-emulator:{_options.ImageTag}")
            .WithAcceptLicenseAgreement(_options.AcceptEula)
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
