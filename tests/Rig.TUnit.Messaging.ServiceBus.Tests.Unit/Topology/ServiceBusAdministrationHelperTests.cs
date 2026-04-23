using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit.Topology;

/// <summary>
/// T012-RED: guard tests for ServiceBusAdministrationHelper.
/// References ServiceBusAdministrationHelper which does not exist yet — compile-fail RED.
/// </summary>
public sealed class ServiceBusAdministrationHelperTests
{
    [Test]
    public async Task ServiceBusAdministrationHelper_NullClient_ThrowsArgumentNullException()
    {
        // Passes null — no real ServiceBusAdministrationClient created
        await Assert.That(() => new ServiceBusAdministrationHelper(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task CreateTopicIfNotExistsAsync_NullName_ThrowsArgumentException(CancellationToken ct)
    {
        // Passes null — guard fires before any network call
        var helper = new ServiceBusAdministrationHelper(null!);
        await Assert.That(async () => await helper.CreateTopicIfNotExistsAsync(null!, ct))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_NullTopic_ThrowsArgumentException(CancellationToken ct)
    {
        var helper = new ServiceBusAdministrationHelper(null!);
        await Assert.That(async () =>
            await helper.CreateSubscriptionIfNotExistsAsync(null!, "sub", ct))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_NullSubscription_ThrowsArgumentException(CancellationToken ct)
    {
        var helper = new ServiceBusAdministrationHelper(null!);
        await Assert.That(async () =>
            await helper.CreateSubscriptionIfNotExistsAsync("topic", null!, ct))
            .Throws<ArgumentException>();
    }
}
