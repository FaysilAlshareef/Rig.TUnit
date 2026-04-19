using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Fusion.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

public sealed class FusionCacheRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("fusion-l1-l2-redis");

        FusionCacheRigBuilder? built = null;
        captured!.UseFusionCache(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("fusion-l1-l2-redis");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("fusion-in-memory");

        var built = new FusionCacheRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("fusion-in-memory");
    }
}
