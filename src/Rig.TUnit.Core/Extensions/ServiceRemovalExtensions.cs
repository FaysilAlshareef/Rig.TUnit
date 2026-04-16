using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Core.Extensions;

public static class ServiceRemovalExtensions
{
    /// <summary>Removes a service registration by service type.</summary>
    public static IServiceCollection RemoveService<TService>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor != null) services.Remove(descriptor);
        return services;
    }

    /// <summary>Removes a service registration by implementation type.</summary>
    public static IServiceCollection RemoveImplementation<TImpl>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(TImpl));
        if (descriptor != null) services.Remove(descriptor);
        return services;
    }

    /// <summary>Removes all registrations whose ServiceType.FullName contains the given name.</summary>
    public static IServiceCollection RemoveByName(this IServiceCollection services, string typeName)
    {
        var toRemove = services.Where(d => d.ServiceType.FullName?.Contains(typeName) == true).ToList();
        foreach (var d in toRemove) services.Remove(d);
        return services;
    }
}
