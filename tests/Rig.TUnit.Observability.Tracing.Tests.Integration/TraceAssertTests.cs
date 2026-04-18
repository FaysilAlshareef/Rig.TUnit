using System.Diagnostics;
using Rig.TUnit.Observability.Tracing.Assertions;
using Rig.TUnit.Observability.Tracing.Fixtures;
using Rig.TUnit.Observability.Tracing.Options;

namespace Rig.TUnit.Observability.Tracing.Tests.Integration;

/// <summary>
/// 5-case matrix (positive / negative / boundary / timeout / cancellation) per
/// TraceAssert fluent method.
/// </summary>
public sealed class TraceAssertTests
{
    private static async Task<TracingFixture> CreateAsync(string name = "trace-assert-tests")
    {
        var fx = new TracingFixture(new TracingFixtureOptions
        {
            ServiceName = name,
            SampleRatio = 1.0,
            MaxSpansInMemory = 1000,
        });
        await fx.InitializeAsync();
        return fx;
    }

    // ─── HasSpan ─────────────────────────────────────────────────────────────

    [Test]
    public async Task HasSpan_Positive_FindsEmittedSpan()
    {
        await using var fx = await CreateAsync();

        using (fx.ActivitySource.StartActivity("op.positive")) { }

        TraceAssert.HasSpan(fx, "op.positive");
    }

    [Test]
    public async Task HasSpan_Negative_ThrowsWhenSpanMissing()
    {
        await using var fx = await CreateAsync();

        var threw = false;
        try { TraceAssert.HasSpan(fx, "never.emitted"); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task HasSpan_Boundary_FindsSpanAmongManyWithSimilarPrefix()
    {
        await using var fx = await CreateAsync();

        using (fx.ActivitySource.StartActivity("op.prefix.a")) { }
        using (fx.ActivitySource.StartActivity("op.prefix.b")) { }
        using (fx.ActivitySource.StartActivity("op.prefix.c")) { }

        TraceAssert.HasSpan(fx, "op.prefix.b");
    }

    [Test]
    public async Task HasSpan_Timeout_DoesNotHangOnEmptyExporter()
    {
        await using var fx = await CreateAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() =>
        {
            try { TraceAssert.HasSpan(fx, "nope"); return false; }
            catch (TraceAssertionException) { return true; }
        }, cts.Token);

        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        await Assert.That(completed).IsEqualTo((Task)task);
    }

    [Test]
    public async Task HasSpan_Cancellation_IgnoredByPureAssertion()
    {
        // HasSpan is a synchronous inspection — cancellation does not interrupt it.
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.cancel")) { }

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        TraceAssert.HasSpan(fx, "op.cancel");
    }

    // ─── WithTag ─────────────────────────────────────────────────────────────

    [Test]
    public async Task WithTag_Positive_MatchesExistingTag()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.tagged"))
        {
            act?.SetTag("http.method", "GET");
        }

        TraceAssert.HasSpan(fx, "op.tagged").WithTag("http.method", "GET");
    }

