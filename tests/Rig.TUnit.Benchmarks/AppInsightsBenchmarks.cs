using BenchmarkDotNet.Attributes;
using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Fixtures;
using Rig.TUnit.Observability.AppInsights.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class AppInsightsBenchmarks
{
    [Benchmark]
    public AppInsightsFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public int Channel_CaptureEvents()
    {
        using var channel = new CapturingTelemetryChannel();
        for (var i = 0; i < 50; i++)
        {
            channel.Send(new EventTelemetry("evt"));
        }
        return channel.Captured.Count;
    }
}
