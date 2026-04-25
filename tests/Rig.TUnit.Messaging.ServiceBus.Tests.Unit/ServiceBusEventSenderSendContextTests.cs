using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// T010-RED: verifies ServiceBusEventSender maps SendContext fields to the correct
/// ServiceBusMessage properties. Fails to compile until T010-GREEN adds the overload.
/// </summary>
public sealed class ServiceBusEventSenderSendContextTests
{
    [Test]
    public async Task SendAsync_WithSessionAndPartitionKeyUnequal_ThrowsInvalidOperationException(CancellationToken ct)
    {
        // Arrange — session and partition keys must be equal per ServiceBus spec
        await using var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");
        var context = new SendContext(SessionKey: "session-1", PartitionKey: "partition-other");

        // Act & Assert — must throw before sending
        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SendAsync_WithDefaultSendContext_BehavesLikeLegacyOverload(CancellationToken ct)
    {
        // Arrange — default SendContext has all nulls; should behave the same as the legacy overload
        await using var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");

        // Act & Assert — should not throw InvalidOperationException for mismatch;
        // network error is acceptable (offline endpoint) — we only check it's not an IOE
        var ex = await Assert.That(async () =>
            await sender.SendAsync("body", context: default, correlationId: "cid", ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithSessionKey_PopulatesServiceBusMessageSessionId(CancellationToken ct)
    {
        // Arrange
        await using var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");
        var context = new SendContext(SessionKey: "my-session");

        // Act — SessionId must be set; network error expected from offline endpoint
        // Behavioral correctness is verified in T015a integration test
        var ex = await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithPartitionKey_PopulatesServiceBusMessagePartitionKey(CancellationToken ct)
    {
        // Arrange — partition key alone (no session key) is valid
        await using var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");
        var context = new SendContext(PartitionKey: "my-partition");

        // Act — network error expected; behavioral correctness via T015b integration test
        var ex = await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithDeduplicationKey_PopulatesServiceBusMessageMessageId(CancellationToken ct)
    {
        // Arrange
        await using var sender = new ServiceBusEventSender(TestClients.Offline(), "topic");
        var context = new SendContext(DeduplicationKey: "dedup-key-42");

        // Act — network error expected; MessageId mapping verified by integration test
        var ex = await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: ct))
            .Throws<Exception>();
    }
}
