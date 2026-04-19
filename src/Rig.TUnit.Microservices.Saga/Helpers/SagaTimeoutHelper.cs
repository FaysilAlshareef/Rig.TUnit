namespace Rig.TUnit.Microservices.Saga.Helpers;

/// <summary>
/// Pure helper that determines whether a step should be considered timed-out
/// given elapsed time and a budget. Lets tests assert timeout behaviour using
/// an injected <see cref="TimeProvider"/> without relying on real clocks.
/// </summary>
public static class SagaTimeoutHelper
{
    /// <summary>
    /// Returns true when <paramref name="elapsed"/> strictly exceeds
    /// <paramref name="timeout"/>. Negative elapsed values are rejected.
    /// </summary>
    public static bool HasTimedOut(TimeSpan elapsed, TimeSpan timeout)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Elapsed time must be non-negative.");
        }
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
        }
        return elapsed > timeout;
    }

    /// <summary>
    /// Returns the elapsed time since <paramref name="startedAt"/> as measured
    /// by <paramref name="clock"/>. Pure function — use <c>FakeTimeProvider</c>
    /// in tests.
    /// </summary>
    public static TimeSpan ElapsedSince(DateTimeOffset startedAt, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var now = clock.GetUtcNow();
        if (now < startedAt) return TimeSpan.Zero;
        return now - startedAt;
    }
}
