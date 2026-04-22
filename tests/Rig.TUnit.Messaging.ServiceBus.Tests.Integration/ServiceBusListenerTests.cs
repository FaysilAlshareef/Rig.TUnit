using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration;

public sealed class ServiceBusListenerTests
{
    private const string Topic = "test-topic";

    // Every test owns its own subscription so that parallel processor-based
    // and raw-receiver tests cannot steal each other's messages.
    // All subscriptions are pre-provisioned in TestInfrastructure/service-bus-config.json.
    private const string Subscription      = "test-subscription";   // contract tests
    private const string DeliverSubscription = "deliver-subscription";
    private const string HeadersSubscription = "headers-subscription";
    private const string AckSubscription   = "ack-subscription";
    private const string NackSubscription  = "nack-subscription";
    private const string DlqSubscription   = "dlq-subscription";
    private const string RetrySubscription = "retry-subscription";

    [Test]
    public async Task ServiceBusEventSender_Send_DeliversMessageToQueue(CancellationToken ct)
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusListener(client, Topic, DeliverSubscription);

        // Act
        await listener.StartAsync(ct);
        await sender.SendAsync($"deliver-{testId}", correlationId: testId, ct: ct);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (!listener.Captured.Any(m => m.CorrelationId == testId) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(200, ct);
        }
        await listener.StopAsync(ct);

        // Assert
        await Assert.That(listener.Captured.Any(m => m.CorrelationId == testId)).IsTrue();
    }

    [Test]
    public async Task ServiceBusEventSender_Send_WithProperties_SetsHeaders(CancellationToken ct)
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusListener(client, Topic, HeadersSubscription);
        var extra = new Dictionary<string, string> { ["x-test-id"] = testId };

        // Act
        await listener.StartAsync(ct);
        await sender.SendAsync($"headers-{testId}", additionalHeaders: extra, ct: ct);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (!listener.Captured.Any(m => m.Headers.TryGetValue("x-test-id", out var v) && v == testId)
               && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(200, ct);
        }
        await listener.StopAsync(ct);

        // Assert
        var captured = listener.Captured.FirstOrDefault(m =>
            m.Headers.TryGetValue("x-test-id", out var v) && v == testId);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Headers["x-test-id"]).IsEqualTo(testId);
    }

    [Test]
    public async Task ServiceBusListener_Ack_CompletesMessage(CancellationToken ct)
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusListener(client, Topic, AckSubscription);

        // Act
        await listener.StartAsync(ct);
        await sender.SendAsync($"ack-{testId}", correlationId: testId, ct: ct);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (!listener.Captured.Any(m => m.CorrelationId == testId) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(200, ct);
        }
        await listener.StopAsync(ct);

        // Assert — listener auto-ACKed the message
        await Assert.That(listener.Captured.Any(m => m.CorrelationId == testId)).IsTrue();
    }

    [Test]
    public async Task ServiceBusListener_Nack_AbandonsMessage(CancellationToken ct)
    {
        // Arrange — NackSubscription is exclusively owned by this test so no other
        // receiver or processor can intercept its messages during parallel test runs.
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, NackSubscription);

        // Act — send, receive with peek-lock, then abandon via raw SDK
        await sender.SendAsync($"nack-{testId}", correlationId: testId, ct: ct);
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
        await Assert.That(msg).IsNotNull();
        await receiver.AbandonMessageAsync(msg!, cancellationToken: ct);

        // Assert — same message redelivered with incremented delivery count
        var redelivered = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
        await Assert.That(redelivered).IsNotNull();
        await Assert.That(redelivered!.MessageId).IsEqualTo(msg!.MessageId);
        await Assert.That(redelivered.DeliveryCount).IsGreaterThan(1);
        await receiver.CompleteMessageAsync(redelivered, ct);
    }

    [Test]
    public async Task ServiceBusListener_DeadLetter_MovesMessageToDeadLetterQueue(CancellationToken ct)
    {
        // Arrange — DlqSubscription is exclusively owned by this test.
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, DlqSubscription);
        await using var dlqReceiver = client.CreateReceiver(Topic, DlqSubscription,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        // Act — send, receive, then dead-letter via raw SDK
        await sender.SendAsync($"dlq-{testId}", correlationId: testId, ct: ct);
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
        await Assert.That(msg).IsNotNull();
        await receiver.DeadLetterMessageAsync(msg!, deadLetterReason: "TestReason", cancellationToken: ct);

        // Assert — our exact message appears in the dead-letter sub-queue.
        // PeekMessageAsync reads without acquiring a peek-lock, so there is nothing
        // to settle and no MessageLockLost on slow/virtualised Docker runtimes.
        // DeadLetterMessageAsync completed above, so the message is available immediately.
        var dlqMsg = await dlqReceiver.PeekMessageAsync(cancellationToken: ct);
        await Assert.That(dlqMsg).IsNotNull();
        await Assert.That(dlqMsg!.MessageId).IsEqualTo(msg!.MessageId);
    }

    [Test]
    public async Task ServiceBusListener_Retry_RedeliversAfterDelay(CancellationToken ct)
    {
        // Arrange — RetrySubscription is exclusively owned by this test so no other
        // receiver or processor can intercept its messages during parallel test runs.
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var receiver = client.CreateReceiver(Topic, RetrySubscription);

        // Act — send, receive, abandon once
        await sender.SendAsync($"retry-{testId}", correlationId: testId, ct: ct);
        var first = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
        await Assert.That(first).IsNotNull();
        var firstDeliveryCount = first!.DeliveryCount;
        await receiver.AbandonMessageAsync(first, cancellationToken: ct);

        // Assert — same message redelivered with incremented delivery count
        var second = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), ct);
        await Assert.That(second).IsNotNull();
        await Assert.That(second!.MessageId).IsEqualTo(first.MessageId);
        await Assert.That(second.DeliveryCount).IsGreaterThan(firstDeliveryCount);
        await receiver.CompleteMessageAsync(second, ct);
    }
}
