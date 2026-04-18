using Amazon.DynamoDBv2;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.LocalStack;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Fixtures;

public sealed class DynamoFixture : DocumentFixtureBase
{
    private LocalStackContainer? _container;
    private IAmazonDynamoDB? _client;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public IAmazonDynamoDB Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new LocalStackBuilder().WithImage("localstack/localstack:3").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _container.StartAsync(cts.Token);

        _client = new AmazonDynamoDBClient(
            awsAccessKeyId: "test",
            awsSecretAccessKey: "test",
            new AmazonDynamoDBConfig
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
