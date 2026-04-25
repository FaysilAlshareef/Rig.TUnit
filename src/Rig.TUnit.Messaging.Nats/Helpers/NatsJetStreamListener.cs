using NATS.Client.JetStream;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Nats.Helpers;

public sealed class NatsJetStreamListener : ListenerBase<NatsMessageRecord>, IAsyncDisposable
{
    private readonly INatsJSContext _jetStream;
    private readonly string _streamName;
    private readonly string[] _filterSubjects;
    private readonly TimeProvider _clock;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public NatsJetStreamListener(
        INatsJSContext jetStream,
        string streamName,
        params string[] filterSubjects)
    {
        _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
        _streamName = streamName;
        _filterSubjects = filterSubjects;
        _clock = TimeProvider.System;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Create the consumer here (before spawning the loop) so any broker
        // error surfaces to the StartAsync caller, and so a fast publisher
        // cannot race a not-yet-created consumer.
        var opts = _filterSubjects.Length > 0
            ? new NatsJSOrderedConsumerOpts { FilterSubjects = _filterSubjects }
            : new NatsJSOrderedConsumerOpts();

        var consumer = await _jetStream
            .CreateOrderedConsumerAsync(_streamName, opts, _cts.Token)
            .ConfigureAwait(false);

        _loop = Task.Run(() => ConsumeLoopAsync(consumer, _cts.Token), _cts.Token);
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
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            _loop = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private async Task ConsumeLoopAsync(INatsJSConsumer consumer, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: ct).ConfigureAwait(false))
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
                headers.TryGetValue("x-session-key", out var sessionKey);

                Record(new CapturedMessage<NatsMessageRecord>(
                    new NatsMessageRecord(msg.Subject, msg.Data, headers),
                    _clock.GetUtcNow(),
                    headers,
                    msg.Data ?? string.Empty,
                    correlationId,
                    sessionKey));

                await msg.AckAsync(cancellationToken: ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }
}
