using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Assertions;

/// <summary>Fluent assertions over an <see cref="OutboxFixture"/>'s rows.</summary>
public static class OutboxAssert
{
    public static OutboxEntryAssertion<T> Contains<T>(OutboxFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return new OutboxEntryAssertion<T>(fixture, typeof(T).FullName ?? typeof(T).Name);
    }

    public static async Task<OutboxDeadLetterAssertion> InDeadLetter<T>(OutboxFixture fixture, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        await Task.CompletedTask;
        var eventType = typeof(T).FullName ?? typeof(T).Name;
        var matches = fixture.DeadLetter.Where(m => m.EventType == eventType).ToArray();
        if (matches.Length == 0)
        {
            throw new OutboxAssertionException(
                $"Expected a dead-letter entry for {eventType} but none found.");
        }
        return new OutboxDeadLetterAssertion(matches);
    }
}

public sealed class OutboxEntryAssertion<T>
{
    private readonly OutboxFixture _fixture;
    private readonly string _eventType;
    private string? _aggregateId;
    private string? _topic;

    internal OutboxEntryAssertion(OutboxFixture fixture, string eventType)
    {
        _fixture = fixture;
        _eventType = eventType;
    }

    public OutboxEntryAssertion<T> WithAggregateId(string aggregateId)
    {
        _aggregateId = aggregateId;
        return this;
    }

    public OutboxEntryAssertion<T> OnTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    /// <summary>Asserts exactly one matching row across the entire outbox (including relayed).</summary>
    public async Task ExactlyOnce(CancellationToken ct = default)
    {
        var matches = await MatchesAsync(ct);
        if (matches.Count != 1)
        {
            throw new OutboxAssertionException(
                $"Expected exactly 1 outbox row matching {_eventType} but found {matches.Count}.");
        }
    }

    /// <summary>Asserts the matched row has been relayed (non-null <c>RelayedAt</c>).</summary>
    public async Task<OutboxEntryAssertion<T>> Relayed(CancellationToken ct = default)
    {
        var matches = await MatchesAsync(ct);
        if (matches.Count == 0 || matches.Any(m => m.RelayedAt is null))
        {
            var pending = matches.Count(m => m.RelayedAt is null);
            throw new OutboxAssertionException(
                $"Expected {_eventType} to be relayed but {pending}/{matches.Count} are still pending.");
        }
        return this;
    }

    /// <summary>Polls the store until the match-set appears within <paramref name="timeout"/>.</summary>
    public async Task Within(TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var matches = await MatchesAsync(ct);
            if (matches.Count > 0) return;
            await Task.Delay(50, ct);
        }
        throw new OutboxAssertionException(
            $"No outbox row matching {_eventType} appeared within {timeout}.");
    }

    private async Task<IReadOnlyList<OutboxMessage>> MatchesAsync(CancellationToken ct)
    {
        var all = await _fixture.Store.ReadAllAsync(ct);
        var matches = all.Where(m => m.EventType == _eventType);
        if (_aggregateId is not null)
        {
            matches = matches.Where(m => m.AggregateId == _aggregateId);
        }
        if (_topic is not null)
        {
            // Topic is derived from the messaging transport — we approximate match by the event's explicit topic tag.
            // For providers that attach a topic header, callers should pre-filter via the store.
            matches = matches.Where(m => m.EventType.Contains(_topic, StringComparison.OrdinalIgnoreCase));
        }
        return matches.ToArray();
    }
}

public sealed class OutboxDeadLetterAssertion
{
    private readonly IReadOnlyList<OutboxMessage> _matches;

    internal OutboxDeadLetterAssertion(IReadOnlyList<OutboxMessage> matches) { _matches = matches; }

    public Task WithReason(string reasonFragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonFragment);
        if (!_matches.Any(m => m.FailureReason?.Contains(reasonFragment, StringComparison.Ordinal) == true))
        {
            throw new OutboxAssertionException(
                $"Dead-letter entries did not contain reason fragment '{reasonFragment}'.");
        }
        return Task.CompletedTask;
    }
}

public sealed class OutboxAssertionException : Exception
{
    public OutboxAssertionException(string message) : base(message) { }
}
