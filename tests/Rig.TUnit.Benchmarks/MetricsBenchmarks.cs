using System.Diagnostics.Metrics;
using BenchmarkDotNet.Attributes;
using Rig.TUnit.Observability.Metrics.Assertions;
using Rig.TUnit.Observability.Metrics.Helpers;
using Rig.TUnit.Observability.Metrics.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class MetricsBenchmarks
{
    [Benchmark]
    public MetricsFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public bool Guard_WithinBudget_Allocates()
        => TagCardinalityGuard.EnsureWithinBudget("tenant", distinctCount: 50, maxCardinality: 100);

    [Benchmark]
    public int Capture_EmitCounter()
    {
        using var capture = new MetricCapture("bench.meter");
        using var meter = new Meter("bench.meter");
        var counter = meter.CreateCounter<long>("hits");
        counter.Add(1);
        return capture.Samples.Count;
    }
}
