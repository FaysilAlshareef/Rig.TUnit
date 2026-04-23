using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T044a-RED: compile-fail until T042-GREEN adds WithTopology/ExchangeType + T040-GREEN adds SendContext overload.
public sealed class TopicFanoutTests
{
    [Test]
    public async Task SendAsync_TopicExchangeRouting_DeliversToMatchingQueueOnly(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var exchange = $"topic-fanout-{Guid.NewGuid():N}";
        var userQueue = $"user-q-{Guid.NewGuid():N}";
        var orderQueue = $"order-q-{Guid.NewGuid():N}";
        var stockQueue = $"stock-q-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                        // CS1061 RED
                    t.Exchange(exchange, ExchangeType.Topic)     // CS0246 RED
                     .BindQueue(userQueue,  "user.*")
                     .BindQueue(orderQueue, "order.*")
                     .BindQueue(stockQueue, "stock.*"));
            }));

        await captured!.ApplyTopologyAsync(ct);                  // CS1061 RED

        await using var sender = new RabbitMqEventSender(fx.ConnectionString, exchange);
        await using var userListener  = new RabbitMqListener(fx.ConnectionString, userQueue);
        await using var orderListener = new RabbitMqListener(fx.ConnectionString, orderQueue);
        await using var stockListener = new RabbitMqListener(fx.ConnectionString, stockQueue);
        await userListener.StartAsync(ct);
        await orderListener.StartAsync(ct);
        await stockListener.StartAsync(ct);

        // Act
        await sender.SendAsync("user-msg",  context: new SendContext(PartitionKey: "user.created"),  ct: ct); // CS1739 RED
        await sender.SendAsync("order-msg", context: new SendContext(PartitionKey: "order.placed"),  ct: ct); // CS1739 RED

        // Assert — user + order queues receive 1 each; stock queue receives nothing
        await MessageAssert.Within(userListener,  TimeSpan.FromSeconds(10), expectedCount: 1, ct);
        await MessageAssert.Within(orderListener, TimeSpan.FromSeconds(10), expectedCount: 1, ct);
        await Assert.That(userListener.Count).IsEqualTo(1);
        await Assert.That(orderListener.Count).IsEqualTo(1);
        await Assert.That(stockListener.Count).IsEqualTo(0);
    }
}
