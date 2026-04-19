using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Nats.Builder;

public static class NatsRigBuilderExtensions
{
    public static RigBuilder UseNats(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<NatsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new NatsRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
