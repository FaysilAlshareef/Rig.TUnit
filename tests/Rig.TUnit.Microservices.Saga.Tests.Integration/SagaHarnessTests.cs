using Rig.TUnit.Microservices.Saga;

namespace Rig.TUnit.Microservices.Saga.Tests.Integration;

public sealed class SagaHarnessTests
{
    [Test]
    public async Task RunAsync_WithAllStepsSucceeding_ReturnsSuccess()
    {
        var harness = new SagaHarness();
        var executed = new List<string>();

        harness.Add(new SagaStep("A", _ => { executed.Add("A"); return Task.CompletedTask; }));
        harness.Add(new SagaStep("B", _ => { executed.Add("B"); return Task.CompletedTask; }));

        var result = await harness.RunAsync();

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Completed.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RunAsync_WhenSecondStepFails_CompensatesFirstInReverse()
    {
        var harness = new SagaHarness();
        var compensations = new List<string>();

        harness.Add(new SagaStep(
            "A",
            _ => Task.CompletedTask,
            _ => { compensations.Add("A"); return Task.CompletedTask; }));
        harness.Add(new SagaStep(
            "B",
            _ => throw new InvalidOperationException("boom"),
            _ => Task.CompletedTask));

        var result = await harness.RunAsync();

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Compensated).Contains("A");
        await Assert.That(compensations).Contains("A");
    }

    [Test]
    public async Task SagaAssert_CompensatedInReverse_PassesOnExpectedOrder()
    {
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask, _ => Task.CompletedTask));
        harness.Add(new SagaStep("B", _ => Task.CompletedTask, _ => Task.CompletedTask));
        harness.Add(new SagaStep("C", _ => throw new InvalidOperationException(), _ => Task.CompletedTask));

        var result = await harness.RunAsync();
        SagaAssert.CompensatedInReverse(result, "B", "A");
        await Task.CompletedTask;
    }
}
