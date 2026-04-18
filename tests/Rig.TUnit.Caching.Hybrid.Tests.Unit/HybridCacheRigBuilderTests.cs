using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Caching.Hybrid.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Hybrid.Tests.Unit;

/// <summary>
/// FR-001/FR-035 unit tests for <see cref="HybridCacheRigBuilder"/> — CRTP shape,
/// sealed, null-guards.
/// </summary>
public sealed class HybridCacheRigBuilderTests
{
    [Test]
    public async Task HybridCacheRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(HybridCacheRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task HybridCacheRigBuilder_TypeMetadata_InheritsCacheRigBuilderCrtp()
    {
        var baseType = typeof(HybridCacheRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(CacheRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(HybridCacheRigBuilder));
    }

    [Test]
    public async Task HybridCacheRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("hybrid-in-memory");

        await Assert.That(() => new HybridCacheRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HybridCacheRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new HybridCacheRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
