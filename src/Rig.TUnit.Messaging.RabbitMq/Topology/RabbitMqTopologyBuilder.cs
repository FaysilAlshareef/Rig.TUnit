using RabbitMQ.Client;

namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public sealed class RabbitMqTopologyBuilder : IRabbitMqTopologyBuilder
{
    private readonly string _connectionString;
    private readonly List<ExchangeSpec> _exchanges = [];
    private readonly List<QueueSpec> _queues = [];

    public RabbitMqTopologyBuilder(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public IRabbitMqExchangeConfig Exchange(string name, ExchangeType type, Action<IRabbitMqExchangeConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var spec = new ExchangeSpec(name, type);
        _exchanges.Add(spec);
        var config = new ExchangeConfig(spec);
        configure?.Invoke(config);
        return config;
    }

    public IRabbitMqTopologyBuilder Queue(string name, Action<IRabbitMqQueueConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var cfg = new QueueConfig();
        configure?.Invoke(cfg);
        _queues.Add(new QueueSpec(name, cfg.BuildArguments()));
        return this;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
        await using var connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

        foreach (var ex in _exchanges)
        {
            var exchangeTypeName = ex.Type switch
            {
                ExchangeType.Direct  => RabbitMQ.Client.ExchangeType.Direct,
                ExchangeType.Topic   => RabbitMQ.Client.ExchangeType.Topic,
                ExchangeType.Fanout  => RabbitMQ.Client.ExchangeType.Fanout,
                ExchangeType.Headers => RabbitMQ.Client.ExchangeType.Headers,
                _                    => RabbitMQ.Client.ExchangeType.Direct,
            };

            await channel.ExchangeDeclareAsync(
                ex.Name, type: exchangeTypeName,
                durable: false, autoDelete: false, arguments: null,
                cancellationToken: ct).ConfigureAwait(false);

            foreach (var binding in ex.Bindings)
            {
                await channel.QueueDeclareAsync(
                    binding.QueueName, durable: false, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: ct).ConfigureAwait(false);
                await channel.QueueBindAsync(
                    binding.QueueName, ex.Name, binding.RoutingKey,
                    arguments: null, cancellationToken: ct).ConfigureAwait(false);
            }
        }

        foreach (var q in _queues)
        {
            // Quorum queues are inherently durable — declaring `durable: false` on a
            // queue with `x-queue-type: quorum` is rejected by RabbitMQ with
            // PRECONDITION_FAILED ("invalid property 'non-durable'").
            var isQuorum = q.Arguments.TryGetValue("x-queue-type", out var qType)
                && string.Equals(qType?.ToString(), "quorum", StringComparison.Ordinal);

            await channel.QueueDeclareAsync(
                q.Name, durable: isQuorum, exclusive: false, autoDelete: false,
                arguments: q.Arguments.Count > 0 ? q.Arguments : null,
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    private sealed record ExchangeSpec(string Name, ExchangeType Type)
    {
        public List<BindingSpec> Bindings { get; } = [];
    }

    private sealed record BindingSpec(string QueueName, string RoutingKey);
    private sealed record QueueSpec(string Name, Dictionary<string, object?> Arguments);

    private sealed class ExchangeConfig : IRabbitMqExchangeConfig
    {
        private readonly ExchangeSpec _spec;

        internal ExchangeConfig(ExchangeSpec spec) => _spec = spec;

        public IRabbitMqExchangeConfig BindQueue(string queueName, string routingKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
            _spec.Bindings.Add(new BindingSpec(queueName, routingKey));
            return this;
        }
    }

    private sealed class QueueConfig : IRabbitMqQueueConfig
    {
        private readonly Dictionary<string, object?> _args = new(StringComparer.Ordinal);

        public IRabbitMqQueueConfig WithMessageTtl(TimeSpan ttl)
        {
            _args["x-message-ttl"] = (int)ttl.TotalMilliseconds;
            return this;
        }

        public IRabbitMqQueueConfig WithMaxLength(int maxLength)
        {
            _args["x-max-length"] = maxLength;
            return this;
        }

        public IRabbitMqQueueConfig WithMaxPriority(int maxPriority)
        {
            _args["x-max-priority"] = maxPriority;
            return this;
        }

        public IRabbitMqQueueConfig WithDeadLetterExchange(string exchange, string? routingKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
            _args["x-dead-letter-exchange"] = exchange;
            if (routingKey is not null)
                _args["x-dead-letter-routing-key"] = routingKey;
            return this;
        }

        public IRabbitMqQueueConfig WithQuorum()
        {
            _args["x-queue-type"] = "quorum";
            return this;
        }

        public Dictionary<string, object?> BuildArguments() => new(_args, StringComparer.Ordinal);
    }
}
