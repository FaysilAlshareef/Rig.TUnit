using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration;

/// <summary>
/// FR-035 live container round-trip for <see cref="KafkaEventSender"/> → <see cref="KafkaListener"/>.
/// Requires Docker.
/// </summary>
public sealed class KafkaListenerLiveTests
{
    [Test]
    public async Task Roundtrip_SendOneMessage_ListenerCapturesIt()
    {
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"rig-test-{Guid.NewGuid():N}";
        var groupId = $"rig-group-{Guid.NewGuid():N}";

        var listener = new KafkaListener(fx.ConnectionString, topic, groupId);
        await listener.StartAsync(CancellationToken.None);

        await using var sender = new KafkaEventSender(fx.ConnectionString, topic);
        await sender.SendAsync("{\"orderId\":42}", correlationId: "abc-123");

        await MessageAssert.Within(listener, TimeSpan.FromSeconds(30), expectedCount: 1);

        await listener.DisposeAsync();

        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(1);
    }
}
