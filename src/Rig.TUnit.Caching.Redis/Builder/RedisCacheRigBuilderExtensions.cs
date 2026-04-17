using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Redis.Builder;

public static class RedisCacheRigBuilderExtensions
{
    /// <summary>Registers Redis in its cache role.</summary>
    public static RigBuilder UseRedisCache(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<RedisCacheRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RedisCacheRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
