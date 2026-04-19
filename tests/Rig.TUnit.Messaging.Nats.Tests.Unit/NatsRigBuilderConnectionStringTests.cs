using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Nats.Builder;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class NatsRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://broker-a:4222,nats://broker-b:4222");

        NatsRigBuilder? built = null;
        captured!.UseNats(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("nats://broker-a:4222,nats://broker-b:4222");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://localhost:4222");

        var built = new NatsRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("nats://localhost:4222");
    }
}
