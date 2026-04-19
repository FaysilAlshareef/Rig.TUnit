using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.Fixtures;
using Rig.TUnit.Observability.Logging.Options;
using Rig.TUnit.Observability.Logging.Providers;

namespace Rig.TUnit.Observability.Logging.Fixtures;

/// <summary>
/// In-memory logging fixture. Owns an <see cref="ILoggerFactory"/> backed by
/// <see cref="InMemoryLoggerProvider"/> so tests can emit entries through the standard
/// <see cref="ILogger{T}"/> API and assert on them via <c>LogAssert</c>.
/// </summary>
public class LoggingFixture : TelemetryFixtureBase
{
    private readonly LoggingFixtureOptions _options;
    private InMemoryLoggerProvider? _provider;
    private ILoggerFactory? _factory;
    private bool _initialized;
    private bool _disposed;

    public LoggingFixture() : this(new LoggingFixtureOptions()) { }

    public LoggingFixture(LoggingFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public LoggingFixture(IOptions<LoggingFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public override string ConnectionString => string.Empty;

    /// <summary>Captured log entries. Populated live — no flush needed.</summary>
    public IReadOnlyList<LogEntry> Entries => _provider?.Entries ?? Array.Empty<LogEntry>();

    /// <summary>Creates a typed <see cref="ILogger{T}"/> against the fixture's in-memory provider.</summary>
    public ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();

    /// <summary>Creates a category-named <see cref="ILogger"/> against the fixture's in-memory provider.</summary>
    public ILogger CreateLogger(string category) => Factory.CreateLogger(category);

    public ILoggerFactory Factory => _factory
        ?? throw new InvalidOperationException("LoggingFixture not initialised — call InitializeAsync first.");

    public override Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;

        _provider = new InMemoryLoggerProvider(_options.MinLevel, _options.MaxEntriesInMemory);
        _factory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(_options.MinLevel);
            b.AddProvider(_provider);
        });
        _initialized = true;
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        _factory?.Dispose();
        _provider?.Dispose();
        _factory = null;
        _provider = null;
        return ValueTask.CompletedTask;
    }
}
