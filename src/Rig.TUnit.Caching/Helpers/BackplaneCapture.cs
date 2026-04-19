using System.Collections.Concurrent;

namespace Rig.TUnit.Caching.Helpers;

/// <summary>
/// Records every backplane message published by a distributed cache so tests can
/// assert on coherency/eviction propagation.
/// </summary>
public abstract class BackplaneCapture
{
    private readonly ConcurrentQueue<BackplaneMessage> _messages = new();

    public IReadOnlyCollection<BackplaneMessage> Messages => _messages.ToArray();

    protected void Record(BackplaneMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Enqueue(message);
    }

    public abstract Task StartAsync(CancellationToken ct);
    public abstract Task StopAsync(CancellationToken ct);
}

public sealed record BackplaneMessage(string Channel, string Payload, DateTimeOffset ReceivedAt);
