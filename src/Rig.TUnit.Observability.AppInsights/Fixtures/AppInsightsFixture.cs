using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.AppInsights.Options;
using Rig.TUnit.Observability.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Fixtures;

/// <summary>
/// In-process Application Insights fixture. Wires a <see cref="TelemetryClient"/>
/// to a <see cref="CapturingTelemetryChannel"/> so tests can assert emitted
/// telemetry deterministically without shipping to the real AI backend.
/// </summary>
public sealed class AppInsightsFixture : TelemetryFixtureBase
{
    private readonly AppInsightsFixtureOptions _options;
    private CapturingTelemetryChannel? _channel;
    private TelemetryClient? _client;
    private TelemetryConfiguration? _config;

    public AppInsightsFixture() : this(new AppInsightsFixtureOptions()) { }

    public AppInsightsFixture(IOptions<AppInsightsFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public AppInsightsFixture(AppInsightsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public TelemetryClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized.");
    public CapturingTelemetryChannel Channel => _channel ?? throw new InvalidOperationException("Fixture not initialized.");
    public override string ConnectionString => _options.InstrumentationKey;
    public override string ServiceName => _options.RoleName;

    public override Task InitializeAsync()
    {
        _channel = new CapturingTelemetryChannel();
        _config = new TelemetryConfiguration
        {
            ConnectionString = $"InstrumentationKey={_options.InstrumentationKey}",
            TelemetryChannel = _channel,
        };
        _client = new TelemetryClient(_config);
        _client.Context.Cloud.RoleName = _options.RoleName;
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _config?.Dispose();
        _channel?.Dispose();
        _config = null;
        _channel = null;
        _client = null;
        return ValueTask.CompletedTask;
    }
}
