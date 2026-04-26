using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class DlqRedriveTests
{
    private const string Topic = "test-topic";

    [Test]
    public async Task SendAsync_MessageAbandonedPastMaxDeliveryCount_AppearsOnDeadLetterQueue(CancellationToken ct)
    {
        // Arrange — create subscription with MaxDeliveryCount=3 so 3 abandons move message to DLQ
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var subName = $"dlq-drive-{Guid.NewGuid():N}";

        var subOptions = new CreateSubscriptionOptions(Topic, subName) { MaxDeliveryCount = 3 };
        await admin.CreateSubscriptionAsync(subOptions, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, subName);
        await using var dlqProbe = new ServiceBusDeadLetterProbe(client, Topic, subName);

        // Act — exhaust delivery attempts to trigger DLQ.
        //
        // The subscription is freshly created on the shared `test-topic`, so
        // every message any other parallel test publishes to that topic is
        // fanned out to it as well. A loop bound by raw `attempt < N` would
        // spend its iterations completing foreign messages and run out before
        // our test message is abandoned MaxDeliveryCount=3 times — the
        // broker then never auto-DLQs and the probe times out.
        //
        // Track abandons of OUR message specifically; foreign messages get
        // completed without consuming the abandon budget. A 60 s wall-clock
        // deadline guards against pathological busy-loop behaviour.
        await sender.SendAsync($"dlq-drive-{testId}", correlationId: testId, ct: ct);

        const int requiredAbandons = 3;
        var abandons = 0;
        var loopDeadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (abandons <= requiredAbandons && DateTimeOffset.UtcNow < loopDeadline)
        {
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), ct);
            if (msg is null)
            {
                // Either the broker has DLQ'd our message (abandons reached
                // MaxDeliveryCount) or there's nothing on the subscription
                // right now. If we already hit the abandon target, stop.
                if (abandons >= requiredAbandons) break;
                continue;
            }
            if (msg.CorrelationId != testId)
            {
                await receiver.CompleteMessageAsync(msg, ct);
                continue;
            }
            await receiver.AbandonMessageAsync(msg, cancellationToken: ct);
            abandons++;
        }

        // Assert — message appears on DLQ with MaxDeliveryCountExceeded reason.
        // Probe window bumped to 90s: the Microsoft Service Bus emulator
        // (servicebus-emulator + SQL Edge) takes longer than real Azure
        // Service Bus to materialise the auto-DLQ after delivery-count
        // exhaustion; 60s default is right for production, 90s gives a
        // comfortable margin on emulator-backed CI.
        await DeadLetterAssert.HasMessage(
            dlqProbe,
            "MaxDeliveryCountExceeded",
            timeout: TimeSpan.FromSeconds(90),
            ct: ct);

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, subName, ct);
    }
}
