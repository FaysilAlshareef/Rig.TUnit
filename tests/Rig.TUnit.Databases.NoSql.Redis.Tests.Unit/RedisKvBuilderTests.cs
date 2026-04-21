using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;
using Rig.TUnit.Databases.NoSql.Redis.Builder;

namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Unit;

public sealed class RedisKvBuilderTests
{
    private const string SampleConn = "localhost:6379";

    [Test]
    public async Task RedisKvRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(RedisKvRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task RedisKvRigBuilder_TypeMetadata_InheritsNoSqlRigBuilderCrtp()
    {
        var baseType = typeof(RedisKvRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType.GetGenericTypeDefinition()).IsEqualTo(typeof(NoSqlRigBuilder<>));
        await Assert.That(baseType.GenericTypeArguments[0]).IsEqualTo(typeof(RedisKvRigBuilder));
    }

    [Test]
    public async Task RedisKvRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => new RedisKvRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RedisKvRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new RedisKvRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RedisKvRigBuilder_ConnectionString_ReflectsSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var builder = new RedisKvRigBuilder(captured!, source);

        await Assert.That(builder.ConnectionString).IsEqualTo(SampleConn);
    }

    [Test]
    public async Task UseRedisKv_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => ((RigBuilder)null!).UseRedisKv(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisKv_NullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseRedisKv(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisKv_NullConfigure_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        await Assert.That(() => captured!.UseRedisKv(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseRedisKv_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);

        var returned = captured!.UseRedisKv(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseRedisKv_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        var calls = 0;

        captured!.UseRedisKv(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseRedisKv_ValidArgs_PassesRedisKvRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConn);
        RedisKvRigBuilder? passed = null;

        captured!.UseRedisKv(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
