using BenchmarkDotNet.Attributes;
using Rig.TUnit.Docker.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class DockerBenchmarks
{
    [Benchmark]
    public DockerFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public DockerFixtureOptions Options_ConstructWithOverrides() => new()
    {
        DefaultImage = "debian:stable-slim",
        IsolatePerTestNetwork = false,
        ReuseImageCache = false,
    };
}
