using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Caching.Fusion.Fixtures;
using Rig.TUnit.Caching.Tests.Contract;

namespace Rig.TUnit.Caching.Fusion.Tests.Integration;

[InheritsTests]
public sealed class FusionContract : CacheRigContract
{
    protected override async ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct)
    {
        var fx = new FusionCacheFixture();
        await fx.InitializeAsync();
        return fx;
    }
}
