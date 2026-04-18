# Rig.TUnit.Resilience

Polly 8.x assertions + `FakeTimeProvider`-driven deterministic retry/backoff
testing. Ships `CircuitBreakerAssert`, `RetryAssert`, `RateLimitAssert`,
`BulkheadAssert`, and a `ChaosInjector` for deterministic failure injection.

## Install

```xml
<PackageReference Include="Rig.TUnit.Resilience" />
```

## Example — circuit breaker

```csharp
var state = new CircuitBreakerStateProvider();
var pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions {
        FailureRatio = 1.0,
        MinimumThroughput = 2,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(10),
        StateProvider = state,
        ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>() })
    .Build();

await CircuitBreakerAssert.For(state).After(failures: 5, async () =>
    await pipeline.ExecuteAsync(static _ => throw new InvalidOperationException()));

CircuitBreakerAssert.For(state).State(CircuitState.Open);
```

## Example — deterministic retry via FakeTimeProvider

```csharp
var clock = new ResilienceClock();
// build a pipeline passing clock.TimeProvider to Polly's TimeProvider option.
clock.Advance(TimeSpan.FromSeconds(30)); // jump past backoff window.
```

## Dependencies
- `Rig.TUnit.Core`
- `Polly`, `Polly.Extensions`, `Microsoft.Extensions.TimeProvider.Testing`

Spec: `003-rig-tunit-ecosystem-expansion` — US7.
