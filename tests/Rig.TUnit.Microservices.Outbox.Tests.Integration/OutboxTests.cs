using System.Collections.Concurrent;
using Rig.TUnit.Microservices.Outbox;
using Rig.TUnit.Microservices.Outbox.Assertions;
using Rig.TUnit.Microservices.Outbox.Fixtures;
using Rig.TUnit.Microservices.Outbox.Simulators;

namespace Rig.TUnit.Microservices.Outbox.Tests.Integration;

public sealed class OutboxTests
{
    private sealed record OrderPlaced(string OrderId);

    private static OutboxMessage NewRow(string aggId = "agg-1")
        => new(
            Id: Guid.NewGuid(),
            AggregateId: aggId,
            EventType: typeof(OrderPlaced).FullName!,
            Payload: "{}",
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: "c-1",
            CausationId: "cs-1",
            Traceparent: "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");

    [Test]
    public async Task Enqueue_Then_Drain_Relays_Message_And_Marks_Relayed()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();

        await fx.Store.EnqueueAsync(NewRow());

        var published = new List<OutboxEventEnvelope>();
        var relay = new OutboxRelaySimulator(fx.Store, (e, _) => { published.Add(e); return Task.CompletedTask; });
        var count = await relay.DrainAsync();

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(published.Count).IsEqualTo(1);
        await OutboxAssert.Contains<OrderPlaced>(fx).Relayed();
    }

    [Test]
    public async Task ExactlyOnce_Under_100_Concurrent_Relay_Runs()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();

        // Seed 10 rows — each must be relayed exactly once across 100 concurrent workers.
        for (var i = 0; i < 10; i++)
        {
            await fx.Store.EnqueueAsync(NewRow($"agg-{i}"));
        }

        var published = new ConcurrentBag<Guid>();
        Task Publish(OutboxEventEnvelope e, CancellationToken _) { published.Add(e.MessageId); return Task.CompletedTask; }

        var workers = Enumerable.Range(0, 100).Select(_ =>
        {
            var relay = new OutboxRelaySimulator(fx.Store, Publish);
            return Task.Run(() => relay.DrainAsync(batchSize: 100));
        }).ToArray();
        await Task.WhenAll(workers);

        // Exactly-once → each MessageId appears at most once across all workers.
        var ids = published.ToArray();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).ToArray();
        await Assert.That(duplicates.Length).IsEqualTo(0);
        await Assert.That(ids.Length).IsEqualTo(10);
    }

    [Test]
    public async Task Transient_Publish_Failure_Marks_Row_Failed_Not_Relayed()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();
        await fx.Store.EnqueueAsync(NewRow());

        var relay = new OutboxRelaySimulator(fx.Store,
            (_, _) => throw new TimeoutException("upstream busy"));
        var count = await relay.DrainAsync();

        await Assert.That(count).IsEqualTo(0);
        var all = await fx.Store.ReadAllAsync();
        await Assert.That(all[0].FailureReason).IsNotNull();
        await Assert.That(all[0].RelayedAt).IsNull();
    }

    [Test]
    public async Task Replay_Republishes_In_Original_Order()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();
        var t0 = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            var row = NewRow($"agg-{i}") with { OccurredAt = t0.AddMilliseconds(i) };
            await fx.Store.EnqueueAsync(row);
        }

        var order = new List<string>();
        var replay = new OutboxReplay(fx.Store, (e, _) => { order.Add(e.AggregateId); return Task.CompletedTask; });
        var count = await replay.ReplayAsync();

        await Assert.That(count).IsEqualTo(5);
        for (var i = 0; i < 5; i++) await Assert.That(order[i]).IsEqualTo($"agg-{i}");
    }

    [Test]
    public async Task OutboxAssert_Contains_WithAggregateId_Filters()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();
        await fx.Store.EnqueueAsync(NewRow("agg-A"));
        await fx.Store.EnqueueAsync(NewRow("agg-B"));

        await OutboxAssert.Contains<OrderPlaced>(fx).WithAggregateId("agg-B").ExactlyOnce();
    }

    [Test]
    public async Task OutboxAssert_DeadLetter_WithReason_Fragment()
    {
        await using var fx = new OutboxFixture();
        await fx.InitializeAsync();
        var row = NewRow() with { FailureReason = "TimeoutException: dead-letter reason" };
        fx.PushDeadLetter(row);

        var dl = await OutboxAssert.InDeadLetter<OrderPlaced>(fx);
        await dl.WithReason("TimeoutException");
    }

    [Test]
    public async Task CustomOutboxStore_Adapts_Developer_Row_Type()
    {
        // Developer has their own row shape — they provide mappers + delegates.
        var rows = new ConcurrentDictionary<Guid, MyRow>();

        var store = CustomOutboxStore<MyRow>.Create(
            mapToMessage: r => new OutboxMessage(r.Id, r.Agg, r.Type, r.Json, r.Ts, RelayedAt: r.RelayedAt, FailureReason: r.FailReason),
            mapFromMessage: m => new MyRow(m.Id, m.AggregateId, m.EventType, m.Payload, m.OccurredAt, null, null),
            enqueueAsync: (r, _) => { rows[r.Id] = r; return Task.CompletedTask; },
            readPendingAsync: (take, _) => Task.FromResult<IReadOnlyList<MyRow>>(
                rows.Values.Where(r => r.RelayedAt is null && r.FailReason is null)
                    .OrderBy(r => r.Ts).Take(take).ToArray()),
            markRelayedAsync: (id, at, _) =>
            {
                rows.AddOrUpdate(id, _ => throw new KeyNotFoundException(), (_, r) => r with { RelayedAt = at });
                return Task.CompletedTask;
            },
            markFailedAsync: (id, reason, _) =>
            {
                rows.AddOrUpdate(id, _ => throw new KeyNotFoundException(), (_, r) => r with { FailReason = reason });
                return Task.CompletedTask;
            });

        await using var fx = new OutboxFixture(store);
        await fx.InitializeAsync();
        await fx.Store.EnqueueAsync(NewRow());

        var relay = new OutboxRelaySimulator(fx.Store, (_, _) => Task.CompletedTask);
        var count = await relay.DrainAsync();

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows.Values.First().RelayedAt).IsNotNull();
    }

    [Test]
    public async Task OutboxSchema_Default_SqlGeneration_UsesConfiguredNames()
    {
        var schema = new OutboxSchema(TableName: "AppOutbox", SchemaName: "events", ParameterPrefix: "$");
        await Assert.That(schema.QualifiedTable).IsEqualTo("[events].[AppOutbox]");
        await Assert.That(schema.BuildInsertSql()).Contains("[events].[AppOutbox]");
        await Assert.That(schema.BuildInsertSql()).Contains("$id");
        await Assert.That(schema.BuildReadPendingSql(50)).Contains("TOP (50)");
        await Assert.That(schema.BuildMarkRelayedSql()).Contains("RelayedAt");
    }

    private sealed record MyRow(
        Guid Id,
        string Agg,
        string Type,
        string Json,
        DateTimeOffset Ts,
        DateTimeOffset? RelayedAt,
        string? FailReason);
}
