using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.ServiceBus.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 backfill unit tests for <see cref="ServiceBusRigBuilderExtensions.UseServiceBus"/> —
/// null-guards, fluent chain, configure-invocation semantics.
/// </summary>
public sealed class UseServiceBusExtensionsTests
{
    private const string SampleConnectionString =
        "Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==";

    [Test]
    public async Task UseServiceBus_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => ((RigBuilder)null!).UseServiceBus(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseServiceBus_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseServiceBus(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseServiceBus_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => captured!.UseServiceBus(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseServiceBus_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        var returned = captured!.UseServiceBus(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseServiceBus_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;

        captured!.UseServiceBus(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseServiceBus_ValidArgs_PassesServiceBusRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        ServiceBusRigBuilder? passed = null;

        captured!.UseServiceBus(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
