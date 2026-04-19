using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Kafka.Builder;

public static class KafkaRigBuilderExtensions
{
    public static RigBuilder UseKafka(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<KafkaRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new KafkaRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
