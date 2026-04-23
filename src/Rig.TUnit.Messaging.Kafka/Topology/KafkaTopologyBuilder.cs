using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Rig.TUnit.Messaging.Kafka.Topology;

/// <summary>
/// Records Kafka topic declarations and applies them idempotently (create-if-not-exists)
/// via the Confluent AdminClient on <see cref="ApplyAsync"/>.
/// </summary>
public sealed class KafkaTopologyBuilder : IKafkaTopologyBuilder
{
    private readonly string _bootstrap;
    private readonly List<TopicSpec> _specs = [];

    public KafkaTopologyBuilder(string bootstrapServers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        _bootstrap = bootstrapServers;
    }

    public IKafkaTopologyBuilder Topic(string name, Action<IKafkaTopicConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var cfg = new TopicConfig();
        configure?.Invoke(cfg);
        _specs.Add(new TopicSpec(name, cfg));
        return this;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        if (_specs.Count == 0)
            return;

        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrap }).Build();

        foreach (var spec in _specs)
        {
            try
            {
                var topicSpec = new TopicSpecification
                {
                    Name = spec.Name,
                    NumPartitions = spec.Config.Partitions,
                    ReplicationFactor = spec.Config.ReplicationFactor,
                };
                if (spec.Config.Configs.Count > 0)
                    topicSpec.Configs = new Dictionary<string, string>(spec.Config.Configs);

                await admin.CreateTopicsAsync([topicSpec]).ConfigureAwait(false);
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                // Idempotent — topic already present, no action needed.
            }
        }
    }

    private sealed record TopicSpec(string Name, TopicConfig Config);

    private sealed class TopicConfig : IKafkaTopicConfig
    {
        public int Partitions { get; private set; } = 1;
        public short ReplicationFactor { get; private set; } = 1;
        public Dictionary<string, string> Configs { get; } = new(StringComparer.Ordinal);

        public IKafkaTopicConfig WithPartitions(int count) { Partitions = count; return this; }
        public IKafkaTopicConfig WithReplicationFactor(short factor) { ReplicationFactor = factor; return this; }
        public IKafkaTopicConfig WithConfig(string key, string value) { Configs[key] = value; return this; }
    }
}
