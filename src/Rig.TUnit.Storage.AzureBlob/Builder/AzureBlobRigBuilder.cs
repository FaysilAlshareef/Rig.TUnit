using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;

namespace Rig.TUnit.Storage.AzureBlob.Builder;

public sealed class AzureBlobRigBuilder : StorageRigBuilder<AzureBlobRigBuilder>
{
    public AzureBlobRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
