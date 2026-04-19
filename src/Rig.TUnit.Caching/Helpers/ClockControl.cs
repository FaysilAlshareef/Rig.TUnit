using Microsoft.Extensions.Time.Testing;

namespace Rig.TUnit.Caching.Helpers;

/// <summary>
/// Thin wrapper around <see cref="FakeTimeProvider"/> that gives cache tests a
/// deterministic clock for TTL / eager-refresh scenarios.
/// </summary>
public sealed class ClockControl
{
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public void Advance(TimeSpan delta) => TimeProvider.Advance(delta);

    public void SetUtcNow(DateTimeOffset utcNow) => TimeProvider.SetUtcNow(utcNow);
}
