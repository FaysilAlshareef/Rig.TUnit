using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rig.TUnit.Observability.Fixtures;
using Rig.TUnit.Observability.Tracing.Options;

namespace Rig.TUnit.Observability.Tracing.Fixtures;

/// <summary>
/// In-memory OTEL tracing fixture. Provides an <see cref="ActivitySource"/> scoped to the
/// fixture's <c>ServiceName</c> and an <c>ExportedSpans</c> list populated by the in-memory
/// exporter. Spans emitted through <see cref="ActivitySource"/> during the test are available
/// to <c>TraceAssert</c> after <see cref="Flush"/> (called automatically before assertions).
/// </summary>
public class TracingFixture : TelemetryFixtureBase
{
    private readonly TracingFixtureOptions _options;
    private readonly List<Activity> _exportedSpans = new();
    private TracerProvider? _tracerProvider;
    private ActivitySource? _activitySource;
    private bool _initialized;
    private bool _disposed;

    public TracingFixture() : this(new TracingFixtureOptions { ServiceName = "rigtunit-tracing" }) { }

    public TracingFixture(TracingFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public TracingFixture(IOptions<TracingFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    /// <inheritdoc />
    public override string ConnectionString => string.Empty;

    /// <summary>Service name emitted as the OTEL <c>service.name</c> resource attribute.</summary>
    public override string ServiceName => $"{_options.ServiceName}-{IsolationKey.Value}";

    /// <summary>Source tests use to create spans. Available after <see cref="InitializeAsync"/>.</summary>
    public ActivitySource ActivitySource => _activitySource
        ?? throw new InvalidOperationException("TracingFixture not initialised — call InitializeAsync first.");

    /// <summary>Spans exported by the in-memory exporter. Call <see cref="Flush"/> first.</summary>
    public IReadOnlyList<Activity> ExportedSpans => _exportedSpans;

    /// <inheritdoc />
    public override Task InitializeAsync()
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        _activitySource = new ActivitySource(ServiceName);

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(ServiceName)
            .ConfigureResource(r => r.AddService(ServiceName))
            .SetSampler(new TraceIdRatioBasedSampler(_options.SampleRatio))
            .AddInMemoryExporter(_exportedSpans)
            .Build();

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <summary>Flushes pending spans to the in-memory exporter. Idempotent.</summary>
    public void Flush()
    {
        _tracerProvider?.ForceFlush();
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }
        _disposed = true;

        _tracerProvider?.Dispose();
        _activitySource?.Dispose();
        _tracerProvider = null;
        _activitySource = null;

        return ValueTask.CompletedTask;
    }
}
