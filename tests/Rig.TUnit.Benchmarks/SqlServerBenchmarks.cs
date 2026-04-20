using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.SqlServer.Fixtures;
using Rig.TUnit.Databases.Sql.SqlServer.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures the per-call overhead of constructing <see cref="SqlServerFixture"/>
/// through its <see cref="IOptions{TOptions}"/> entry point — representative of the
/// DI-resolved construction path tests actually hit.
/// </summary>
[MemoryDiagnoser]
public class SqlServerBenchmarks
{
    private IOptions<SqlServerFixtureOptions> _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        _options = Options.Create(new SqlServerFixtureOptions());
    }

    [Benchmark]
    public SqlServerFixture Construct_FromIOptions() => new(_options);
}
