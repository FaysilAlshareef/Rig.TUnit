using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Fusion.Builder;

public static class FusionCacheRigBuilderExtensions
{
    public static RigBuilder UseFusionCache(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<FusionCacheRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FusionCacheRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
