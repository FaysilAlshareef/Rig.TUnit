using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Kafka.Builder;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 unit tests for <see cref="KafkaRigBuilderExtensions.UseKafka"/> —
/// null-guards, fluent chain, configure-invocation semantics.
/// </summary>
public sealed class UseKafkaExtensionsTests
{
    private const string SampleConnectionString = "localhost:9092";

    [Test]
    public async Task UseKafka_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => ((RigBuilder)null!).UseKafka(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKafka_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseKafka(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKafka_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => captured!.UseKafka(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseKafka_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        var returned = captured!.UseKafka(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseKafka_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;

        captured!.UseKafka(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseKafka_ValidArgs_PassesKafkaRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        KafkaRigBuilder? passed = null;

        captured!.UseKafka(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
