using System.Collections.Concurrent;

namespace Rig.TUnit.Databases.NoSql.Helpers;

/// <summary>
/// Abstract change-feed capture — concrete providers (Cosmos, Mongo change streams,
/// EventStore catch-up) register received documents here so tests can assert on them.
/// </summary>
public abstract class ChangeFeedCapture<TDocument>
{
    private readonly ConcurrentQueue<TDocument> _captured = new();

    public IReadOnlyCollection<TDocument> Captured => _captured.ToArray();

    protected void Record(TDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _captured.Enqueue(document);
    }

    public abstract Task StartAsync(CancellationToken ct);
    public abstract Task StopAsync(CancellationToken ct);
}
