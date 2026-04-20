using Rig.TUnit.Resilience;
using Rig.TUnit.Resilience.Assertions;

namespace Rig.TUnit.Resilience.Tests.Unit;

/// <summary>
/// Unit coverage for resilience helpers — no live Polly pipelines, pure helper exercise.
/// Polly-integrated scenarios live in Rig.TUnit.Resilience.Tests.Integration.
/// </summary>
public sealed class ResilienceUnitTests
{
    [Test]
    public async Task Advance_WithTenSecondDelta_MovesClockForward()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new ResilienceClock(start);

        clock.Advance(TimeSpan.FromSeconds(10));

        await Assert.That(clock.Now).IsEqualTo(start.AddSeconds(10));
    }

    [Test]
    public async Task SetUtcNow_WithFutureInstant_UpdatesClock()
    {
        var clock = new ResilienceClock(DateTimeOffset.UtcNow);
        var target = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        clock.SetUtcNow(target);

        await Assert.That(clock.Now).IsEqualTo(target);
    }

    [Test]
    public async Task Ctor_WithoutInitial_UsesCurrentUtc()
    {
        var clock = new ResilienceClock();

        await Assert.That(clock.TimeProvider).IsNotNull();
    }

    [Test]
    public async Task For_WithNullTelemetry_ThrowsArgumentNull()
    {
        await Assert.That(() => RetryAssert.For(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Count_WhenAttemptsMatchExpected_ReturnsFluentSelf()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.FromMilliseconds(100), null));
        telemetry.Record(new RetryAttempt(2, TimeSpan.FromMilliseconds(100), null));

        var asserter = RetryAssert.For(telemetry).Count(2);

        await Assert.That(asserter).IsNotNull();
    }

    [Test]
    public async Task Count_WhenAttemptsDiffer_ThrowsResilienceAssertion()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.Zero, null));

        await Assert.That(() => RetryAssert.For(telemetry).Count(5))
            .ThrowsExactly<ResilienceAssertionException>();
    }

    [Test]
    public async Task WithBackoffInterval_WhenAllAttemptsInRange_Passes()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.FromMilliseconds(100), null));
        telemetry.Record(new RetryAttempt(2, TimeSpan.FromMilliseconds(105), null));

        var asserter = RetryAssert.For(telemetry)
            .WithBackoffInterval(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10));

        await Assert.That(asserter).IsNotNull();
    }

    [Test]
    public async Task WithBackoffInterval_WhenOutOfTolerance_ThrowsResilienceAssertion()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.FromMilliseconds(300), null));

        await Assert.That(() => RetryAssert.For(telemetry)
                .WithBackoffInterval(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)))
            .ThrowsExactly<ResilienceAssertionException>();
    }
}
