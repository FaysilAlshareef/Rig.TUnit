using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 guard-only tests for <see cref="KafkaListener"/>. Accepts bootstrap + topic +
/// groupId — Consumer is built lazily on StartAsync, so ctor null-guards fire before
/// any network call.
/// </summary>
public sealed class KafkaListenerTests
{
    private const string OfflineBootstrap = "192.0.2.1:9092";

    [Test]
    public async Task Ctor_NullBootstrap_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener(null!, "topic", "group"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceBootstrap_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener("   ", "topic", "group"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener(OfflineBootstrap, null!, "group"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener(OfflineBootstrap, "   ", "group"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullGroupId_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener(OfflineBootstrap, "topic", null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceGroupId_ThrowsArgumentException()
    {
        await Assert.That(() => new KafkaListener(OfflineBootstrap, "topic", "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new KafkaListener(OfflineBootstrap, "topic", "group"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Count_AfterConstruction_IsZero()
    {
        var listener = new KafkaListener(OfflineBootstrap, "topic", "group");

        await Assert.That(listener.Count).IsEqualTo(0);
    }
}
