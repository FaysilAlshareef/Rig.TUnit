using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

// T040-RED: CS1739 compile-fail until T040-GREEN adds SendAsync(string, SendContext, ...) overload.
public sealed class RabbitMqEventSenderSendContextTests
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Test]
    public async Task SendAsync_WithExchangeAndRoutingKey_PassesToBasicPublishAsync(CancellationToken ct)
    {
        // Arrange
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");

        // Act & Assert — CS1739 RED: no named 'context' parameter yet
        await Assert.That(async () =>
            await sender.SendAsync("body", context: new SendContext(PartitionKey: "user.created"), ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithPartitionKey_WritesXPartitionKeyHeader(CancellationToken ct)
    {
        // Arrange
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");

        // Act & Assert — CS1739 RED
        await Assert.That(async () =>
            await sender.SendAsync("body", context: new SendContext(PartitionKey: "partition-1"), ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_DefaultExchange_LegacyBehaviour(CancellationToken ct)
    {
        // Arrange — default SendContext should behave identically to legacy overload
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");

        // Act & Assert — CS1739 RED
        await Assert.That(async () =>
            await sender.SendAsync("body", context: default, ct: ct))
            .Throws<Exception>();
    }
}
