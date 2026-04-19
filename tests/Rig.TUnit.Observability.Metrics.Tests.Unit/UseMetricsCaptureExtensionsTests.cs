using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.Metrics.Builder;

namespace Rig.TUnit.Observability.Metrics.Tests.Unit;

public sealed class UseMetricsCaptureExtensionsTests
{
    private const string SampleMeter = "sample.meter";

    [Test]
    public async Task UseMetricsCapture_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleMeter);
        await Assert.That(() => ((RigBuilder)null!).UseMetricsCapture(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMetricsCapture_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseMetricsCapture(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMetricsCapture_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleMeter);
        await Assert.That(() => captured!.UseMetricsCapture(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMetricsCapture_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleMeter);
        var returned = captured!.UseMetricsCapture(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseMetricsCapture_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleMeter);
        var calls = 0;
        captured!.UseMetricsCapture(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseMetricsCapture_ValidArgs_PassesMetricsRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleMeter);
        MetricsRigBuilder? passed = null;
        captured!.UseMetricsCapture(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
