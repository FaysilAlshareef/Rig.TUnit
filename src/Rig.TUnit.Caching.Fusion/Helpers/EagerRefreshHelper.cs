using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Helpers;

/// <summary>
/// Pure-function decision logic for FusionCache's eager-refresh behaviour. Determines
/// whether a background refresh should be triggered for an entry that has entered its
/// eager window — e.g. refresh at 80% of TTL so the next consumer sees a fresh value.
/// </summary>
public static class EagerRefreshHelper
{
    /// <summary>
    /// Returns <c>true</c> when the elapsed time is inside the eager-refresh window
    /// (<c>Duration * EagerRefreshThreshold</c> ≤ elapsed &lt; <c>Duration</c>). Returns
    /// <c>false</c> if no threshold is configured, the window hasn't started yet, or the
    /// entry is already stale.
    /// </summary>
    public static bool ShouldEagerRefresh(FusionCacheEntryOptions options, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed must be non-negative.");
        }

        if (options.EagerRefreshThreshold is not float threshold)
        {
            return false;
        }

        var eagerStart = TimeSpan.FromTicks((long)(options.Duration.Ticks * threshold));
        return elapsed >= eagerStart && elapsed < options.Duration;
    }
}
