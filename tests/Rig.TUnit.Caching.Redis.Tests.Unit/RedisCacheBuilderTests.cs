using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Caching.Redis.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Redis.Tests.Unit;

public sealed class RedisCacheBuilderTests
{
    private const string SampleConn = "localhost:6379";

    [Test]
    public async Task RedisCacheRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(RedisCacheRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task RedisCacheRigBuilder_TypeMetadata_InheritsCacheRigBuilderCrtp()
    {
        var baseType = typeof(RedisCacheRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType.GetGenericTypeDefinition()).IsEqualTo(typeof(CacheRigBuilder<>));
        await Assert.That(baseType.GenericTypeArguments[0]).IsEqualTo(typeof(RedisCacheRigBuilder));
    }

    [Test]
    public async Task RedisCacheRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => new RedisCacheRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RedisCacheRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new RedisCacheRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RedisCacheRigBuilder_ConnectionString_ReflectsSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var builder = new RedisCacheRigBuilder(captured!, source);

        await Assert.That(builder.ConnectionString).IsEqualTo(SampleConn);
    }

    [Test]
    public async Task UseRedisCache_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => ((RigBuilder)null!).UseRedisCache(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisCache_NullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseRedisCache(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisCache_NullConfigure_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => captured!.UseRedisCache(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisCache_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var returned = captured!.UseRedisCache(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseRedisCache_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;

        captured!.UseRedisCache(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseRedisCache_ValidArgs_PassesRedisCacheRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        RedisCacheRigBuilder? passed = null;

        captured!.UseRedisCache(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
