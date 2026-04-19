using Microsoft.Extensions.Logging;

namespace Rig.TUnit.Observability.Logging;

/// <summary>Captured log entry emitted through the in-memory <c>ILoggerProvider</c>.</summary>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    EventId EventId,
    string Message,
    string? OriginalFormat,
    IReadOnlyList<KeyValuePair<string, object?>> Properties,
    IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>> Scopes,
    Exception? Exception);
