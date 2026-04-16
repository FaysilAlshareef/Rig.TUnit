using Testcontainers.ServiceBus;
using TUnit.Core.Interfaces;

namespace Rig.TUnit.ServiceBus.Fixtures;

public sealed class ServiceBusFixture : IAsyncInitializer, IAsyncDisposable
{
    private ServiceBusContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Service Bus container not started");

    public string ConfigFilePath { get; init; } = "TestInfrastructure/service-bus-config.json";

    public async Task InitializeAsync()
    {
        var builder = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true);

        var resolvedPath = Path.GetFullPath(ConfigFilePath);
        if (File.Exists(resolvedPath))
        {
            builder = builder.WithConfig(resolvedPath);
        }

        _container = builder.Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }
}
