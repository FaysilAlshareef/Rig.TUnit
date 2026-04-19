using Rig.TUnit.Storage.Contracts;
using Rig.TUnit.Storage.Tests.Contract;

namespace Rig.TUnit.Storage.S3.Tests.Integration;

[InheritsTests]
public sealed class S3Contract : StorageRigContract
{
    protected override async ValueTask<IStorageRig> CreateStorageRigAsync(CancellationToken ct)
        => await SharedS3Fixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IStorageRig rig) => ValueTask.CompletedTask;
}
