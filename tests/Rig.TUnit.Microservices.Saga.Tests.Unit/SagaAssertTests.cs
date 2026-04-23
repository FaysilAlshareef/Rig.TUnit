namespace Rig.TUnit.Microservices.Saga.Tests.Unit;

public sealed class SagaAssertTests
{
    [Test]
    public async Task SagaAssertionException_Message_IsPreserved()
    {
        var ex = new SagaAssertionException("saga failed");

        await Assert.That(ex.Message).IsEqualTo("saga failed");
    }

    [Test]
    public async Task CompensationFailure_Properties_AreSetCorrectly()
    {
        var failure = new CompensationFailure("StepA", "timeout");

        await Assert.That(failure.StepName).IsEqualTo("StepA");
        await Assert.That(failure.Reason).IsEqualTo("timeout");
    }

    [Test]
    public Task SagaAssert_CompensatedInReverse_CorrectOrder_DoesNotThrow()
    {
        var result = new SagaResult(false, ["A", "B"], ["B", "A"],
            Array.Empty<CompensationFailure>(), new InvalidOperationException());

        SagaAssert.CompensatedInReverse(result, "B", "A");
        return Task.CompletedTask;
    }

    [Test]
    public async Task SagaAssert_CompensatedInReverse_CountMismatch_ThrowsSagaAssertionException()
    {
        var result = new SagaResult(false, ["A", "B"], ["B"],
            Array.Empty<CompensationFailure>(), new InvalidOperationException());

        await Assert.That(() => SagaAssert.CompensatedInReverse(result, "B", "A"))
            .ThrowsExactly<SagaAssertionException>();
    }

    [Test]
    public async Task SagaAssert_CompensatedInReverse_OrderMismatch_ThrowsSagaAssertionException()
    {
        var result = new SagaResult(false, ["A", "B"], ["A", "B"],
            Array.Empty<CompensationFailure>(), new InvalidOperationException());

        await Assert.That(() => SagaAssert.CompensatedInReverse(result, "B", "A"))
            .ThrowsExactly<SagaAssertionException>();
    }

    [Test]
    public async Task SagaHarness_Add_NullStep_ThrowsArgumentNullException()
    {
        var harness = new SagaHarness();

        await Assert.That(() => harness.Add(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SagaHarness_RunAsync_AllStepsSucceed_ReturnsSuccessResult()
    {
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask));
        harness.Add(new SagaStep("B", _ => Task.CompletedTask));

        var result = await harness.RunAsync();

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Completed.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SagaHarness_RunAsync_StepFails_CompensatesCompletedStepsInReverse()
    {
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask, _ => Task.CompletedTask));
        harness.Add(new SagaStep("B", _ => throw new InvalidOperationException("boom")));

        var result = await harness.RunAsync();

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Compensated).IsEquivalentTo(new[] { "A" });
    }
}
