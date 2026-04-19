using BenchmarkDotNet.Attributes;
using Rig.TUnit.Caching.Hybrid.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for HybridCache Options construction + fluent wiring.
/// In-process cache — no container. Uses <see cref="InProcessEmitBenchmarkConfig"/>
/// to avoid BDN's 2-minute external build timeout on the large transitive graph.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class HybridCacheBenchmarks
{
    [Benchmark]
    public HybridCacheFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public HybridCacheFixtureOptions Options_ConstructWithOverrides() => new()
    {
        DefaultExpirationSeconds = 120,
        LocalCacheExpirationSeconds = 30,
        MaximumPayloadBytes = 4096,
        MaximumKeyLength = 256,
    };
}
