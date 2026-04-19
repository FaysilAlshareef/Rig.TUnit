using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Sqs.Builder;

public static class SqsRigBuilderExtensions
{
    public static RigBuilder UseSqs(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<SqsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SqsRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
