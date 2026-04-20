using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class LoggingBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T051 populates this benchmark.");
}
