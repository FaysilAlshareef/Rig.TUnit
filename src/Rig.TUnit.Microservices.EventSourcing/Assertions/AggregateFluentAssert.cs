namespace Rig.TUnit.Microservices.EventSourcing.Assertions;

/// <summary>
/// Fluent `.For(aggregate, raised).Raised&lt;TEvent&gt;().WithData(pred)` assertion
/// helper — complements the shorter <c>AggregateAssert.Raised&lt;T&gt;(raised)</c>
/// in <see cref="EventSourcingHarness{T}"/> with a richer surface that carries
/// the aggregate instance along with the raised events.
/// </summary>
public static class AggregateFluentAssert
{
    public static AggregateAssertion<TAggregate> For<TAggregate>(TAggregate aggregate, IReadOnlyList<object> raised)
    {
        ArgumentNullException.ThrowIfNull(raised);
        return new AggregateAssertion<TAggregate>(aggregate, raised);
    }
}

public sealed class AggregateAssertion<TAggregate>
{
    private readonly TAggregate _aggregate;
    private readonly IReadOnlyList<object> _raised;

    internal AggregateAssertion(TAggregate aggregate, IReadOnlyList<object> raised)
    {
        _aggregate = aggregate;
        _raised = raised;
    }

    public RaisedEventAssertion<TEvent> Raised<TEvent>() where TEvent : class
    {
        var matches = _raised.OfType<TEvent>().ToArray();
        return new RaisedEventAssertion<TEvent>(matches);
    }

    public AggregateAssertion<TAggregate> NotRaised<TEvent>() where TEvent : class
    {
        var matches = _raised.OfType<TEvent>().ToArray();
        if (matches.Length > 0)
        {
            throw new AggregateAssertionException(
                $"Expected no {typeof(TEvent).Name} events but found {matches.Length}.");
        }
        return this;
    }
}

public sealed class RaisedEventAssertion<TEvent> where TEvent : class
{
    private readonly IReadOnlyList<TEvent> _matches;

    internal RaisedEventAssertion(IReadOnlyList<TEvent> matches) => _matches = matches;

    public int Count => _matches.Count;

    public RaisedEventAssertion<TEvent> Exactly(int expected)
    {
        if (_matches.Count != expected)
        {
            throw new AggregateAssertionException(
                $"Expected exactly {expected} {typeof(TEvent).Name} event(s) but found {_matches.Count}.");
        }
        return this;
    }

    public RaisedEventAssertion<TEvent> AtLeast(int expected)
    {
        if (_matches.Count < expected)
        {
            throw new AggregateAssertionException(
                $"Expected at least {expected} {typeof(TEvent).Name} event(s) but found {_matches.Count}.");
        }
        return this;
    }

    public RaisedEventAssertion<TEvent> WithData(Func<TEvent, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!_matches.Any(predicate))
        {
            throw new AggregateAssertionException(
                $"No {typeof(TEvent).Name} event matched the predicate.");
        }
        return this;
    }
}

public sealed class AggregateAssertionException : Exception
{
    public AggregateAssertionException(string message) : base(message) { }
}
