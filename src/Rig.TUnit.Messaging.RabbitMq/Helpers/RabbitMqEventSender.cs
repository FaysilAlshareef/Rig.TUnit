using RabbitMQ.Client;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Helpers;

/// <summary>
/// Async publisher for RabbitMQ. Two routing modes:
/// <list type="bullet">
/// <item>Default-exchange (constructor <c>exchange</c> is <see langword="null"/>): publishes
/// to the broker's nameless direct exchange with the queue name as the routing key.</item>
/// <item>Named-exchange (constructor <c>exchange</c> is set): publishes to the named exchange
/// with the routing key sourced from <see cref="SendContext.PartitionKey"/>, falling back to
/// the constructor <c>defaultRoutingKey</c> and finally to the queue name.</item>
/// </list>
/// The sender does NOT auto-declare the queue — topology must already exist (set up either
/// via the <c>WithTopology</c> builder, the listener, or out-of-band).
/// </summary>
public sealed class RabbitMqEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string _queue;
    private readonly string? _exchange;
    private readonly string? _defaultRoutingKey;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventSender(
        string connectionString,
        string? queue = null,
        string? exchange = null,
        string? defaultRoutingKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        if (string.IsNullOrWhiteSpace(queue) && string.IsNullOrWhiteSpace(exchange))
        {
            throw new ArgumentException(
                "At least one of 'queue' or 'exchange' must be specified.",
                nameof(queue));
        }
        _connectionString = connectionString;
        _queue = queue ?? string.Empty;
        _exchange = exchange;
        _defaultRoutingKey = defaultRoutingKey;
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

        var routingKey = context.PartitionKey ?? _defaultRoutingKey ?? _queue;
        return SendCoreAsync(body, exchange: _exchange ?? "", routingKey, correlationId, causationId, traceparent, extra, ct);
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        var routingKey = _defaultRoutingKey ?? _queue;
        await SendCoreAsync(body, exchange: _exchange ?? "", routingKey, correlationId, causationId, traceparent, additionalHeaders, ct)
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
