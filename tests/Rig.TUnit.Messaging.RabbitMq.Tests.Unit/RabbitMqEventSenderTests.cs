using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class RabbitMqEventSenderTests
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Test]
    public async Task Ctor_NullConnectionString_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqEventSender(null!, "queue"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceConnectionString_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqEventSender("   ", "queue"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullQueue_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqEventSender(OfflineUri, null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceQueue_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqEventSender(OfflineUri, "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new RabbitMqEventSender(OfflineUri, "queue"))
            .ThrowsNothing();
    }

    [Test]
    public async Task DisposeAsync_BeforeAnySend_IsSafe()
    {
        var sender = new RabbitMqEventSender(OfflineUri, "queue");

        await Assert.That(async () => await sender.DisposeAsync()).ThrowsNothing();
    }
}
