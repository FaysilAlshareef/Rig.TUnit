using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.Postgresql.Builder;

/// <summary>Fluent-chain entry-point: <c>rig.UsePostgres(source, sql =&gt; sql.ReplaceDbContext&lt;MyDb&gt;())</c>.</summary>
public static class PostgresRigBuilderExtensions
{
    public static RigBuilder UsePostgres(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<PostgresRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PostgresRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
