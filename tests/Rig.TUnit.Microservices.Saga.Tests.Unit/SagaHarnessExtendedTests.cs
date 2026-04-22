namespace Rig.TUnit.Microservices.Saga.Tests.Unit;

public sealed class SagaHarnessExtendedTests
{
    // ─── Failure types ────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_StepThrowsTimeoutException_ReturnsFailureAndCompensates()
    {
        // Arrange
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask, _ => Task.CompletedTask));
        harness.Add(new SagaStep("B", async _ => { await Task.CompletedTask; throw new TimeoutException("timed out"); }));

        // Act
        var result = await harness.RunAsync();

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Failure).IsAssignableTo<TimeoutException>();
        await Assert.That(result.Compensated).Contains("A");
    }

    [Test]
    public async Task RunAsync_StepThrowsHttpRequestException_ReturnsFailureAndCompensates()
    {
        // Arrange
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask, _ => Task.CompletedTask));
        harness.Add(new SagaStep("B", async _ => { await Task.CompletedTask; throw new HttpRequestException("network error"); }));

        // Act
        var result = await harness.RunAsync();

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Failure).IsAssignableTo<HttpRequestException>();
        await Assert.That(result.Compensated).Contains("A");
    }

    // ─── Step with Timeout (linked CancellationTokenSource path) ─────────────

    [Test]
    public async Task RunAsync_WithStepTimeout_ExecutesStepSuccessfully()
    {
        // Arrange — timeout is generous enough for the step to complete immediately
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A", _ => Task.CompletedTask,
            Timeout: TimeSpan.FromSeconds(5)));

        // Act
        var result = await harness.RunAsync();

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Completed).Contains("A");
    }

    // ─── Compensation failure capture ─────────────────────────────────────────

    [Test]
    public async Task RunAsync_CompensationThrowsInvalidOperationException_RecordsCompensationFailure()
    {
        // Arrange
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A",
            _ => Task.CompletedTask,
            async _ => { await Task.CompletedTask; throw new InvalidOperationException("compensation failed"); }));
        harness.Add(new SagaStep("B",
            async _ => { await Task.CompletedTask; throw new InvalidOperationException("step failed"); }));

        // Act
        var result = await harness.RunAsync();

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.CompensationFailures.Count).IsEqualTo(1);
        await Assert.That(result.CompensationFailures[0].StepName).IsEqualTo("A");
    }

    [Test]
    public async Task RunAsync_CompensationThrowsTimeoutException_RecordsCompensationFailure()
    {
        // Arrange
        var harness = new SagaHarness();
        harness.Add(new SagaStep("A",
            _ => Task.CompletedTask,
            async _ => { await Task.CompletedTask; throw new TimeoutException("compensation timed out"); }));
        harness.Add(new SagaStep("B",
            async _ => { await Task.CompletedTask; throw new InvalidOperationException("step failed"); }));

        // Act
        var result = await harness.RunAsync();

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.CompensationFailures.Count).IsEqualTo(1);
        await Assert.That(result.CompensationFailures[0].StepName).IsEqualTo("A");
    }
}
