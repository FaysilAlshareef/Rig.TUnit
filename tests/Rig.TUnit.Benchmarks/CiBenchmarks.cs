using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T030 RED sentinel — T031 replaces with real CI enricher benchmarks.
/// </summary>
[MemoryDiagnoser]
public class CiBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T031 populates this benchmark.");
}
