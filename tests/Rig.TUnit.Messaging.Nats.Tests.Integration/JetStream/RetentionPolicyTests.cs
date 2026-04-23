using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T055c-RED: compile-fail until T051-GREEN adds NatsJetStreamFixture + T052-GREEN adds NatsJetStreamEventSender.
public sealed class RetentionPolicyTests
{
    [Test]
    public async Task Stream_WithMaxMsgs10_DropsOldestWhenLimitExceeded(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"retention-{Guid.NewGuid():N}";
        var subject    = $"retain.{streamName}";

        // Create stream with Limits retention capped at 10 messages
        await fx.EnsureStreamAsync(streamName, [subject], maxMsgs: 10, ct: ct);  // CS1061 RED

        await using var sender = new NatsJetStreamEventSender(fx.JetStream, subject);  // CS0246 RED

        // Act — send 15 messages; broker should purge oldest 5 automatically
        for (var i = 0; i < 15; i++)
        {
            await sender.SendAsync($"msg-{i}", ct: ct);
        }

        await Task.Delay(500, ct);

        // Assert — stream reports at most 10 messages stored
        var stream = await fx.GetStreamAsync(streamName, ct);  // CS1061 RED
        await Assert.That(stream.Info.State.Messages).IsLessThanOrEqualTo(10UL);
    }
}
