using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.RabbitMq.Options;
using Testcontainers.RabbitMq;

namespace Rig.TUnit.Messaging.RabbitMq.Fixtures;

public sealed class RabbitMqFixture : MessagingFixtureBase
{
    private readonly RabbitMqFixtureOptions _options;
    private RabbitMqContainer? _container;

    public RabbitMqFixture() : this(new RabbitMqFixtureOptions()) { }
    public RabbitMqFixture(IOptions<RabbitMqFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }
    public RabbitMqFixture(RabbitMqFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new RabbitMqBuilder()
            .WithImage($"rabbitmq:{_options.ImageTag}")
            .WithUsername(_options.Username)
            .WithPassword(_options.Password)
            .Build();
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
