using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

/// <summary>
/// FR-035 guard-only tests for <see cref="RabbitMqListener"/>. Connection is created lazily
/// on StartAsync — ctor null/whitespace-guards fire before any network call.
/// </summary>
public sealed class RabbitMqListenerTests
{
    private const string OfflineUri = "amqp://guest:guest@192.0.2.1:5672";

    [Test]
    public async Task Ctor_NullConnectionString_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqListener(null!, "queue"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceConnectionString_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqListener("   ", "queue"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullQueue_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqListener(OfflineUri, null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceQueue_ThrowsArgumentException()
    {
        await Assert.That(() => new RabbitMqListener(OfflineUri, "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new RabbitMqListener(OfflineUri, "queue"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Count_AfterConstruction_IsZero()
    {
        var listener = new RabbitMqListener(OfflineUri, "queue");

        await Assert.That(listener.Count).IsEqualTo(0);
    }
}
