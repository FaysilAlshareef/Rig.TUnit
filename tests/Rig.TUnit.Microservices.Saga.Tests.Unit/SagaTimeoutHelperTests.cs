using Microsoft.Extensions.Time.Testing;
using Rig.TUnit.Microservices.Saga.Helpers;

namespace Rig.TUnit.Microservices.Saga.Tests.Unit;

public sealed class SagaTimeoutHelperTests
{
    [Test]
    public async Task HasTimedOut_Over_ReturnsTrue()
    {
        var result = SagaTimeoutHelper.HasTimedOut(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HasTimedOut_Under_ReturnsFalse()
    {
        var result = SagaTimeoutHelper.HasTimedOut(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HasTimedOut_Equal_ReturnsFalse()
    {
        var result = SagaTimeoutHelper.HasTimedOut(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HasTimedOut_NegativeElapsed_Throws()
    {
        await Assert.That(() => SagaTimeoutHelper.HasTimedOut(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1)))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task HasTimedOut_ZeroTimeout_Throws()
    {
        await Assert.That(() => SagaTimeoutHelper.HasTimedOut(TimeSpan.FromSeconds(1), TimeSpan.Zero))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ElapsedSince_NullClock_Throws()
    {
        await Assert.That(() => SagaTimeoutHelper.ElapsedSince(DateTimeOffset.UtcNow, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ElapsedSince_WithFakeClock_ReturnsExactElapsed()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(start);
        clock.Advance(TimeSpan.FromSeconds(42));
        var elapsed = SagaTimeoutHelper.ElapsedSince(start, clock);
        await Assert.That(elapsed).IsEqualTo(TimeSpan.FromSeconds(42));
    }

    [Test]
    public async Task ElapsedSince_FutureStart_ReturnsZero()
    {
        var start = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var elapsed = SagaTimeoutHelper.ElapsedSince(start, clock);
        await Assert.That(elapsed).IsEqualTo(TimeSpan.Zero);
    }
}
