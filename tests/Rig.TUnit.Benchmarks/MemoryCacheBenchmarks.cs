using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class MemoryCacheBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T041 populates this benchmark.");
}
