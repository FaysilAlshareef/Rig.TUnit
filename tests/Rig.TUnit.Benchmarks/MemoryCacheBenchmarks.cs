using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Rig.TUnit.Caching.Memory.Fixtures;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class MemoryCacheBenchmarks
{
    private MemoryCacheFixture _fixture = null!;
    private IMemoryCache _cache = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new MemoryCacheFixture();
        await _fixture.InitializeAsync();
        _cache = _fixture.Cache;
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _fixture.DisposeAsync();

    [Benchmark]
    public string? Set_Then_Get()
    {
        _cache.Set("bench-key", "payload");
        return _cache.Get<string>("bench-key");
    }
}
