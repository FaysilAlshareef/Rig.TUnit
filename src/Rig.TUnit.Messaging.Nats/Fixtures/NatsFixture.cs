using Rig.TUnit.Messaging.Fixtures;
using Testcontainers.Nats;

namespace Rig.TUnit.Messaging.Nats.Fixtures;

public sealed class NatsFixture : MessagingFixtureBase
{
    private NatsContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new NatsBuilder().WithImage("nats:2.10-alpine").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _container.StartAsync(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
