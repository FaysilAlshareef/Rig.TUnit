# Rig.TUnit.Microservices.Outbox

Transactional-outbox testing helpers. Ships `OutboxFixture`, `OutboxRelaySimulator`,
`OutboxAssert`, and `OutboxReplay`. Works with any backing store via `IOutboxStore`
— drop in `CustomOutboxStore<TRow>` to plug your own row type.

## Install

```xml
<PackageReference Include="Rig.TUnit.Microservices.Outbox" />
```

## Example — default in-memory store

```csharp
await using var fx = new OutboxFixture();
await fx.InitializeAsync();
await fx.Store.EnqueueAsync(new OutboxMessage(Guid.NewGuid(), "agg-1", "OrderPlaced", "{}", DateTimeOffset.UtcNow));

var relay = new OutboxRelaySimulator(fx.Store, (envelope, ct) => bus.PublishAsync(envelope, ct));
await relay.DrainAsync();

await OutboxAssert.Contains<OrderPlaced>(fx).Relayed();
```

## Example — plug your own schema + row type

```csharp
var store = CustomOutboxStore<MyOutboxRow>.Create(
    mapToMessage:      row => new OutboxMessage(row.Id, row.AggId, row.Type, row.Json, row.Timestamp),
    mapFromMessage:    m   => new MyOutboxRow { Id = m.Id, AggId = m.AggregateId /* ... */ },
    enqueueAsync:      (row, ct) => _db.Outbox.AddAsync(row, ct).AsTask(),
    readPendingAsync:  (take, ct) => _db.Outbox.Where(r => r.RelayedAt == null).OrderBy(r => r.Ts).Take(take).ToListAsync(ct),
    markRelayedAsync:  (id, at, ct) => _db.Outbox.Where(r => r.Id == id).ExecuteUpdateAsync(u => u.SetProperty(x => x.RelayedAt, at), ct),
    markFailedAsync:   (id, reason, ct) => _db.Outbox.Where(r => r.Id == id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Reason, reason), ct));
await using var fx = new OutboxFixture(store, new OutboxSchema(TableName: "AppOutbox"));
```

`OutboxSchema` exposes pre-built parameterised SQL for default providers:
`BuildInsertSql`, `BuildReadPendingSql(N)`, `BuildMarkRelayedSql`, `BuildMarkFailedSql`.

Exactly-once verified under 100 concurrent relay runs (InMemoryOutboxStore uses CAS
claim on read).

Spec: `003-rig-tunit-ecosystem-expansion` — FR:100, US8.
