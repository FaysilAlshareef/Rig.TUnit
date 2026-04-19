using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 backfill guard-only tests for <see cref="ServiceBusListener"/>. The listener
/// is constructed with a <see cref="TestClients.Offline"/> client pointed at TEST-NET-1,
/// so even the "happy" constructor path never issues a network call.
/// </summary>
public sealed class ServiceBusListenerTests
{
    [Test]
    public async Task Ctor_NullClient_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ServiceBusListener(null!, "topic", "sub"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusListener(TestClients.Offline(), null!, "sub"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusListener(TestClients.Offline(), "   ", "sub"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_NullSubscription_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusListener(TestClients.Offline(), "topic", null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceSubscription_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusListener(TestClients.Offline(), "topic", "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new ServiceBusListener(TestClients.Offline(), "topic", "sub"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Count_AfterConstruction_IsZero()
    {
        var listener = new ServiceBusListener(TestClients.Offline(), "topic", "sub");

        await Assert.That(listener.Count).IsEqualTo(0);
    }
}
