using Rig.TUnit.Caching.Helpers;
using StackExchange.Redis;

namespace Rig.TUnit.Caching.Redis.Helpers;

/// <summary>
/// Captures Redis pub/sub messages used as a cache-invalidation backplane.
/// </summary>
public sealed class RedisBackplaneCapture : BackplaneCapture, IAsyncDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly string _channel;
    private ChannelMessageQueue? _queue;

    public RedisBackplaneCapture(IConnectionMultiplexer connection, string channel)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        _channel = channel;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        var sub = _connection.GetSubscriber();
        _queue = await sub.SubscribeAsync(RedisChannel.Literal(_channel)).ConfigureAwait(false);
        _queue.OnMessage(msg =>
        {
            Record(new BackplaneMessage(_channel, msg.Message.ToString(), DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        });
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_queue is not null)
        {
            await _queue.UnsubscribeAsync().ConfigureAwait(false);
            _queue = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
