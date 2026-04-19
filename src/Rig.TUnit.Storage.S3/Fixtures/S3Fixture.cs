using Amazon.S3;
using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.Fixtures;
using Rig.TUnit.Storage.S3.Options;
using Testcontainers.LocalStack;

namespace Rig.TUnit.Storage.S3.Fixtures;

public sealed class S3Fixture : StorageFixtureBase
{
    private readonly S3FixtureOptions _options;
    private LocalStackContainer? _container;
    private IAmazonS3? _client;

    public S3Fixture() : this(new S3FixtureOptions()) { }
    public S3Fixture(IOptions<S3FixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }
    public S3Fixture(S3FixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public IAmazonS3 Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new LocalStackBuilder($"localstack/localstack:{_options.ImageTag}")
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        _client = new AmazonS3Client(
            awsAccessKeyId: "test",
            awsSecretAccessKey: "test",
            clientConfig: new AmazonS3Config
            {
                ServiceURL = _container.GetConnectionString(),
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
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
