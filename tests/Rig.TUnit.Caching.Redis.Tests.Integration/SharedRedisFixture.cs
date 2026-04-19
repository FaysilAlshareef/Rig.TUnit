using Rig.TUnit.Caching.Redis.Fixtures;

namespace Rig.TUnit.Caching.Redis.Tests.Integration;

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
