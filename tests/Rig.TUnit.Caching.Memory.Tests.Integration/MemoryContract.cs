using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Caching.Memory.Fixtures;
using Rig.TUnit.Caching.Tests.Contract;

namespace Rig.TUnit.Caching.Memory.Tests.Integration;

[InheritsTests]
public sealed class MemoryContract : CacheRigContract
{
    protected override async ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct)
    {
        var fx = new MemoryCacheFixture();
        await fx.InitializeAsync();
        return fx;
    }
}
