using Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

internal static class SharedKurrentDbFixture
{
    private static readonly Lazy<Task<KurrentDbFixture>> Instance = new(async () =>
    {
        var fx = new KurrentDbFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<KurrentDbFixture> GetAsync() => Instance.Value;
}
