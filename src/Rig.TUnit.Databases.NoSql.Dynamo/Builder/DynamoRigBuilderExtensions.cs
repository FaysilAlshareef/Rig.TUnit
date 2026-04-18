using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Builder;

public static class DynamoRigBuilderExtensions
{
    public static RigBuilder UseDynamo(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<DynamoRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new DynamoRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
