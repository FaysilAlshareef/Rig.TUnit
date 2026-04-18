using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.AzureBlob.Options;
using Rig.TUnit.Storage.Fixtures;
using Testcontainers.Azurite;

namespace Rig.TUnit.Storage.AzureBlob.Fixtures;

public sealed class AzureBlobFixture : StorageFixtureBase
{
    private readonly AzureBlobFixtureOptions _options;
    private AzuriteContainer? _container;
    private BlobServiceClient? _client;

    public AzureBlobFixture() : this(new AzureBlobFixtureOptions()) { }
    public AzureBlobFixture(IOptions<AzureBlobFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }
    public AzureBlobFixture(AzureBlobFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public BlobServiceClient Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new AzuriteBuilder()
            .WithImage($"mcr.microsoft.com/azure-storage/azurite:{_options.ImageTag}")
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
        _client = new BlobServiceClient(_container.GetConnectionString());
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
            _client = null;
        }
    }
}
