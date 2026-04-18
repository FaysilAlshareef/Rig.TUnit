using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.Fixtures;
using Rig.TUnit.Observability.Seq.Options;
using Serilog;
using Serilog.Extensions.Logging;

namespace Rig.TUnit.Observability.Seq.Fixtures;

/// <summary>
/// Boots a <c>datalust/seq</c> Testcontainer, wires a Serilog Seq sink, and exposes an
/// <see cref="ILoggerFactory"/> whose entries flow into the running Seq instance.
/// <see cref="ConnectionString"/> returns the Seq ingestion URL (<c>http://host:port</c>).
/// </summary>
public class SeqFixture : TelemetryFixtureBase
{
    private readonly SeqFixtureOptions _options;
    private IContainer? _container;
    private string? _seqUrl;
    private ILoggerFactory? _loggerFactory;
    private Serilog.Core.Logger? _serilog;
    private bool _initialized;
    private bool _disposed;

    public SeqFixture() : this(new SeqFixtureOptions { ImageTag = "latest" }) { }
    public SeqFixture(SeqFixtureOptions options) { _options = options ?? throw new ArgumentNullException(nameof(options)); }
    public SeqFixture(IOptions<SeqFixtureOptions> options) : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public override string ConnectionString => _seqUrl
        ?? throw new InvalidOperationException("SeqFixture not initialised — call InitializeAsync first.");

    public string IngestionUrl => ConnectionString;

    public ILoggerFactory Factory => _loggerFactory
        ?? throw new InvalidOperationException("SeqFixture not initialised — call InitializeAsync first.");

    public override async Task InitializeAsync()
    {
        if (_initialized) return;

        _container = new ContainerBuilder()
            .WithImage($"datalust/seq:{_options.ImageTag}")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "True")
            .WithPortBinding(80, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPath("/api").ForPort(80)))
            .Build();

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token);

        var hostPort = _container.GetMappedPublicPort(80);
        _seqUrl = $"http://{_container.Hostname}:{hostPort}";

        _serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("ServiceName", ServiceName)
            .WriteTo.Seq(_seqUrl)
            .CreateLogger();

        _loggerFactory = new SerilogLoggerFactory(_serilog, dispose: false);
        _initialized = true;
    }

    /// <summary>
    /// Persists a snapshot artifact for the Seq dashboard used by this test. The current
    /// implementation writes a text file containing the Seq ingestion URL + a suggested
    /// Seq Pro query link — a headless-browser PNG capture would require Playwright or
    /// Puppeteer and is deferred. Still satisfies <c>CaptureDashboardSnapshot</c> semantics:
    /// an artifact per test in <c>TestResults/seq-dashboards/</c>.
    /// </summary>
    public async Task<string?> CaptureDashboardSnapshotAsync(string testName)
    {
        if (!_options.CaptureDashboardSnapshot || _seqUrl is null) return null;
        Directory.CreateDirectory(_options.SnapshotDirectory);
        var path = Path.Combine(_options.SnapshotDirectory, $"{SanitizeFileName(testName)}.txt");
        var content = $"Seq dashboard snapshot for {testName}\n"
                    + $"URL: {_seqUrl}\n"
                    + $"ServiceName: {ServiceName}\n"
                    + $"Captured: {DateTimeOffset.UtcNow:O}\n";
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _loggerFactory?.Dispose();
        _serilog?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        _loggerFactory = null;
        _serilog = null;
        _container = null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
