using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Topology;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class ServiceBusSessionListenerTests
{
    private const string Topic = "test-topic";

    [Test]
    public async Task SessionListener_10Sessions10Messages_CapturesSessionKeyOnEveryMessage(CancellationToken ct)
    {
        // Arrange
        const int sessionCount = 10;
        const int messagesPerSession = 10;
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var subName = $"sess-cap-{Guid.NewGuid():N}";

        await helper.CreateSubscriptionIfNotExistsAsync(Topic, subName, requiresSession: true, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusSessionListener(client, Topic, subName);

        // Act
        await listener.StartAsync(ct);

        for (var s = 0; s < sessionCount; s++)
        {
            var sessionKey = $"cap-session-{s}";
            for (var i = 0; i < messagesPerSession; i++)
            {
                await sender.SendAsync(
                    $"cap-msg-{s}-{i}",
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

        // Assert — every message has a populated SessionKey
        await Assert.That(listener.Captured.Count).IsGreaterThanOrEqualTo(totalExpected);
        var missingSessionKey = listener.Captured.Where(m => m.SessionKey is null).ToList();
        await Assert.That(missingSessionKey.Count).IsEqualTo(0);

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, subName, ct);
    }

    [Test]
    public async Task SessionListener_ParallelSessions_EachSessionHandlerSeesOwnMessagesInOrder(CancellationToken ct)
    {
        // Arrange — 5 sessions × 20 messages, interleaved
        const int sessionCount = 5;
        const int messagesPerSession = 20;
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var subName = $"sess-ord-{Guid.NewGuid():N}";

        await helper.CreateSubscriptionIfNotExistsAsync(Topic, subName, requiresSession: true, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusSessionListener(client, Topic, subName);

        // Act — interleave messages across sessions
        await listener.StartAsync(ct);

        for (var i = 0; i < messagesPerSession; i++)
        {
            for (var s = 0; s < sessionCount; s++)
            {
                await sender.SendAsync(
                    $"ord-msg-{s}-{i}",
                    context: new SendContext(SessionKey: $"ord-session-{s}"),
                    ct: ct);
            }
        }

        var totalExpected = sessionCount * messagesPerSession;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (listener.Captured.Count < totalExpected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(500, ct);
        }

        await listener.StopAsync(ct);

        // Assert — per-session ordering preserved
        await Assert.That(listener.Captured.Count).IsGreaterThanOrEqualTo(totalExpected);
        OrderingAssert.PerKeyMonotonic(listener, m => m.SessionId!, m => m.SequenceNumber);

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, subName, ct);
    }
}
