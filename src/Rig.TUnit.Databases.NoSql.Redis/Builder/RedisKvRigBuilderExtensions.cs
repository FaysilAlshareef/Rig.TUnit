using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Redis.Builder;

public static class RedisKvRigBuilderExtensions
{
    public static RigBuilder UseRedisKv(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<RedisKvRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RedisKvRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
