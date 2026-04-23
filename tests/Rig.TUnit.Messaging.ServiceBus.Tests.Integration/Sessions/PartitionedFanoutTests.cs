using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration.Sessions;

public sealed class PartitionedFanoutTests
{
    private const string Topic = "test-topic";
    private const string FanoutSubscription = "partitioned-fanout-subscription";

    [Test]
    public async Task SendAsync_MessagesWithDistinctPartitionKeys_AllReachSubscription(CancellationToken ct)
    {
        // Arrange — 5 distinct partition keys, 4 messages each = 20 total
        const int keyCount = 5;
        const int messagesPerKey = 4;
        var partitionKeys = Enumerable.Range(0, keyCount).Select(i => $"pk-{i}").ToArray();

        var fx = await SharedServiceBusFixture.GetAsync();
        await using var client = new ServiceBusClient(fx.ConnectionString);
        await using var sender = new ServiceBusEventSender(client, Topic);
        await using var listener = new ServiceBusListener(client, Topic, FanoutSubscription);

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

        var totalExpected = keyCount * messagesPerKey;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (listener.Captured.Count < totalExpected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(300, ct);
        }

        await listener.StopAsync(ct);

        // Assert — every partition key's messages arrived
        await Assert.That(listener.Captured.Count).IsGreaterThanOrEqualTo(totalExpected);
        foreach (var pk in partitionKeys)
        {
            var forKey = listener.Captured.Where(m =>
                m.Body.StartsWith($"fanout-{pk}-", StringComparison.Ordinal)).ToList();
            await Assert.That(forKey.Count).IsGreaterThanOrEqualTo(messagesPerKey);
        }
    }
}
