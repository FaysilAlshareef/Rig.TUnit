using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Redis.Builder;

/// <summary>
/// Cache-role Redis builder. A bare <c>UseRedis</c> is intentionally NOT exposed because
/// Redis plays two roles (cache vs key-value). Use <see cref="RedisCacheRigBuilderExtensions.UseRedisCache"/>
/// to pick the cache role, and <c>UseRedisKv</c> (in <c>Rig.TUnit.Databases.NoSql.Redis</c>)
/// for the key-value role.
/// </summary>
public sealed class RedisCacheRigBuilder : CacheRigBuilder<RedisCacheRigBuilder>
{
    public RedisCacheRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
