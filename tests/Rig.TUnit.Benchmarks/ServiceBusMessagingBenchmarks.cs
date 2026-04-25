using Azure.Messaging.ServiceBus;
using BenchmarkDotNet.Attributes;
using Rig.TUnit.Messaging.Helpers;
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

    /// <summary>
    /// Feature 007: allocation cost of a <see cref="ServiceBusMessage"/> routed through the
    /// session processor path — SessionId + PartitionKey populated from <see cref="SendContext.SessionKey"/>.
    /// </summary>
    [Benchmark]
    public ServiceBusMessage SessionProcessor_VsNonSession_Throughput()
    {
        var ctx = new SendContext(SessionKey: "customer-42");
        return new ServiceBusMessage("payload")
        {
            SessionId    = ctx.SessionKey,
            PartitionKey = ctx.SessionKey,
            MessageId    = Guid.NewGuid().ToString(),
        };
    }

    /// <summary>
    /// Baseline: same <see cref="ServiceBusMessage"/> allocation without a session context.
    /// Pair with <see cref="SessionProcessor_VsNonSession_Throughput"/> to compare overhead.
    /// </summary>
    [Benchmark]
    public ServiceBusMessage SessionProcessor_NoSession_Baseline()
        => new("payload") { MessageId = Guid.NewGuid().ToString() };
}
