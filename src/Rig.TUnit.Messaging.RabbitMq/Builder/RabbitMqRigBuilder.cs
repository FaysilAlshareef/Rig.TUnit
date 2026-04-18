using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.RabbitMq.Builder;

public sealed class RabbitMqRigBuilder : MessagingRigBuilder<RabbitMqRigBuilder>
{
    public RabbitMqRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
