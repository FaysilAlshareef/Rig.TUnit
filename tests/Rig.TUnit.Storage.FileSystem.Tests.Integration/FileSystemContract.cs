using Rig.TUnit.Storage.Contracts;
using Rig.TUnit.Storage.FileSystem.Fixtures;
using Rig.TUnit.Storage.Tests.Contract;

namespace Rig.TUnit.Storage.FileSystem.Tests.Integration;

[InheritsTests]
public sealed class FileSystemContract : StorageRigContract
{
    protected override async ValueTask<IStorageRig> CreateStorageRigAsync(CancellationToken ct)
    {
        var fx = new FileSystemFixture();
        await fx.InitializeAsync();
        return fx;
    }
}
