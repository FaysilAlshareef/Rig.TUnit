using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.Outbox;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class OutboxBenchmarks
{
    [Benchmark]
    public OutboxMessage Construct_OutboxMessage()
        => new(Guid.NewGuid(), "agg", "E", "{}", DateTimeOffset.UtcNow);
}
