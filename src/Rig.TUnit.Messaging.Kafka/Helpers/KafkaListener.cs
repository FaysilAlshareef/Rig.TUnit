using Confluent.Kafka;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Helpers;

public sealed class KafkaListener : ListenerBase<ConsumeResult<string, string>>, IAsyncDisposable
{
    private readonly string _bootstrap;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly TimeProvider _clock;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private IConsumer<string, string>? _consumer;

    public KafkaListener(string bootstrapServers, string topic, string groupId, TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        _bootstrap = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
        _clock = clock ?? TimeProvider.System;
    }

    public override Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(_topic);

        _loop = Task.Run(() => ConsumeLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
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
        if (_consumer is not null)
        {
            _consumer.Close();
            _consumer.Dispose();
            _consumer = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private void ConsumeLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = _consumer!.Consume(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (result is null || result.Message is null)
            {
                continue;
            }

            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            if (result.Message.Headers is not null)
            {
                foreach (var h in result.Message.Headers)
                {
                    headers[h.Key] = System.Text.Encoding.UTF8.GetString(h.GetValueBytes());
                }
            }

            headers.TryGetValue("x-correlation-id", out var correlationId);

            Record(new CapturedMessage<ConsumeResult<string, string>>(
                result,
                _clock.GetUtcNow(),
                headers,
                result.Message.Value,
                correlationId));
        }
    }
}
