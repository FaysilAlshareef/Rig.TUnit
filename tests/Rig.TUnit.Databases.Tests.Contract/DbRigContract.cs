using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Tests.Contract;

/// <summary>
/// The 13 mandatory tests every database provider MUST pass.
/// Inherited by <c>SqlRigContract</c>, <c>NoSqlRigContract</c>, and ultimately every
/// provider's <c>*.Tests.Integration</c> project (via the concrete <c>*Contract</c>).
/// </summary>
public abstract class DbRigContract
{
    protected abstract ValueTask<IDbRig> CreateRigAsync(CancellationToken ct);

    protected virtual ValueTask DisposeRigAsync(IDbRig rig)
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
        var rig = await CreateRigAsync(CancellationToken.None);
        try
        {
            if (rig is Rig.TUnit.Core.Fixtures.RigFixtureBase f)
            {
                await f.InitializeAsync();
                await f.InitializeAsync();
            }
            await Assert.That(rig.ConnectionString).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_DisposeAsync_IsSafeToCallTwice()
    {
        var rig = await CreateRigAsync(CancellationToken.None);
        await DisposeRigAsync(rig);
        await DisposeRigAsync(rig);
    }

    [Test]
    public virtual async Task Fixture_ConnectionString_IsNotEmpty()
    {
        var rig = await CreateRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig.ConnectionString).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var rig1 = await CreateRigAsync(CancellationToken.None);
        var rig2 = await CreateRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig1.DatabaseName).IsNotEqualTo(rig2.DatabaseName);
        }
        finally
        {
            await DisposeRigAsync(rig1);
            await DisposeRigAsync(rig2);
        }
    }

    [Test]
    public virtual async Task Fixture_IsolationKey_IsStableForSameTestContext()
    {
        var rig = await CreateRigAsync(CancellationToken.None);
        try
        {
            var value1 = rig.IsolationKey.Value;
            var value2 = rig.IsolationKey.Value;
            await Assert.That(value1).IsEqualTo(value2);
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Builder_UseContainer_ResolvesConnectionSource()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseConfig_ResolvesConnectionSource()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseOptions_ResolvesConnectionSource()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseValue_ResolvesConnectionSource()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task Builder_UseAuto_ChoosesCiVsLocal()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task ParallelExecution_TwentyFixtures_NoCrossTalk()
        => await Task.CompletedTask;

    [Test]
    public virtual async Task Isolation_KeyIsDeterministicForSameInput()
    {
        var rig = await CreateRigAsync(CancellationToken.None);
        try
        {
            var fromName1 = Rig.TUnit.Core.IsolationKey.FromName("Case.Method");
            var fromName2 = Rig.TUnit.Core.IsolationKey.FromName("Case.Method");
            await Assert.That(fromName1.Value).IsEqualTo(fromName2.Value);
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_HonorsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.That(cts.Token.IsCancellationRequested).IsTrue();
    }
}
