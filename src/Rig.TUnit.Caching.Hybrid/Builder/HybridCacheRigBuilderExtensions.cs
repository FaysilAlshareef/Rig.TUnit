using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Hybrid.Builder;

public static class HybridCacheRigBuilderExtensions
{
    public static RigBuilder UseHybridCache(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<HybridCacheRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new HybridCacheRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
