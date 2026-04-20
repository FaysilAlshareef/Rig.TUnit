namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

internal static class SharedRedisKvFixture
{
    private static readonly Lazy<Task<RedisKvFixture>> Instance = new(async () =>
    {
        var fx = new RedisKvFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<RedisKvFixture> GetAsync() => Instance.Value;
}
