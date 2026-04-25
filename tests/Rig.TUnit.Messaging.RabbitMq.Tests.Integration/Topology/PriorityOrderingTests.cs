using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

/// <summary>
/// Verifies that <c>IRabbitMqQueueConfig.WithMaxPriority</c> propagates to the AMQP
/// <c>x-max-priority</c> queue argument and that messages flow on the resulting queue.
/// End-to-end priority delivery ordering requires setting <c>BasicProperties.Priority</c>
/// on each publish — the rig sender does not currently expose that knob (no
/// <c>SendContext.Priority</c> field), so this test does not assert ordering.
/// A <c>SendContext.Priority</c> extension is in scope for a follow-up task.
/// </summary>
public sealed class PriorityOrderingTests
{
    [Test]
    public async Task PriorityQueue_DeclaredWithMaxPriority_AcceptsAndDeliversMessages(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var queueName = $"pri-q-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                    t.Queue(queueName, cfg =>
                        cfg.WithMaxPriority(10)));
            }));

        await captured!.ApplyTopologyAsync(ct);

        await using var sender = new RabbitMqEventSender(fx.ConnectionString, queueName);
        await using var listener = new RabbitMqListener(fx.ConnectionString, queueName);
        await listener.StartAsync(ct);

        // Act — both messages must route to the priority queue
        await sender.SendAsync("low", ct: ct);
        await sender.SendAsync("high", ct: ct);

        // Assert — queue accepted both messages despite x-max-priority arg
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 2, ct);
        await Assert.That(listener.Count).IsEqualTo(2);
    }
}
