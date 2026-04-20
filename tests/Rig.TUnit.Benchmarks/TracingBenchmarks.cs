using BenchmarkDotNet.Attributes;
using Rig.TUnit.Observability.Tracing.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class TracingBenchmarks
{
    [Benchmark]
    public TracingFixtureOptions Construct_DefaultOptions() => new() { ServiceName = "bench" };
}
