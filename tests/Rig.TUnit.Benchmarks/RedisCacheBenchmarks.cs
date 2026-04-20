using BenchmarkDotNet.Attributes;
using Rig.TUnit.Caching.Redis.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Unit-level benchmark: options construction cost. The network-touching Redis benchmarks
/// live in live-tests (require a real Redis) — here we measure the POCO overhead.
/// </summary>
[MemoryDiagnoser]
public class RedisCacheBenchmarks
{
    [Benchmark]
    public RedisFixtureOptions Construct_DefaultOptions() => new();
}
