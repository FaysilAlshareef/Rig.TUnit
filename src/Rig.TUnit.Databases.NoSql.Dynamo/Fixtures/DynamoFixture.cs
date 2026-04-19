using Amazon.DynamoDBv2;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Dynamo.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.LocalStack;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Fixtures;

public sealed class DynamoFixture : DocumentFixtureBase
{
    private readonly DynamoFixtureOptions _options;
    private LocalStackContainer? _container;
    private IAmazonDynamoDB? _client;

    public DynamoFixture() : this(new DynamoFixtureOptions()) { }

    public DynamoFixture(IOptions<DynamoFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public DynamoFixture(DynamoFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public IAmazonDynamoDB Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new LocalStackBuilder($"localstack/localstack:{_options.ImageTag}").Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        _client = new AmazonDynamoDBClient(
            awsAccessKeyId: "test",
            awsSecretAccessKey: "test",
            new AmazonDynamoDBConfig
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
