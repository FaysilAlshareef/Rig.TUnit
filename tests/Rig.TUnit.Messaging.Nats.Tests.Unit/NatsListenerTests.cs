using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

/// <summary>
/// FR-035 guard-only tests for <see cref="NatsListener"/>. Connection is established lazily
/// on StartAsync — ctor null/whitespace-guards fire before any network call.
/// </summary>
public sealed class NatsListenerTests
{
    private const string OfflineUrl = "nats://192.0.2.1:4222";

    [Test]
    public async Task Ctor_NullUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsListener(null!, "subject"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsListener("   ", "subject"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullSubject_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsListener(OfflineUrl, null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceSubject_ThrowsArgumentException()
    {
        await Assert.That(() => new NatsListener(OfflineUrl, "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new NatsListener(OfflineUrl, "subject"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Count_AfterConstruction_IsZero()
    {
        var listener = new NatsListener(OfflineUrl, "subject");

        await Assert.That(listener.Count).IsEqualTo(0);
    }
}
