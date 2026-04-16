using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Core.Builder;

public static class RigBuilderExtensions
{
    public static IServiceCollection AddRigTUnit(
        this IServiceCollection services,
        Action<RigBuilder> configure)
    {
        var builder = new RigBuilder(services);
        configure(builder);
        return services;
    }
}
