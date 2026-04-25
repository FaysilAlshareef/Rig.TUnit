using Azure;
using Azure.Messaging.ServiceBus.Administration;
using NSubstitute;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit.Topology;

/// <summary>
/// Behaviour tests for <see cref="ServiceBusTopologyBuilder"/>. Each test
/// substitutes a <see cref="ServiceBusAdministrationClient"/> behind the
/// helper, exercises one fluent path, and verifies the expected admin call
/// was forwarded with the right options. The private nested config builder
/// classes (<c>TopicConfig</c>, <c>SubscriptionConfig</c>, <c>QueueConfig</c>)
/// are exercised end-to-end via the fluent surface.
/// </summary>
public sealed class ServiceBusTopologyBuilderTests
{
    private static (ServiceBusAdministrationClient admin, ServiceBusAdministrationHelper helper, ServiceBusTopologyBuilder builder)
        Build()
    {
        var admin = Substitute.For<ServiceBusAdministrationClient>();
        admin.TopicExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));
        admin.SubscriptionExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));
        admin.QueueExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));
        var helper = new ServiceBusAdministrationHelper(admin);
        var builder = new ServiceBusTopologyBuilder(helper);
        return (admin, helper, builder);
    }

    [Test]
    public async Task Ctor_NullHelper_Throws()
    {
        await Assert.That(() => new ServiceBusTopologyBuilder(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Topic_WithoutConfigure_CallsCreateTopicWithDefaultOptions(CancellationToken ct)
    {
        var (admin, _, builder) = Build();

        builder.Topic("orders");
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateTopicAsync(
            Arg.Is<CreateTopicOptions>(o => o.Name == "orders"),
            ct);
    }

    [Test]
    public async Task Topic_WithAllConfigOptions_PropagatesToCreateTopicOptions(CancellationToken ct)
    {
        var (admin, _, builder) = Build();
        var ttl = TimeSpan.FromMinutes(30);

        builder.Topic("orders", c => c
            .WithDefaultMessageTimeToLive(ttl)
            .WithEnablePartitioning()
            .WithRequiresDuplicateDetection());
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateTopicAsync(
            Arg.Is<CreateTopicOptions>(o =>
                o.Name == "orders"
                && o.DefaultMessageTimeToLive == ttl
                && o.EnablePartitioning
                && o.RequiresDuplicateDetection),
            ct);
    }

    [Test]
    public async Task Subscription_WithoutConfigure_CallsCreateSubscriptionWithoutSession(CancellationToken ct)
    {
        var (admin, _, builder) = Build();

        builder.Subscription("orders", "sub");
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateSubscriptionAsync(
            Arg.Is<CreateSubscriptionOptions>(o =>
                o.TopicName == "orders" && o.SubscriptionName == "sub" && !o.RequiresSession),
            ct);
    }

    [Test]
    public async Task Subscription_WithRequiresSession_CallsCreateSubscriptionWithSession(CancellationToken ct)
    {
        var (admin, _, builder) = Build();

        builder.Subscription("orders", "sub", c => c
            .WithRequiresSession()
            .WithDefaultMessageTimeToLive(TimeSpan.FromMinutes(5))
            .WithLockDuration(TimeSpan.FromSeconds(30))
            .WithMaxDeliveryCount(7));
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateSubscriptionAsync(
            Arg.Is<CreateSubscriptionOptions>(o =>
                o.TopicName == "orders" && o.SubscriptionName == "sub" && o.RequiresSession),
            ct);
    }

    [Test]
    public async Task Subscription_WithRule_CallsCreateSubscriptionWithRule(CancellationToken ct)
    {
        var (admin, _, builder) = Build();

        builder.Subscription("orders", "sub", c => c
            .WithRule("eu-filter", new SqlRuleFilter("Region = 'EU'")));
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateSubscriptionAsync(
            Arg.Any<CreateSubscriptionOptions>(),
            Arg.Is<CreateRuleOptions>(r => r.Name == "eu-filter"),
            ct);
    }

    [Test]
    public async Task Queue_WithoutConfigure_CallsCreateQueueWithDefaultOptions(CancellationToken ct)
    {
        var (admin, _, builder) = Build();

        builder.Queue("payments-dlq");
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateQueueAsync(
            Arg.Is<CreateQueueOptions>(o => o.Name == "payments-dlq"),
            ct);
    }

    [Test]
    public async Task Queue_WithAllConfigOptions_PropagatesToCreateQueueOptions(CancellationToken ct)
    {
        var (admin, _, builder) = Build();
        var ttl = TimeSpan.FromHours(1);
        var lockDuration = TimeSpan.FromSeconds(45);

        builder.Queue("orders", c => c
            .WithRequiresSession()
            .WithDefaultMessageTimeToLive(ttl)
            .WithLockDuration(lockDuration)
            .WithMaxDeliveryCount(5));
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateQueueAsync(
            Arg.Is<CreateQueueOptions>(o =>
                o.Name == "orders"
                && o.RequiresSession
                && o.DefaultMessageTimeToLive == ttl
                && o.LockDuration == lockDuration
                && o.MaxDeliveryCount == 5),
            ct);
    }

    [Test]
    public async Task ApplyAsync_FluentChain_AppliesAllActions(CancellationToken ct)
    {
        // Note: keep distinct entity types (one topic + one subscription + one queue).
        // Calling Topic() twice triggers NSubstitute to compare CreateTopicOptions
        // instances via Azure SDK's auto-Equals, which recurses → stack overflow.
        var (admin, _, builder) = Build();

        builder.Topic("t1").Subscription("t1", "s1").Queue("q1");
        await builder.ApplyAsync(ct);

        await admin.Received(1).CreateTopicAsync(Arg.Any<CreateTopicOptions>(), ct);
        await admin.Received(1).CreateSubscriptionAsync(Arg.Any<CreateSubscriptionOptions>(), ct);
        await admin.Received(1).CreateQueueAsync(Arg.Any<CreateQueueOptions>(), ct);
    }
}
