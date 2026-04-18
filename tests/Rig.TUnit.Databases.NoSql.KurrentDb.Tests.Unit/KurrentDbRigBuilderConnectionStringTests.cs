using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.KurrentDb.Builder;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

public sealed class KurrentDbRigBuilderConnectionStringTests
{
    private const string Sample = "esdb://unit-test:2113?tls=false";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var builder = new KurrentDbRigBuilder(captured!, RigConnect.FromValue(Sample));
        await Assert.That(builder.ConnectionString).IsEqualTo(Sample);
    }

    [Test]
    public async Task ConnectionString_DifferentSources_ReturnDistinctValues()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var a = new KurrentDbRigBuilder(captured!, RigConnect.FromValue("esdb://a:2113?tls=false"));
        var b = new KurrentDbRigBuilder(captured!, RigConnect.FromValue("esdb://b:2113?tls=false"));
        await Assert.That(a.ConnectionString).IsNotEqualTo(b.ConnectionString);
    }
}
