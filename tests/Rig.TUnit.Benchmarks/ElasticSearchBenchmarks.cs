using BenchmarkDotNet.Attributes;
using Elastic.Clients.Elasticsearch;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T034-RED benchmark for ElasticSearch Options + Client constructor allocations.
/// Pure-function benchmarks — no container.
/// </summary>
[MemoryDiagnoser]
public class ElasticSearchBenchmarks
{
    [Benchmark]
    public ElasticSearchFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public ElasticSearchFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "9.0.0",
        StartupTimeoutSeconds = 240,
    };

    [Benchmark]
    public ElasticsearchClient Client_ConstructAgainstUri()
        => new(new Uri("http://192.0.2.1:9200"));
}
