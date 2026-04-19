using Microsoft.Extensions.Time.Testing;

namespace Rig.TUnit.Resilience;

/// <summary>
/// Wraps <see cref="FakeTimeProvider"/> so Polly pipelines advance deterministically
/// without using <c>Task.Delay</c>. Tests call <see cref="Advance"/> to jump forward.
/// </summary>
public sealed class ResilienceClock
{
    public FakeTimeProvider TimeProvider { get; }

    public ResilienceClock(DateTimeOffset? initial = null)
    {
        TimeProvider = new FakeTimeProvider(initial ?? DateTimeOffset.UtcNow);
    }

    public void Advance(TimeSpan by) => TimeProvider.Advance(by);
    public void SetUtcNow(DateTimeOffset when) => TimeProvider.SetUtcNow(when);
    public DateTimeOffset Now => TimeProvider.GetUtcNow();
}
