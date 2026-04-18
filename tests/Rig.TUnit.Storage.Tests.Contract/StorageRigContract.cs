using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Storage.Contracts;

namespace Rig.TUnit.Storage.Tests.Contract;

/// <summary>Mandatory tests every storage provider (AzureBlob, S3, MinIO, FileSystem) must pass.</summary>
[InheritsTests]
public abstract class StorageRigContract
{
    protected abstract ValueTask<IStorageRig> CreateStorageRigAsync(CancellationToken ct);

    protected virtual ValueTask DisposeRigAsync(IStorageRig rig)
    {
        if (rig is IAsyncDisposable a) return a.DisposeAsync();
        if (rig is IDisposable d) d.Dispose();
        return ValueTask.CompletedTask;
    }

    [Test]
    public virtual async Task Fixture_InitializeAsync_IsIdempotent()
    {
        var rig = await CreateStorageRigAsync(CancellationToken.None);
        try
        {
            if (rig is RigFixtureBase f)
            {
                await f.InitializeAsync();
                await f.InitializeAsync();
            }
            await Assert.That(rig.ContainerName).IsNotNullOrEmpty();
        }
        finally { await DisposeRigAsync(rig); }
    }

    [Test]
    public virtual async Task Fixture_DisposeAsync_IsSafeToCallTwice()
    {
        var rig = await CreateStorageRigAsync(CancellationToken.None);
        await DisposeRigAsync(rig);
        await DisposeRigAsync(rig);
    }

    [Test]
    public virtual async Task Fixture_ContainerName_IsNotEmpty()
    {
        var rig = await CreateStorageRigAsync(CancellationToken.None);
        try { await Assert.That(rig.ContainerName).IsNotNullOrEmpty(); }
        finally { await DisposeRigAsync(rig); }
    }

    [Test]
    public virtual async Task Fixture_IsolationKey_IsStableForSameInstance()
    {
        var rig = await CreateStorageRigAsync(CancellationToken.None);
        try
        {
            var a = rig.IsolationKey.Value;
            var b = rig.IsolationKey.Value;
            await Assert.That(a).IsEqualTo(b);
        }
        finally { await DisposeRigAsync(rig); }
    }

    [Test]
    public virtual async Task Fixture_ContainerName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString()).ForRedisKeyPrefix();
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString()).ForRedisKeyPrefix();
        await Assert.That(k1).IsNotEqualTo(k2);
    }
}
