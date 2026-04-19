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

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
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
            exchange: "",
            routingKey: _queue,
            mandatory: false,
            basicProperties: props,
            body: System.Text.Encoding.UTF8.GetBytes(body),
            cancellationToken: ct).ConfigureAwait(false);
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