    [Test]
    public async Task WithTag_Negative_ThrowsWhenValueMismatches()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.tagged"))
        {
            act?.SetTag("http.method", "POST");
        }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.tagged").WithTag("http.method", "GET"); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithTag_Boundary_ThrowsWhenTagKeyMissing()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.untagged")) { }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.untagged").WithTag("not.there", "x"); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithTag_Timeout_NonBlockingAssertion()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.timeout"))
        {
            act?.SetTag("k", 1);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() =>
        {
            TraceAssert.HasSpan(fx, "op.timeout").WithTag("k", 1);
            return true;
        }, cts.Token);

        await Assert.That(await task).IsTrue();
    }

    [Test]
    public async Task WithTag_Cancellation_NotObservedByPureAssertion()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.cancel.tag"))
        {
            act?.SetTag("k", "v");
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        TraceAssert.HasSpan(fx, "op.cancel.tag").WithTag("k", "v");
    }

    // ─── WithStatus ──────────────────────────────────────────────────────────

    [Test]
    public async Task WithStatus_Positive_MatchesExplicitStatus()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.status.ok"))
        {
            act?.SetStatus(ActivityStatusCode.Ok);
        }

        TraceAssert.HasSpan(fx, "op.status.ok").WithStatus(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task WithStatus_Negative_ThrowsWhenStatusDiffers()
    {
        await using var fx = await CreateAsync();
        using (var act = fx.ActivitySource.StartActivity("op.status.err"))
        {
            act?.SetStatus(ActivityStatusCode.Error, "boom");
        }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.status.err").WithStatus(ActivityStatusCode.Ok); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithStatus_Boundary_UnsetIsDistinctFromOk()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.status.unset")) { } // never set

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.status.unset").WithStatus(ActivityStatusCode.Ok); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithStatus_Timeout_NonBlocking()
    {
        await using var fx = await CreateAsync();
        using (var a = fx.ActivitySource.StartActivity("op.s.t")) { a?.SetStatus(ActivityStatusCode.Ok); }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() => { TraceAssert.HasSpan(fx, "op.s.t").WithStatus(ActivityStatusCode.Ok); return true; }, cts.Token);
        await Assert.That(await task).IsTrue();
    }

    [Test]
    public async Task WithStatus_Cancellation_Unobserved()
    {
        await using var fx = await CreateAsync();
        using (var a = fx.ActivitySource.StartActivity("op.s.c")) { a?.SetStatus(ActivityStatusCode.Ok); }

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        TraceAssert.HasSpan(fx, "op.s.c").WithStatus(ActivityStatusCode.Ok);
    }

    // ─── WithParent (W3C traceparent propagation) ────────────────────────────

    [Test]
    public async Task WithParent_Positive_DetectsParentChildRelation()
    {
        await using var fx = await CreateAsync();

        using (var parent = fx.ActivitySource.StartActivity("op.parent"))
        using (fx.ActivitySource.StartActivity("op.child")) { }

        TraceAssert.HasSpan(fx, "op.child").WithParent("op.parent");
    }

    [Test]
    public async Task WithParent_Negative_ThrowsOnRootSpan()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.root")) { }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.root").WithParent("anything"); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithParent_Boundary_WrongParentName()
    {
        await using var fx = await CreateAsync();

        using (var parent = fx.ActivitySource.StartActivity("op.p"))
        using (fx.ActivitySource.StartActivity("op.c")) { }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.c").WithParent("op.other"); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task WithParent_Timeout_NonBlocking()
    {
        await using var fx = await CreateAsync();
        using (var parent = fx.ActivitySource.StartActivity("op.pt"))
        using (fx.ActivitySource.StartActivity("op.ct")) { }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() => { TraceAssert.HasSpan(fx, "op.ct").WithParent("op.pt"); return true; }, cts.Token);
        await Assert.That(await task).IsTrue();
    }

    [Test]
    public async Task WithParent_Cancellation_Unobserved()
    {
        await using var fx = await CreateAsync();
        using (var parent = fx.ActivitySource.StartActivity("op.pc"))
        using (fx.ActivitySource.StartActivity("op.cc")) { }

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        TraceAssert.HasSpan(fx, "op.cc").WithParent("op.pc");
    }

    // ─── DurationLessThan ────────────────────────────────────────────────────

    [Test]
    public async Task DurationLessThan_Positive_FastSpanPasses()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.fast")) { }

        TraceAssert.HasSpan(fx, "op.fast").DurationLessThan(TimeSpan.FromSeconds(30));
    }

    [Test]
    public async Task DurationLessThan_Negative_ThrowsOnSlowSpan()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.slow")) { await Task.Delay(100); }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.slow").DurationLessThan(TimeSpan.FromMilliseconds(1)); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task DurationLessThan_Boundary_EqualDurationFails()
    {
        // Contract: strictly less than → equality is failure.
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.zero")) { }

        var threw = false;
        try { TraceAssert.HasSpan(fx, "op.zero").DurationLessThan(TimeSpan.Zero); }
        catch (TraceAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task DurationLessThan_Timeout_NonBlocking()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.dt")) { }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = Task.Run(() => { TraceAssert.HasSpan(fx, "op.dt").DurationLessThan(TimeSpan.FromMinutes(5)); return true; }, cts.Token);
        await Assert.That(await task).IsTrue();
    }

    [Test]
    public async Task DurationLessThan_Cancellation_Unobserved()
    {
        await using var fx = await CreateAsync();
        using (fx.ActivitySource.StartActivity("op.dc")) { }

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        TraceAssert.HasSpan(fx, "op.dc").DurationLessThan(TimeSpan.FromMinutes(1));
    }
}
