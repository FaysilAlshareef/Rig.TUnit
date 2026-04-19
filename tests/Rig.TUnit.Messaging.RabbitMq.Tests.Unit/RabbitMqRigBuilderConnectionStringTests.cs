using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.RabbitMq.Builder;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class RabbitMqRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("amqp://u:p@host:5672/vhost");

        RabbitMqRigBuilder? built = null;
        captured!.UseRabbitMq(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("amqp://u:p@host:5672/vhost");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("amqp://guest:guest@localhost:5672");

        var built = new RabbitMqRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("amqp://guest:guest@localhost:5672");
    }
}
