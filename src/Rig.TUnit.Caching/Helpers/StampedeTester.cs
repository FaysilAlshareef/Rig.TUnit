namespace Rig.TUnit.Caching.Helpers;

/// <summary>
/// Fires simultaneous misses for the same key and asserts the producer was invoked
/// exactly once (stampede protection).
/// </summary>
public sealed class StampedeTester
{
    private readonly Func<string, CancellationToken, Task<string>> _producer;
    private int _invocations;

    public StampedeTester(Func<string, CancellationToken, Task<string>> producer)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    }

    public int Invocations => _invocations;

    public async Task<IReadOnlyList<string>> RaceAsync(string key, int concurrentCallers, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(concurrentCallers);

        var barrier = new TaskCompletionSource();
        var callers = Enumerable.Range(0, concurrentCallers)
            .Select(async _ =>
            {
                await barrier.Task.ConfigureAwait(false);
                Interlocked.Increment(ref _invocations);
                return await _producer(key, ct).ConfigureAwait(false);
            })
            .ToArray();

        barrier.SetResult();
        return await Task.WhenAll(callers).ConfigureAwait(false);
    }
}
