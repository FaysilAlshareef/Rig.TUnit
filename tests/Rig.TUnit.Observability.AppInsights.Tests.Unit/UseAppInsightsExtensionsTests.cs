using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.AppInsights.Builder;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class UseAppInsightsExtensionsTests
{
    private const string SampleKey = "00000000-0000-0000-0000-000000000000";

    [Test]
    public async Task UseAppInsights_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleKey);
        await Assert.That(() => ((RigBuilder)null!).UseAppInsights(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAppInsights_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseAppInsights(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAppInsights_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleKey);
        await Assert.That(() => captured!.UseAppInsights(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAppInsights_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleKey);
        var returned = captured!.UseAppInsights(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseAppInsights_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleKey);
        var calls = 0;
        captured!.UseAppInsights(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }
}
