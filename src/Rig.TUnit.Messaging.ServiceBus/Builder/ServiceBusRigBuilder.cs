using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Builder;

public sealed class ServiceBusRigBuilder : MessagingRigBuilder<ServiceBusRigBuilder>
{
    public ServiceBusRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
