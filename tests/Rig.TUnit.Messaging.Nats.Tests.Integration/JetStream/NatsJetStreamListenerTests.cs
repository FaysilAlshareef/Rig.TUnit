using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T053-RED: compile-fail until T053-GREEN adds NatsJetStreamListener.
public sealed class NatsJetStreamListenerTests
{
    [Test]
    public async Task OrderedConsumer_RecordsSessionKeyFromSubjectSegment(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"session-key-{Guid.NewGuid():N}";
        var subject    = $"events.session.{streamName}";

        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);

        await using var sender   = new NatsJetStreamEventSender(fx.JetStream, subject);
        await using var listener = new NatsJetStreamListener(fx.JetStream, streamName);  // CS0246 RED
        await listener.StartAsync(ct);

        // Act — send with SessionKey populated
        await sender.SendAsync("payload-1", new SendContext(SessionKey: "session-abc"), ct: ct);

        // Assert — SessionKey is captured from the header
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 1, ct);
        await Assert.That(listener.Captured.First().SessionKey).IsEqualTo("session-abc");
    }

    [Test]
    public async Task OrderedConsumer_NoDuplicatesAcrossAck(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"no-dup-{Guid.NewGuid():N}";
        var subject    = $"events.dup.{streamName}";

        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);

        await using var sender   = new NatsJetStreamEventSender(fx.JetStream, subject);
        await using var listener = new NatsJetStreamListener(fx.JetStream, streamName);  // CS0246 RED
        await listener.StartAsync(ct);

        // Act — send 5 messages
        for (var i = 0; i < 5; i++)
        {
            await sender.SendAsync($"msg-{i}", ct: ct);
        }

        // Assert — exactly 5 received, no duplicates
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 5, ct);
        await Assert.That(listener.Count).IsEqualTo(5);

        var bodies = listener.Captured.Select(m => m.Body).ToArray();
        await Assert.That(bodies.Distinct().Count()).IsEqualTo(5);
    }
}
