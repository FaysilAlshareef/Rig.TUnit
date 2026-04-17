using StackExchange.Redis;

namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Integration;

/// <summary>
/// KV-role quirks: hash fields, SET/GET roundtrip, and SCAN-based key iteration.
/// </summary>
public sealed class RedisKvQuirkTests
{
    private static async Task<ConnectionMultiplexer> ConnectAsync()
    {
        var fx = await SharedRedisKvFixture.GetAsync().ConfigureAwait(false);
        return await ConnectionMultiplexer.ConnectAsync(fx.ConnectionString).ConfigureAwait(false);
    }

    [Test]
    public async Task Kv_SetGet_RoundTripsStringValue(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var db = mx.GetDatabase();
        var key = $"kv_{Guid.NewGuid():N}";

        await db.StringSetAsync(key, "alpha");
        var value = (string?)await db.StringGetAsync(key);

        await Assert.That(value).IsEqualTo("alpha");
    }

    [Test]
    public async Task Kv_HashFields_RoundTripIndependently(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var db = mx.GetDatabase();
        var key = $"hash_{Guid.NewGuid():N}";

        await db.HashSetAsync(key, [new HashEntry("a", "1"), new HashEntry("b", "2")]);
        var a = (string?)await db.HashGetAsync(key, "a");
        var b = (string?)await db.HashGetAsync(key, "b");

        await Assert.That(a).IsEqualTo("1");
        await Assert.That(b).IsEqualTo("2");
    }

    [Test]
    public async Task Kv_ScanKeys_YieldsInsertedKeys(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var db = mx.GetDatabase();
        var prefix = $"kvscan_{Guid.NewGuid():N}";

        for (var i = 0; i < 3; i++)
        {
            await db.StringSetAsync($"{prefix}:{i}", i.ToString());
        }

        var server = mx.GetServer(mx.GetEndPoints()[0]);
        var scanned = server.Keys(pattern: $"{prefix}:*", pageSize: 10).ToList();

        await Assert.That(scanned.Count).IsEqualTo(3);
    }
}
