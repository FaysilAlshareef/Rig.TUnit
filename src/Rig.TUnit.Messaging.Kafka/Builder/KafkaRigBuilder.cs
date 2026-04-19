using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.Kafka.Builder;

public sealed class KafkaRigBuilder : MessagingRigBuilder<KafkaRigBuilder>
{
    public KafkaRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
