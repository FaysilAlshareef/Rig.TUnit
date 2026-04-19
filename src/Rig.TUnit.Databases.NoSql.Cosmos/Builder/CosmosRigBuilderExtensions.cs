using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Builder;

public static class CosmosRigBuilderExtensions
{
    public static RigBuilder UseCosmos(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<CosmosRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CosmosRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
