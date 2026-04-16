using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.ServiceBus.Fixtures;

namespace Rig.TUnit.ServiceBus.Extensions;

public static class ServiceBusContainerExtensions
{
    public static IServiceCollection UseServiceBusContainer(
        this IServiceCollection services,
        ServiceBusFixture fixture)
    {
        services.RemoveService<ServiceBusClient>();
        services.AddSingleton(new ServiceBusClient(fixture.ConnectionString));
        return services;
    }
}
