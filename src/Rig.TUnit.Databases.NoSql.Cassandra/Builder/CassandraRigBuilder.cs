using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Builder;

public sealed class CassandraRigBuilder : NoSqlRigBuilder<CassandraRigBuilder>
{
    public CassandraRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
