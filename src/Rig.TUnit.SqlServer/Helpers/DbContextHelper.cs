using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.SqlServer.Helpers;

/// <summary>
/// Test infrastructure helper that intentionally uses IServiceProvider to create
/// isolated scopes per operation, simulating separate request lifetimes in tests.
/// The service locator usage here is by design — this is not business logic.
/// </summary>
public sealed class DbContextHelper<TContext>(IServiceProvider provider) where TContext : DbContext
{
    public async Task<TResult> Query<TResult>(Func<TContext, Task<TResult>> query)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        return await query(context);
    }

    public async Task<T> InsertAsync<T>(T entity) where T : class
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return entity;
    }

    public async Task<List<T>> InsertAsync<T>(List<T> entities) where T : class
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Set<T>().AddRangeAsync(entities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return entities;
    }

    /// <summary>
    /// Invokes the supplied async seed action against a fresh DbContext scope, then commits the
    /// changes with <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> and clears the change tracker.
    /// </summary>
    public async Task SeedAsync(Func<TContext, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await seed(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Invokes the supplied synchronous seed action against a fresh DbContext scope, then commits the
    /// changes with <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> and clears the change tracker.
    /// </summary>
    public async Task SeedAsync(Action<TContext> seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        seed(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
