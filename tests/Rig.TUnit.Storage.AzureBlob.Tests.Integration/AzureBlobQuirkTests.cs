using System.Text;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Integration;

/// <summary>Azure Blob quirks: container creation, upload-download roundtrip, metadata.</summary>
public sealed class AzureBlobQuirkTests
{
    [Test]
    public async Task CreateContainer_WhenNotExists_Succeeds()
    {
        var fx = await SharedAzureBlobFixture.GetAsync();
        var name = "c-" + Guid.NewGuid().ToString("N");
        var container = fx.Client.GetBlobContainerClient(name);
        await container.CreateIfNotExistsAsync();

        var exists = await container.ExistsAsync();
        await Assert.That(exists.Value).IsTrue();
    }

    [Test]
    public async Task UploadThenDownload_RoundtripsBlobContent()
    {
        var fx = await SharedAzureBlobFixture.GetAsync();
        var name = "c-" + Guid.NewGuid().ToString("N");
        var container = fx.Client.GetBlobContainerClient(name);
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient("hello.txt");
        using var up = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        await blob.UploadAsync(up);
        var dl = await blob.DownloadContentAsync();
        var content = dl.Value.Content.ToString();

        await Assert.That(content).IsEqualTo("hello world");
    }

    [Test]
    public async Task SetMetadata_ThenGetProperties_ReturnsMetadata()
    {
        var fx = await SharedAzureBlobFixture.GetAsync();
        var name = "c-" + Guid.NewGuid().ToString("N");
        var container = fx.Client.GetBlobContainerClient(name);
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient("meta.txt");
        using var up = new MemoryStream(Encoding.UTF8.GetBytes("x"));
        await blob.UploadAsync(up);
        await blob.SetMetadataAsync(new Dictionary<string, string> { ["owner"] = "rigtunit" });
        var props = await blob.GetPropertiesAsync();

        await Assert.That(props.Value.Metadata["owner"]).IsEqualTo("rigtunit");
    }
}
