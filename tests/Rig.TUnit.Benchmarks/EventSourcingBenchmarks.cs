using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.EventSourcing.Assertions;
using Rig.TUnit.Microservices.EventSourcing.Helpers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class EventSourcingBenchmarks
{
    private sealed record SampleEvent(string Id);

    [Benchmark]
    public int AggregateAssert_RaisedCount()
    {
        var raised = new object[]
        {
            new SampleEvent("a"),
            new SampleEvent("b"),
            new SampleEvent("c"),
        };
        return AggregateFluentAssert.For(new object(), raised).Raised<SampleEvent>().Count;
    }

    [Benchmark]
    public SchemaEvolutionReport Schema_Analyze()
        => SchemaEvolutionHelper.Analyze<SampleEvent>("""{"id":"x"}""");
}
