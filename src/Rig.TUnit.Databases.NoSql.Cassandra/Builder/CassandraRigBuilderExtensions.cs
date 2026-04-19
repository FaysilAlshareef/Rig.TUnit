using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Builder;

public static class CassandraRigBuilderExtensions
{
    public static RigBuilder UseCassandra(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<CassandraRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CassandraRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
