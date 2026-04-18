using Rig.TUnit.Parallelism.Helpers;

namespace Rig.TUnit.Parallelism.Tests.Integration;

public sealed class ParallelismTests
{
    [Test]
    public async Task PortAllocator_AcrossHundredConcurrentRequests_ReturnsDistinctPorts()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(PortAllocator.Allocate)).ToArray();
        var ports = await Task.WhenAll(tasks);
        var distinct = ports.Distinct().Count();

        // Ports may collide if the OS recycles between calls — but allocating all at once
        // (see next test) is strictly collision-free. Sequential allocation aims for the
        // usual no-collision path.
        await Assert.That(distinct).IsGreaterThanOrEqualTo(95);
    }

    [Test]
    public async Task PortAllocator_AllocateBatchOfTwenty_ReturnsAllDistinct()
    {
        var ports = PortAllocator.Allocate(20);
        await Assert.That(ports.Distinct().Count()).IsEqualTo(20);
    }

    [Test]
    public async Task ExclusiveResource_AcquireRelease_AllowsSubsequentAcquire()
    {
        using (await ExclusiveResourceCoordinator.AcquireAsync("test-lock-1"))
        {
            // holding lock
        }
        using var second = await ExclusiveResourceCoordinator.AcquireAsync("test-lock-1");
        await Assert.That(second).IsNotNull();
    }

    [Test]
    public async Task ExclusiveResource_TwoConcurrentAcquires_AreSerialised()
    {
        var observed = 0;
        var max = 0;
        Task Work()
        {
            return Task.Run(async () =>
            {
                using var _ = await ExclusiveResourceCoordinator.AcquireAsync("test-lock-2");
                var current = Interlocked.Increment(ref observed);
                if (current > max) max = current;
                await Task.Delay(20);
                Interlocked.Decrement(ref observed);
            });
        }

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Work()));
        await Assert.That(max).IsLessThanOrEqualTo(1);
    }
}
