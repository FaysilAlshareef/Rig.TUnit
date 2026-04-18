using BenchmarkDotNet.Attributes;
using Rig.TUnit.Databases.Sql.MySql.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class MySqlBenchmarks
{
    [Benchmark]
    public MySqlFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public MySqlFixtureOptions Options_ConstructWithOverrides()
        => new() { ImageTag = "8.0", StartupTimeoutSeconds = 240 };
}
