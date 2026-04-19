using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Mongo.Builder;

public static class MongoRigBuilderExtensions
{
    public static RigBuilder UseMongo(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<MongoRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MongoRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
