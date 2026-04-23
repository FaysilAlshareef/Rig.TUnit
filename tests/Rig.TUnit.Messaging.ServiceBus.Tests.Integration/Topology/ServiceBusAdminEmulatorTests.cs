using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Topology;

/// <summary>
/// T012-RED: verifies ServiceBusAdministrationHelper against the live emulator.
/// References IServiceBusTopologyBuilder and ServiceBusAdministrationHelper
/// which do not exist yet — compile-fail RED until T012-GREEN.
/// </summary>
public sealed class ServiceBusAdminEmulatorTests
{
    [Test]
    public async Task CreateTopicAndSubscription_WithAdministrationHelper_CreatesIdempotently(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var topicName = $"admin-test-{Guid.NewGuid():N}";
        var subName = "admin-sub";

        // Act — create twice (idempotency)
        await helper.CreateTopicIfNotExistsAsync(topicName, ct);
        await helper.CreateTopicIfNotExistsAsync(topicName, ct);
        await helper.CreateSubscriptionIfNotExistsAsync(topicName, subName, ct);
        await helper.CreateSubscriptionIfNotExistsAsync(topicName, subName, ct);

        // Assert — topic and subscription exist
        await Assert.That((await admin.TopicExistsAsync(topicName, ct)).Value).IsTrue();
        await Assert.That((await admin.SubscriptionExistsAsync(topicName, subName, ct)).Value).IsTrue();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
    }

    [Test]
    public async Task CreateSubscriptionWithSqlFilter_WithAdministrationHelper_AppliesRule(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var topicName = $"filter-test-{Guid.NewGuid():N}";
        var subName = "eu-sub";

        // Act
        await helper.CreateTopicIfNotExistsAsync(topicName, ct);
        await helper.CreateSubscriptionWithRuleAsync(
            topicName,
            subName,
            ruleName: "eu-filter",
            filter: new SqlRuleFilter("Region = 'EU'"),
            ct: ct);

        // Assert — rule exists on the subscription
        var rules = admin.GetRulesAsync(topicName, subName, ct);
        var ruleNames = new List<string>();
        await foreach (var rule in rules)
            ruleNames.Add(rule.Name);

        await Assert.That(ruleNames.Contains("eu-filter")).IsTrue();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
    }

    [Test]
    public async Task CreateSessionEnabledSubscription_WithAdministrationHelper_SetsRequiresSession(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var topicName = $"session-test-{Guid.NewGuid():N}";
        var subName = "session-sub";

        // Act
        await helper.CreateTopicIfNotExistsAsync(topicName, ct);
        await helper.CreateSubscriptionIfNotExistsAsync(
            topicName,
            subName,
            requiresSession: true,
            ct: ct);

        // Assert
        var props = await admin.GetSubscriptionAsync(topicName, subName, ct);
        await Assert.That(props.Value.RequiresSession).IsTrue();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
    }
}
