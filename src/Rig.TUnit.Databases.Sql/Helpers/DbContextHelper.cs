using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Rig.TUnit.Databases.Sql.Helpers;

/// <summary>
/// EF-provider-agnostic CRUD helper used inside tests. Scoped instance per test;
/// disposes the wrapped <typeparamref name="TContext"/> when finished.
/// </summary>
/// <typeparam name="TContext">The application's DbContext.</typeparam>
public sealed class DbContextHelper<TContext>(TContext context) : IAsyncDisposable where TContext : DbContext
{
    public TContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        Func<TContext, IQueryable<T>> query,
        CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query(Context).AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task InsertAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Context.Set<T>().AddAsync(entity, ct).ConfigureAwait(false);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        await Context.Set<T>().AddRangeAsync(entities, ct).ConfigureAwait(false);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Update(entity);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SeedAsync(Func<TContext, CancellationToken, Task> seed, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(seed);
        await seed(Context, ct).ConfigureAwait(false);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SeedAsync(Action<TContext> seed, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(seed);
        seed(Context);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<T> WithTransactionAsync<T>(
        Func<TContext, IDbContextTransaction, CancellationToken, Task<T>> body,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        var tx = await Context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (tx)
        {
            var result = await body(Context, tx, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return result;
        }
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
