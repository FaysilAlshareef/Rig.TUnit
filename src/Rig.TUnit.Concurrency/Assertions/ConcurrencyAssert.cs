namespace Rig.TUnit.Concurrency.Assertions;

/// <summary>
/// Runs two writers in parallel and asserts exactly one succeeds while the other
/// throws the expected concurrency exception (typically <c>DbUpdateConcurrencyException</c>
/// from EF, <c>MongoWriteException</c> from Mongo, or a Cosmos 412).
/// </summary>
public sealed class ConcurrencyAssert<TEntity>
{
    private readonly TEntity _entity;
    internal ConcurrencyAssert(TEntity entity) { _entity = entity; }

    public static ConcurrencyAssert<TEntity> TwoWriters(TEntity entity) => new(entity);

    /// <summary>
    /// Runs <paramref name="writerA"/> and <paramref name="writerB"/> concurrently
    /// and asserts exactly one throws <typeparamref name="TException"/> while the
    /// other completes normally.
    /// </summary>
    public async Task OneWinsWith<TException>(Func<TEntity, Task> writerA, Func<TEntity, Task> writerB)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(writerA);
        ArgumentNullException.ThrowIfNull(writerB);

        var a = Task.Run(async () =>
        {
            try { await writerA(_entity); return (Winner: true, Loser: (Exception?)null); }
            catch (TException ex) { return (Winner: false, Loser: (Exception?)ex); }
        });
        var b = Task.Run(async () =>
        {
            try { await writerB(_entity); return (Winner: true, Loser: (Exception?)null); }
            catch (TException ex) { return (Winner: false, Loser: (Exception?)ex); }
        });
        var ra = await a;
        var rb = await b;

        var winners = (ra.Winner ? 1 : 0) + (rb.Winner ? 1 : 0);
        if (winners != 1)
        {
            throw new ConcurrencyAssertionException(
                $"Expected exactly one writer to win but {winners} won.");
        }
    }
}

public static class ConcurrencyAssert
{
    public static ConcurrencyAssert<T> TwoWriters<T>(T entity) => ConcurrencyAssert<T>.TwoWriters(entity);
}

public sealed class ConcurrencyAssertionException : Exception
{
    public ConcurrencyAssertionException(string message) : base(message) { }
}

/// <summary>
/// Fluent helpers for HTTP precondition assertions — <c>If-Match</c> / <c>If-None-Match</c>.
/// </summary>
public static class Precondition
{
    public static async Task IfMatchFails(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if ((int)response.StatusCode != 412)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new ConcurrencyAssertionException(
                $"Expected HTTP 412 Precondition Failed but got {(int)response.StatusCode}. Body: {body}");
        }
    }

    public static Task NotModified(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if ((int)response.StatusCode != 304)
        {
            throw new ConcurrencyAssertionException(
                $"Expected HTTP 304 Not Modified but got {(int)response.StatusCode}.");
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tracks per-aggregate sequence numbers and asserts idempotent application.
/// Mirror of the projection-side inbox pattern.
/// </summary>
public sealed class SequenceIdempotencyChecker
{
    private readonly Dictionary<string, long> _applied = new();

    public bool TryApply(string aggregateId, long sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        lock (_applied)
        {
            if (_applied.TryGetValue(aggregateId, out var last) && sequence <= last)
            {
                return false;
            }
            _applied[aggregateId] = sequence;
            return true;
        }
    }

    public long? LastApplied(string aggregateId)
    {
        lock (_applied)
        {
            return _applied.TryGetValue(aggregateId, out var last) ? last : null;
        }
    }
}
