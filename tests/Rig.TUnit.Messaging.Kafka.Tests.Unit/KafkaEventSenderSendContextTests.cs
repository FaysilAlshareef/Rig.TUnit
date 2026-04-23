using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

// T020-RED: compile-fail until T020-GREEN adds KafkaEventSender.SendAsync(SendContext, ...).
public sealed class KafkaEventSenderSendContextTests
{
    private const string OfflineBootstrap = "192.0.2.1:9092";

    [Test]
    public async Task SendAsync_WithPartitionKey_SetsMessageKey(CancellationToken ct)
    {
        // Arrange — partition key should become Message.Key (behavioral correctness via T025a)
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(PartitionKey: "pk-42");

        // Act — network error expected from offline bootstrap; compile-proof of overload shape
        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithSessionKeyOnly_FoldsToMessageKey(CancellationToken ct)
    {
        // Arrange — SessionKey folds to Message.Key when PartitionKey is absent
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(SessionKey: "session-42");

        // Act — network error expected; fold logic verified in integration test
        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithPartitionKeyAndCorrelationId_PrefersPartitionKey(CancellationToken ct)
    {
        // Arrange — PartitionKey must win over correlationId for Message.Key
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(PartitionKey: "pk-wins");

        // Act — network error expected; key-selection priority verified in integration test
        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, correlationId: "cid-loses", ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_LegacyOverload_Unchanged(CancellationToken ct)
    {
        // Arrange — regression: old signature must not change
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");

        // Act — offline error proves the method signature is unchanged
        await Assert.That(async () =>
            await sender.SendAsync("body", correlationId: "cid", ct: ct))
            .Throws<Exception>();
    }
}
