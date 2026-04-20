using Rig.TUnit.Caching.Redis.Fixtures;

namespace Rig.TUnit.Caching.Redis.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

/// <summary>
/// Process-wide shared <see cref="RedisFixture"/> so the <c>redis:7-alpine</c>
/// container boots once across every test class in this assembly. The Testcontainers
/// Ryuk reaper cleans the container up when the test host exits.
/// </summary>
internal static class SharedRedisFixture
{
    private static readonly Lazy<Task<RedisFixture>> Instance = new(async () =>
    {
        var fx = new RedisFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<RedisFixture> GetAsync() => Instance.Value;
}
