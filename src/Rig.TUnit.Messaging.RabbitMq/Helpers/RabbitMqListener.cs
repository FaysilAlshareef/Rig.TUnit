using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Helpers;

public sealed class RabbitMqListener : ListenerBase<BasicDeliverEventArgs>, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string _queue;
    private readonly string? _exchange;
    private readonly string? _exchangeType;
    private readonly string? _routingKey;
    private readonly TimeProvider _clock;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqListener(
        string connectionString,
        string queue,
        string? exchange = null,
        string? exchangeType = null,
        string? routingKey = null,
        TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        _connectionString = connectionString;
        _queue = queue;
        _exchange = exchange;
        _exchangeType = exchangeType;
        _routingKey = routingKey;
        _clock = clock ?? TimeProvider.System;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
        _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
        await _channel.QueueDeclareAsync(_queue, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct).ConfigureAwait(false);

        if (_exchange is not null)
        {
            await _channel.ExchangeDeclareAsync(
                _exchange,
                type: _exchangeType ?? "direct",
                durable: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct).ConfigureAwait(false);

            await _channel.QueueBindAsync(
                _queue,
                _exchange,
                routingKey: _routingKey ?? "#",
                arguments: null,
                cancellationToken: ct).ConfigureAwait(false);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            if (ea.BasicProperties.Headers is not null)
            {
                foreach (var kv in ea.BasicProperties.Headers)
                {
                    headers[kv.Key] = kv.Value switch
                    {
                        byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                        _ => kv.Value?.ToString() ?? "",
                    };
                }
            }

            headers.TryGetValue("x-partition-key", out var sessionKey);

            Record(new CapturedMessage<BasicDeliverEventArgs>(
                ea,
                _clock.GetUtcNow(),
                headers,
                System.Text.Encoding.UTF8.GetString(ea.Body.Span),
                ea.BasicProperties.CorrelationId,
                sessionKey));

            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(_queue, autoAck: true, consumer, ct).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(ct).ConfigureAwait(false);
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }
        if (_connection is not null)
        {
            await _connection.CloseAsync(ct).ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);
}
