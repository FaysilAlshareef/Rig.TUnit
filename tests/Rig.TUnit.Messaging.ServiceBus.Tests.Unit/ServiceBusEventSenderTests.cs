using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 backfill guard-only tests for <see cref="ServiceBusEventSender"/>. The sender
/// is constructed with a <see cref="TestClients.Offline"/> client pointed at TEST-NET-1,
/// so no network call is issued.
/// </summary>
public sealed class ServiceBusEventSenderTests
{
    [Test]
    public async Task Ctor_NullClient_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ServiceBusEventSender(null!, "topic"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusEventSender(TestClients.Offline(), null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceTopic_ThrowsArgumentException()
    {
        await Assert.That(() => new ServiceBusEventSender(TestClients.Offline(), "   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_ValidArgs_DoesNotThrow()
    {
        await Assert.That(() => new ServiceBusEventSender(TestClients.Offline(), "topic"))
            .ThrowsNothing();
    }

    [Test]
    public async Task DisposeAsync_BeforeAnySend_IsSafe()
    {
        var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");

        await Assert.That(async () => await sender.DisposeAsync()).ThrowsNothing();
    }
}
