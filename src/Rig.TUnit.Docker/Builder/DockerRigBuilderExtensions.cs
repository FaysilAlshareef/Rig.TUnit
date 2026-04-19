using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Docker.Builder;

public static class DockerRigBuilderExtensions
{
    public static RigBuilder UseDocker(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<DockerRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new DockerRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
