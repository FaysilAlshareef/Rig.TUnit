using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class NatsEventSenderTests
{
    private const string OfflineUrl = "nats://192.0.2.1:4222";

    [Test]
    public async Task Ctor_NullUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsEventSender(null!, "subject"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsEventSender("   ", "subject"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullSubject_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsEventSender(OfflineUrl, null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceSubject_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsEventSender(OfflineUrl, "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new NatsEventSender(OfflineUrl, "subject"))
            .ThrowsNothing();
    }

    [Test]
    public async Task DisposeAsync_BeforeAnySend_IsSafe()
    {
        var sender = new NatsEventSender(OfflineUrl, "subject");

        await Assert.That(async () => await sender.DisposeAsync()).ThrowsNothing();
    }
}
