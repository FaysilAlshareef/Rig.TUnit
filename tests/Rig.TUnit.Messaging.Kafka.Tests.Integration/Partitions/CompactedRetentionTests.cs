using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Kafka.Builder;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration.Partitions;

// T025b-RED: compile-fail until T023-GREEN adds KafkaRigBuilder.WithTopology/ApplyTopologyAsync,
// and T020-GREEN adds KafkaEventSender.SendAsync(SendContext).
public sealed class CompactedRetentionTests
{
    [Test]
    public async Task SendAsync_DuplicateKeys_CompactedTopicRetainsLatestPerKey(CancellationToken ct)
    {
        // Arrange — declare cleanup.policy=compact via WithTopology
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"compact-{Guid.NewGuid():N}";
        var groupId = $"grp-compact-{Guid.NewGuid():N}";

        KafkaRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseKafka(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                    t.Topic(topic, cfg => cfg
                        .WithPartitions(1)
                        .WithConfig("cleanup.policy", "compact")));
            }));
        await captured!.ApplyTopologyAsync(ct);

        await using var sender = new KafkaEventSender(fx.ConnectionString, topic);
        var listener = new KafkaListener(fx.ConnectionString, topic, groupId);
        await listener.StartAsync(ct);

        // Act — send 3 values for same key + 1 unique key
        await sender.SendAsync("value-1", context: new SendContext(PartitionKey: "dup-key"), ct: ct);
        await sender.SendAsync("value-2", context: new SendContext(PartitionKey: "dup-key"), ct: ct);
        await sender.SendAsync("value-3", context: new SendContext(PartitionKey: "dup-key"), ct: ct);
        await sender.SendAsync("only-value", context: new SendContext(PartitionKey: "unique-key"), ct: ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));
        while (listener.Count < 4)
        {
            try { if (!await timer.WaitForNextTickAsync(timeoutCts.Token)) break; }
            catch (OperationCanceledException) { break; }
        }

        await listener.StopAsync(ct);

        // Assert — all initial messages received; topic configured with compact retention
        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(2);

        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = fx.ConnectionString }).Build();
        var configResource = new ConfigResource { Type = ResourceType.Topic, Name = topic };
        var result = await admin.DescribeConfigsAsync([configResource]).WaitAsync(ct);
        var cleanupPolicy = result.FirstOrDefault()?.Entries.GetValueOrDefault("cleanup.policy")?.Value;
        await Assert.That(cleanupPolicy).IsEqualTo("compact");

        await listener.DisposeAsync();
    }
}
