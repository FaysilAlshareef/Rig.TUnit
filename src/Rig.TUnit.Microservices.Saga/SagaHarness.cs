namespace Rig.TUnit.Microservices.Saga;

public sealed record SagaStep(
    string Name,
    Func<CancellationToken, Task> Execute,
    Func<CancellationToken, Task>? Compensate = null,
    TimeSpan? Timeout = null);

public sealed class SagaHarness
{
    private readonly List<SagaStep> _steps = new();

    public SagaHarness Add(SagaStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
        return this;
    }

    public async Task<SagaResult> RunAsync(CancellationToken ct = default)
    {
        var completed = new List<string>();
        var compensationFailures = new List<CompensationFailure>();
        Exception? failure = null;

        foreach (var step in _steps)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (step.Timeout is { } t)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(t);
                    await step.Execute(cts.Token);
                }
                else
                {
                    await step.Execute(ct);
                }
                completed.Add(step.Name);
            }
            catch (InvalidOperationException ex) { failure = ex; break; }
            catch (TimeoutException ex) { failure = ex; break; }
            catch (HttpRequestException ex) { failure = ex; break; }
        }

        if (failure is null) return new SagaResult(true, completed, Array.Empty<string>(), compensationFailures, null);

        var compensated = new List<string>();
        for (var i = completed.Count - 1; i >= 0; i--)
        {
            var name = completed[i];
            var step = _steps.First(s => s.Name == name);
            if (step.Compensate is null) continue;
            try
            {
                await step.Compensate(CancellationToken.None);
                compensated.Add(name);
            }
            catch (InvalidOperationException ex)
            {
                compensationFailures.Add(new CompensationFailure(name, ex.Message));
            }
            catch (TimeoutException ex)
            {
                compensationFailures.Add(new CompensationFailure(name, ex.Message));
            }
        }
        return new SagaResult(false, completed, compensated, compensationFailures, failure);
    }
}

public sealed record SagaResult(
    bool Success,
    IReadOnlyList<string> Completed,
    IReadOnlyList<string> Compensated,
    IReadOnlyList<CompensationFailure> CompensationFailures,
    Exception? Failure);

public sealed record CompensationFailure(string StepName, string Reason);

public static class SagaAssert
{
    public static void CompensatedInReverse(SagaResult result, params string[] expectedCompensationOrder)
    {
        if (result.Compensated.Count != expectedCompensationOrder.Length)
        {
            throw new SagaAssertionException(
                $"Expected {expectedCompensationOrder.Length} compensations but observed {result.Compensated.Count}.");
        }
        for (var i = 0; i < expectedCompensationOrder.Length; i++)
        {
            if (result.Compensated[i] != expectedCompensationOrder[i])
            {
                throw new SagaAssertionException(
                    $"Compensation order mismatch at index {i}: expected '{expectedCompensationOrder[i]}', got '{result.Compensated[i]}'.");
            }
        }
    }
}

public sealed class SagaAssertionException : Exception
{
    public SagaAssertionException(string message) : base(message) { }
}
