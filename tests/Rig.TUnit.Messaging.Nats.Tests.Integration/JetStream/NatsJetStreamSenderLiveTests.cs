using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T052-RED: compile-fail until T052-GREEN adds NatsJetStreamEventSender + T053-GREEN adds NatsJetStreamListener.
public sealed class NatsJetStreamSenderLiveTests
{
    [Test]
    public async Task SendAsync_PublishesToStream_MessageArrivesOnListener(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"sender-live-{Guid.NewGuid():N}";
        var subject    = $"live.{streamName}";

        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);

        await using var sender   = new NatsJetStreamEventSender(fx.JetStream, subject);  // CS0246 RED
        await using var listener = new NatsJetStreamListener(fx.JetStream, streamName);  // CS0246 RED
        await listener.StartAsync(ct);

        // Act
        await sender.SendAsync("hello-jetstream", new SendContext(SessionKey: "s1"), ct: ct);

        // Assert
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 1, ct);
        await Assert.That(listener.Count).IsEqualTo(1);
        await Assert.That(listener.Captured.First().SessionKey).IsEqualTo("s1");
    }
}
