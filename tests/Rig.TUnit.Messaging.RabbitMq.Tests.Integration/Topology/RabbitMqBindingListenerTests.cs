using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T041-RED: compile-fail until T041-GREEN adds exchange+binding support to RabbitMqListener
//           and populates CapturedMessage.SessionKey from x-partition-key header.
public sealed class RabbitMqBindingListenerTests
{
    [Test]
    public async Task ListenOn_TopicExchangeWithBinding_ReceivesOnlyMatchingRoutingKey_AndPopulatesSessionKey(
        CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var exchange = $"bind-test-{Guid.NewGuid():N}";
        var queue    = $"bind-q-{Guid.NewGuid():N}";

        await using var sender = new RabbitMqEventSender(fx.ConnectionString, exchange);

        // RabbitMqListener with exchange + binding — CS1061 RED until T041-GREEN
        await using var listener = new RabbitMqListener(
            fx.ConnectionString, queue,
            exchange: exchange, exchangeType: "topic", routingKey: "user.*"); // CS1739 RED

        await listener.StartAsync(ct);

        // Act — only "user.*" routing key should match
        await sender.SendAsync("user-event", context: new SendContext(PartitionKey: "user.created"), ct: ct);
        await sender.SendAsync("order-event", context: new SendContext(PartitionKey: "order.placed"), ct: ct);

        // Assert — listener receives only the user message; SessionKey set from x-partition-key
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1, ct);
        await Assert.That(listener.Count).IsEqualTo(1);
        await Assert.That(listener.Captured.First().SessionKey).IsEqualTo("user.created");
    }
}
