using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.Snapshots.Scrubbers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SnapshotsBenchmarks
{
    private const string Payload = "{\"Id\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"OccurredAt\":\"2026-04-20T12:34:56Z\",\"CorrelationId\":\"abc-123\",\"Sequence\":42}";

    [Benchmark]
    public string Apply_AllRules() => MicroserviceScrubbers.Apply(Payload);
}
