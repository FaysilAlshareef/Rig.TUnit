using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Caching.Fusion.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

public sealed class FusionCacheRigBuilderTests
{
    [Test]
    public async Task FusionCacheRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(FusionCacheRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task FusionCacheRigBuilder_TypeMetadata_InheritsCacheRigBuilderCrtp()
    {
        var baseType = typeof(FusionCacheRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(CacheRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(FusionCacheRigBuilder));
    }

    [Test]
    public async Task FusionCacheRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("fusion-in-memory");

        await Assert.That(() => new FusionCacheRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task FusionCacheRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new FusionCacheRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
