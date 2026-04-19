using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.SqlServer.Builder;

/// <summary>Fluent-chain entry-point: <c>rig.UseSqlServer(source, sql =&gt; sql.ReplaceDbContext&lt;MyDb&gt;())</c>.</summary>
public static class SqlServerRigBuilderExtensions
{
    public static RigBuilder UseSqlServer(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<SqlServerRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SqlServerRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
