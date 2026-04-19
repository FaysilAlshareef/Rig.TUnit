using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.RabbitMq.Builder;

public static class RabbitMqRigBuilderExtensions
{
    public static RigBuilder UseRabbitMq(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<RabbitMqRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RabbitMqRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
