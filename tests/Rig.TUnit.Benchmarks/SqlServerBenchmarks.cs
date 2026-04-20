using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T046 RED sentinel — T047 replaces with the per-call overhead of the
/// SqlServerFixture in an IOptions-bound configuration path.
/// </summary>
[MemoryDiagnoser]
public class SqlServerBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T047 populates this benchmark.");
}
