using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Rig.TUnit.Databases.Sql.Helpers;

/// <summary>
/// Thin wrapper around <see cref="IDbContextTransaction"/> with automatic rollback
/// when the test throws — use via <c>await using</c>.
/// </summary>
public sealed class TransactionScope : IAsyncDisposable
{
    private readonly IDbContextTransaction _tx;
    private bool _committed;

    private TransactionScope(IDbContextTransaction tx)
    {
        _tx = tx;
    }

    public static async Task<TransactionScope> BeginAsync(DbContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        return new TransactionScope(tx);
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        _committed = true;
        return _tx.CommitAsync(ct);
    }

    public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            // Best-effort rollback during dispose: a prior commit/rollback may have already
            // closed the transaction. EF throws InvalidOperationException in that case; it
            // is safe to ignore because the test outcome is already established.
            try
            {
                await _tx.RollbackAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
            }
        }
        await _tx.DisposeAsync().ConfigureAwait(false);
    }
}
