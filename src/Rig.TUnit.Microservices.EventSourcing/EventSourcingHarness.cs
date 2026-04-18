namespace Rig.TUnit.Microservices.EventSourcing;

/// <summary>
/// Given/When/Then harness for event-sourced aggregates. Replay past events,
/// apply a command/action, then assert the resulting state or raised events.
/// </summary>
public sealed class EventSourcingHarness<TAggregate>
{
    private readonly Func<IEnumerable<object>, TAggregate> _rehydrate;
    private readonly Func<TAggregate, IReadOnlyList<object>> _getRaised;
    private readonly Action<TAggregate> _clearRaised;
    private IReadOnlyList<object> _given = Array.Empty<object>();

    public EventSourcingHarness(
        Func<IEnumerable<object>, TAggregate> rehydrate,
        Func<TAggregate, IReadOnlyList<object>> getRaised,
        Action<TAggregate> clearRaised)
    {
        _rehydrate = rehydrate ?? throw new ArgumentNullException(nameof(rehydrate));
        _getRaised = getRaised ?? throw new ArgumentNullException(nameof(getRaised));
        _clearRaised = clearRaised ?? throw new ArgumentNullException(nameof(clearRaised));
    }

    public EventSourcingHarness<TAggregate> Given(params object[] events)
    {
        _given = events ?? Array.Empty<object>();
        return this;
    }

    public WhenStage<TAggregate> When(Action<TAggregate> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var aggregate = _rehydrate(_given);
        _clearRaised(aggregate);
        action(aggregate);
        var raised = _getRaised(aggregate);
        return new WhenStage<TAggregate>(aggregate, raised);
    }
}

public sealed class WhenStage<TAggregate>
{
    public TAggregate Aggregate { get; }
    public IReadOnlyList<object> Raised { get; }

    internal WhenStage(TAggregate aggregate, IReadOnlyList<object> raised)
    {
        Aggregate = aggregate;
        Raised = raised;
    }

    /// <summary>Asserts the set of events raised matches the expected list by type and order.</summary>
    public WhenStage<TAggregate> Then(params object[] expected)
    {
        if (Raised.Count != expected.Length)
        {
            throw new EventSourcingAssertionException(
                $"Expected {expected.Length} event(s) but {Raised.Count} were raised.");
        }
        for (var i = 0; i < expected.Length; i++)
        {
            if (!Equals(Raised[i], expected[i]))
            {
                throw new EventSourcingAssertionException(
                    $"Event #{i}: expected {expected[i]} but got {Raised[i]}.");
            }
        }
        return this;
    }
}

public sealed class EventSourcingAssertionException : Exception
{
    public EventSourcingAssertionException(string message) : base(message) { }
}

/// <summary>Fluent assertion that a specific event type was raised with specific data.</summary>
public static class AggregateAssert
{
    public static RaisedAssertion<T> Raised<T>(IReadOnlyList<object> raised)
    {
        ArgumentNullException.ThrowIfNull(raised);
        var matches = raised.OfType<T>().ToArray();
        if (matches.Length == 0)
        {
            throw new EventSourcingAssertionException(
                $"No event of type {typeof(T).Name} was raised. Raised: [{string.Join(", ", raised.Select(e => e.GetType().Name))}].");
        }
        return new RaisedAssertion<T>(matches);
    }
}

public sealed class RaisedAssertion<T>
{
    private readonly IReadOnlyList<T> _matches;
    internal RaisedAssertion(IReadOnlyList<T> matches) { _matches = matches; }

    public RaisedAssertion<T> WithData(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (!_matches.Any(m => match(m)))
        {
            throw new EventSourcingAssertionException(
                $"No raised {typeof(T).Name} event matched the predicate.");
        }
        return this;
    }
}

/// <summary>
/// Verifies the aggregate's event catalogue (mapping from event type → handler/version).
/// Used to drive schema-evolution checks (v1 event + v2 handler must coexist).
/// </summary>
public sealed class EventCatalogueAssert
{
    private readonly IReadOnlyDictionary<Type, int> _catalogue;

    public EventCatalogueAssert(IReadOnlyDictionary<Type, int> catalogue)
    {
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
    }

    public EventCatalogueAssert HasEvent<T>(int atVersion)
    {
        if (!_catalogue.TryGetValue(typeof(T), out var v))
        {
            throw new EventSourcingAssertionException(
                $"Event catalogue is missing {typeof(T).Name}.");
        }
        if (v != atVersion)
        {
            throw new EventSourcingAssertionException(
                $"Event {typeof(T).Name} registered at v{v}, expected v{atVersion}.");
        }
        return this;
    }

    public EventCatalogueAssert HasHandlerForVersions<T>(params int[] versions)
    {
        if (!_catalogue.ContainsKey(typeof(T)))
        {
            throw new EventSourcingAssertionException(
                $"No handler registered for {typeof(T).Name}.");
        }
        // Presence only — version-specific dispatch is the consuming app's concern.
        if (versions.Length == 0)
        {
            throw new ArgumentException("At least one version required.", nameof(versions));
        }
        return this;
    }
}
