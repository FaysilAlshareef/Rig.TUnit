using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Caching.Memory.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Memory.Tests.Unit;

public sealed class MemoryCacheBuilderTests
{
    [Test]
    public async Task MemoryCacheRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(MemoryCacheRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task MemoryCacheRigBuilder_TypeMetadata_InheritsCacheRigBuilderCrtp()
    {
        var baseType = typeof(MemoryCacheRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType.GetGenericTypeDefinition()).IsEqualTo(typeof(CacheRigBuilder<>));
        await Assert.That(baseType.GenericTypeArguments[0]).IsEqualTo(typeof(MemoryCacheRigBuilder));
    }

    [Test]
    public async Task MemoryCacheRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = new InMemoryConnectionSource();

        await Assert.That(() => new MemoryCacheRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task MemoryCacheRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new MemoryCacheRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task InMemoryConnectionSource_ConnectionString_ReturnsInMemoryLiteral()
    {
        var source = new InMemoryConnectionSource();

        await Assert.That(source.ConnectionString).IsEqualTo("in-memory");
    }

    [Test]
    public async Task UseMemoryCache_NullRig_ThrowsArgumentNullException()
    {
        await Assert.That(() => ((RigBuilder)null!).UseMemoryCache())
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseMemoryCache_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        var returned = captured!.UseMemoryCache();

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseMemoryCache_WithConfigure_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var calls = 0;

        captured!.UseMemoryCache(_ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseMemoryCache_WithConfigure_PassesMemoryCacheRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        MemoryCacheRigBuilder? passed = null;

        captured!.UseMemoryCache(b => passed = b);

        await Assert.That(passed).IsNotNull();
    }

    [Test]
    public async Task UseMemoryCache_NullConfigure_DoesNotThrow()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        var returned = captured!.UseMemoryCache(null);

        await Assert.That(returned).IsSameReferenceAs(captured);
    }
}
