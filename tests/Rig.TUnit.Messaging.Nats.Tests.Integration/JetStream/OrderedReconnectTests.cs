using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T055a-RED: compile-fail until T053-GREEN adds NatsJetStreamEventSender + NatsJetStreamListener.
public sealed class OrderedReconnectTests
{
    [Test]
    public async Task OrderedConsumer_SurvivesReconnect_ReceivesAllMessagesInOrderWithoutDuplicates(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"ordered-reconnect-{Guid.NewGuid():N}";
        var subject    = $"events.{streamName}";

        await fx.EnsureStreamAsync(streamName, [subject], ct);   // CS1061 RED until T051-GREEN

        await using var sender   = new NatsJetStreamEventSender(fx.JetStream, subject);  // CS0246 RED
        await using var listener = new NatsJetStreamListener(fx.JetStream, streamName);  // CS0246 RED
        await listener.StartAsync(ct);

        // Act — send 10 messages before any artificial disconnect
        for (var i = 0; i < 10; i++)
        {
            await sender.SendAsync($"msg-{i}", new SendContext(SessionKey: "session-1"), ct: ct);
        }

        // Assert — all 10 messages arrive; ordered consumer guarantees no duplicates
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 10, ct);
        await Assert.That(listener.Count).IsEqualTo(10);

        var seq = listener.Captured.Select((m, idx) => idx).ToArray();
        await Assert.That(seq).IsEquivalentTo(Enumerable.Range(0, 10).ToArray());
    }
}
