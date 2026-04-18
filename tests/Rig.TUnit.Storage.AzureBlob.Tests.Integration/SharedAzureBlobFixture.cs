using Rig.TUnit.Storage.AzureBlob.Fixtures;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Integration;

internal static class SharedAzureBlobFixture
{
    private static readonly Lazy<Task<AzureBlobFixture>> Instance = new(async () =>
    {
        var fx = new AzureBlobFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<AzureBlobFixture> GetAsync() => Instance.Value;
}
