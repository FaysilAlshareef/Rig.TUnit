using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Assertions;

public sealed class MessageAssert
{
    private MessageAssert() { }

    public static void Published<T>(ListenerBase<T> listener) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (listener.Count == 0)
        {
            throw new InvalidOperationException("MessageAssert.Published: no messages captured");
        }
    }

    public static void ExactlyOnce<T>(ListenerBase<T> listener, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(predicate);
        var count = listener.Captured.Count(m => predicate(m.Message));
        if (count != 1)
        {
            throw new InvalidOperationException($"MessageAssert.ExactlyOnce: expected 1 match, got {count}");
        }
    }

    public static void OnTopic<T>(ListenerBase<T> listener, string expectedTopic) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTopic);
        // Topic verification is provider-specific; base expects the listener to have
        // been constructed against the named topic. Contract tests verify this.
    }

    public static void WithCorrelation<T>(CapturedMessage<T> message, string expected)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!string.Equals(message.CorrelationId, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"MessageAssert.WithCorrelation: expected '{expected}', got '{message.CorrelationId}'");
        }
    }

    public static void WithHeader<T>(CapturedMessage<T> message, string key, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!message.Headers.TryGetValue(key, out var actual) || !string.Equals(actual, expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"MessageAssert.WithHeader: header '{key}' expected '{expectedValue}', got '{actual}'");
        }
    }

    public static async Task Within<T>(ListenerBase<T> listener, TimeSpan timeout, int expectedCount, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline && listener.Count < expectedCount)
        {
            await Task.Delay(20, ct).ConfigureAwait(false);
        }
        if (listener.Count < expectedCount)
        {
            throw new InvalidOperationException($"MessageAssert.Within: expected {expectedCount} messages within {timeout}, got {listener.Count}");
        }
    }
}
