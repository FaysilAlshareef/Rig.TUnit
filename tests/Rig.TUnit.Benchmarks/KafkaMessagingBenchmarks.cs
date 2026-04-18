using BenchmarkDotNet.Attributes;
using Rig.TUnit.Messaging.Kafka.Helpers;
using Rig.TUnit.Messaging.Kafka.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for Kafka Options + Listener/Sender construction.
/// No Kafka broker required — bootstrap points at non-routable TEST-NET-1 so any lazy
/// socket connect inside Confluent.Kafka does not impact ctor-allocation measurements.
/// Uses <see cref="InProcessEmitBenchmarkConfig"/> so BDN does not spawn a separate
/// child build (avoids the 2-minute build timeout on the large transitive graph).
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class KafkaMessagingBenchmarks
{
    private const string OfflineBootstrap = "192.0.2.1:9092";

    [Benchmark]
    public KafkaFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public KafkaFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "7.7.0",
        StartupTimeoutSeconds = 180,
    };

    [Benchmark]
    public KafkaListener Listener_Construct()
        => new(OfflineBootstrap, "topic", "group");

    [Benchmark]
    public KafkaEventSender Sender_Construct()
        => new(OfflineBootstrap, "topic");
}
