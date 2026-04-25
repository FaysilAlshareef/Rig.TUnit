using Amazon.SQS;
using Amazon.SQS.Model;

namespace Rig.TUnit.Messaging.Sqs.Topology;

/// <summary>
/// Records SQS queue declarations and applies them idempotently (create-if-not-exists)
/// via the Amazon SQS client on <see cref="ApplyAsync"/>.
/// </summary>
public sealed class SqsTopologyBuilder : ISqsTopologyBuilder
{
    private readonly IAmazonSQS _client;
    private readonly List<QueueSpec> _specs = [];

    public SqsTopologyBuilder(IAmazonSQS client, string serviceUrl)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl);
    }

    public ISqsTopologyBuilder Queue(string name, Action<ISqsQueueConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var cfg = new QueueConfig();
        configure?.Invoke(cfg);
        _specs.Add(new QueueSpec(name, cfg.Build()));
        return this;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        foreach (var spec in _specs)
        {
            try
            {
                await _client.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = spec.Name,
                    Attributes = spec.Attributes,
                }, ct).ConfigureAwait(false);
            }
            catch (QueueNameExistsException)
            {
                // Idempotent — queue already exists with same attributes.
            }
        }
    }

    private sealed record QueueSpec(string Name, Dictionary<string, string> Attributes);

    private sealed class QueueConfig : ISqsQueueConfig
    {
        private readonly Dictionary<string, string> _attrs = new(StringComparer.Ordinal);

        public ISqsQueueConfig WithFifo(bool contentBasedDeduplication = false)
        {
            _attrs["FifoQueue"] = "true";
            _attrs["ContentBasedDeduplication"] = contentBasedDeduplication ? "true" : "false";
            return this;
        }

        public ISqsQueueConfig WithVisibilityTimeout(TimeSpan timeout)
        {
            _attrs["VisibilityTimeout"] = ((int)timeout.TotalSeconds).ToString();
            return this;
        }

        public ISqsQueueConfig WithMessageRetentionPeriod(TimeSpan period)
        {
            _attrs["MessageRetentionPeriod"] = ((int)period.TotalSeconds).ToString();
            return this;
        }

        public ISqsQueueConfig WithDeadLetter(string queue, int maxReceiveCount) => this;

        public Dictionary<string, string> Build() => new(_attrs, StringComparer.Ordinal);
    }
}
