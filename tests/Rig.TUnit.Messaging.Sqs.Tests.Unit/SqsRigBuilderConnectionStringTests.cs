using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Sqs.Builder;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

public sealed class SqsRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://localhost:4566");

        SqsRigBuilder? built = null;
        captured!.UseSqs(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("http://localhost:4566");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://sqs.example.com");

        var built = new SqsRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("http://sqs.example.com");
    }
}
