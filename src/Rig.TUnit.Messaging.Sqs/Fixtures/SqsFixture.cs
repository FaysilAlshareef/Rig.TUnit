using Amazon.SQS;
using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Fixtures;
using Rig.TUnit.Messaging.Sqs.Options;
using Testcontainers.LocalStack;

namespace Rig.TUnit.Messaging.Sqs.Fixtures;

public sealed class SqsFixture : MessagingFixtureBase
{
    private readonly SqsFixtureOptions _options;
    private LocalStackContainer? _container;
    private IAmazonSQS? _client;

    public SqsFixture() : this(new SqsFixtureOptions()) { }

    public SqsFixture(IOptions<SqsFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public SqsFixture(SqsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public IAmazonSQS Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new LocalStackBuilder($"localstack/localstack:{_options.ImageTag}").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        _client = new AmazonSQSClient(
            awsAccessKeyId: _options.AccessKeyId,
            awsSecretAccessKey: _options.SecretAccessKey,
            new AmazonSQSConfig
            {
                ServiceURL = _container.GetConnectionString(),
                AuthenticationRegion = _options.Region,
            });
    }

    public override async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
