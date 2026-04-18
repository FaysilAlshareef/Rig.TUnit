using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.S3.Builder;

public static class S3RigBuilderExtensions
{
    public static RigBuilder UseS3(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<S3RigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new S3RigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
