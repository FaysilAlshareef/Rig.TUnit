using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T022 RED sentinel — T023 replaces with representative pipeline benchmark.
/// </summary>
[MemoryDiagnoser]
public class MediatorPipelineBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T023 populates this benchmark.");
}
