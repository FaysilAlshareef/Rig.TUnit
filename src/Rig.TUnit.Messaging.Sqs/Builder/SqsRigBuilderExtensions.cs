using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Sqs.Fixtures;

namespace Rig.TUnit.Messaging.Sqs.Builder;

public static class SqsRigBuilderExtensions
{
    public static RigBuilder UseSqs(
        this RigBuilder rig,
        SqsFixture fixture,
        Action<SqsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SqsRigBuilder(rig, fixture, fixture.Client);
        configure(builder);
        return rig;
    }

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
