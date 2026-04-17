using Rig.TUnit.Core;

namespace Rig.TUnit.Parallelism.Tests.Contract;

/// <summary>
/// Every provider's integration test class MUST inherit this contract to prove the
/// fixture can run 20 parallel instances with zero cross-talk.
/// </summary>
[InheritsTests]
public abstract class ParallelIsolationContract
{
    protected abstract ValueTask<IParallelRig> CreateRigAsync(CancellationToken ct);

    [Test]
    public virtual async Task TwentyParallelFixtures_ProduceUniqueIsolationKeys()
    {
        const int parallelism = 20;
        var tasks = Enumerable.Range(0, parallelism)
            .Select(async i =>
            {
                var rig = await CreateRigAsync(CancellationToken.None);
                var key = rig.IsolationKey;
                await rig.DisposeAsync();
                return key.Value;
            })
            .ToArray();

        var keys = await Task.WhenAll(tasks);
        var distinct = keys.Distinct(StringComparer.Ordinal).Count();

        await Assert.That(distinct).IsEqualTo(parallelism);
    }
}

/// <summary>Minimal contract every parallel-tested rig exposes.</summary>
public interface IParallelRig : IAsyncDisposable
{
    IsolationKey IsolationKey { get; }
}
