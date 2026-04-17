using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.Redis.Builder;

/// <summary>Key-value-role Redis builder. Pairs with <c>RedisCacheRigBuilder</c> (cache role).</summary>
public sealed class RedisKvRigBuilder : NoSqlRigBuilder<RedisKvRigBuilder>
{
    public RedisKvRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
