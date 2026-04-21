using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.AppInsights.Builder;
using Rig.TUnit.Observability.Builder;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsRigBuilderTests
{
    [Test]
    public async Task AppInsightsRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(AppInsightsRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task AppInsightsRigBuilder_TypeMetadata_InheritsTelemetryRigBuilderCrtp()
    {
        var baseType = typeof(AppInsightsRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(TelemetryRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(AppInsightsRigBuilder));
    }

    [Test]
    public async Task AppInsightsRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("00000000-0000-0000-0000-000000000000");
        await Assert.That(() => new AppInsightsRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AppInsightsRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new AppInsightsRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AppInsightsRigBuilder_InstrumentationKey_ReturnsValueFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("00000000-0000-0000-0000-000000000001");
        var builder = new AppInsightsRigBuilder(captured!, source);

        await Assert.That(builder.InstrumentationKey).IsEqualTo("00000000-0000-0000-0000-000000000001");
    }
}
