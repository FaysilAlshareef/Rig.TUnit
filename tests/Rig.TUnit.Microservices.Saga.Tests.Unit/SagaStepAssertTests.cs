using Rig.TUnit.Microservices.Saga;
using Rig.TUnit.Microservices.Saga.Assertions;

namespace Rig.TUnit.Microservices.Saga.Tests.Unit;

public sealed class SagaStepAssertTests
{
    private static SagaResult SampleResult(bool success, string[] completed, string[] compensated)
        => new(success, completed, compensated, Array.Empty<CompensationFailure>(), success ? null : new InvalidOperationException("boom"));

    [Test]
    public async Task For_WithNullResult_Throws()
    {
        await Assert.That(() => SagaStepAssert.For(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Succeeded_WhenSagaSucceeded_DoesNotThrow()
    {
        var result = SampleResult(true, new[] { "a" }, Array.Empty<string>());
        var thrown = false;
        try { SagaStepAssert.For(result).Succeeded(); } catch (SagaStepAssertionException) { thrown = true; }
        await Assert.That(thrown).IsFalse();
    }

    [Test]
    public async Task Succeeded_WhenSagaFailed_Throws()
    {
        var result = SampleResult(false, new[] { "a" }, new[] { "a" });
        await Assert.That(() => SagaStepAssert.For(result).Succeeded())
            .ThrowsExactly<SagaStepAssertionException>();
    }

    [Test]
    public async Task Failed_WhenSagaFailed_DoesNotThrow()
    {
        var result = SampleResult(false, new[] { "a" }, new[] { "a" });
        var thrown = false;
        try { SagaStepAssert.For(result).Failed(); } catch (SagaStepAssertionException) { thrown = true; }
        await Assert.That(thrown).IsFalse();
    }

    [Test]
    public async Task Step_WhenCompleted_DoesNotThrow()
    {
        var result = SampleResult(true, new[] { "a", "b" }, Array.Empty<string>());
        var thrown = false;
        try { SagaStepAssert.For(result).Step("a").Completed(); } catch (SagaStepAssertionException) { thrown = true; }
        await Assert.That(thrown).IsFalse();
    }

    [Test]
    public async Task Step_WhenMissing_Throws()
    {
        var result = SampleResult(true, new[] { "a" }, Array.Empty<string>());
        await Assert.That(() => SagaStepAssert.For(result).Step("b").Completed())
            .ThrowsExactly<SagaStepAssertionException>();
    }

    [Test]
    public async Task Step_WhenCompensated_DoesNotThrow()
    {
        var result = SampleResult(false, new[] { "a" }, new[] { "a" });
        var thrown = false;
        try { SagaStepAssert.For(result).Step("a").Compensated(); } catch (SagaStepAssertionException) { thrown = true; }
        await Assert.That(thrown).IsFalse();
    }

    [Test]
    public async Task NotCompensated_WhenWasCompensated_Throws()
    {
        var result = SampleResult(false, new[] { "a" }, new[] { "a" });
        await Assert.That(() => SagaStepAssert.For(result).Step("a").NotCompensated())
            .ThrowsExactly<SagaStepAssertionException>();
    }

    [Test]
    public async Task Step_WithNullName_Throws()
    {
        var result = SampleResult(true, new[] { "a" }, Array.Empty<string>());
        await Assert.That(() => SagaStepAssert.For(result).Step(null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
