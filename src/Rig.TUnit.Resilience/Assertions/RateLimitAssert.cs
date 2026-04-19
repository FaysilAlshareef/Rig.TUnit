namespace Rig.TUnit.Resilience.Assertions;

/// <summary>
/// Fluent assertions over rate-limiter behaviour. The caller supplies a
/// <see cref="RateLimitTelemetry"/> populated via their Polly rate-limit events.
/// </summary>
public sealed class RateLimitAssert
{
    private readonly RateLimitTelemetry _telemetry;
    private int? _expectedPermits;
    private TimeSpan? _window;

    private RateLimitAssert(RateLimitTelemetry telemetry) { _telemetry = telemetry; }

    public static RateLimitAssert For(RateLimitTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        return new RateLimitAssert(telemetry);
    }

    public RateLimitAssert Permits(int expected) { _expectedPermits = expected; return this; }
    public RateLimitAssert PerSecond() { _window = TimeSpan.FromSeconds(1); return this; }
    public RateLimitAssert Per(TimeSpan window) { _window = window; return this; }

    public RateLimitAssert Rejects(int overLimit)
    {
        if (_telemetry.RejectedCount < overLimit)
        {
            throw new ResilienceAssertionException(
                $"Expected at least {overLimit} rejections but observed {_telemetry.RejectedCount}.");
        }
        return this;
    }

    public RateLimitAssert Permitted(int expected)
    {
        if (_telemetry.PermittedCount != expected)
        {
            throw new ResilienceAssertionException(
                $"Expected {expected} permitted calls but observed {_telemetry.PermittedCount}.");
        }
        return this;
    }
}

public sealed class RateLimitTelemetry
{
    private int _permitted;
    private int _rejected;
    public int PermittedCount => _permitted;
    public int RejectedCount => _rejected;
    public void RecordPermitted() => Interlocked.Increment(ref _permitted);
    public void RecordRejected() => Interlocked.Increment(ref _rejected);
}
