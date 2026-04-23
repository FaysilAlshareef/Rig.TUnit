using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.ServiceBus.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Topology;

/// <summary>
/// T013-RED: guard tests for ServiceBusRigBuilder.WithTopology.
/// References WithTopology and ApplyTopologyAsync which do not exist yet — compile-fail RED.
/// </summary>
public sealed class ServiceBusRigBuilderWithTopologyTests
{
    [Test]
    public async Task WithTopology_CreatesTopicSubscriptionQueue_OnEmulator(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var topicName = $"rig-topology-{Guid.NewGuid():N}";
        var subName = "rig-sub";
        var queueName = $"rig-queue-{Guid.NewGuid():N}";

        ServiceBusRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseServiceBus(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(topology =>
                {
                    topology.Topic(topicName);
                    topology.Subscription(topicName, subName);
                    topology.Queue(queueName);
                });
            }));

        // Act
        await captured!.ApplyTopologyAsync(ct);

        // Assert
        await Assert.That((await admin.TopicExistsAsync(topicName, ct)).Value).IsTrue();
        await Assert.That((await admin.SubscriptionExistsAsync(topicName, subName, ct)).Value).IsTrue();
        await Assert.That((await admin.QueueExistsAsync(queueName, ct)).Value).IsTrue();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
        await admin.DeleteQueueAsync(queueName, ct);
    }

    [Test]
    public async Task WithTopology_CalledTwice_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var topicName = $"rig-idempotent-{Guid.NewGuid():N}";

        ServiceBusRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseServiceBus(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(topology => topology.Topic(topicName));
            }));

        // Act — apply same topology twice (idempotency)
        await captured!.ApplyTopologyAsync(ct);
        await captured!.ApplyTopologyAsync(ct);

        // Assert — no exception; topic exists once
        await Assert.That((await admin.TopicExistsAsync(topicName, ct)).Value).IsTrue();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
    }

    [Test]
    public async Task WithTopology_ReturnsSameBuilderForChain(CancellationToken ct)
    {
        var fx = await SharedServiceBusFixture.GetAsync();

        ServiceBusRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseServiceBus(fx, builder => captured = builder));

        var returned = captured!.WithTopology(_ => { });

        await Assert.That(returned).IsNotNull();
        await Assert.That(ReferenceEquals(returned, captured)).IsTrue();
    }
}
