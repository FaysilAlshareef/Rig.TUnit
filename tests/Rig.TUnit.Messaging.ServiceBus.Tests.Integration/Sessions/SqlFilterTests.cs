using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class SqlFilterTests
{
    private const string Topic = "test-topic";
    private const string EuFilterSubscription = "sql-filter-eu-subscription";
    private const string AllSubscription = "sql-filter-all-subscription";

    [Test]
    public async Task SendAsync_WithRegionLabel_OnlyEuFilterSubscriptionReceivesEuMessages(CancellationToken ct)
    {
        // Arrange — EuFilterSubscription has SqlRuleFilter("Region='EU'") applied via WithTopology
        var testId = Guid.NewGuid().ToString("N");
        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var euListener = new ServiceBusListener(client, Topic, EuFilterSubscription);
        await using var allListener = new ServiceBusListener(client, Topic, AllSubscription);

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
    }
}
