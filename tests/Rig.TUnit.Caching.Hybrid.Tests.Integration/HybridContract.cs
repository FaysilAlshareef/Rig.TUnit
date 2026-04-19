using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Caching.Hybrid.Fixtures;
using Rig.TUnit.Caching.Tests.Contract;

namespace Rig.TUnit.Caching.Hybrid.Tests.Integration;

[InheritsTests]
public sealed class HybridContract : CacheRigContract
{
    protected override async ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct)
    {
        var fx = new HybridCacheFixture();
        await fx.InitializeAsync();
        return fx;
    }
}
