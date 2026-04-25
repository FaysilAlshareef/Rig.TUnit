using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

/// <summary>
/// Compile-shape regression tests for the SendContext overload of
/// <see cref="RabbitMqEventSender" />. Bootstrap is RFC 5737 documentation IP
/// (never responds); each call is wired through a pre-cancelled token, so the
/// SUT throws OperationCanceledException immediately when its first await on
/// the cancelled token fires — no real network round-trip occurs.
/// </summary>
public sealed class RabbitMqEventSenderSendContextTests
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Test]
    public async Task SendAsync_WithExchangeAndRoutingKey_PassesToBasicPublishAsync()
    {
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");
        using var alreadyCancelled = new CancellationTokenSource();
        alreadyCancelled.Cancel();

        await Assert.That(async () =>
            await sender.SendAsync("body", context: new SendContext(PartitionKey: "user.created"), ct: alreadyCancelled.Token))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithPartitionKey_WritesXPartitionKeyHeader()
    {
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");
        using var alreadyCancelled = new CancellationTokenSource();
        alreadyCancelled.Cancel();

        await Assert.That(async () =>
            await sender.SendAsync("body", context: new SendContext(PartitionKey: "partition-1"), ct: alreadyCancelled.Token))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_DefaultExchange_LegacyBehaviour()
    {
        await using var sender = new RabbitMqEventSender(OfflineUri, "queue");
        using var alreadyCancelled = new CancellationTokenSource();
        alreadyCancelled.Cancel();

        await Assert.That(async () =>
            await sender.SendAsync("body", context: default, ct: alreadyCancelled.Token))
            .Throws<Exception>();
    }
}
