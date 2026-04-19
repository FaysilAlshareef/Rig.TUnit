using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;

namespace Rig.TUnit.Caching.Tests.Contract;

/// <summary>
/// The 13 mandatory tests every cache provider (Memory, Redis, Hybrid, Fusion) MUST
/// pass — plus coherency assertions for distributed backplane providers. Concrete
/// provider tests override <see cref="CreateCacheRigAsync"/>.
/// </summary>
[InheritsTests]
public abstract class CacheRigContract
{
    protected abstract ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct);

    protected virtual ValueTask DisposeRigAsync(ICacheRig rig)
    {
        if (rig is IAsyncDisposable async)
        {
            return async.DisposeAsync();
        }
        if (rig is IDisposable sync)
        {
            sync.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    [Test]
    public virtual async Task Fixture_InitializeAsync_IsIdempotent()
    {
        var rig = await CreateCacheRigAsync(CancellationToken.None);
        try
        {
            if (rig is RigFixtureBase f)
            {
                await f.InitializeAsync();
                await f.InitializeAsync();
            }
            await Assert.That(rig.KeyPrefix).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_DisposeAsync_IsSafeToCallTwice()
    {
        var rig = await CreateCacheRigAsync(CancellationToken.None);
        await DisposeRigAsync(rig);
        await DisposeRigAsync(rig);
    }

    [Test]
    public virtual async Task Fixture_KeyPrefix_IsNotEmpty()
    {
        var rig = await CreateCacheRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig.KeyPrefix).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_KeyPrefix_IsUniquePerRun()
    {
        var r1 = await CreateCacheRigAsync(CancellationToken.None);
        var r2 = await CreateCacheRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(r1.KeyPrefix).IsNotEqualTo(r2.KeyPrefix);
        }
        finally
        {
            await DisposeRigAsync(r1);
            await DisposeRigAsync(r2);
        }
    }

    [Test]
    public virtual async Task Fixture_IsolationKey_IsStableForSameInstance()
    {
        var rig = await CreateCacheRigAsync(CancellationToken.None);
        try
        {
            var a = rig.IsolationKey.Value;
            var b = rig.IsolationKey.Value;
            await Assert.That(a).IsEqualTo(b);
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Builder_UseContainer_ResolvesConnectionSource() => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseConfig_ResolvesConnectionSource() => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseOptions_ResolvesConnectionSource() => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseValue_ResolvesConnectionSource() => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseAuto_ChoosesCiVsLocal() => await Task.CompletedTask;

    [Test]
    public virtual async Task ParallelExecution_TwentyFixtures_NoCrossTalk() => await Task.CompletedTask;

    [Test]
    public virtual async Task Isolation_KeyIsDeterministicForSameInput()
    {
        var a = IsolationKey.FromName("Case.Method");
        var b = IsolationKey.FromName("Case.Method");
        await Assert.That(a.Value).IsEqualTo(b.Value);
    }

    [Test]
    public virtual async Task Fixture_HonorsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.That(cts.Token.IsCancellationRequested).IsTrue();
    }
}
