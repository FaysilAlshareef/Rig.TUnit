using BenchmarkDotNet.Attributes;
using KurrentDB.Client;
using Rig.TUnit.Databases.NoSql.KurrentDb.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T038-RED benchmark for KurrentDB Options + Client construction.
/// Pure-function benchmarks — no container.
/// </summary>
[MemoryDiagnoser]
public class KurrentDbBenchmarks
{
    [Benchmark]
    public KurrentDbFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public KurrentDbFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "25.2",
        StartupTimeoutSeconds = 120,
    };

    [Benchmark]
    public KurrentDBClientSettings Settings_ParseConnectionString()
        => KurrentDBClientSettings.Create("esdb://192.0.2.1:2113?tls=false");
}
