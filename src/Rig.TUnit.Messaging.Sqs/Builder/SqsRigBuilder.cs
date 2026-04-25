using Amazon.SQS;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Sqs.Topology;

namespace Rig.TUnit.Messaging.Sqs.Builder;

public sealed class SqsRigBuilder : MessagingRigBuilder<SqsRigBuilder>
{
    private readonly IAmazonSQS? _sqsClient;
    private SqsTopologyBuilder? _topology;

    public SqsRigBuilder(RigBuilder root, IRigConnectionSource source, IAmazonSQS? sqsClient = null)
        : base(root, source)
    {
        _sqsClient = sqsClient;
    }

    public string ConnectionString => Source.ConnectionString;

    public SqsRigBuilder WithTopology(Action<ISqsTopologyBuilder> configure)
    {
        if (_topology is null)
        {
            if (_sqsClient is null)
                throw new InvalidOperationException(
                    "WithTopology requires an IAmazonSQS client. Pass a SqsFixture to UseSqs.");
            _topology = new SqsTopologyBuilder(_sqsClient, ConnectionString);
        }
        configure(_topology);
        return this;
    }

    public Task ApplyTopologyAsync(CancellationToken ct = default)
    {
        if (_topology is null)
            return Task.CompletedTask;
        return _topology.ApplyAsync(ct);
    }
}
