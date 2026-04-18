using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 guard-only tests for <see cref="KafkaEventSender"/>. Producer is built lazily —
/// ctor null-guards fire before any network call.
/// </summary>
public sealed class KafkaEventSenderTests
{
    private const string OfflineBootstrap = "192.0.2.1:9092";

    [Test]
    public async Task Ctor_NullBootstrap_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaEventSender(null!, "topic"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceBootstrap_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaEventSender("   ", "topic"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaEventSender(OfflineBootstrap, null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaEventSender(OfflineBootstrap, "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new KafkaEventSender(OfflineBootstrap, "topic"))
            .ThrowsNothing();
    }

    [Test]
    public async Task DisposeAsync_BeforeAnySend_IsSafe()
    {
        var sender = new KafkaEventSender(OfflineBootstrap, "topic");

        await Assert.That(async () => await sender.DisposeAsync()).ThrowsNothing();
    }
}
