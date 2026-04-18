using Amazon.SQS;
using Rig.TUnit.Messaging.Fixtures;
using Testcontainers.LocalStack;

namespace Rig.TUnit.Messaging.Sqs.Fixtures;

public sealed class SqsFixture : MessagingFixtureBase
{
    private LocalStackContainer? _container;
    private IAmazonSQS? _client;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public IAmazonSQS Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new LocalStackBuilder("localstack/localstack:3").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await _container.StartAsync(cts.Token);

        _client = new AmazonSQSClient(
            awsAccessKeyId: "test",
            awsSecretAccessKey: "test",
            new AmazonSQSConfig
            {
                ServiceURL = _container.GetConnectionString(),
                AuthenticationRegion = "us-east-1",
            });
    }

    public override async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
