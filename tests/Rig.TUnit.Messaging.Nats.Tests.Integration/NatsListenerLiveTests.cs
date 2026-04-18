using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration;

/// <summary>
/// FR-035 live container round-trip for <see cref="NatsEventSender"/> → <see cref="NatsListener"/>.
/// Requires Docker.
/// </summary>
public sealed class NatsListenerLiveTests
{
    [Test]
    public async Task Roundtrip_SendOneMessage_ListenerCapturesIt()
    {
        var fx = await SharedNatsFixture.GetAsync();
        var subject = $"rig.test.{Guid.NewGuid():N}";

        var listener = new NatsListener(fx.ConnectionString, subject);
        await listener.StartAsync(CancellationToken.None);

        await Task.Yield();

        await using var sender = new NatsEventSender(fx.ConnectionString, subject);
        await sender.SendAsync("{\"orderId\":42}", correlationId: "abc-123");

        await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1);

        await listener.DisposeAsync();

        await Assert.That(listener.Count).IsGreaterThanOrEqualTo(1);
    }
}
