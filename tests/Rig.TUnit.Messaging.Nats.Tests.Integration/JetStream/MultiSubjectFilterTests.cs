using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T055b-RED: compile-fail until T053-GREEN adds NatsJetStreamEventSender + NatsJetStreamListener.
public sealed class MultiSubjectFilterTests
{
    [Test]
    public async Task Consumer_WithFilterSubjects_ReceivesOnlyMatchingSubjects(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"multi-filter-{Guid.NewGuid():N}";
        var aSubject   = $"a.{streamName}";
        var bSubject   = $"b.{streamName}";
        var cSubject   = $"c.{streamName}";

        await fx.EnsureStreamAsync(streamName, [aSubject, bSubject, cSubject], ct: ct);  // CS1061 RED

        await using var sender   = new NatsJetStreamEventSender(fx.JetStream, aSubject);  // CS0246 RED
        await using var bSender  = new NatsJetStreamEventSender(fx.JetStream, bSubject);
        await using var cSender  = new NatsJetStreamEventSender(fx.JetStream, cSubject);

        // listener filters on a.* and b.* only — c.* messages must be invisible
        await using var listener = new NatsJetStreamListener(  // CS0246 RED
            fx.JetStream, streamName, aSubject, bSubject);
        await listener.StartAsync(ct);

        // Act
        await sender.SendAsync("msg-a", ct: ct);
        await bSender.SendAsync("msg-b", ct: ct);
        await cSender.SendAsync("msg-c", ct: ct);   // must NOT appear in listener

        // Assert — only a and b arrive
        await MessageAssert.Within(listener, TimeSpan.FromSeconds(15), expectedCount: 2, ct);
        await Assert.That(listener.Count).IsEqualTo(2);
    }
}
