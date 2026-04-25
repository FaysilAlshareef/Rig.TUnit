using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T044d-RED: compile-fail until T042-GREEN adds WithTopology + quorum queue config.
public sealed class QuorumQueueTests
{
    [Test]
    public async Task QuorumQueue_AcceptsAndDeliversMessages(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var queueName = $"quorum-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                             // CS1061 RED
                    t.Queue(queueName, cfg =>
                        cfg.WithQuorum()));                           // CS1061 RED
            }));

        await captured!.ApplyTopologyAsync(ct);                       // CS1061 RED

        await using var sender   = new RabbitMqEventSender(fx.ConnectionString, queueName);
        await using var listener = new RabbitMqListener(fx.ConnectionString, queueName);
        await listener.StartAsync(ct);

        // Act
        await sender.SendAsync("quorum-msg", ct: ct);

        // Assert — message delivered on the quorum queue
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 1, ct);
        await Assert.That(listener.Count).IsEqualTo(1);
    }
}
