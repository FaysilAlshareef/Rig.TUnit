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
    public async Task CreateTopicIfNotExistsAsync_NullName_ThrowsException(CancellationToken ct)
    {
        // Constructor guard and method guard both throw — wrap both in the lambda
        await Assert.That(async () =>
        {
            var helper = new ServiceBusAdministrationHelper(null!);
            await helper.CreateTopicIfNotExistsAsync(null!, ct);
        }).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_NullTopic_ThrowsException(CancellationToken ct)
    {
        await Assert.That(async () =>
        {
            var helper = new ServiceBusAdministrationHelper(null!);
            await helper.CreateSubscriptionIfNotExistsAsync(null!, "sub", ct);
        }).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_NullSubscription_ThrowsException(CancellationToken ct)
    {
        await Assert.That(async () =>
        {
            var helper = new ServiceBusAdministrationHelper(null!);
            await helper.CreateSubscriptionIfNotExistsAsync("topic", null!, ct);
        }).Throws<ArgumentNullException>();
    }
}
