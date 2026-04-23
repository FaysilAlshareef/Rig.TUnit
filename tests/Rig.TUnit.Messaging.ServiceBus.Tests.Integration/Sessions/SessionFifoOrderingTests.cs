using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class SessionFifoOrderingTests
{
    private const string Topic = "test-topic";
    private const string SessionSubscription = "session-ordering-subscription";

    [Test]
    public async Task SendAsync_100MessagesAcross10SessionKeys_PerKeyMonotonicOrderingPreserved(CancellationToken ct)
    {
        // Arrange
        const int sessionCount = 10;
        const int messagesPerSession = 10;
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusSessionListener(client, Topic, SessionSubscription);

        // Act
        await listener.StartAsync(ct);

        for (var s = 0; s < sessionCount; s++)
        {
            var sessionKey = $"session-{s}";
            for (var i = 0; i < messagesPerSession; i++)
            {
                await sender.SendAsync(
                    $"msg-{s}-{i}",
                    context: new SendContext(SessionKey: sessionKey),
                    ct: ct);
            }
        }

        var totalExpected = sessionCount * messagesPerSession;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (listener.Captured.Count < totalExpected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(500, ct);
        }

        await listener.StopAsync(ct);

        // Assert
        await Assert.That(listener.Captured.Count).IsGreaterThanOrEqualTo(totalExpected);
        OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey!, m => m.SequenceNumber);
    }
}
