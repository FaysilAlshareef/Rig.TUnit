using System.Diagnostics.Metrics;
using Rig.TUnit.Observability.Metrics.Assertions;

namespace Rig.TUnit.Observability.Metrics.Tests.Integration;

public sealed class MetricAssertTests
{
    [Test]
    public async Task Counter_Incremented_TracksTotal()
    {
        using var capture = new MetricCapture("rigtunit.test.m1");
        using var meter = new Meter("rigtunit.test.m1");
        var counter = meter.CreateCounter<long>("orders.created");

        counter.Add(1);
        counter.Add(3);

        MetricAssert.Counter(capture, "orders.created").Incremented(4);
        await Task.CompletedTask;
    }

    [Test]
    public async Task Counter_WithTag_FiltersByTagValue()
    {
        using var capture = new MetricCapture("rigtunit.test.m2");
        using var meter = new Meter("rigtunit.test.m2");
        var counter = meter.CreateCounter<long>("requests");

        counter.Add(1, new KeyValuePair<string, object?>("route", "/orders"));
        counter.Add(2, new KeyValuePair<string, object?>("route", "/users"));

        MetricAssert.Counter(capture, "requests").WithTag("route", "/users").Incremented(2);
        await Task.CompletedTask;
    }

    [Test]
    public async Task Counter_WithWrongTotal_ThrowsAssertionException()
    {
        using var capture = new MetricCapture("rigtunit.test.m3");
        using var meter = new Meter("rigtunit.test.m3");
        var counter = meter.CreateCounter<long>("c");
        counter.Add(10);

        var threw = false;
        try { MetricAssert.Counter(capture, "c").Incremented(999); }
        catch (MetricAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}
