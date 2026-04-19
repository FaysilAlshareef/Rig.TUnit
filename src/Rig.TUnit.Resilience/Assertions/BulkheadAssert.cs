namespace Rig.TUnit.Resilience.Assertions;

/// <summary>
/// Fluent assertions over bulkhead (concurrency limiter) behaviour.
/// </summary>
public sealed class BulkheadAssert
{
    private readonly BulkheadTelemetry _telemetry;

    private BulkheadAssert(BulkheadTelemetry telemetry) { _telemetry = telemetry; }

    public static BulkheadAssert For(BulkheadTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        return new BulkheadAssert(telemetry);
    }

    public BulkheadAssert MaxConcurrencyObserved(int expected)
    {
        if (_telemetry.MaxConcurrency != expected)
        {
            throw new ResilienceAssertionException(
                $"Expected max concurrency {expected} but observed {_telemetry.MaxConcurrency}.");
        }
        return this;
    }

    public BulkheadAssert Rejected(int atLeast)
    {
        if (_telemetry.RejectedCount < atLeast)
        {
            throw new ResilienceAssertionException(
                $"Expected at least {atLeast} rejections but observed {_telemetry.RejectedCount}.");
        }
        return this;
    }
}

public sealed class BulkheadTelemetry
{
    private int _currentConcurrency;
    private int _maxConcurrency;
    private int _rejected;

    public int MaxConcurrency => _maxConcurrency;
    public int RejectedCount => _rejected;

    public void RecordEnter()
    {
        var cur = Interlocked.Increment(ref _currentConcurrency);
        InterlockedMax(ref _maxConcurrency, cur);
    }

    public void RecordExit() => Interlocked.Decrement(ref _currentConcurrency);
    public void RecordRejected() => Interlocked.Increment(ref _rejected);

    private static void InterlockedMax(ref int target, int candidate)
    {
        int snapshot;
        do
        {
            snapshot = target;
            if (candidate <= snapshot) return;
        }
        while (Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot);
    }
}

/// <summary>
/// Injects failures deterministically into an async workflow — used to drive
/// circuit breakers, retries, and other resilience primitives.
/// </summary>
public sealed class ChaosInjector
{
    private int _sequence;
    private readonly Func<int, bool> _shouldFail;

    public ChaosInjector(Func<int, bool> shouldFail)
    {
        _shouldFail = shouldFail ?? throw new ArgumentNullException(nameof(shouldFail));
    }

    public static ChaosInjector EveryNth(int n) => new(i => i % n == 0);
    public static ChaosInjector FailFirst(int count) => new(i => i <= count);

    public Task<T> InvokeAsync<T>(Func<Task<T>> body, Exception? toThrow = null)
    {
        var seq = Interlocked.Increment(ref _sequence);
        if (_shouldFail(seq))
        {
            throw toThrow ?? new InvalidOperationException($"ChaosInjector: induced failure at attempt {seq}.");
        }
        return body();
    }
}
