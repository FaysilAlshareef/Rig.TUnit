using Azure.Messaging.ServiceBus.Administration;

namespace Rig.TUnit.Messaging.ServiceBus.Topology;

/// <summary>
/// Records topology declarations and applies them idempotently via
/// <see cref="ServiceBusAdministrationHelper"/> on <see cref="ApplyAsync"/>.
/// </summary>
public sealed class ServiceBusTopologyBuilder : IServiceBusTopologyBuilder
{
    private readonly ServiceBusAdministrationHelper _helper;
    private readonly List<Func<CancellationToken, Task>> _actions = new();

    public ServiceBusTopologyBuilder(ServiceBusAdministrationHelper helper)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
    }

    public IServiceBusTopologyBuilder Topic(string name, Action<IServiceBusTopicConfig>? configure = null)
    {
        var cfg = new TopicConfig();
        configure?.Invoke(cfg);
        _actions.Add(ct => _helper.CreateTopicIfNotExistsAsync(
            name,
            cfg.BuildOptions(name),
            ct));
        return this;
    }

    public IServiceBusTopologyBuilder Subscription(
        string topic,
        string name,
        Action<IServiceBusSubscriptionConfig>? configure = null)
    {
        var cfg = new SubscriptionConfig();
        configure?.Invoke(cfg);
        _actions.Add(ct =>
        {
            if (cfg.SqlRule is not null)
            {
                return _helper.CreateSubscriptionWithRuleAsync(
                    topic, name,
                    cfg.SqlRule.Value.Name,
                    cfg.SqlRule.Value.Filter,
                    cfg.RequiresSession,
                    ct);
            }
            return _helper.CreateSubscriptionIfNotExistsAsync(topic, name, cfg.RequiresSession, ct);
        });
        return this;
    }

    public IServiceBusTopologyBuilder Queue(string name, Action<IServiceBusQueueConfig>? configure = null)
    {
        var cfg = new QueueConfig();
        configure?.Invoke(cfg);
        _actions.Add(ct => _helper.CreateQueueIfNotExistsAsync(name, cfg.BuildOptions(name), ct));
        return this;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        foreach (var action in _actions)
            await action(ct).ConfigureAwait(false);
    }

    private sealed class TopicConfig : IServiceBusTopicConfig
    {
        private TimeSpan? _ttl;
        private bool _partition;
        private bool _dedupe;

        public IServiceBusTopicConfig WithDefaultMessageTimeToLive(TimeSpan ttl) { _ttl = ttl; return this; }
        public IServiceBusTopicConfig WithEnablePartitioning(bool enabled = true) { _partition = enabled; return this; }
        public IServiceBusTopicConfig WithRequiresDuplicateDetection(bool enabled = true) { _dedupe = enabled; return this; }

        public CreateTopicOptions BuildOptions(string name)
        {
            var o = new CreateTopicOptions(name);
            if (_ttl.HasValue) o.DefaultMessageTimeToLive = _ttl.Value;
            o.EnablePartitioning = _partition;
            o.RequiresDuplicateDetection = _dedupe;
            return o;
        }
    }

    private sealed class SubscriptionConfig : IServiceBusSubscriptionConfig
    {
        public bool RequiresSession { get; private set; }
        public (string Name, SqlRuleFilter Filter)? SqlRule { get; private set; }

        public IServiceBusSubscriptionConfig WithRequiresSession(bool required = true) { RequiresSession = required; return this; }
        public IServiceBusSubscriptionConfig WithDefaultMessageTimeToLive(TimeSpan ttl) => this;
        public IServiceBusSubscriptionConfig WithLockDuration(TimeSpan lockDuration) => this;
        public IServiceBusSubscriptionConfig WithMaxDeliveryCount(int count) => this;
        public IServiceBusSubscriptionConfig WithRule(string name, SqlRuleFilter filter) { SqlRule = (name, filter); return this; }
    }

    private sealed class QueueConfig : IServiceBusQueueConfig
    {
        private bool _session;
        private TimeSpan? _ttl;
        private TimeSpan? _lock;
        private int? _maxDelivery;

        public IServiceBusQueueConfig WithRequiresSession(bool required = true) { _session = required; return this; }
        public IServiceBusQueueConfig WithDefaultMessageTimeToLive(TimeSpan ttl) { _ttl = ttl; return this; }
        public IServiceBusQueueConfig WithLockDuration(TimeSpan lockDuration) { _lock = lockDuration; return this; }
        public IServiceBusQueueConfig WithMaxDeliveryCount(int count) { _maxDelivery = count; return this; }

        public CreateQueueOptions BuildOptions(string name)
        {
            var o = new CreateQueueOptions(name);
            o.RequiresSession = _session;
            if (_ttl.HasValue) o.DefaultMessageTimeToLive = _ttl.Value;
            if (_lock.HasValue) o.LockDuration = _lock.Value;
            if (_maxDelivery.HasValue) o.MaxDeliveryCount = _maxDelivery.Value;
            return o;
        }
    }
}
