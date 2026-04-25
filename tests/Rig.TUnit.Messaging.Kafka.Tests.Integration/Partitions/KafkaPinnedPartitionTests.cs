using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration.Partitions;

// T024-RED: compile-fail until T024-GREEN adds KafkaListener.Assign(int).
public sealed class KafkaPinnedPartitionTests
{
    [Test]
    public async Task Assign_ToPartition0_OnlyReceivesMessagesRoutedToThatPartition(CancellationToken ct)
    {
        // Arrange — 3-partition topic; listener pinned to partition 0
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"pinned-{Guid.NewGuid():N}";
        var groupId = $"grp-pinned-{Guid.NewGuid():N}";

        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = fx.ConnectionString }).Build();
        await admin.CreateTopicsAsync(
            [new TopicSpecification { Name = topic, NumPartitions = 3, ReplicationFactor = 1 }])
            .WaitAsync(ct);

        await using var sender = new KafkaEventSender(fx.ConnectionString, topic);
        var listener = new KafkaListener(fx.ConnectionString, topic, groupId);

        // Act — assign listener to partition 0 only; compile-fail until T024-GREEN
        listener.Assign(0);
        await listener.StartAsync(ct);

        const string pinnedKey = "pinned-key";
        await sender.SendAsync("msg", context: new Rig.TUnit.Messaging.Helpers.SendContext(PartitionKey: pinnedKey), ct: ct);
        await sender.SendAsync("other-msg", context: new Rig.TUnit.Messaging.Helpers.SendContext(PartitionKey: "other-key"), ct: ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));
        while (listener.Count < 1)
        {
            try { if (!await timer.WaitForNextTickAsync(timeoutCts.Token)) break; }
            catch (OperationCanceledException) { break; }
        }

        await listener.StopAsync(ct);

        // Assert — only messages that hash to partition 0 should be received
        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(0);
        foreach (var msg in listener.Captured)
        {
            await Assert.That(msg.Message.Partition.Value).IsEqualTo(0);
        }

        await listener.DisposeAsync();
    }
}
