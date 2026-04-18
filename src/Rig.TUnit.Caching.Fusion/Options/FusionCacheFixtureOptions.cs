using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Caching.Fusion.Options;

/// <summary>
/// Configuration for <see cref="Fixtures.FusionCacheFixture"/>. Controls default entry
/// Duration, fail-safe fallback window, and eager-refresh threshold (0..1) used by the
/// <c>ZiggyCreatures.Caching.Fusion</c> cache.
/// </summary>
public sealed class FusionCacheFixtureOptions
{
    public const string SectionName = "RigTUnit:FusionCache";

    [Range(1, 86400)]
    public int DefaultDurationSeconds { get; init; } = 60;

    public bool IsFailSafeEnabled { get; init; } = true;

    [Range(1, 86400 * 7)]
    public int FailSafeMaxDurationSeconds { get; init; } = 3600;

    /// <summary>Fraction of Duration after which eager refresh is triggered. 0.8 = refresh in background after 80% of TTL has elapsed.</summary>
    [Range(typeof(float), "0.01", "1.0")]
    public float EagerRefreshThreshold { get; init; } = 0.8f;
}
