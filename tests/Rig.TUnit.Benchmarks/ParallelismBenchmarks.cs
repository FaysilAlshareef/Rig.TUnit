using BenchmarkDotNet.Attributes;
using Rig.TUnit.Parallelism.Helpers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class ParallelismBenchmarks
{
    [Benchmark]
    public int PortAllocator_Allocate() => PortAllocator.Allocate();

    [Benchmark]
    public async Task ExclusiveResourceCoordinator_AcquireRelease()
    {
        using var _ = await ExclusiveResourceCoordinator.AcquireAsync("bench-shared");
    }
}
