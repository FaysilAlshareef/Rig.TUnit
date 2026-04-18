using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;

namespace Rig.TUnit.Storage.FileSystem.Builder;

public sealed class FileSystemRigBuilder : StorageRigBuilder<FileSystemRigBuilder>
{
    public FileSystemRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
