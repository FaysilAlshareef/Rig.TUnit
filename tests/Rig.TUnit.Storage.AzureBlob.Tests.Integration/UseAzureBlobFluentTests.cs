using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.AzureBlob.Builder;
using Rig.TUnit.Storage.AzureBlob.Helpers;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Integration;

public sealed class UseAzureBlobFluentTests
{
    [Test]
    public async Task UseAzureBlob_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("UseDevelopmentStorage=true");

        AzureBlobRigBuilder? configured = null;
        captured!.UseAzureBlob(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }

    [Test]
    public async Task Fixture_Initialize_ThenUploadAndDownload_Succeeds()
    {
        var fx = await SharedAzureBlobFixture.GetAsync();
        var containerName = $"integ-{Guid.NewGuid():N}";
        var container = fx.Client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        try
        {
            var blob = container.GetBlobClient("test.txt");
            await blob.UploadAsync(BinaryData.FromString("hello"), overwrite: true);

            var download = await blob.DownloadContentAsync();
            var body = download.Value.Content.ToString();

            await Assert.That(body).IsEqualTo("hello");
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    [Test]
    public async Task SasBuilder_BuildQueryString_ContainsExpectedParameters()
    {
        var query = AzureBlobSasBuilder.BuildQueryString(
            "demo", "file.txt", "r", TimeSpan.FromMinutes(5), TimeProvider.System);

        await Assert.That(query).IsNotNullOrEmpty();
        await Assert.That(query).Contains("sp=r");
    }
}
