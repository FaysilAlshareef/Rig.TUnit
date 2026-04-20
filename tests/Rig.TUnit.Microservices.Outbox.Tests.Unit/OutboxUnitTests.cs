namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxUnitTests
{
    [Test]
    public async Task OutboxMessage_Ctor_RoundTripsRequiredFields()
    {
        var now = DateTimeOffset.UtcNow;
        var msg = new OutboxMessage(
            Id: Guid.NewGuid(),
            AggregateId: "agg",
            EventType: "OrderPlaced",
            Payload: "{}",
            OccurredAt: now);

        await Assert.That(msg.AggregateId).IsEqualTo("agg");
        await Assert.That(msg.EventType).IsEqualTo("OrderPlaced");
        await Assert.That(msg.DeliveryAttempts).IsEqualTo(0);
        await Assert.That(msg.RelayedAt).IsNull();
    }

    [Test]
    public async Task OutboxMessage_WithOverride_ReturnsNewRecord()
    {
        var msg = new OutboxMessage(Guid.NewGuid(), "agg", "E", "{}", DateTimeOffset.UtcNow);
        var relayed = msg with { RelayedAt = DateTimeOffset.UtcNow, DeliveryAttempts = 1 };

        await Assert.That(relayed.RelayedAt).IsNotNull();
        await Assert.That(relayed.DeliveryAttempts).IsEqualTo(1);
        await Assert.That(msg.RelayedAt).IsNull();
    }

    [Test]
    public async Task OutboxEventEnvelope_Ctor_ExposesMessageId()
    {
        var id = Guid.NewGuid();
        var env = new OutboxEventEnvelope(id, "agg", "E", "{}", DateTimeOffset.UtcNow, null, null, null);

        await Assert.That(env.MessageId).IsEqualTo(id);
    }

    [Test]
    public async Task OutboxMessage_ValueEquality_IsByFieldValues()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var a = new OutboxMessage(id, "agg", "E", "{}", now);
        var b = new OutboxMessage(id, "agg", "E", "{}", now);

        await Assert.That(a).IsEqualTo(b);
    }
}
