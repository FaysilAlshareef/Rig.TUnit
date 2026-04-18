using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.MySql.Builder;

public static class MySqlRigBuilderExtensions
{
    public static RigBuilder UseMySql(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<MySqlRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MySqlRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
