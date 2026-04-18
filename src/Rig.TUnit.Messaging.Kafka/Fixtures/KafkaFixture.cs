using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.Kafka.Options;
using Testcontainers.Kafka;

namespace Rig.TUnit.Messaging.Kafka.Fixtures;

public sealed class KafkaFixture : MessagingFixtureBase
{
    private readonly KafkaFixtureOptions _options;
    private KafkaContainer? _container;

    public KafkaFixture() : this(new KafkaFixtureOptions()) { }
    public KafkaFixture(IOptions<KafkaFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }
    public KafkaFixture(KafkaFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetBootstrapAddress()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new KafkaBuilder()
            .WithImage($"confluentinc/cp-kafka:{_options.ImageTag}")
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
