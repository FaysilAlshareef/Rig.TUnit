using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Fusion.Builder;

public sealed class FusionCacheRigBuilder : CacheRigBuilder<FusionCacheRigBuilder>
{
    public FusionCacheRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
