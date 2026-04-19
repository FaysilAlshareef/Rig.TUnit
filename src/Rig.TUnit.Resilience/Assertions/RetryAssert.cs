namespace Rig.TUnit.Resilience.Assertions;

/// <summary>
/// Fluent assertions over observed retry counts and backoff intervals. The caller
/// supplies a <c>RetryTelemetry</c> (populated by their Polly OnRetry hook).
/// </summary>
public sealed class RetryAssert
{
    private readonly RetryTelemetry _telemetry;

    private RetryAssert(RetryTelemetry telemetry) { _telemetry = telemetry; }

    public static RetryAssert For(RetryTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        return new RetryAssert(telemetry);
    }

    public RetryAssert Count(int expected)
    {
        if (_telemetry.Attempts.Count != expected)
        {
            throw new ResilienceAssertionException(
                $"Expected {expected} retry attempts but observed {_telemetry.Attempts.Count}.");
        }
        return this;
    }

    public RetryAssert WithBackoffInterval(TimeSpan expected, TimeSpan? tolerance = null)
    {
        tolerance ??= TimeSpan.FromMilliseconds(10);
        foreach (var attempt in _telemetry.Attempts)
        {
            var diff = (attempt.Delay - expected).Duration();
            if (diff > tolerance)
            {
                throw new ResilienceAssertionException(
                    $"Expected backoff interval {expected} (±{tolerance}) but observed {attempt.Delay}.");
            }
        }
        return this;
    }
}

/// <summary>Record of a single retry attempt. Populated by the caller's OnRetry hook.</summary>
public sealed record RetryAttempt(int AttemptNumber, TimeSpan Delay, Exception? LastException);

/// <summary>Container captured by the caller's OnRetry hook and inspected via <see cref="RetryAssert"/>.</summary>
public sealed class RetryTelemetry
{
    private readonly List<RetryAttempt> _attempts = new();
    public IReadOnlyList<RetryAttempt> Attempts => _attempts;
    public void Record(RetryAttempt attempt) => _attempts.Add(attempt);
}
