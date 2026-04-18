using Rig.TUnit.Storage.Contracts;
using Rig.TUnit.Storage.Tests.Contract;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Integration;

[InheritsTests]
public sealed class AzureBlobContract : StorageRigContract
{
    protected override async ValueTask<IStorageRig> CreateStorageRigAsync(CancellationToken ct)
        => await SharedAzureBlobFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IStorageRig rig) => ValueTask.CompletedTask;
}
