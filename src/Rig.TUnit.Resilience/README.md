# Rig.TUnit.Resilience

> Polly 8.x assertions + `FakeTimeProvider`-driven deterministic retry/backoff testing: `CircuitBreakerAssert`, `RetryAssert`, `RateLimitAssert`, `BulkheadAssert`, `ChaosInjector`.

## What this package is

The Rig.TUnit kit for testing Polly-based resilience pipelines. Four
fluent assertion families cover the four Polly strategies
(CircuitBreaker, Retry, RateLimit, Bulkhead), each parameterisable
against a `CircuitBreakerStateProvider` or equivalent. `ResilienceClock`
wraps `FakeTimeProvider` so retries and breaker windows can be
jumped over deterministically — no `Thread.Sleep`, no flake.
`ChaosInjector` lets tests flip operations to fail on demand to
exercise breaker / retry paths.

## When to use it

- Testing retry + circuit breaker policies end-to-end.
- Verifying rate limits throttle when saturated.
- Asserting bulkhead isolation prevents cascading failures.
- **Not for**: unit-testing business logic — wrap your service with
  the real pipeline in an integration test.

## Prerequisites

- .NET 10 SDK
- `Polly` 8.x + `Polly.Extensions` (transitive)
- `Microsoft.Extensions.TimeProvider.Testing` (transitive)

## Quick start

```csharp
using Polly;
using Polly.CircuitBreaker;
using Rig.TUnit.Resilience;

var state = new CircuitBreakerStateProvider();
var pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 1.0,
        MinimumThroughput = 2,
        StateProvider = state,
    })
    .Build();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultBreakDuration` | `TimeSpan` | `30s` | Applied to breaker strategies |
| `DefaultSamplingDuration` | `TimeSpan` | `30s` | Breaker sampling window |
| `ChaosFailureRate` | `double` | `0.0` | `ChaosInjector.Execute` failure probability |

## Fixture + helper APIs

- `Rig.TUnit.Resilience.CircuitBreakerAssert`
- `Rig.TUnit.Resilience.RetryAssert`
- `Rig.TUnit.Resilience.RateLimitAssert`
- `Rig.TUnit.Resilience.BulkheadAssert`
- `Rig.TUnit.Resilience.Helpers.ResilienceClock`
- `Rig.TUnit.Resilience.Helpers.ChaosInjector`

## Per-test isolation

Each assertion is stateless; state providers + pipelines are per-test.
`ResilienceClock` owns its own `FakeTimeProvider` per instance. Safe
under full parallelism.

## Parallelism + performance

- Zero containers.
- Per-assertion: microseconds plus the cost of pipeline execution.
- Safe under full parallelism.

## Troubleshooting

- **Retry backoff does not fire in tests** — pipelines default to the
  system `TimeProvider`; inject `ResilienceClock.TimeProvider` into the
  `ResiliencePipelineBuilder.TimeProvider` property.
- **`CircuitBreakerAssert.After(failures: N)` reports wrong state** —
  the breaker's `MinimumThroughput` was not met; tests must fire
  enough calls to satisfy the sampling threshold.

See [docs/troubleshooting.md#resilience](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Polly 8.x breaking API change — `ResiliencePipeline<T>` replaces
  `IAsyncPolicy<T>`; assertions assume the new API.
- Sampling-window math: `MinimumThroughput` + `FailureRatio` combine —
  `FailureRatio=1.0, MinimumThroughput=5` means 5 consecutive failures
  in the sampling window trip the breaker.
- Bulkhead is provided by `Polly.RateLimiting` in v8+ (no more separate
  `Bulkhead` strategy).

## Benchmarks

See [`ResilienceBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ResilienceBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Http`](../Rig.TUnit.Http/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
