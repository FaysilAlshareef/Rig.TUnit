using Minio;
using Rig.TUnit.Storage.Fixtures;
using Testcontainers.Minio;

namespace Rig.TUnit.Storage.MinIO.Fixtures;

public sealed class MinIOFixture : StorageFixtureBase
{
    private MinioContainer? _container;
    private IMinioClient? _client;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public IMinioClient Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new MinioBuilder()
            .WithImage("minio/minio:latest")
            .WithUsername("minioadmin")
            .WithPassword("minioadmin")
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _container.StartAsync(cts.Token);

        var endpoint = new Uri(_container.GetConnectionString());
        _client = new MinioClient()
            .WithEndpoint($"{endpoint.Host}:{endpoint.Port}")
            .WithCredentials("minioadmin", "minioadmin")
            .Build();
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
