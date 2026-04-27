using NATS.Client.Core;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Nats.Helpers;

/// <summary>Wrapper reference type so <see cref="ListenerBase{T}"/> can carry NATS messages
/// (NATS.Client.Core v2's <c>NatsMsg&lt;T&gt;</c> is a struct; generic constraints require a class).</summary>
public sealed record NatsMessageRecord(string Subject, string? Data, IReadOnlyDictionary<string, string> Headers);

public sealed class NatsListener : ListenerBase<NatsMessageRecord>, IAsyncDisposable
{
    private readonly string _url;
    private readonly string _subject;
    private readonly TimeProvider _clock;
    private NatsConnection? _connection;
    private INatsSub<string>? _sub;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public NatsListener(string url, string subject, TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        _url = url;
        _subject = subject;
        _clock = clock ?? TimeProvider.System;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        _connection = new NatsConnection(new NatsOpts { Url = _url });
        await _connection.ConnectAsync().ConfigureAwait(false);

        // Register the subscription EAGERLY before returning. The previous
        // implementation called `_connection.SubscribeAsync<T>(...)` from
        // inside a Task.Run loop. That method is lazy — the SUB protocol
        // message isn't sent to the server until the first MoveNextAsync
        // on the IAsyncEnumerable runs. NATS Core is fire-and-forget with
        // no server-side buffering, so a publisher that calls SendAsync
        // immediately after StartAsync could see its message land at the
        // broker BEFORE the SUB is registered, and the broker drops it.
        // PingAsync forces a flush + PONG round-trip; the server processes
        // commands in order, so a successful PING after SUB confirms SUB
        // has been registered.
        _sub = await _connection
            .SubscribeCoreAsync<string>(_subject, cancellationToken: ct)
            .ConfigureAwait(false);
        await _connection.PingAsync(ct).ConfigureAwait(false);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => SubscribeLoopAsync(_sub, _cts.Token), _cts.Token);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
        }
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            _loop = null;
        }
        if (_sub is not null)
        {
            await _sub.DisposeAsync().ConfigureAwait(false);
            _sub = null;
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private async Task SubscribeLoopAsync(INatsSub<string> sub, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in sub.Msgs.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var headers = new Dictionary<string, string>(StringComparer.Ordinal);
                if (msg.Headers is not null)
                {
                    foreach (var h in msg.Headers)
                    {
                        headers[h.Key] = string.Join(",", h.Value.ToArray());
                    }
                }

                headers.TryGetValue("x-correlation-id", out var correlationId);

                Record(new CapturedMessage<NatsMessageRecord>(
                    new NatsMessageRecord(msg.Subject, msg.Data, headers),
                    _clock.GetUtcNow(),
                    headers,
                    msg.Data ?? string.Empty,
                    correlationId));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
