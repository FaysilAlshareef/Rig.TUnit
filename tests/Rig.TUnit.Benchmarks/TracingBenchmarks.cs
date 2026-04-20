using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class TracingBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T055 populates this benchmark.");
}
