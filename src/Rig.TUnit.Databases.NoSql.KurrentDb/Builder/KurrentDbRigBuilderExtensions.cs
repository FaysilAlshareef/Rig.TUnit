using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Builder;

public static class KurrentDbRigBuilderExtensions
{
    public static RigBuilder UseKurrentDb(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<KurrentDbRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new KurrentDbRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
