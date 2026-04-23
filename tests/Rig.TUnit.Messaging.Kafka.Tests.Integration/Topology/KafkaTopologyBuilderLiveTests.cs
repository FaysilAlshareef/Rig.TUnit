using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Kafka.Builder;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration.Topology;

// T023-RED: compile-fail until T023-GREEN adds KafkaRigBuilder.WithTopology/ApplyTopologyAsync.
public sealed class KafkaTopologyBuilderLiveTests
{
    [Test]
    public async Task WithTopology_CreatesTopicWithPartitions_OnBroker(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"topo-live-{Guid.NewGuid():N}";

        KafkaRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseKafka(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                    t.Topic(topic, cfg => cfg.WithPartitions(3)));
            }));

        // Act
        await captured!.ApplyTopologyAsync(ct);

        // Assert — topic exists and has expected partition count
        using var admin = new Confluent.Kafka.AdminClientBuilder(
            new Confluent.Kafka.AdminClientConfig { BootstrapServers = fx.ConnectionString }).Build();
        var metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5));
        var partitionCount = metadata.Topics.FirstOrDefault(t => t.Topic == topic)?.Partitions.Count;
        await Assert.That(partitionCount).IsEqualTo(3);
    }

    [Test]
    public async Task WithTopology_CalledTwice_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"topo-idem-{Guid.NewGuid():N}";

        KafkaRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseKafka(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t => t.Topic(topic, cfg => cfg.WithPartitions(2)));
            }));

        // Act — apply twice; second call must not throw (idempotent create-if-not-exists)
        await captured!.ApplyTopologyAsync(ct);
        await Assert.That(async () =>
            await captured!.ApplyTopologyAsync(ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task WithTopology_ReturnsSameBuilderForChain(CancellationToken ct)
    {
        // Arrange — chain syntax must return the same builder instance
        var fx = await SharedKafkaFixture.GetAsync();

        KafkaRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseKafka(fx, builder =>
            {
                captured = builder;
                var returned = builder.WithTopology(t =>
                    t.Topic($"chain-{Guid.NewGuid():N}"));
                Assert.That(returned).IsEqualTo(builder);
            }));

        await Assert.That(captured).IsNotNull();
    }
}
