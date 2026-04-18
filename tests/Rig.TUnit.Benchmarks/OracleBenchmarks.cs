using BenchmarkDotNet.Attributes;
using Rig.TUnit.Databases.Sql.Oracle.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class OracleBenchmarks
{
    [Benchmark]
    public OracleFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public OracleFixtureOptions Options_ConstructWithOverrides()
        => new() { Image = "gvenzl/oracle-free:23.4", StartupTimeoutSeconds = 600 };
}
