using Rig.TUnit.Resilience.Assertions;

namespace Rig.TUnit.Resilience.Tests.Unit;

public sealed class BulkheadAssertTests
{
    [Test]
    public async Task For_NullTelemetry_ThrowsArgumentNullException()
    {
        await Assert.That(() => BulkheadAssert.For(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task MaxConcurrencyObserved_Matching_DoesNotThrow()
    {
        var telemetry = new BulkheadTelemetry();
        telemetry.RecordEnter();
        telemetry.RecordExit();

        BulkheadAssert.For(telemetry).MaxConcurrencyObserved(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task MaxConcurrencyObserved_Mismatch_ThrowsResilienceAssertionException()
    {
        var telemetry = new BulkheadTelemetry();
        telemetry.RecordEnter();

        await Assert.That(() => BulkheadAssert.For(telemetry).MaxConcurrencyObserved(5))
            .ThrowsExactly<ResilienceAssertionException>();
    }

    [Test]
    public Task Rejected_AtLeastMet_DoesNotThrow()
    {
        var telemetry = new BulkheadTelemetry();
        telemetry.RecordRejected();
        telemetry.RecordRejected();

        BulkheadAssert.For(telemetry).Rejected(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Rejected_TooFew_ThrowsResilienceAssertionException()
    {
        var telemetry = new BulkheadTelemetry();

        await Assert.That(() => BulkheadAssert.For(telemetry).Rejected(1))
            .ThrowsExactly<ResilienceAssertionException>();
    }

    [Test]
    public async Task ChaosInjector_EveryNth_InducesFailureAtNthAttempt()
    {
        var injector = ChaosInjector.EveryNth(2);

        var result1 = await injector.InvokeAsync(() => Task.FromResult<string?>("ok"));
        await Assert.That(result1).IsEqualTo("ok");

        await Assert.That(() => injector.InvokeAsync(() => Task.FromResult<string?>("ok")))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ChaosInjector_FailFirst_InducesFailureForInitialAttempts()
    {
        var injector = ChaosInjector.FailFirst(1);

        await Assert.That(() => injector.InvokeAsync(() => Task.FromResult<string?>("ok")))
            .ThrowsExactly<InvalidOperationException>();

        var result = await injector.InvokeAsync(() => Task.FromResult<string?>("ok"));
        await Assert.That(result).IsEqualTo("ok");
    }

    [Test]
    public async Task ChaosInjector_NullShouldFail_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ChaosInjector(null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
