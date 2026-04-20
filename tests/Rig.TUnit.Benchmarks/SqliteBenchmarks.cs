using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SqliteBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T045 populates this benchmark.");
}
