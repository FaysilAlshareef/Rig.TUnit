using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.ServiceBus.Builder;

/// <summary>
/// Fluent builder that wires Azure Service Bus test infrastructure into an <see cref="IServiceCollection"/>.
/// </summary>
public sealed class ServiceBusRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _source;

    internal ServiceBusRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _source = source;
    }

    /// <summary>
    /// Replaces the default <see cref="ServiceBusClient"/> registration with one connected to the
    /// source connection string.
    /// </summary>
    public ServiceBusRigBuilder ReplaceClient()
    {
        _services.RemoveService<ServiceBusClient>();
        _services.AddSingleton(new ServiceBusClient(_source.ConnectionString));
        return this;
    }

    /// <summary>
    /// Replaces a custom Service Bus client wrapper with a factory that receives the source connection string.
    /// </summary>
    public ServiceBusRigBuilder ReplaceClient<TClient>(Func<string, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        _services.RemoveService<TClient>();
        _services.AddSingleton<TClient>(_ => factory(_source.ConnectionString));
        return this;
    }

    /// <summary>
    /// Replaces a custom Service Bus client wrapper with a factory that receives the source connection string
    /// and the resolving <see cref="IServiceProvider"/>.
    /// </summary>
    public ServiceBusRigBuilder ReplaceClient<TClient>(Func<string, IServiceProvider, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        _services.RemoveService<TClient>();
        _services.AddSingleton<TClient>(sp => factory(_source.ConnectionString, sp));
        return this;
    }
}
