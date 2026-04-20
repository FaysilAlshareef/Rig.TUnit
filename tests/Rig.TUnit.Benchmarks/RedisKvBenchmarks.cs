using BenchmarkDotNet.Attributes;
using NSubstitute;
using Rig.TUnit.Databases.NoSql.Redis.Helpers;
using StackExchange.Redis;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Unit-level benchmark: KeyScanHelper construction cost. Live SCAN performance
/// lives in live-tests since it needs a real Redis multiplexer.
/// </summary>
[MemoryDiagnoser]
public class RedisKvBenchmarks
{
    private IConnectionMultiplexer _mux = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mux = Substitute.For<IConnectionMultiplexer>();
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(Substitute.For<IDatabase>());
    }

    [Benchmark]
    public KeyScanHelper Construct_KeyScanHelper() => new(_mux);
}
