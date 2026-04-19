using StackExchange.Redis;

namespace Rig.TUnit.Caching.Redis.Tests.Integration;

/// <summary>
/// Redis provider quirks: TTL precision at millisecond level, SCAN-based key
/// iteration (preferred over KEYS), and pub/sub backplane signalling.
/// </summary>
public sealed class RedisCacheQuirkTests
{
    private static async Task<ConnectionMultiplexer> ConnectAsync()
    {
        var fixture = await SharedRedisFixture.GetAsync().ConfigureAwait(false);
        return await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString).ConfigureAwait(false);
    }

    [Test]
    public async Task Quirk_TtlPrecision_IsMillisecondLevel(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var db = mx.GetDatabase();
        var key = $"ttl_{Guid.NewGuid():N}";

        await db.StringSetAsync(key, "v", TimeSpan.FromMilliseconds(2500));
        var remaining = await db.KeyTimeToLiveAsync(key);

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining!.Value.TotalMilliseconds).IsGreaterThan(100);
    }

    [Test]
    public async Task Quirk_ScanOverKeys_YieldsMatchingEntries(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var db = mx.GetDatabase();
        var prefix = $"scan_{Guid.NewGuid():N}";

        for (var i = 0; i < 5; i++)
        {
            await db.StringSetAsync($"{prefix}:{i}", i.ToString());
        }

        var server = mx.GetServer(mx.GetEndPoints()[0]);
        var scanned = server.Keys(pattern: $"{prefix}:*", pageSize: 10).ToList();

        await Assert.That(scanned.Count).IsEqualTo(5);
    }

    [Test]
    public async Task Quirk_PubSubBackplane_DeliversPublishedMessage(CancellationToken ct)
    {
        using var mx = await ConnectAsync();
        var sub = mx.GetSubscriber();
        var channel = RedisChannel.Literal($"ch_{Guid.NewGuid():N}");

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await sub.SubscribeAsync(channel, (_, v) => received.TrySetResult(v.ToString()));

        await sub.PublishAsync(channel, "hello");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => received.TrySetCanceled());
        var payload = await received.Task;

        await Assert.That(payload).IsEqualTo("hello");
    }
}
