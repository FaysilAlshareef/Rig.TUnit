using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration;

/// <summary>
/// FR-035 live container round-trip for <see cref="RabbitMqEventSender"/> → <see cref="RabbitMqListener"/>.
/// Requires Docker.
/// </summary>
public sealed class RabbitMqListenerLiveTests
{
    [Test]
    public async Task Roundtrip_SendOneMessage_ListenerCapturesIt()
    {
        var fx = await SharedRabbitMqFixture.GetAsync();
        var queue = $"rig-test-{Guid.NewGuid():N}";

        var listener = new RabbitMqListener(fx.ConnectionString, queue);
        await listener.StartAsync(CancellationToken.None);

        await using var sender = new RabbitMqEventSender(fx.ConnectionString, queue);
        await sender.SendAsync("{\"orderId\":42}", correlationId: "abc-123");

        await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1);

        await listener.DisposeAsync();

        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(1);
    }
}
