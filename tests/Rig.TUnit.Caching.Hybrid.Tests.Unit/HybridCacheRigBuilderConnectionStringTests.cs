using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Caching.Hybrid.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Hybrid.Tests.Unit;

public sealed class HybridCacheRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("hybrid-in-memory-l1-only");

        HybridCacheRigBuilder? built = null;
        captured!.UseHybridCache(source, b => built = b);

        await Assert.That(built!.ConnectionString).IsEqualTo("hybrid-in-memory-l1-only");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("hybrid-in-memory");

        var built = new HybridCacheRigBuilder(captured!, source);

        await Assert.That(built.ConnectionString).IsEqualTo("hybrid-in-memory");
    }
}
