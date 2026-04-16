using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.SqlServer.Fixtures;

namespace Rig.TUnit.SqlServer.Extensions;

public static class SqlServerContainerExtensions
{
    public static IServiceCollection UseSqlServerContainerIsolated<TContext>(
        this IServiceCollection services,
        SqlServerFixture fixture) where TContext : DbContext
    {
        services.RemoveByName(typeof(TContext).Name);

        var dbName = $"test_{Guid.NewGuid():N}";
        var connectionString = $"{fixture.ConnectionString};Database={dbName}";

        services.AddDbContext<TContext>(options =>
            options.UseSqlServer(connectionString));

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Database.EnsureCreated();

        return services;
    }
}
