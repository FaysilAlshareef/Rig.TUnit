using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;

namespace Rig.TUnit.Storage.S3.Builder;

public sealed class S3RigBuilder : StorageRigBuilder<S3RigBuilder>
{
    public S3RigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
