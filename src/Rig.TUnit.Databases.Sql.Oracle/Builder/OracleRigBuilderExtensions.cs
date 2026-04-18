using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.Oracle.Builder;

public static class OracleRigBuilderExtensions
{
    public static RigBuilder UseOracle(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<OracleRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OracleRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
