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
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var subName = $"dlq-drive-{Guid.NewGuid():N}";

        var subOptions = new CreateSubscriptionOptions(Topic, subName) { MaxDeliveryCount = 3 };
        await admin.CreateSubscriptionAsync(subOptions, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, subName);
        await using var dlqProbe = new ServiceBusDeadLetterProbe(client, Topic, subName);

        // Act — exhaust delivery attempts to trigger DLQ
        await sender.SendAsync($"dlq-drive-{testId}", correlationId: testId, ct: ct);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
            if (msg is null) break;
            if (msg.CorrelationId != testId)
            {
                await receiver.CompleteMessageAsync(msg, ct);
                continue;
            }
            await receiver.AbandonMessageAsync(msg, cancellationToken: ct);
        }

        // Assert — message appears on DLQ with MaxDeliveryCountExceeded reason
        await DeadLetterAssert.HasMessage(dlqProbe, "MaxDeliveryCountExceeded", ct);

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, subName, ct);
    }
}
