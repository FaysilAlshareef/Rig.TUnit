using System.Collections.Concurrent;

namespace Rig.TUnit.Microservices.Inbox;

/// <summary>
/// Tracks per-aggregate sequence numbers to enforce idempotent event application.
/// Duplicate or out-of-order events are rejected. Thread-safe.
/// </summary>
public sealed class SequenceTracker
{
    private readonly ConcurrentDictionary<string, long> _applied = new();

    public bool TryApply(string aggregateId, long sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));

        while (true)
        {
            if (_applied.TryGetValue(aggregateId, out var last))
            {
                if (sequence <= last) return false;
                if (_applied.TryUpdate(aggregateId, sequence, last)) return true;
            }
            else
            {
                if (_applied.TryAdd(aggregateId, sequence)) return true;
            }
        }
    }

    public long? LastApplied(string aggregateId)
        => _applied.TryGetValue(aggregateId, out var v) ? v : null;
}

/// <summary>Fixture wrapping a <see cref="SequenceTracker"/> for inbox-pattern tests.</summary>
public sealed class InboxFixture : Rig.TUnit.Core.Fixtures.RigFixtureBase
{
    public SequenceTracker Tracker { get; } = new();
    public override string ConnectionString => "inbox-in-memory";
    public override Task InitializeAsync() => Task.CompletedTask;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
