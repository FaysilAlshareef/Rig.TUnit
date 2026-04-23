using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T044c-RED: compile-fail until T042-GREEN adds WithTopology + priority queue config.
public sealed class PriorityOrderingTests
{
    [Test]
    public async Task PriorityQueue_HighPriorityMessageDeliveredFirst(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var queueName = $"pri-q-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                             // CS1061 RED
                    t.Queue(queueName, cfg =>
                        cfg.WithMaxPriority(10)));                    // CS1061 RED
            }));

        await captured!.ApplyTopologyAsync(ct);                       // CS1061 RED

        await using var sender   = new RabbitMqEventSender(fx.ConnectionString, queueName);
        await using var listener = new RabbitMqListener(fx.ConnectionString, queueName);
        await listener.StartAsync(ct);

        // Act — send low-priority then high-priority (high should be delivered first)
        await sender.SendAsync("low",  context: new SendContext(PartitionKey: "0"),  ct: ct); // CS1739 RED
        await sender.SendAsync("high", context: new SendContext(PartitionKey: "10"), ct: ct); // CS1739 RED

        // Assert — 2 messages received; high-priority comes first
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 2, ct);
        await Assert.That(listener.Captured.First().Body).IsEqualTo("high");
    }
}
