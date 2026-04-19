using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.MinIO.Builder;

public static class MinIORigBuilderExtensions
{
    public static RigBuilder UseMinIO(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<MinIORigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MinIORigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
