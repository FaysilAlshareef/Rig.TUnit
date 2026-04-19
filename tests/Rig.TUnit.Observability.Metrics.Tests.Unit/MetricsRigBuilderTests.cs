using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.Builder;
using Rig.TUnit.Observability.Metrics.Builder;

namespace Rig.TUnit.Observability.Metrics.Tests.Unit;

public sealed class MetricsRigBuilderTests
{
    [Test]
    public async Task MetricsRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(MetricsRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task MetricsRigBuilder_TypeMetadata_InheritsTelemetryRigBuilderCrtp()
    {
        var baseType = typeof(MetricsRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(TelemetryRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(MetricsRigBuilder));
    }

    [Test]
    public async Task MetricsRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("meter-1");
        await Assert.That(() => new MetricsRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MetricsRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new MetricsRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MetricsRigBuilder_MeterName_PassesThroughFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("orders.service");
        var built = new MetricsRigBuilder(captured!, source);
        await Assert.That(built.MeterName).IsEqualTo("orders.service");
    }
}
