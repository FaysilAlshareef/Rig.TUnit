using Rig.TUnit.Storage.S3.Fixtures;

namespace Rig.TUnit.Storage.S3.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

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
