using BenchmarkDotNet.Attributes;
using Rig.TUnit.Messaging.Nats.Helpers;
using Rig.TUnit.Messaging.Nats.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for Nats Options + Listener/Sender construction.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class NatsMessagingBenchmarks
{
    private const string OfflineUrl = "nats://192.0.2.1:4222";

    [Benchmark]
    public NatsFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public NatsFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "2.11-alpine",
        StartupTimeoutSeconds = 60,
    };

    [Benchmark]
    public NatsListener Listener_Construct()
        => new(OfflineUrl, "subject");

    [Benchmark]
    public NatsEventSender Sender_Construct()
        => new(OfflineUrl, "subject");
}
