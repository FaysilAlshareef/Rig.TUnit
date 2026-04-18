using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;

namespace Rig.TUnit.Storage.MinIO.Builder;

public sealed class MinIORigBuilder : StorageRigBuilder<MinIORigBuilder>
{
    public MinIORigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
