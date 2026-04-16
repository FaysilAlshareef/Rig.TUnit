namespace Rig.TUnit.Core.Helpers;

/// <summary>
/// Generic async polling utility. Waits for a condition to become true.
/// </summary>
public static class WaitHelper
{
    /// <summary>
    /// Polls a synchronous condition until it returns true or the timeout is exceeded.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown when the timeout is exceeded.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the external token is cancelled.</exception>
    public static async Task WaitForAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (!condition())
            {
                await Task.Delay(interval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Condition not met within {timeout.TotalSeconds:F1}s timeout.");
        }
    }

    /// <summary>
    /// Polls an async condition until it returns true or the timeout is exceeded.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown when the timeout is exceeded.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the external token is cancelled.</exception>
    public static async Task WaitForAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (!await condition())
            {
                await Task.Delay(interval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Condition not met within {timeout.TotalSeconds:F1}s timeout.");
        }
    }

    /// <summary>
    /// Polls until a value is returned (non-null) or the timeout is exceeded.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown when the timeout is exceeded.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the external token is cancelled.</exception>
    public static async Task<T> WaitForResultAsync<T>(
        Func<Task<T?>> producer,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (true)
            {
                var result = await producer();
                if (result is not null) return result;

                await Task.Delay(interval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Result not produced within {timeout.TotalSeconds:F1}s timeout.");
        }
    }
}
