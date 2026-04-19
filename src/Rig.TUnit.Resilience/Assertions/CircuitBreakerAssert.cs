using Polly.CircuitBreaker;

namespace Rig.TUnit.Resilience.Assertions;

/// <summary>
/// Fluent assertions over a <see cref="CircuitBreakerStateProvider"/> exposed by
/// a Polly circuit breaker strategy.
/// </summary>
public sealed class CircuitBreakerAssert
{
    private readonly CircuitBreakerStateProvider _provider;

    private CircuitBreakerAssert(CircuitBreakerStateProvider provider) { _provider = provider; }

    public static CircuitBreakerAssert For(CircuitBreakerStateProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new CircuitBreakerAssert(provider);
    }

    public CircuitBreakerAssert State(CircuitState expected)
    {
        if (_provider.CircuitState != expected)
        {
            throw new ResilienceAssertionException(
                $"Expected circuit state {expected} but was {_provider.CircuitState}.");
        }
        return this;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> <paramref name="failures"/> times so the
    /// circuit accumulates failures toward its threshold. Each invocation is expected
    /// to throw — the failure is tracked (caller-observable via <paramref name="observedFailures"/>)
    /// and the loop continues. <see cref="BrokenCircuitException"/> short-circuits the loop
    /// since the circuit is now open.
    /// </summary>
    public async Task<CircuitBreakerAssert> After(int failures, Func<Task> action,
        IList<Exception>? observedFailures = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (failures < 0) throw new ArgumentOutOfRangeException(nameof(failures));
        for (var i = 0; i < failures; i++)
        {
            try
            {
                await action();
            }
            catch (BrokenCircuitException)
            {
                // Circuit has opened — further attempts would fail-fast. Stop the loop.
                break;
            }
            catch (Exception ex)
            {
                // Expected failure that drives the circuit toward its threshold.
                // Not swallowed silently — recorded so the caller can inspect.
                observedFailures?.Add(ex);
            }
        }
        return this;
    }
}

public sealed class ResilienceAssertionException : Exception
{
    public ResilienceAssertionException(string message) : base(message) { }
}
