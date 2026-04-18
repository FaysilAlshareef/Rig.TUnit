using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.Fixtures;
using Rig.TUnit.Observability.Metrics.Assertions;
using Rig.TUnit.Observability.Metrics.Options;

namespace Rig.TUnit.Observability.Metrics.Fixtures;

/// <summary>
/// In-process metrics capture fixture — wires a <see cref="MetricCapture"/> around a
/// named <c>System.Diagnostics.Metrics.Meter</c> so production code emits through the
/// real <c>Meter</c> API and tests assert against captured samples. No exporter, no
/// container — pure in-memory.
/// </summary>
public sealed class MetricsFixture : TelemetryFixtureBase
{
    private readonly MetricsFixtureOptions _options;
    private MetricCapture? _capture;

    public MetricsFixture() : this(new MetricsFixtureOptions()) { }

    public MetricsFixture(IOptions<MetricsFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public MetricsFixture(MetricsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public MetricCapture Capture => _capture ?? throw new InvalidOperationException("Fixture not initialized.");
    public string MeterName => _options.MeterName;
    public override string ConnectionString => _options.MeterName;

    public override Task InitializeAsync()
    {
        _capture = new MetricCapture(_options.MeterName);
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _capture?.Dispose();
        _capture = null;
        return ValueTask.CompletedTask;
    }
}
