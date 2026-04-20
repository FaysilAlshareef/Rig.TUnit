using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T026 RED sentinel — T027 replaces with a per-request benchmark of
/// <c>HttpClientHelper</c> against a real WebApplicationFactory.
/// </summary>
[MemoryDiagnoser]
public class WebApiBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T027 populates this benchmark.");
}
