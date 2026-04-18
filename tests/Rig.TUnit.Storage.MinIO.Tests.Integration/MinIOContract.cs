using Rig.TUnit.Storage.Contracts;
using Rig.TUnit.Storage.Tests.Contract;

namespace Rig.TUnit.Storage.MinIO.Tests.Integration;

[InheritsTests]
public sealed class MinIOContract : StorageRigContract
{
    protected override async ValueTask<IStorageRig> CreateStorageRigAsync(CancellationToken ct)
        => await SharedMinIOFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IStorageRig rig) => ValueTask.CompletedTask;
}
