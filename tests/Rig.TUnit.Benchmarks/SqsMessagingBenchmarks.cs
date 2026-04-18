using Amazon.SQS;
using BenchmarkDotNet.Attributes;
using NSubstitute;
using Rig.TUnit.Messaging.Sqs.Helpers;
using Rig.TUnit.Messaging.Sqs.Options;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for Sqs Options + Listener/Sender construction.
/// Uses an NSubstitute <see cref="IAmazonSQS"/> — no AWS call issued.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class SqsMessagingBenchmarks
{
    private static readonly IAmazonSQS OfflineClient = Substitute.For<IAmazonSQS>();
    private const string QueueUrl = "https://sqs.example.com/queue";

    [Benchmark]
    public SqsFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public SqsFixtureOptions Options_ConstructWithOverrides() => new()
    {
        ImageTag = "3.4",
        StartupTimeoutSeconds = 120,
        Region = "eu-west-1",
        AccessKeyId = "custom",
        SecretAccessKey = "secret",
    };

    [Benchmark]
    public SqsListener Listener_Construct()
        => new(OfflineClient, QueueUrl);

    [Benchmark]
    public SqsEventSender Sender_Construct()
        => new(OfflineClient, QueueUrl);
}
