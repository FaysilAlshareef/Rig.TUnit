using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.Metrics.Fixtures;
using Rig.TUnit.Observability.Metrics.Options;

namespace Rig.TUnit.Observability.Metrics.Tests.Unit;

public sealed class MetricsFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_UsesDefaultOptions()
    {
        await using var fx = new MetricsFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.MeterName).IsEqualTo("Rig.TUnit.Metrics");
    }

    [Test]
    public async Task Ctor_IOptions_UsesOptions()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new MetricsFixtureOptions { MeterName = "svc" });
        await using var fx = new MetricsFixture(opts);
        await fx.InitializeAsync();
        await Assert.That(fx.MeterName).IsEqualTo("svc");
    }

    [Test]
    public async Task Ctor_NullOptions_Throws()
    {
        await Assert.That(() => new MetricsFixture((MetricsFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullIOptions_Throws()
    {
        await Assert.That(() => new MetricsFixture((IOptions<MetricsFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Capture_BeforeInitialize_Throws()
    {
        await using var fx = new MetricsFixture();
        await Assert.That(() => fx.Capture).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ConnectionString_AfterInitialize_ReturnsMeterName()
    {
        await using var fx = new MetricsFixture(new MetricsFixtureOptions { MeterName = "x.y" });
        await fx.InitializeAsync();
        await Assert.That(fx.ConnectionString).IsEqualTo("x.y");
    }

    [Test]
    public async Task InitializeAsync_CapturesMeasurements_OnNamedMeter()
    {
        var opts = new MetricsFixtureOptions { MeterName = "test.meter.1" };
        await using var fx = new MetricsFixture(opts);
        await fx.InitializeAsync();

        using var meter = new Meter("test.meter.1");
        var counter = meter.CreateCounter<long>("hits");
        counter.Add(5);

        var samples = fx.Capture.Samples;
        await Assert.That(samples.Count(s => s.Name == "hits")).IsGreaterThanOrEqualTo(1);
    }
}
