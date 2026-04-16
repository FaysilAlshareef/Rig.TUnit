using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.ServiceBus.Extensions;
using Rig.TUnit.ServiceBus.Fixtures;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Extensions;

[NotInParallel("ServiceBus")]
public class ServiceBusContainerExtensionsTests
{
    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseServiceBusContainer_ReplacesExistingRegistration(ServiceBusFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(_ => null!);

        // Act
        services.UseServiceBusContainer(fixture);

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        await Assert.That(client).IsNotNull();
        await Assert.That(client.IsClosed).IsFalse();
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseServiceBusContainer_CanCreateClientFromReplacedConnection(ServiceBusFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseServiceBusContainer(fixture);
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client.IsClosed).IsFalse();
    }
}
