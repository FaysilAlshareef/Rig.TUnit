using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Helpers;

public sealed class KafkaListener : ListenerBase<ConsumeResult<string, string>>, IAsyncDisposable
{
    private readonly string _bootstrap;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _joinTimeout;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private IConsumer<string, string>? _consumer;
    private TaskCompletionSource? _partitionsAssigned;

    public KafkaListener(
        string bootstrapServers,
        string topic,
        string groupId,
        TimeProvider? clock = null,
        TimeSpan? joinTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        _bootstrap = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
        _clock = clock ?? TimeProvider.System;
        _joinTimeout = joinTimeout ?? TimeSpan.FromSeconds(30);
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        // Pre-create the topic before subscribing. Broker-side auto-create
        // only fires on produce, so a consumer that subscribes to a
        // not-yet-existing topic gets an empty metadata response and the
        // first rebalance never completes — StartAsync would cancel out
        // before the sender catches up.
        await EnsureTopicExistsAsync(ct).ConfigureAwait(false);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _partitionsAssigned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };
        _consumer = new ConsumerBuilder<string, string>(config)
            .SetPartitionsAssignedHandler((_, _) => _partitionsAssigned.TrySetResult())
            .Build();
        _consumer.Subscribe(_topic);

        _loop = Task.Run(() => ConsumeLoop(_cts.Token), _cts.Token);

        // StartAsync must not return before the first rebalance completes.
        // Otherwise a fast publisher can send and commit while the consumer
        // is still negotiating group membership, and the message is never
        // dispatched to this listener within a bounded test budget.
        using var joinCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        joinCts.CancelAfter(_joinTimeout);
        await using var reg = joinCts.Token.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(),
            _partitionsAssigned);
        await _partitionsAssigned.Task.ConfigureAwait(false);
    }

    private async Task EnsureTopicExistsAsync(CancellationToken ct)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrap }).Build();
        try
        {
            await admin.CreateTopicsAsync(
                [new TopicSpecification { Name = _topic, NumPartitions = 1, ReplicationFactor = 1 }])
                .WaitAsync(ct).ConfigureAwait(false);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Topic was already there — fine, the consumer can proceed.
        }
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
                result.Message.Value ?? string.Empty,
                correlationId));
        }
    }
}
