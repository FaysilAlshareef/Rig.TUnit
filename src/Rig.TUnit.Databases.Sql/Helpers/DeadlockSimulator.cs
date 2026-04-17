using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.Helpers;

/// <summary>
/// Runs two SaveChangesAsync operations that acquire locks in opposing order,
/// deterministically reproducing a deadlock so retry-policies can be exercised.
/// </summary>
/// <remarks>
/// The helper inverts the normal "throw on conflict" contract intentionally: it reports
/// per-writer success/failure as a pair so the test can assert that exactly one side
/// was chosen as the deadlock victim. The <see cref="DbUpdateException"/> carries the
/// SQL Server 1205 / Postgres 40P01 deadlock signal; the test is the error boundary.
/// </remarks>
public static class DeadlockSimulator
{
    public static async Task<(bool firstSucceeded, bool secondSucceeded)> RunAsync(
        DbContext first,
        DbContext second,
        Func<DbContext, CancellationToken, Task> writerA,
        Func<DbContext, CancellationToken, Task> writerB,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(writerA);
        ArgumentNullException.ThrowIfNull(writerB);

        var task1 = RunOneAsync(first, writerA, ct);
        var task2 = RunOneAsync(second, writerB, ct);

        var results = await Task.WhenAll(task1, task2).ConfigureAwait(false);
        return (results[0], results[1]);
    }

    private static async Task<bool> RunOneAsync(
        DbContext context,
        Func<DbContext, CancellationToken, Task> writer,
        CancellationToken ct)
    {
        try
        {
            await writer(context, ct).ConfigureAwait(false);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            // Deadlock victim is expected here — this helper's entire purpose is to
            // normalize the exception into a pair of booleans so the caller's assertion
            // can verify the "exactly-one-winner" property.
            return false;
        }
    }
}
