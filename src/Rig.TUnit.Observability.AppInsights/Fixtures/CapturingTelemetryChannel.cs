using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;

namespace Rig.TUnit.Observability.AppInsights.Fixtures;

/// <summary>
/// Internal <see cref="ITelemetryChannel"/> that captures every telemetry item
/// in a thread-safe queue instead of shipping to Application Insights. Enables
/// deterministic assertions in unit and integration tests with zero network
/// egress.
/// </summary>
public sealed class CapturingTelemetryChannel : ITelemetryChannel
{
    private readonly ConcurrentQueue<ITelemetry> _items = new();

    public IReadOnlyCollection<ITelemetry> Captured => _items;

    public bool? DeveloperMode { get; set; } = true;

    public string? EndpointAddress { get; set; }

    public void Send(ITelemetry item)
    {
        if (item is null) return;
        _items.Enqueue(item);
    }

    public void Flush()
    {
    }

    public void Clear()
    {
        while (_items.TryDequeue(out _)) { }
    }

    public void Dispose()
    {
        Clear();
    }
}
