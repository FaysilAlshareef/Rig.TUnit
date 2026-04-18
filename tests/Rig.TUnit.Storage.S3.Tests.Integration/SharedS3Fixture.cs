using Rig.TUnit.Storage.S3.Fixtures;

namespace Rig.TUnit.Storage.S3.Tests.Integration;

internal static class SharedS3Fixture
{
    private static readonly Lazy<Task<S3Fixture>> Instance = new(async () =>
    {
        var fx = new S3Fixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<S3Fixture> GetAsync() => Instance.Value;
}
