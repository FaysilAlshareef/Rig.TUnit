namespace Rig.TUnit.Microservices.Inbox.Assertions;

/// <summary>Fluent assertions over a <see cref="SequenceTracker"/>.</summary>
public sealed class InboxAssert
{
    private readonly SequenceTracker _tracker;
    private readonly string _aggregateId;
    private readonly long _sequence;

    private InboxAssert(SequenceTracker tracker, string aggregateId, long sequence)
    {
        _tracker = tracker;
        _aggregateId = aggregateId;
        _sequence = sequence;
    }

    public static InboxAssert SequenceApplied(SequenceTracker tracker, string aggregateId, long sequence)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        var last = tracker.LastApplied(aggregateId);
        if (last is null || last < sequence)
        {
            throw new InboxAssertionException(
                $"Expected sequence {sequence} applied for {aggregateId} but last applied was {last?.ToString() ?? "none"}.");
        }
        return new InboxAssert(tracker, aggregateId, sequence);
    }

    /// <summary>Re-applying the same sequence must be a no-op.</summary>
    public InboxAssert Idempotent()
    {
        var accepted = _tracker.TryApply(_aggregateId, _sequence);
        if (accepted)
        {
            throw new InboxAssertionException(
                $"Idempotency violation: re-applying sequence {_sequence} on {_aggregateId} was accepted.");
        }
        return this;
    }
}

public sealed class InboxAssertionException : Exception
{
    public InboxAssertionException(string message) : base(message) { }
}
