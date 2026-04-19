using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Caching.Fusion.Helpers;

/// <summary>
/// Pure-function decision logic for FusionCache's fail-safe fallback behaviour.
/// Determines whether a stale value should be returned when the producer factory throws.
/// </summary>
public static class FailSafeHelper
{
    /// <summary>
    /// Returns <c>true</c> when fail-safe fallback applies given the entry options and the
    /// elapsed time since the entry became stale. Fail-safe applies when:
    /// 1. <see cref="FusionCacheEntryOptions.IsFailSafeEnabled"/> is true, AND
    /// 2. <paramref name="elapsed"/> is within <see cref="FusionCacheEntryOptions.FailSafeMaxDuration"/>.
    /// </summary>
    public static bool IsFailSafeApplicable(FusionCacheEntryOptions options, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed must be non-negative.");
        }

        if (!options.IsFailSafeEnabled)
        {
            return false;
        }

        return elapsed <= options.FailSafeMaxDuration;
    }
}
