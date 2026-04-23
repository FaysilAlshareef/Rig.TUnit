using Confluent.Kafka;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Helpers;

public sealed class KafkaEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly string _bootstrap;
    private readonly string _topic;
    private IProducer<string, string>? _producer;

    public KafkaEventSender(string bootstrapServers, string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        _bootstrap = bootstrapServers;
        _topic = topic;
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        _producer ??= new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = _bootstrap }).Build();

        var headers = BuildHeaders(correlationId, causationId, traceparent, additionalHeaders);

        var message = new Message<string, string>
        {
            Key = correlationId ?? Guid.NewGuid().ToString(),
            Value = body,
            Headers = [],
        };
        foreach (var kv in headers)
        {
            message.Headers.Add(kv.Key, System.Text.Encoding.UTF8.GetBytes(kv.Value));
        }

        await _producer.ProduceAsync(_topic, message, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends <paramref name="body"/> with optional routing hints from <paramref name="context"/>.
    /// <list type="bullet">
    ///   <item><see cref="SendContext.PartitionKey"/> → <see cref="Message{TKey,TValue}.Key"/>
    ///     (preferred over <paramref name="correlationId"/>)</item>
    ///   <item><see cref="SendContext.SessionKey"/> → <see cref="Message{TKey,TValue}.Key"/>
    ///     when <see cref="SendContext.PartitionKey"/> is absent (session key folds to partition key)</item>
    /// </list>
    /// </summary>
    public async Task SendAsync(
        string body,
        SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        _producer ??= new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = _bootstrap }).Build();

        var headers = BuildHeaders(context, correlationId, causationId, traceparent, additionalHeaders);
        var key = context.PartitionKey ?? context.SessionKey ?? correlationId ?? Guid.NewGuid().ToString();

        var message = new Message<string, string>
        {
            Key = key,
            Value = body,
            Headers = [],
        };
        foreach (var kv in headers)
        {
            message.Headers.Add(kv.Key, System.Text.Encoding.UTF8.GetBytes(kv.Value));
        }

        await _producer.ProduceAsync(_topic, message, ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        if (_producer is not null)
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
            _producer = null;
        }
        return ValueTask.CompletedTask;
    }
}
