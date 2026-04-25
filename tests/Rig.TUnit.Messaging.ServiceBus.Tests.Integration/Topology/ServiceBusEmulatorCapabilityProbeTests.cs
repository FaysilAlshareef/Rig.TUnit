using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Topology;

/// <summary>
/// Probes emulator v1.1.2 capabilities at the Azure.Messaging.ServiceBus 7.20.1 SDK level.
/// Results drive the capability table in docs/providers/service-bus.md and any
/// skip annotations in Phase 1 tests per C-004.
/// </summary>
public sealed class ServiceBusEmulatorCapabilityProbeTests
{
    private const string ProbeTopic = "capability-probe-topic";
    private const string ProbeNamespace = "sbemulatorns";

    [Test]
    public async Task Emulator_CreateSessionEnabledSubscription_Succeeds(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var subName = $"probe-session-{Guid.NewGuid():N}";

        // Act — create topic + session-enabled subscription idempotently
        if (!await admin.TopicExistsAsync(ProbeTopic, ct))
            await admin.CreateTopicAsync(ProbeTopic, ct);

        var subProps = new CreateSubscriptionOptions(ProbeTopic, subName)
        {
            RequiresSession = true
        };
        var result = await admin.CreateSubscriptionAsync(subProps, ct);

        // Assert
        await Assert.That(result.Value.RequiresSession).IsTrue();

        // Cleanup
        await admin.DeleteSubscriptionAsync(ProbeTopic, subName, ct);
    }

    [Test]
    public async Task Emulator_CreateSqlRuleFilter_Succeeds(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var subName = $"probe-sqlfilter-{Guid.NewGuid():N}";

        if (!await admin.TopicExistsAsync(ProbeTopic, ct))
            await admin.CreateTopicAsync(ProbeTopic, ct);

        var subProps = new CreateSubscriptionOptions(ProbeTopic, subName);
        await admin.CreateSubscriptionAsync(subProps, ct);

        // Act — create a SQL rule filter
        var rule = new CreateRuleOptions("eu-filter", new SqlRuleFilter("Region = 'EU'"));
        var ruleResult = await admin.CreateRuleAsync(ProbeTopic, subName, rule, ct);

        // Assert
        await Assert.That(ruleResult.Value.Name).IsEqualTo("eu-filter");
        await Assert.That(ruleResult.Value.Filter is SqlRuleFilter).IsTrue();

        // Cleanup
        await admin.DeleteSubscriptionAsync(ProbeTopic, subName, ct);
    }

    [Test]
    public async Task Emulator_CreatePartitionedTopic_Succeeds(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var topicName = $"probe-partitioned-{Guid.NewGuid():N}";

        // Act — emulator v1.1.2 may or may not honour EnablePartitioning
        var topicProps = new CreateTopicOptions(topicName)
        {
            EnablePartitioning = true
        };
        var result = await admin.CreateTopicAsync(topicProps, ct);

        // Assert — emulator accepts the request; actual partitioning is best-effort
        await Assert.That(result).IsNotNull();

        // Cleanup
        await admin.DeleteTopicAsync(topicName, ct);
    }

    [Test]
    public async Task Emulator_DeadLetterQueueReceiver_IsAccessible(CancellationToken ct)
    {
        // Arrange — uses existing pre-provisioned dlq-subscription
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);

        // Act — DLQ sub-queue receiver creation should not throw
        await using var dlqReceiver = client.CreateReceiver(
            "test-topic",
            "dlq-subscription",
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        // Assert — DLQ receiver is accessible; peek completes without throwing
        await dlqReceiver.PeekMessageAsync(cancellationToken: ct);
        await Assert.That(dlqReceiver.FullyQualifiedNamespace).IsNotEmpty();
    }
}
