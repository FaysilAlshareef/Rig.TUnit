using System.Diagnostics.Metrics;

namespace Rig.TUnit.Observability.Metrics.Assertions;

/// <summary>
/// Captures every measurement emitted by a given <see cref="Meter"/> via a
/// <see cref="MeterListener"/> and exposes fluent assertions.
/// </summary>
public sealed class MetricCapture : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<MetricSample> _samples = new();
    private readonly object _lock = new();

    public IReadOnlyList<MetricSample> Samples
    {
        get { lock (_lock) { return _samples.ToArray(); } }
    }

    public MetricCapture(string meterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meterName);
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == meterName) listener.EnableMeasurementEvents(instrument);
            },
        };
        _listener.SetMeasurementEventCallback<long>(OnMeasured);
        _listener.SetMeasurementEventCallback<double>(OnMeasured);
        _listener.SetMeasurementEventCallback<int>((inst, v, tags, state) => OnMeasured(inst, v, tags, state));
        _listener.Start();
    }

    private void OnMeasured<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        var tagsArray = tags.ToArray();
        lock (_lock)
        {
            _samples.Add(new MetricSample(instrument.Name, Convert.ToDouble(value), tagsArray));
        }
    }

    public void Dispose() => _listener.Dispose();
}

public sealed record MetricSample(string Name, double Value, IReadOnlyList<KeyValuePair<string, object?>> Tags);

public static class MetricAssert
{
    public static MetricCounterAssertion Counter(MetricCapture capture, string name)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var matches = capture.Samples.Where(s => s.Name == name).ToArray();
        return new MetricCounterAssertion(matches);
    }
}

public sealed class MetricCounterAssertion
{
    private IReadOnlyList<MetricSample> _matches;
    internal MetricCounterAssertion(IReadOnlyList<MetricSample> matches) { _matches = matches; }

    public MetricCounterAssertion Incremented(int expectedTotal)
    {
        var total = _matches.Sum(m => m.Value);
        if ((int)total != expectedTotal)
        {
            throw new MetricAssertionException(
                $"Counter expected total {expectedTotal} but observed {total}.");
        }
        return this;
    }

    public MetricCounterAssertion WithTag(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _matches = _matches.Where(m => m.Tags.Any(t => t.Key == key && Equals(t.Value, value))).ToArray();
        if (_matches.Count == 0)
        {
            throw new MetricAssertionException(
                $"No counter sample carried tag '{key}={value}'.");
        }
        return this;
    }
}

public sealed class MetricAssertionException : Exception
{
    public MetricAssertionException(string message) : base(message) { }
}
