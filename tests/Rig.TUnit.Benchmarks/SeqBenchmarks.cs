using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SeqBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T053 populates this benchmark.");
}
