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

public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    string? CorrelationId);
