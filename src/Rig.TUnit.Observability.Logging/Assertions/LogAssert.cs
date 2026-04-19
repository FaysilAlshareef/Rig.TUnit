using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Fixtures;

namespace Rig.TUnit.Observability.Logging.Assertions;

/// <summary>
/// Fluent assertion entry-point for entries captured by a <see cref="LoggingFixture"/>.
/// </summary>
public static class LogAssert
{
    /// <summary>Begins an assertion chain over entries at the given <paramref name="level"/>.</summary>
    public static LogEntryAssertion Logged(LoggingFixture fixture, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return new LogEntryAssertion(fixture, fixture.Entries.Where(e => e.Level == level));
    }
}

/// <summary>Fluent assertion over a set of <see cref="LogEntry"/> matches.</summary>
public sealed class LogEntryAssertion
{
    private readonly LoggingFixture _fixture;
    private IEnumerable<LogEntry> _matches;

    internal LogEntryAssertion(LoggingFixture fixture, IEnumerable<LogEntry> matches)
    {
        _fixture = fixture;
        _matches = matches;
    }

    /// <summary>Narrows the match set to entries whose structured properties contain <c>key=value</c>.</summary>
    public LogEntryAssertion WithProperty(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _matches = _matches.Where(e =>
            e.Properties.Any(p => p.Key == key && Equals(p.Value, value)));
        return this;
    }

    /// <summary>Narrows to entries whose scope stack contains <c>key=value</c>.</summary>
    public LogEntryAssertion InScope(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _matches = _matches.Where(e =>
            e.Scopes.Any(scope => scope.Any(p => p.Key == key && Equals(p.Value, value))));
        return this;
    }

    /// <summary>Narrows to entries whose rendered message contains <paramref name="fragment"/>.</summary>
    public LogEntryAssertion WithMessage(string fragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fragment);
        _matches = _matches.Where(e =>
            e.Message.Contains(fragment, StringComparison.Ordinal));
        return this;
    }

    /// <summary>Asserts the narrowed set contains exactly one entry.</summary>
    public LogEntryAssertion Once()
    {
        var list = _matches.ToArray();
        if (list.Length != 1)
        {
            throw new LogAssertionException(
                $"Expected exactly 1 matching log entry but found {list.Length}. "
                + $"Fixture has {_fixture.Entries.Count} total entries.");
        }
        _matches = list;
        return this;
    }

    /// <summary>Asserts the narrowed set contains <paramref name="expected"/> entries.</summary>
    public LogEntryAssertion Times(int expected)
    {
        if (expected < 0) throw new ArgumentOutOfRangeException(nameof(expected));
        var list = _matches.ToArray();
        if (list.Length != expected)
        {
            throw new LogAssertionException(
                $"Expected {expected} matching log entries but found {list.Length}.");
        }
        _matches = list;
        return this;
    }

    /// <summary>Returns the narrowed matches (materialised).</summary>
    public IReadOnlyList<LogEntry> Entries() => _matches.ToArray();
}

/// <summary>Thrown when a <see cref="LogAssert"/> expectation is not met.</summary>
public sealed class LogAssertionException : Exception
{
    public LogAssertionException(string message) : base(message) { }
}
