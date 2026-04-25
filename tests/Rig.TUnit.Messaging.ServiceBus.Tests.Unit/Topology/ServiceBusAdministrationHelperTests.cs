using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NSubstitute;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit.Topology;

/// <summary>
/// Behaviour tests for <see cref="ServiceBusAdministrationHelper"/> using a
/// substituted <see cref="ServiceBusAdministrationClient"/>. Exercises the
/// idempotent paths (early-return when entity exists) and the race-safe
/// catch branches that swallow <c>MessagingEntityAlreadyExists</c>.
/// </summary>
public sealed class ServiceBusAdministrationHelperTests
{
    [Test]
    public async Task ServiceBusAdministrationHelper_NullClient_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ServiceBusAdministrationHelper(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task CreateTopicIfNotExistsAsync_NullName_ThrowsException(CancellationToken ct)
    {
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

    // ─── Idempotent / race-safe behavior on real (mocked) admin client ─────

    [Test]
    public async Task CreateTopicIfNotExistsAsync_WhenTopicExists_DoesNotCallCreate(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.TopicExistsAsync("orders", ct).Returns(Response.FromValue(true, Substitute.For<Response>()));
        var helper = new ServiceBusAdministrationHelper(admin);

        await helper.CreateTopicIfNotExistsAsync("orders", ct);

        await admin.DidNotReceive().CreateTopicAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateTopicIfNotExistsAsync_WhenTopicMissing_CallsCreate(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.TopicExistsAsync("orders", ct).Returns(Response.FromValue(false, Substitute.For<Response>()));
        var helper = new ServiceBusAdministrationHelper(admin);

        await helper.CreateTopicIfNotExistsAsync("orders", ct);

        await admin.Received(1).CreateTopicAsync("orders", ct);
    }

    [Test]
    public async Task CreateTopicIfNotExistsAsync_WhenCreateRacesAndTopicAppears_SwallowsConflict(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.TopicExistsAsync("orders", ct).Returns(Response.FromValue(false, Substitute.For<Response>()));
        admin.CreateTopicAsync("orders", ct)
            .Returns<Task<Response<TopicProperties>>>(_ =>
                throw new ServiceBusException(
                    "race",
                    ServiceBusFailureReason.MessagingEntityAlreadyExists));
        var helper = new ServiceBusAdministrationHelper(admin);

        await Assert.That(async () => await helper.CreateTopicIfNotExistsAsync("orders", ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task CreateTopicIfNotExistsAsync_WhenCreateThrowsOtherFailure_RethrowsServiceBusException(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.TopicExistsAsync("orders", ct).Returns(Response.FromValue(false, Substitute.For<Response>()));
        admin.CreateTopicAsync("orders", ct)
            .Returns<Task<Response<TopicProperties>>>(_ =>
                throw new ServiceBusException(
                    "broker-down",
                    ServiceBusFailureReason.ServiceBusy));
        var helper = new ServiceBusAdministrationHelper(admin);

        await Assert.That(async () => await helper.CreateTopicIfNotExistsAsync("orders", ct))
            .Throws<ServiceBusException>();
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_WhenSubscriptionExists_DoesNotCallCreate(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.SubscriptionExistsAsync("orders", "sub", ct).Returns(Response.FromValue(true, Substitute.For<Response>()));
        var helper = new ServiceBusAdministrationHelper(admin);

        await helper.CreateSubscriptionIfNotExistsAsync("orders", "sub", ct);

        await admin.DidNotReceive().CreateSubscriptionAsync(Arg.Any<CreateSubscriptionOptions>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateSubscriptionIfNotExistsAsync_WhenCreateRaces_SwallowsConflict(CancellationToken ct)
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.SubscriptionExistsAsync("orders", "sub", ct).Returns(Response.FromValue(false, Substitute.For<Response>()));
        admin.CreateSubscriptionAsync(Arg.Any<CreateSubscriptionOptions>(), ct)
            .Returns<Task<Response<SubscriptionProperties>>>(_ =>
                throw new ServiceBusException(
                    "race",
                    ServiceBusFailureReason.MessagingEntityAlreadyExists));
        var helper = new ServiceBusAdministrationHelper(admin);

        await Assert.That(async () => await helper.CreateSubscriptionIfNotExistsAsync("orders", "sub", ct))
            .ThrowsNothing();
    }
}
