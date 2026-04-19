using BenchmarkDotNet.Attributes;
using Rig.TUnit.Messaging.RabbitMq.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for RabbitMq Options + Listener/Sender construction.
/// Connections are lazy — ctor does not establish an AMQP session, safe to benchmark
/// against a TEST-NET-1 URI.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class RabbitMqMessagingBenchmarks
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Benchmark]
    public RabbitMqFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public RabbitMqFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "3.13-management",
        StartupTimeoutSeconds = 120,
        Username = "admin",
        Password = "secret",
    };

    [Benchmark]
    public RabbitMqListener Listener_Construct()
        => new(OfflineUri, "queue");

    [Benchmark]
    public RabbitMqEventSender Sender_Construct()
        => new(OfflineUri, "queue");
}
