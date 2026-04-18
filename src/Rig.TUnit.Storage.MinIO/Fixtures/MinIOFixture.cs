using Microsoft.Extensions.Options;
using Minio;
using Rig.TUnit.Storage.Fixtures;
using Rig.TUnit.Storage.MinIO.Options;
using Testcontainers.Minio;

namespace Rig.TUnit.Storage.MinIO.Fixtures;

public sealed class MinIOFixture : StorageFixtureBase
{
    private readonly MinIOFixtureOptions _options;
    private MinioContainer? _container;
    private IMinioClient? _client;

    public MinIOFixture() : this(new MinIOFixtureOptions()) { }

    public MinIOFixture(IOptions<MinIOFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public MinIOFixture(MinIOFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public IMinioClient Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new MinioBuilder($"minio/minio:{_options.ImageTag}")
            .WithUsername(_options.Username)
            .WithPassword(_options.Password)
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        var endpoint = new Uri(_container.GetConnectionString());
        _client = new MinioClient()
            .WithEndpoint($"{endpoint.Host}:{endpoint.Port}")
            .WithCredentials(_options.Username, _options.Password)
            .Build();
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
