using Azure.Messaging.ServiceBus;
using BenchmarkDotNet.Attributes;
using Rig.TUnit.Messaging.ServiceBus.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 backfill benchmark for ServiceBus Options + Client constructor allocations.
/// Pure-function benchmarks — no container. The client is constructed against a
/// non-routable sb:// endpoint (RFC-5737 TEST-NET-1) so no network call is made.
/// </summary>
[MemoryDiagnoser]
public class ServiceBusMessagingBenchmarks
{
    private const string OfflineConnectionString =
        "Endpoint=sb://192.0.2.1.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==";

    [Benchmark]
    public ServiceBusFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public ServiceBusFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "1.1.3",
        SqlEdgeImageTag = "1.0.8",
        ConfigFilePath = "custom/config.json",
        AcceptEula = true,
        StartupTimeoutSeconds = 180,
    };

    [Benchmark]
    public ServiceBusClient Client_ConstructAgainstUri()
        => new(OfflineConnectionString);
}
