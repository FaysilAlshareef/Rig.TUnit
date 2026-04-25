using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class PartitionedFanoutTests
{
    private const string Topic = "test-topic";

    [Test]
    public async Task SendAsync_MessagesWithDistinctPartitionKeys_AllReachSubscription(CancellationToken ct)
    {
        // Arrange — 5 distinct partition keys, 4 messages each = 20 total
        const int keyCount = 5;
        const int messagesPerKey = 4;
        var partitionKeys = Enumerable.Range(0, keyCount).Select(i => $"pk-{i}").ToArray();

        var fx = await SharedServiceBusFixture.GetAsync();
        var admin = new ServiceBusAdministrationClient(fx.AdminConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var subName = $"fanout-{Guid.NewGuid():N}";

        await helper.CreateSubscriptionIfNotExistsAsync(Topic, subName, requiresSession: false, ct);

        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusListener(client, Topic, subName);

        // Act
        await listener.StartAsync(ct);

        foreach (var pk in partitionKeys)
        {
            for (var i = 0; i < messagesPerKey; i++)
            {
                await sender.SendAsync(
                    $"fanout-{pk}-{i}",
                    context: new SendContext(PartitionKey: pk),
                    ct: ct);
            }
        }

        // Wait for broker delivery on a per-partition-key basis. Looping on the
        // total `Captured.Count` is unreliable because the prefetched processor
        // can redeliver a message whose lock expires before completion: total
        // count then crosses the threshold while one slow partition still has
        // unique-message arrivals pending. Counting **distinct** bodies per key
        // also guards the final assertion against redelivery duplicates so a
        // pk that legitimately landed all four messages can't fail because
        // another pk inflated its share.
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var snapshot = listener.Captured;
            var allReady = partitionKeys.All(pk =>
                snapshot.Select(m => m.Body)
                        .Where(b => b.StartsWith($"fanout-{pk}-", StringComparison.Ordinal))
                        .Distinct(StringComparer.Ordinal)
                        .Count() >= messagesPerKey);
            if (allReady) break;
            await Task.Delay(300, ct);
        }

        await listener.StopAsync(ct);

        // Assert — every partition key's distinct messages arrived
        foreach (var pk in partitionKeys)
        {
            var distinctForKey = listener.Captured
                .Select(m => m.Body)
                .Where(b => b.StartsWith($"fanout-{pk}-", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Count();
            await Assert.That(distinctForKey).IsGreaterThanOrEqualTo(messagesPerKey);
        }

        // Cleanup
        await admin.DeleteSubscriptionAsync(Topic, subName, ct);
    }
}
