using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.ServiceBus.Builder;
using Rig.TUnit.ServiceBus.Fixtures;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Builder;

public class ServiceBusRigBuilderTests
{
    private sealed class Wrapper(string connectionString)
    {
        public string ConnectionString { get; } = connectionString;
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceClient_ViaBuilder_ReplacesServiceBusClient(ServiceBusFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRigTUnit(rig =>
            rig.UseServiceBus(fixture, sb => sb.ReplaceClient()));

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        await Assert.That(client).IsNotNull();
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceClient_CustomWrapper_ReceivesConnectionString(ServiceBusFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRigTUnit(rig =>
            rig.UseServiceBus(fixture, sb =>
                sb.ReplaceClient(conn => new Wrapper(conn))));

        // Assert
        var provider = services.BuildServiceProvider();
        var wrapper = provider.GetRequiredService<Wrapper>();
        await Assert.That(wrapper.ConnectionString).IsEqualTo(fixture.ConnectionString);
    }
}
