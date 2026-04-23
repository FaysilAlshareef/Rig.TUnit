using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class SqlFilterTests
{
    private const string Topic = "test-topic";

    [Test]
    public async Task SendAsync_WithRegionLabel_OnlyEuFilterSubscriptionReceivesEuMessages(CancellationToken ct)
    {
        // Arrange — EU sub has SqlRuleFilter("Region='EU'"); all-sub receives everything
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var euSubName = $"sql-eu-{Guid.NewGuid():N}";
        var allSubName = $"sql-all-{Guid.NewGuid():N}";

        await helper.CreateSubscriptionWithRuleAsync(
            Topic, euSubName, "eu-filter",
            new SqlRuleFilter("Region = 'EU'"), ct: ct);
        await helper.CreateSubscriptionIfNotExistsAsync(Topic, allSubName, requiresSession: false, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var euListener = new ServiceBusListener(client, Topic, euSubName);
        await using var allListener = new ServiceBusListener(client, Topic, allSubName);

        var euHeaders = new Dictionary<string, string> { ["Region"] = "EU", ["x-test-id"] = testId };
        var usHeaders = new Dictionary<string, string> { ["Region"] = "US", ["x-test-id"] = testId };

        // Act
        await euListener.StartAsync(ct);
        await allListener.StartAsync(ct);

        await sender.SendAsync(
            $"eu-msg-{testId}",
            context: new SendContext(),
            additionalHeaders: euHeaders,
            ct: ct);

        await sender.SendAsync(
            $"us-msg-{testId}",
            context: new SendContext(),
            additionalHeaders: usHeaders,
            ct: ct);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (allListener.Captured.Count(m =>
                   m.Headers.TryGetValue("x-test-id", out var v) && v == testId) < 2
               && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(300, ct);
        }

        await euListener.StopAsync(ct);
        await allListener.StopAsync(ct);

        // Assert — EU subscription only gets the EU message; all-subscription gets both
        var euCaptured = euListener.Captured
            .Where(m => m.Headers.TryGetValue("x-test-id", out var v) && v == testId)
            .ToList();
        await Assert.That(euCaptured.Count).IsEqualTo(1);
        await Assert.That(euCaptured[0].Body).Contains($"eu-msg-{testId}");

        var allCaptured = allListener.Captured
            .Where(m => m.Headers.TryGetValue("x-test-id", out var v) && v == testId)
            .ToList();
        await Assert.That(allCaptured.Count).IsEqualTo(2);

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, euSubName, ct);
        await admin.DeleteSubscriptionAsync(Topic, allSubName, ct);
    }
}
