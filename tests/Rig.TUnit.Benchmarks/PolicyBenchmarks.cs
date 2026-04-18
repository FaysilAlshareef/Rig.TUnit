using BenchmarkDotNet.Attributes;
using Rig.TUnit.Security.Policies.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class PolicyBenchmarks
{
    [Benchmark]
    public PolicyFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public PolicyFixtureOptions Options_ConstructWithOverrides() => new() { DefaultScheme = "Bearer" };
}
