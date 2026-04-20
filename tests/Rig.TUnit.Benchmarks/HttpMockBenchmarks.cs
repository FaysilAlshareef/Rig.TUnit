using BenchmarkDotNet.Attributes;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Feature 005 T028 RED sentinel — T029 replaces with a per-request HttpMock benchmark.
/// The existing HttpClientHelperBenchmarks covers a different surface (WebAPI helper).
/// </summary>
[MemoryDiagnoser]
public class HttpMockBenchmarks
{
    [Benchmark]
    public int Placeholder() => throw new InvalidOperationException("RED: baseline not implemented — T029 populates this benchmark.");
}
