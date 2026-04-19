using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Assertions;

/// <summary>
/// Fluent assertion surface over a <see cref="CapturingTelemetryChannel"/> —
/// mirrors the shape of <c>TraceAssert</c> and <c>MetricAssert</c> for
/// consistency across observability packages.
/// </summary>
public static class AppInsightsAssert
{
    public static AppInsightsEventAssertion Event(CapturingTelemetryChannel channel, string name)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var matches = channel.Captured.OfType<EventTelemetry>().Where(e => e.Name == name).ToArray();
        return new AppInsightsEventAssertion(matches);
    }

    public static AppInsightsDependencyAssertion Dependency(CapturingTelemetryChannel channel, string type)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        var matches = channel.Captured.OfType<DependencyTelemetry>().Where(d => d.Type == type).ToArray();
        return new AppInsightsDependencyAssertion(matches);
    }

    public static AppInsightsExceptionAssertion Exception<TException>(CapturingTelemetryChannel channel)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(channel);
        var matches = channel.Captured
            .OfType<ExceptionTelemetry>()
            .Where(e => e.Exception is TException)
            .ToArray();
        return new AppInsightsExceptionAssertion(matches);
    }
}

public sealed class AppInsightsEventAssertion
{
    private readonly IReadOnlyList<EventTelemetry> _matches;
    public AppInsightsEventAssertion(IReadOnlyList<EventTelemetry> matches) => _matches = matches;

    public int Count => _matches.Count;

    public AppInsightsEventAssertion Exactly(int expected)
    {
        if (_matches.Count != expected)
        {
            throw new AppInsightsAssertionException(
                $"Expected exactly {expected} event(s) but found {_matches.Count}.");
        }
        return this;
    }
}

public sealed class AppInsightsDependencyAssertion
{
    private readonly IReadOnlyList<DependencyTelemetry> _matches;
    public AppInsightsDependencyAssertion(IReadOnlyList<DependencyTelemetry> matches) => _matches = matches;

    public int Count => _matches.Count;

    public AppInsightsDependencyAssertion AtLeast(int expected)
    {
        if (_matches.Count < expected)
        {
            throw new AppInsightsAssertionException(
                $"Expected at least {expected} dependency call(s) but found {_matches.Count}.");
        }
        return this;
    }
}

public sealed class AppInsightsExceptionAssertion
{
    private readonly IReadOnlyList<ExceptionTelemetry> _matches;
    public AppInsightsExceptionAssertion(IReadOnlyList<ExceptionTelemetry> matches) => _matches = matches;

    public int Count => _matches.Count;

    public AppInsightsExceptionAssertion Exactly(int expected)
    {
        if (_matches.Count != expected)
        {
            throw new AppInsightsAssertionException(
                $"Expected exactly {expected} exception(s) but found {_matches.Count}.");
        }
        return this;
    }
}

public sealed class AppInsightsAssertionException : Exception
{
    public AppInsightsAssertionException(string message) : base(message) { }
}
