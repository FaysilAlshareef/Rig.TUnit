using Rig.TUnit.Caching.Helpers;

namespace Rig.TUnit.Caching.Tests.Unit;

public sealed class ClockControlTests
{
    [Test]
    public async Task ClockControl_TimeProvider_InitialisedToExpectedEpoch()
    {
        var clock = new ClockControl();
        var expected = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await Assert.That(clock.TimeProvider.GetUtcNow()).IsEqualTo(expected);
    }

    [Test]
    public async Task Advance_MovesTimeByDelta()
    {
        var clock = new ClockControl();
        var before = clock.TimeProvider.GetUtcNow();

        clock.Advance(TimeSpan.FromHours(1));

        await Assert.That(clock.TimeProvider.GetUtcNow()).IsEqualTo(before.AddHours(1));
    }

    [Test]
    public async Task SetUtcNow_SetsTimeToSpecifiedValue()
    {
        var clock = new ClockControl();
        var target = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);

        clock.SetUtcNow(target);

        await Assert.That(clock.TimeProvider.GetUtcNow()).IsEqualTo(target);
    }

    [Test]
    public async Task Advance_CalledTwice_IsAdditive()
    {
        var clock = new ClockControl();
        var start = clock.TimeProvider.GetUtcNow();

        clock.Advance(TimeSpan.FromMinutes(30));
        clock.Advance(TimeSpan.FromMinutes(30));

        await Assert.That(clock.TimeProvider.GetUtcNow()).IsEqualTo(start.AddHours(1));
    }
}
