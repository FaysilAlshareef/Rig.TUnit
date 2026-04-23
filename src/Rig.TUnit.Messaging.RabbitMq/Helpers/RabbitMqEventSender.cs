using RabbitMQ.Client;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Helpers;

public sealed class RabbitMqEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string _queue;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventSender(string connectionString, string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        _connectionString = connectionString;
        _queue = queue;
    }

    public Task SendAsync(
        string body,
        SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        var extra = context.PartitionKey is not null
            ? MergeHeader(additionalHeaders, "x-partition-key", context.PartitionKey)
            : additionalHeaders;

        var routingKey = context.PartitionKey ?? _queue;
        return SendCoreAsync(body, exchange: "", routingKey, correlationId, causationId, traceparent, extra, ct);
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        await SendCoreAsync(body, exchange: "", _queue, correlationId, causationId, traceparent, additionalHeaders, ct)
            .ConfigureAwait(false);
    }

    private async Task SendCoreAsync(
        string body,
        string exchange,
        string routingKey,
        string? correlationId,
        string? causationId,
        string? traceparent,
        IReadOnlyDictionary<string, string>? additionalHeaders,
        CancellationToken ct)
    {
        if (_connection is null)
        {
            var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
            _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
            await _channel.QueueDeclareAsync(_queue, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct).ConfigureAwait(false);
        }

        var headers = BuildHeaders(correlationId, causationId, traceparent, additionalHeaders);
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid().ToString(),
            Headers = headers.ToDictionary(
                kv => kv.Key,
                kv => (object?)System.Text.Encoding.UTF8.GetBytes(kv.Value)),
        };

        await _channel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: System.Text.Encoding.UTF8.GetBytes(body),
            cancellationToken: ct).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, string> MergeHeader(
        IReadOnlyDictionary<string, string>? existing,
        string key,
        string value)
    {
        var merged = existing is not null
            ? new Dictionary<string, string>(existing, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        merged[key] = value;
        return merged;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
