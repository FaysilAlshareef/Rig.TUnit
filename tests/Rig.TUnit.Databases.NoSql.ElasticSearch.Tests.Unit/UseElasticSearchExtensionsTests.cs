using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Builder;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class UseElasticSearchExtensionsTests
{
    private const string Sample = "http://localhost:9200";

    [Test]
    public async Task UseElasticSearch_NullRig_Throws()
    {
        var source = RigConnect.FromValue(Sample);
        await Assert.That(() => ((RigBuilder)null!).UseElasticSearch(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseElasticSearch_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseElasticSearch(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseElasticSearch_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        await Assert.That(() => captured!.UseElasticSearch(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseElasticSearch_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        var returned = captured!.UseElasticSearch(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseElasticSearch_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        var calls = 0;
        captured!.UseElasticSearch(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseElasticSearch_ValidArgs_PassesBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(Sample);
        ElasticSearchRigBuilder? passed = null;
        captured!.UseElasticSearch(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
