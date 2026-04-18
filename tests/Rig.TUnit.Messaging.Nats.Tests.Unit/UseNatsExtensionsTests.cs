using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Nats.Builder;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class UseNatsExtensionsTests
{
    private const string SampleConnectionString = "nats://localhost:4222";

    [Test]
    public async Task UseNats_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => ((RigBuilder)null!).UseNats(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseNats_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseNats(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseNats_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => captured!.UseNats(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseNats_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        var returned = captured!.UseNats(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseNats_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;

        captured!.UseNats(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseNats_ValidArgs_PassesNatsRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        NatsRigBuilder? passed = null;

        captured!.UseNats(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
