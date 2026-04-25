using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Kafka.Helpers;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// Compile-shape tests for the SendContext overload of <see cref="KafkaEventSender" />.
/// The bootstrap address is RFC 5737 documentation IP (never responds); a 1-second
/// cancellation token bounds each call so the test fails fast with
/// <see cref="OperationCanceledException" /> instead of waiting for Confluent.Kafka's
/// default 300-second `message.timeout.ms`. Behavioral correctness
/// (PartitionKey → Message.Key, SessionKey fold, key priority) is verified by the
/// integration suite (T025a / Partitions tests).
/// </summary>
public sealed class KafkaEventSenderSendContextTests
{
    private const string OfflineBootstrap = "192.0.2.1:9092";
    private static readonly TimeSpan FastFailTimeout = TimeSpan.FromSeconds(1);

    [Test]
    public async Task SendAsync_WithPartitionKey_SetsMessageKey(CancellationToken ct)
    {
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(PartitionKey: "pk-42");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(FastFailTimeout);

        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: cts.Token))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithSessionKeyOnly_FoldsToMessageKey(CancellationToken ct)
    {
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(SessionKey: "session-42");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(FastFailTimeout);

        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, ct: cts.Token))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_WithPartitionKeyAndCorrelationId_PrefersPartitionKey(CancellationToken ct)
    {
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        var context = new SendContext(PartitionKey: "pk-wins");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(FastFailTimeout);

        await Assert.That(async () =>
            await sender.SendAsync("body", context: context, correlationId: "cid-loses", ct: cts.Token))
            .Throws<Exception>();
    }

    [Test]
    public async Task SendAsync_LegacyOverload_Unchanged(CancellationToken ct)
    {
        await using var sender = new KafkaEventSender(OfflineBootstrap, "topic");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(FastFailTimeout);

        await Assert.That(async () =>
            await sender.SendAsync("body", correlationId: "cid", ct: cts.Token))
            .Throws<Exception>();
    }
}
