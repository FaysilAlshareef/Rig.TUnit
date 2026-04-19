using BenchmarkDotNet.Attributes;
using Rig.TUnit.Databases.NoSql.Cosmos.Helpers;
using Rig.TUnit.Databases.NoSql.Cosmos.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class CosmosBenchmarks
{
    [Benchmark]
    public CosmosFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public double RuCapture_RecordAndSum()
    {
        var capture = new RuChargeCapture();
        for (var i = 0; i < 100; i++)
        {
            capture.Record("op", 1.5);
        }
        return capture.TotalRu;
    }

    [Benchmark]
    public double PartitionChecker_MaxShare()
    {
        var counts = new Dictionary<string, int> { ["a"] = 10, ["b"] = 10, ["c"] = 10 };
        return PartitionKeyDistributionChecker.MaxShare(counts);
    }
}
