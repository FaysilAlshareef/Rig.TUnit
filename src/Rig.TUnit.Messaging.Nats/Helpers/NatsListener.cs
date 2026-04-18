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

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => SubscribeLoopAsync(_cts.Token), _cts.Token);

        await Task.Yield();
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
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private async Task SubscribeLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var msg in _connection!.SubscribeAsync<string>(_subject, cancellationToken: ct).ConfigureAwait(false))
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
                    msg.Data,
                    correlationId));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
