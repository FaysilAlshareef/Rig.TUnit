using Rig.TUnit.Storage.MinIO.Fixtures;

namespace Rig.TUnit.Storage.MinIO.Tests.Integration;

internal static class SharedMinIOFixture
{
    private static readonly Lazy<Task<MinIOFixture>> Instance = new(async () =>
    {
        var fx = new MinIOFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<MinIOFixture> GetAsync() => Instance.Value;
}
