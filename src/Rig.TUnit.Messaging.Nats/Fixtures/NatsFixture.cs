using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.Nats.Options;
using Testcontainers.Nats;

namespace Rig.TUnit.Messaging.Nats.Fixtures;

public sealed class NatsFixture : MessagingFixtureBase
{
    private readonly NatsFixtureOptions _options;
    private NatsContainer? _container;

    public NatsFixture() : this(new NatsFixtureOptions()) { }

    public NatsFixture(IOptions<NatsFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public NatsFixture(NatsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new NatsBuilder($"nats:{_options.ImageTag}").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
