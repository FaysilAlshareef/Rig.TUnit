using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T032 RED sentinel — T033 replaces with real concurrency benchmarks.
/// </summary>
[MemoryDiagnoser]
public class ConcurrencyBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T033 populates this benchmark.");
}
