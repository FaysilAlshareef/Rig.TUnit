using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Rig.TUnit.Observability.Logging.Providers;

/// <summary>
/// <see cref="ILoggerProvider"/> that captures every emitted entry into an in-memory
/// ring buffer. Scope stack is maintained per-async-context via <see cref="AsyncLocal{T}"/>.
/// Thread-safe.
/// </summary>
public sealed class InMemoryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;
    private readonly LogLevel _minLevel;
    private IExternalScopeProvider? _externalScope;

    public InMemoryLoggerProvider(LogLevel minLevel = LogLevel.Debug, int maxEntries = 50_000)
    {
        if (maxEntries <= 0) throw new ArgumentOutOfRangeException(nameof(maxEntries));
        _minLevel = minLevel;
        _maxEntries = maxEntries;
    }

    public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _externalScope = scopeProvider;

    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(categoryName, this);

    public void Dispose() { }

    internal void Append(LogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }
    }

    internal LogLevel MinLevel => _minLevel;
    internal IExternalScopeProvider? ExternalScope => _externalScope;

    private sealed class InMemoryLogger(string category, InMemoryLoggerProvider owner) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => owner.ExternalScope?.Push(state) ?? NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= owner.MinLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var properties = new List<KeyValuePair<string, object?>>();
            string? originalFormat = null;
            if (state is IReadOnlyList<KeyValuePair<string, object?>> stateList)
            {
                foreach (var kv in stateList)
                {
                    if (kv.Key == "{OriginalFormat}") originalFormat = kv.Value?.ToString();
                    else properties.Add(kv);
                }
            }

            var scopes = new List<IReadOnlyList<KeyValuePair<string, object?>>>();
            owner.ExternalScope?.ForEachScope(static (scope, accumulator) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object?>> scopeList)
                {
                    accumulator.Add(scopeList);
                }
                else if (scope is IEnumerable<KeyValuePair<string, object?>> scopeSeq)
                {
                    accumulator.Add(scopeSeq.ToArray());
                }
                else if (scope is not null)
                {
                    accumulator.Add(new[] { new KeyValuePair<string, object?>("Scope", scope) });
                }
            }, scopes);

            owner.Append(new LogEntry(
                Timestamp: DateTimeOffset.UtcNow,
                Level: logLevel,
                Category: category,
                EventId: eventId,
                Message: formatter(state, exception),
                OriginalFormat: originalFormat,
                Properties: properties,
                Scopes: scopes,
                Exception: exception));
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
