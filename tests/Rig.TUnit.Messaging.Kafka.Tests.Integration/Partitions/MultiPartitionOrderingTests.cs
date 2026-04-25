using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration.Partitions;

// T025a-RED: compile-fail until T020-GREEN adds KafkaEventSender.SendAsync(SendContext) overload.
public sealed class MultiPartitionOrderingTests
{
    [Test]
    public async Task SendAsync_5KeysAcross6Partitions_PerKeyMonotonicOrderingPreserved(CancellationToken ct)
    {
        // Arrange — 6-partition topic, 5 keys × 20 messages
        const int keyCount = 5;
        const int messagesPerKey = 20;
        var keys = Enumerable.Range(0, keyCount).Select(i => $"key-{i}").ToArray();

        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"mp-ordering-{Guid.NewGuid():N}";
        var groupId = $"grp-{Guid.NewGuid():N}";

        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = fx.ConnectionString }).Build();
        await admin.CreateTopicsAsync(
            [new TopicSpecification { Name = topic, NumPartitions = 6, ReplicationFactor = 1 }])
            .WaitAsync(ct);

        await using var sender = new KafkaEventSender(fx.ConnectionString, topic);
        var listener = new KafkaListener(fx.ConnectionString, topic, groupId);
        await listener.StartAsync(ct);

        // Act — send messages for each key in interleaved order
        for (var i = 0; i < messagesPerKey; i++)
        {
            foreach (var key in keys)
            {
                await sender.SendAsync(
                    $"msg-{key}-{i}",
                    context: new SendContext(PartitionKey: key),
                    ct: ct);
            }
        }

        var totalExpected = keyCount * messagesPerKey;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (listener.Count < totalExpected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(300, ct);
        }

        await listener.StopAsync(ct);

        // Assert — Kafka routes same key to same partition; offset is monotonically increasing per partition
        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(totalExpected);
        OrderingAssert.PerKeyMonotonic(listener, m => m.Message.Key, m => m.Offset.Value);

        await listener.DisposeAsync();
    }
}
