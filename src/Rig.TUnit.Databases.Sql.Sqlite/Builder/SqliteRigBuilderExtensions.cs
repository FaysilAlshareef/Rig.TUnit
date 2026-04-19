using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.Sqlite.Builder;

public static class SqliteRigBuilderExtensions
{
    public static RigBuilder UseSqlite(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<SqliteRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SqliteRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
