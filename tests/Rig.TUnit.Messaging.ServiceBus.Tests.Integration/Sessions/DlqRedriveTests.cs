using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class DlqRedriveTests
{
    private const string Topic = "test-topic";
    // Pre-provisioned with MaxDeliveryCount=3 so abandoning 3 times moves message to DLQ.
    private const string DlqDriveSubscription = "dlq-drive-subscription";

    [Test]
    public async Task SendAsync_MessageAbandonedPastMaxDeliveryCount_AppearsOnDeadLetterQueue(CancellationToken ct)
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, DlqDriveSubscription);
        // ServiceBusDeadLetterProbe is created by T012-GREEN (compile-fail RED driver)
        await using var dlqProbe = new ServiceBusDeadLetterProbe(client, Topic, DlqDriveSubscription);

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
    }
}
