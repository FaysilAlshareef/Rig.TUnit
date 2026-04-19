namespace Rig.TUnit.Caching.Assertions;

/// <summary>
/// High-level cache assertions. Each method takes an <see cref="ICacheProbe"/> bridge
/// that providers implement, so the assertion API stays provider-agnostic.
/// </summary>
public sealed class CacheAssert
{
    private CacheAssert() { }

    public static Task Stampede(ICacheProbe probe, string key, int concurrentCallers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.StampedeAsync(key, concurrentCallers, ct);
    }

    public static Task TagInvalidation(ICacheProbe probe, string tag, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.TagInvalidationAsync(tag, ct);
    }

    public static Task Coherent(ICacheProbe probe, string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.CoherentAsync(key, ct);
    }

    public static Task FailSafe(ICacheProbe probe, string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.FailSafeAsync(key, ct);
    }

    public static Task NegativeCached(ICacheProbe probe, string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.NegativeCachedAsync(key, ct);
    }

    public static Task HitRate(ICacheProbe probe, double expectedRatio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.HitRateAsync(expectedRatio, ct);
    }

    public static Task EagerRefresh(ICacheProbe probe, string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.EagerRefreshAsync(key, ct);
    }
}

public interface ICacheProbe
{
    Task StampedeAsync(string key, int concurrentCallers, CancellationToken ct);
    Task TagInvalidationAsync(string tag, CancellationToken ct);
    Task CoherentAsync(string key, CancellationToken ct);
    Task FailSafeAsync(string key, CancellationToken ct);
    Task NegativeCachedAsync(string key, CancellationToken ct);
    Task HitRateAsync(double expectedRatio, CancellationToken ct);
    Task EagerRefreshAsync(string key, CancellationToken ct);
}
