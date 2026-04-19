using BenchmarkDotNet.Attributes;
using Rig.TUnit.Core;
using Rig.TUnit.Databases.NoSql.Cassandra.Helpers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T026-RED benchmark for <see cref="KeyspacePerTestHelper.BuildSafeKeyspace"/>. Pure-function
/// benchmark — no container. Measures allocations on typical + max-length isolation keys.
/// </summary>
[MemoryDiagnoser]
public class CassandraKeyspaceBenchmarks
{
    private IsolationKey _shortKey;
    private IsolationKey _longKey;

    [GlobalSetup]
    public void Setup()
    {
        _shortKey = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        _longKey = IsolationKey.FromName(new string('x', 128));
    }

    [Benchmark]
    public string BuildSafeKeyspace_ShortKey() => KeyspacePerTestHelper.BuildSafeKeyspace("test", _shortKey);

    [Benchmark]
    public string BuildSafeKeyspace_LongKey() => KeyspacePerTestHelper.BuildSafeKeyspace("test", _longKey);
}
