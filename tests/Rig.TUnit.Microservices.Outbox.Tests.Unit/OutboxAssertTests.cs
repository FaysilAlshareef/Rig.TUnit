using Rig.TUnit.Microservices.Outbox.Assertions;
using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxAssertionExceptionTests
{
    [Test]
    public async Task OutboxAssertionException_Message_IsPreserved()
    {
        var ex = new OutboxAssertionException("entry missing");

        await Assert.That(ex.Message).IsEqualTo("entry missing");
    }

    [Test]
    public async Task OutboxAssertionException_IsExceptionSubtype()
    {
        var ex = new OutboxAssertionException("x");

        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public sealed class CustomOutboxStoreTests
{
    private static CustomOutboxStore<OutboxMessage> BuildStore(List<OutboxMessage>? rows = null)
    {
        rows ??= [];
        return CustomOutboxStore<OutboxMessage>.Create(
            mapToMessage: m => m,
            mapFromMessage: m => m,
            enqueueAsync: (row, _) => { rows.Add(row); return Task.CompletedTask; },
            readPendingAsync: (take, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>(rows.Take(take).ToList()),
            markRelayedAsync: (_, _, _) => Task.CompletedTask,
            markFailedAsync: (_, _, _) => Task.CompletedTask);
    }

    [Test]
    public async Task Create_NullMapToMessage_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            null!, m => m, (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]),
            (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_NullMapFromMessage_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            m => m, null!, (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]),
            (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_NullEnqueueAsync_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            m => m, m => m, null!,
            (_, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]),
            (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_NullReadPendingAsync_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            m => m, m => m, (_, _) => Task.CompletedTask, null!,
            (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_NullMarkRelayedAsync_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            m => m, m => m, (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]),
            null!, (_, _, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_NullMarkFailedAsync_ThrowsArgumentNullException()
    {
        await Assert.That(() => CustomOutboxStore<OutboxMessage>.Create(
            m => m, m => m, (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]),
            (_, _, _) => Task.CompletedTask, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EnqueueAsync_NullMessage_ThrowsArgumentNullException()
    {
        var store = BuildStore();

        await Assert.That(() => store.EnqueueAsync(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ReadPageAsync_ZeroPageSize_ThrowsArgumentOutOfRangeException()
    {
        var store = BuildStore();

        await Assert.That(async () => await store.ReadPageAsync(0)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ReadAllAsync_AfterEnqueue_ReturnsMappedRow()
    {
        var rows = new List<OutboxMessage>();
        var store = BuildStore(rows);
        var msg = new OutboxMessage(Guid.NewGuid(), "agg-1", "OrderPlaced", "{}", DateTimeOffset.UtcNow);

        await store.EnqueueAsync(msg);
        var all = await store.ReadAllAsync();

        await Assert.That(all.Count).IsEqualTo(1);
        await Assert.That(all[0].AggregateId).IsEqualTo("agg-1");
    }
}

public sealed class OutboxAssertContainsTests
{
    private sealed record SampleOutboxEvent(string Id);

    [Test]
    public async Task Contains_NullFixture_ThrowsArgumentNullException()
    {
        await Assert.That(() => OutboxAssert.Contains<SampleOutboxEvent>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Contains_ExactlyOnce_WithMatchingEntry_DoesNotThrow()
    {
        var fixture = new OutboxFixture();
        var eventType = typeof(SampleOutboxEvent).FullName!;
        await fixture.Store.EnqueueAsync(
            new OutboxMessage(Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow));

        await OutboxAssert.Contains<SampleOutboxEvent>(fixture).ExactlyOnce();
    }

    [Test]
    public async Task Contains_ExactlyOnce_WithNoMatchingEntry_ThrowsOutboxAssertionException()
    {
        var fixture = new OutboxFixture();

        await Assert.That(() => OutboxAssert.Contains<SampleOutboxEvent>(fixture).ExactlyOnce())
            .ThrowsExactly<OutboxAssertionException>();
    }
}
