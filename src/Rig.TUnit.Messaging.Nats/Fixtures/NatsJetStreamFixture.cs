using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.Nats.Options;
using Testcontainers.Nats;

namespace Rig.TUnit.Messaging.Nats.Fixtures;

public sealed class NatsJetStreamFixture : MessagingFixtureBase
{
    private readonly NatsFixtureOptions _options;
    private NatsContainer? _container;
    private NatsConnection? _connection;
    private NatsJSContext? _jetStream;

    public NatsJetStreamFixture() : this(new NatsFixtureOptions()) { }

    public NatsJetStreamFixture(NatsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public INatsJSContext JetStream => _jetStream
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new NatsBuilder($"nats:{_options.ImageTag}").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        _connection = new NatsConnection(new NatsOpts { Url = ConnectionString });
        await _connection.ConnectAsync().ConfigureAwait(false);
        _jetStream = new NatsJSContext(_connection);
    }

    public async Task EnsureStreamAsync(
        string name,
        IEnumerable<string> subjects,
        long? maxMsgs = null,
        CancellationToken ct = default)
    {
        var config = new StreamConfig(name, subjects.ToList())
        {
            Retention = StreamConfigRetention.Limits,
            MaxMsgs = maxMsgs ?? -1
        };

        try
        {
            await JetStream.CreateStreamAsync(config, ct).ConfigureAwait(false);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 400)
        {
            // Stream already exists — update to ensure shape matches (idempotent)
            await JetStream.UpdateStreamAsync(config, ct).ConfigureAwait(false);
        }
    }

    public async Task<INatsJSStream> GetStreamAsync(string name, CancellationToken ct = default)
        => await JetStream.GetStreamAsync(name, cancellationToken: ct).ConfigureAwait(false);

    public override async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
