namespace Rig.TUnit.Microservices.Saga.Assertions;

/// <summary>
/// Fluent `.For(result).Step(name).Compensated()` assertion surface over a
/// <see cref="SagaResult"/> — complements the terser
/// <c>SagaAssert.CompensatedInReverse</c> for scenarios where per-step
/// assertions read more naturally.
/// </summary>
public static class SagaStepAssert
{
    public static SagaStepAssertion For(SagaResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new SagaStepAssertion(result);
    }
}

public sealed class SagaStepAssertion
{
    private readonly SagaResult _result;

    internal SagaStepAssertion(SagaResult result) => _result = result;

    public StepAssertion Step(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new StepAssertion(_result, name);
    }

    public SagaStepAssertion Succeeded()
    {
        if (!_result.Success)
        {
            throw new SagaStepAssertionException(
                $"Expected saga to succeed but it failed: {_result.Failure?.Message ?? "<no exception>"}.");
        }
        return this;
    }

    public SagaStepAssertion Failed()
    {
        if (_result.Success)
        {
            throw new SagaStepAssertionException("Expected saga to fail but it succeeded.");
        }
        return this;
    }
}

public sealed class StepAssertion
{
    private readonly SagaResult _result;
    private readonly string _name;

    internal StepAssertion(SagaResult result, string name)
    {
        _result = result;
        _name = name;
    }

    public StepAssertion Completed()
    {
        if (!_result.Completed.Contains(_name))
        {
            throw new SagaStepAssertionException($"Expected step '{_name}' to have completed.");
        }
        return this;
    }

    public StepAssertion Compensated()
    {
        if (!_result.Compensated.Contains(_name))
        {
            throw new SagaStepAssertionException($"Expected step '{_name}' to have been compensated.");
        }
        return this;
    }

    public StepAssertion NotCompensated()
    {
        if (_result.Compensated.Contains(_name))
        {
            throw new SagaStepAssertionException($"Expected step '{_name}' NOT to be compensated.");
        }
        return this;
    }
}

public sealed class SagaStepAssertionException : Exception
{
    public SagaStepAssertionException(string message) : base(message) { }
}
