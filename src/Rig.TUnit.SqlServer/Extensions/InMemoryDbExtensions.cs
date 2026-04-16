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

        // Capture a stable database name so every scope in this container points at the same
        // in-memory store. Without this the options delegate would regenerate the Guid per scope.
        var databaseName = $"test_{Guid.NewGuid():N}";

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        return services;
    }
}
