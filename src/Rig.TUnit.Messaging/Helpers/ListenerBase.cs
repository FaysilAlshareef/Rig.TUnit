using System.Collections.Concurrent;

namespace Rig.TUnit.Messaging.Helpers;

/// <summary>
/// Captures every message delivered to a subscription. Concrete providers wire their
/// transport's receive-handler to call <see cref="Record"/>.
/// </summary>
public abstract class ListenerBase<TMessage>
{
    private readonly ConcurrentQueue<CapturedMessage<TMessage>> _captured = new();

    public IReadOnlyCollection<CapturedMessage<TMessage>> Captured => _captured.ToArray();

    public int Count => _captured.Count;

    protected void Record(CapturedMessage<TMessage> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _captured.Enqueue(message);
    }

    public abstract Task StartAsync(CancellationToken ct);
    public abstract Task StopAsync(CancellationToken ct);
}

/// <summary>
/// Envelope recording every message a listener observes.
/// </summary>
/// <param name="Message">The raw broker-typed message (e.g., <c>ServiceBusReceivedMessage</c>).</param>
/// <param name="ReceivedAt">The timestamp at which the listener recorded the message.</param>
/// <param name="Headers">The flattened header dictionary exposed by the provider.</param>
/// <param name="Body">
/// The message body as a string. Providers coerce absent bodies to <see cref="string.Empty"/> so
/// tests never observe <see langword="null"/>.
/// </param>
/// <param name="CorrelationId">The optional <c>x-correlation-id</c> header, when present.</param>
/// <param name="SessionKey">
/// The per-session ordering key populated by session-aware listeners (ServiceBus <c>SessionId</c>,
/// SQS <c>MessageGroupId</c>, etc.). <see langword="null"/> for listeners that do not resolve a key.
/// </param>
public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    string? CorrelationId,
    string? SessionKey = null);
