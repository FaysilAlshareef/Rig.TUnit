using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.SqlServer.Extensions;

public static class InMemoryDbExtensions
{
    public static IServiceCollection UseInMemoryDatabase<TContext>(
        this IServiceCollection services) where TContext : DbContext
    {
        services.RemoveByName(typeof(TContext).Name);
        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase($"test_{Guid.NewGuid():N}"));
        return services;
    }
}
