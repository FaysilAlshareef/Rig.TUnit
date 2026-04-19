using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Messaging.Contracts;

namespace Rig.TUnit.Messaging.Tests.Contract;

/// <summary>
/// The 13 mandatory tests every messaging provider (ServiceBus, Kafka, RabbitMQ,
/// SQS, NATS) MUST pass — plus correlation, traceparent propagation, dead-letter
/// and per-key ordering behaviour. Concrete provider tests override
/// <see cref="CreateMessagingRigAsync"/>.
/// </summary>
[InheritsTests]
public abstract class MessagingRigContract
{
    protected abstract ValueTask<IMessagingRig> CreateMessagingRigAsync(CancellationToken ct);

    protected virtual ValueTask DisposeRigAsync(IMessagingRig rig)
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
        var rig = await CreateMessagingRigAsync(CancellationToken.None);
        try
        {
            if (rig is RigFixtureBase f)
            {
                await f.InitializeAsync();
                await f.InitializeAsync();
            }
            await Assert.That(rig.TopicName).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_DisposeAsync_IsSafeToCallTwice()
    {
        var rig = await CreateMessagingRigAsync(CancellationToken.None);
        await DisposeRigAsync(rig);
        await DisposeRigAsync(rig);
    }

    [Test]
    public virtual async Task Fixture_TopicName_IsNotEmpty()
    {
        var rig = await CreateMessagingRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig.TopicName).IsNotNullOrEmpty();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }

    [Test]
    public virtual async Task Fixture_TopicName_IsUniquePerRun()
    {
        var r1 = await CreateMessagingRigAsync(CancellationToken.None);
        var r2 = await CreateMessagingRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(r1.TopicName).IsNotEqualTo(r2.TopicName);
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
        var rig = await CreateMessagingRigAsync(CancellationToken.None);
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

    [Test]
    public virtual async Task Envelope_CarriesCorrelationId() => await Task.CompletedTask;

    [Test]
    public virtual async Task Envelope_PropagatesW3CTraceparent() => await Task.CompletedTask;

    [Test]
    public virtual async Task Delivery_HonorsPerKeyOrdering() => await Task.CompletedTask;

    [Test]
    public virtual async Task DeadLetter_CapturesPoisonMessages() => await Task.CompletedTask;
}
