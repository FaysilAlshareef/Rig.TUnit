using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration.Partitions;

// T022-RED: compile-fail until T022-GREEN extends KafkaListener with partitions/topicConfigs ctor params.
public sealed class KafkaTopicConfigTests
{
    [Test]
    public async Task EnsureTopicExistsAsync_WithPartitionsAndConfigs_CreatesTopicWithExactShape(CancellationToken ct)
    {
        // Arrange — 3-partition topic with a custom retention.ms
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"cfg-{Guid.NewGuid():N}";
        var groupId = $"grp-cfg-{Guid.NewGuid():N}";

        var listener = new KafkaListener(
            fx.ConnectionString, topic, groupId,
            partitions: 3,
            topicConfigs: new Dictionary<string, string> { ["retention.ms"] = "3600000" });

        // Act — StartAsync calls EnsureTopicExistsAsync internally with the provided params
        await listener.StartAsync(ct);
        await listener.StopAsync(ct);

        // Assert — topic was created with the configured shape
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = fx.ConnectionString }).Build();
        var configResource = new ConfigResource { Type = ResourceType.Topic, Name = topic };
        var result = await admin.DescribeConfigsAsync([configResource]).WaitAsync(ct);
        var retention = result.FirstOrDefault()?.Entries.GetValueOrDefault("retention.ms")?.Value;
        await Assert.That(retention).IsEqualTo("3600000");

        var metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5));
        var partitionCount = metadata.Topics.FirstOrDefault(t => t.Topic == topic)?.Partitions.Count;
        await Assert.That(partitionCount).IsEqualTo(3);

        await listener.DisposeAsync();
    }
}
