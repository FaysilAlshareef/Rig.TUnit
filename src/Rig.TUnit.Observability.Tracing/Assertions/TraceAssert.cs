using System.Diagnostics;
using Rig.TUnit.Observability.Tracing.Fixtures;

namespace Rig.TUnit.Observability.Tracing.Assertions;

/// <summary>
/// Fluent assertion entry-point for OTEL spans captured by a <see cref="TracingFixture"/>.
/// Forces a flush before the first lookup so tests don't need to call <c>Flush()</c> manually.
/// Propagates W3C traceparent relationships via <see cref="Activity.ParentId"/>.
/// </summary>
public static class TraceAssert
{
    /// <summary>
    /// Asserts that a span with the given operation name was exported. Subsequent fluent calls
    /// attach further conditions (tag / status / parent / duration).
    /// </summary>
    public static SpanAssertion HasSpan(TracingFixture fixture, string operationName)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        fixture.Flush();

        var match = fixture.ExportedSpans.FirstOrDefault(s =>
            string.Equals(s.OperationName, operationName, StringComparison.Ordinal)
            || string.Equals(s.DisplayName, operationName, StringComparison.Ordinal));

        if (match is null)
        {
            var names = string.Join(", ", fixture.ExportedSpans.Select(s => s.OperationName));
            throw new TraceAssertionException(
                $"Expected span '{operationName}' — none found. Exported spans: [{names}].");
        }

        return new SpanAssertion(fixture, match);
    }
}

/// <summary>Fluent per-span assertion builder.</summary>
public sealed class SpanAssertion
{
    private readonly TracingFixture _fixture;
    private readonly Activity _activity;

    internal SpanAssertion(TracingFixture fixture, Activity activity)
    {
        _fixture = fixture;
        _activity = activity;
    }

    /// <summary>Asserts the span carries the specified tag/attribute.</summary>
    public SpanAssertion WithTag(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var tag = _activity.TagObjects.FirstOrDefault(t => t.Key == key);
        if (tag.Equals(default(KeyValuePair<string, object?>)) && !_activity.TagObjects.Any(t => t.Key == key))
        {
            var known = string.Join(", ", _activity.TagObjects.Select(t => t.Key));
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' is missing tag '{key}'. Known tags: [{known}].");
        }

        if (!Equals(tag.Value, value))
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' tag '{key}' expected '{value}' but was '{tag.Value}'.");
        }

        return this;
    }

    /// <summary>Asserts the span's OTEL status (<see cref="ActivityStatusCode"/>).</summary>
    public SpanAssertion WithStatus(ActivityStatusCode expected)
    {
        if (_activity.Status != expected)
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' expected status '{expected}' but was '{_activity.Status}'.");
        }
        return this;
    }

    /// <summary>
    /// Asserts the span's parent span (resolved by W3C traceparent) has the given operation name.
    /// </summary>
    public SpanAssertion WithParent(string parentOperationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentOperationName);

        if (_activity.ParentSpanId == default)
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' has no parent (root span). Expected parent '{parentOperationName}'.");
        }

        var parent = _fixture.ExportedSpans.FirstOrDefault(s =>
            s.SpanId == _activity.ParentSpanId
            && s.TraceId == _activity.TraceId);

        if (parent is null)
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' references parent span-id '{_activity.ParentSpanId}' but it was not exported.");
        }

        if (!string.Equals(parent.OperationName, parentOperationName, StringComparison.Ordinal))
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' parent is '{parent.OperationName}', expected '{parentOperationName}'.");
        }

        return this;
    }

    /// <summary>Asserts the span's duration is strictly less than the given limit.</summary>
    public SpanAssertion DurationLessThan(TimeSpan max)
    {
        if (_activity.Duration >= max)
        {
            throw new TraceAssertionException(
                $"Span '{_activity.OperationName}' duration {_activity.Duration} is not less than {max}.");
        }
        return this;
    }
}

/// <summary>Thrown when a <see cref="TraceAssert"/> expectation is not met.</summary>
public sealed class TraceAssertionException : Exception
{
    public TraceAssertionException(string message) : base(message) { }
}
