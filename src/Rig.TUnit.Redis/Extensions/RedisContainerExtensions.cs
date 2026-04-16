using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.Redis.Fixtures;
using StackExchange.Redis;

namespace Rig.TUnit.Redis.Extensions;

public static class RedisContainerExtensions
{
    public static IServiceCollection UseRedisContainer(
        this IServiceCollection services,
        RedisFixture fixture)
    {
        services.RemoveService<IConnectionMultiplexer>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(fixture.ConnectionString));
        return services;
    }
}
