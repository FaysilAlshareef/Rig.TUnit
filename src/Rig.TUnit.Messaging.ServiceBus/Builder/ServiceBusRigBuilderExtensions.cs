using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Builder;

public static class ServiceBusRigBuilderExtensions
{
    public static RigBuilder UseServiceBus(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<ServiceBusRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ServiceBusRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
