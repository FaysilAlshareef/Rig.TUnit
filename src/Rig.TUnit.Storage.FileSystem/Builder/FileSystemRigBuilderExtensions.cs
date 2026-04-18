using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.FileSystem.Builder;

public static class FileSystemRigBuilderExtensions
{
    public static RigBuilder UseFileSystem(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<FileSystemRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FileSystemRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
