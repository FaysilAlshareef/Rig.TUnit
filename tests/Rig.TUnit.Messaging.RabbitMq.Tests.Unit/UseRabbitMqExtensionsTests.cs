using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.RabbitMq.Builder;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class UseRabbitMqExtensionsTests
{
    private const string SampleConnectionString = "amqp://guest:guest@localhost:5672";

    [Test]
    public async Task UseRabbitMq_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => ((RigBuilder)null!).UseRabbitMq(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRabbitMq_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseRabbitMq(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRabbitMq_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => captured!.UseRabbitMq(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRabbitMq_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        var returned = captured!.UseRabbitMq(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseRabbitMq_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;

        captured!.UseRabbitMq(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseRabbitMq_ValidArgs_PassesRabbitMqRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        RabbitMqRigBuilder? passed = null;

        captured!.UseRabbitMq(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
