namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Integration;

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
