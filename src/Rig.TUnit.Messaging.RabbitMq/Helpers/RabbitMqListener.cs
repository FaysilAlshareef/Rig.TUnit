using System.Collections.Concurrent;
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
    private readonly ConcurrentQueue<Exception> _errors = new();
    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>
    /// Decode failures (malformed header bytes, non-UTF8 body) surfaced from <c>ReceivedAsync</c>.
    /// The consumer runs with <c>autoAck: true</c>, so without this surface a malformed message
    /// would be acked and silently dropped — tests would only see "no messages received".
    /// </summary>
    public IReadOnlyCollection<Exception> Errors => _errors.ToArray();

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

        // Passive-then-active queue declare: passive (`QueueDeclarePassiveAsync`)
        // verifies an existing queue without redefining its arguments — this avoids
        // PRECONDITION_FAILED when topology pre-declared the queue with custom
        // args (x-max-priority, x-dead-letter-exchange, x-queue-type=quorum, etc.).
        // If the queue does not exist, the broker closes the channel with a 404
        // (OperationInterruptedException, ReplyCode == 404 / NotFound); we then
        // open a fresh channel and create the queue with no args (legacy path
        // for tests that don't use the topology builder). The catch is
        // narrowly-filtered on ShutdownReason.ReplyCode, so any other broker
        // failure (auth, connection lost, etc.) propagates to the caller.
        try
        {
            await _channel.QueueDeclarePassiveAsync(_queue, ct).ConfigureAwait(false);
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            when (ex.ShutdownReason?.ReplyCode == 404)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
            await _channel.QueueDeclareAsync(
                _queue, durable: false, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: ct).ConfigureAwait(false);
        }

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
            CaptureDelivery(ea);
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

    private void CaptureDelivery(BasicDeliverEventArgs ea)
    {
        // The consumer runs autoAck:true — the broker has already removed the
        // message by the time we get here. A throw inside the ReceivedAsync
        // callback would silently drop the payload and tests would only see
        // "no messages". Decode failures are returned as a typed error via
        // the Errors collection so consumers can assert on them — this is
        // the "return a typed error" branch of the project error-handling rule.
        try
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
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            _errors.Enqueue(ex);
        }
        catch (ArgumentException ex)
        {
            _errors.Enqueue(ex);
        }
    }
}
