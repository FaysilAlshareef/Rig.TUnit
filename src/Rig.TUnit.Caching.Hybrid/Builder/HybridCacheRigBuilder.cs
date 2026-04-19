using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Hybrid.Builder;

public sealed class HybridCacheRigBuilder : CacheRigBuilder<HybridCacheRigBuilder>
{
    public HybridCacheRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
