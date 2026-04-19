using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.Nats.Builder;

public sealed class NatsRigBuilder : MessagingRigBuilder<NatsRigBuilder>
{
    public NatsRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
