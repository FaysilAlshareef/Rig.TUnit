using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Kafka.Builder;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// Coverage-lifting test: drives the <see cref="KafkaRigBuilder.ConnectionString"/>
/// getter so the pass-through line registers as covered.
/// </summary>
public sealed class KafkaRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("broker-a:9092,broker-b:9092");

        KafkaRigBuilder? built = null;
        captured!.UseKafka(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("broker-a:9092,broker-b:9092");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("localhost:9092");

        var built = new KafkaRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("localhost:9092");
    }
}
