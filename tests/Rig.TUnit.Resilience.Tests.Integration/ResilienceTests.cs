using Polly;
using Polly.CircuitBreaker;
using Rig.TUnit.Resilience;
using Rig.TUnit.Resilience.Assertions;

namespace Rig.TUnit.Resilience.Tests.Integration;

public sealed class ResilienceTests
{
    // ─── CircuitBreakerAssert ────────────────────────────────────────────────

    [Test]
    public async Task CircuitBreaker_Positive_OpensAfterThresholdFailures()
    {
        var stateProvider = new CircuitBreakerStateProvider();
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(10),
                StateProvider = stateProvider,
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            })
            .Build();

        var observed = new List<Exception>();
        await CircuitBreakerAssert.For(stateProvider)
            .After(5, async () => await pipeline.ExecuteAsync(static _ =>
                throw new InvalidOperationException("boom")), observed);

        CircuitBreakerAssert.For(stateProvider).State(CircuitState.Open);
    }

    [Test]
    public async Task CircuitBreaker_Negative_StaysClosedBelowThreshold()
    {
        var stateProvider = new CircuitBreakerStateProvider();
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(10),
                StateProvider = stateProvider,
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            })
            .Build();

        for (var i = 0; i < 3; i++)
        {
            try { await pipeline.ExecuteAsync(static _ => throw new InvalidOperationException("boom")); }
            catch (InvalidOperationException) { }
        }

        CircuitBreakerAssert.For(stateProvider).State(CircuitState.Closed);
    }

    [Test]
    public async Task CircuitBreaker_Boundary_ThrowsOnStateMismatch()
    {
        var stateProvider = new CircuitBreakerStateProvider();
        _ = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                StateProvider = stateProvider,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            })
            .Build();

        var threw = false;
        try { CircuitBreakerAssert.For(stateProvider).State(CircuitState.Open); }
        catch (ResilienceAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task CircuitBreaker_Timeout_AssertionCompletesQuickly()
    {
        var stateProvider = new CircuitBreakerStateProvider();
        _ = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                StateProvider = stateProvider,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            })
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() => CircuitBreakerAssert.For(stateProvider).State(CircuitState.Closed), cts.Token);
        await task;
    }

    [Test]
    public async Task CircuitBreaker_Cancellation_NotObserved()
    {
        var stateProvider = new CircuitBreakerStateProvider();
        _ = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                StateProvider = stateProvider,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            })
            .Build();

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        CircuitBreakerAssert.For(stateProvider).State(CircuitState.Closed);
        await Task.CompletedTask;
    }

    // ─── ClockControl / FakeTimeProvider ─────────────────────────────────────

    [Test]
    public async Task Clock_Advance_MovesFakeTimeProviderForward()
    {
        var clock = new ResilienceClock(new DateTimeOffset(2026, 4, 17, 0, 0, 0, TimeSpan.Zero));
        var before = clock.Now;
        clock.Advance(TimeSpan.FromHours(1));
        var after = clock.Now;

        await Assert.That((after - before).TotalHours).IsEqualTo(1.0);
    }

    // ─── RetryAssert ─────────────────────────────────────────────────────────

    [Test]
    public async Task Retry_Count_MatchesRecordedAttempts()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.FromMilliseconds(100), null));
        telemetry.Record(new RetryAttempt(2, TimeSpan.FromMilliseconds(100), null));
        telemetry.Record(new RetryAttempt(3, TimeSpan.FromMilliseconds(100), null));

        RetryAssert.For(telemetry).Count(3).WithBackoffInterval(TimeSpan.FromMilliseconds(100));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Retry_Count_WrongCount_Throws()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.Zero, null));

        var threw = false;
        try { RetryAssert.For(telemetry).Count(5); }
        catch (ResilienceAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Retry_BackoffInterval_OutOfTolerance_Throws()
    {
        var telemetry = new RetryTelemetry();
        telemetry.Record(new RetryAttempt(1, TimeSpan.FromMilliseconds(500), null));

        var threw = false;
        try { RetryAssert.For(telemetry).WithBackoffInterval(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)); }
        catch (ResilienceAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ─── RateLimitAssert ─────────────────────────────────────────────────────

    [Test]
    public async Task RateLimit_Permitted_And_Rejected_Counts_Observed()
    {
        var telemetry = new RateLimitTelemetry();
        for (var i = 0; i < 10; i++) telemetry.RecordPermitted();
        for (var i = 0; i < 4; i++) telemetry.RecordRejected();

        RateLimitAssert.For(telemetry).Permits(10).PerSecond().Permitted(10).Rejects(4);
        await Task.CompletedTask;
    }

    [Test]
    public async Task RateLimit_Rejects_InsufficientCount_Throws()
    {
        var telemetry = new RateLimitTelemetry();
        telemetry.RecordRejected();

        var threw = false;
        try { RateLimitAssert.For(telemetry).Rejects(5); }
        catch (ResilienceAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ─── BulkheadAssert + ChaosInjector ──────────────────────────────────────

    [Test]
    public async Task Bulkhead_MaxConcurrency_IsTracked()
    {
        var tel = new BulkheadTelemetry();
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            tel.RecordEnter();
            await Task.Yield();
            await Task.Delay(50);
            tel.RecordExit();
        }).ToArray();
        await Task.WhenAll(tasks);

        await Assert.That(tel.MaxConcurrency).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Chaos_EveryNth_FailsOnMultiple()
    {
        var injector = ChaosInjector.EveryNth(2);
        var failures = 0;
        var succeeded = 0;
        for (var i = 0; i < 6; i++)
        {
            try
            {
                await injector.InvokeAsync(() => Task.FromResult(1));
                succeeded++;
            }
            catch (InvalidOperationException)
            {
                failures++;
            }
        }
        await Assert.That(failures).IsEqualTo(3);
        await Assert.That(succeeded).IsEqualTo(3);
    }

    [Test]
    public async Task Chaos_FailFirst_FailsOnlyInitialAttempts()
    {
        var injector = ChaosInjector.FailFirst(2);
        var failures = 0;
        var succeeded = 0;
        for (var i = 0; i < 5; i++)
        {
            try
            {
                await injector.InvokeAsync(() => Task.FromResult(1));
                succeeded++;
            }
            catch (InvalidOperationException)
            {
                failures++;
            }
        }
        await Assert.That(failures).IsEqualTo(2);
        await Assert.That(succeeded).IsEqualTo(3);
    }
}
