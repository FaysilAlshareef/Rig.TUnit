# Planning — EventSourcing schema evolution + projection rebuild (F-040)

**Feature ID**: F-040
**Family**: Microservices
**Status**: planned
**Depends on**: F-038 (outbox semantics for projections)
**Target release**: v0.15
**Estimated tasks**: ~42 (Phase 0: 7 · 3 packages × 10 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Microservices.EventSourcing` and `Rig.TUnit.Microservices.Snapshots` exist but with thin assertion surfaces. The production-reality gaps:

- **Schema evolution**: an event class is renamed / a field added / a value-type changed → upcaster runs to lift V1 events to V3.
- **Snapshot mismatch**: stored snapshot version no longer matches code → rebuild from events.
- **Projection rebuild**: a new read model is added → projection replays from event 0; assert idempotency under partial failure mid-rebuild.
- **Stream optimistic concurrency**: `expectedVersion` mismatch on append → `WrongExpectedVersion`.
- **Cross-stream queries** (KurrentDb `$by_category` / `$by_event_type`) — system-projection results.

## What we deliver

```csharp
public interface IEventStoreBuilder
{
    IEventStoreBuilder Stream(string name, int expectedVersion);
    IEventStoreBuilder Snapshot(string aggregateId, int version, object state);
    IEventStoreBuilder Projection(string name, IEventHandler handler);
    IEventStoreBuilder Upcaster(int fromVersion, int toVersion, Func<object, object> upcast);
}

public abstract partial class EventStoreFixture
{
    public EventStoreFixture WithStore(Action<IEventStoreBuilder> configure);
    public Task RebuildProjectionAsync(string name, CancellationToken ct);
}

public static class EventSourcingAssert
{
    public static StreamAssertion Stream(EventStoreFixture fixture, string streamName);
    public static SnapshotAssertion Snapshot(EventStoreFixture fixture, string aggregateId);
    public static ProjectionAssertion Projection(EventStoreFixture fixture, string name);
}

public sealed class StreamAssertion
{
    public StreamAssertion HasEvents(int count);
    public StreamAssertion OfType(Type eventType).Count(int n);
    public StreamAssertion AppendedWithVersion(int v).Or(WrongExpectedVersionException);
    public StreamAssertion Upcasted().FromVersion(int from).ToVersion(int to);
}

public sealed class SnapshotAssertion
{
    public SnapshotAssertion AtVersion(int v);
    public SnapshotAssertion Mismatch().Rebuilt();
}

public sealed class ProjectionAssertion
{
    public ProjectionAssertion Rebuild().From(int eventNumber).IdempotentUnderRestart();
    public ProjectionAssertion EmittedTo(string stream).Count(int n);
}
```

## Gaps closed (from MS-4 + MS-5 in the gap analysis)

- Event upcaster execution.
- Snapshot version mismatch + rebuild.
- Projection rebuild idempotency.
- Stream optimistic concurrency.
- Cross-stream system-projection results.

## Providers in scope

3: `src/Rig.TUnit.Microservices.EventSourcing`, `src/Rig.TUnit.Microservices.Snapshots`, `src/Rig.TUnit.Databases.NoSql.KurrentDb`.

## Exit criteria

- `IEventStoreBuilder`, `EventSourcingAssert.*` ship with 100 % line coverage.
- ≥ 6 RED-leading scenarios.
- `docs/providers/event-sourcing.md` (new) covers upcasting and projection-rebuild patterns.

## Dependencies on other planned features

- Upstream: F-038.
- Downstream: F-041 (consumer-driven contracts for event schemas).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 040-eventsourcing-evolution-and-projection

Read first:
- planning/eventsourcing-evolution-and-projection/README.md
- planning/outbox-inbox-correctness/README.md (F-038 must be shipped)
- src/Rig.TUnit.Microservices.EventSourcing/* and Snapshots/* (current state)
- src/Rig.TUnit.Databases.NoSql.KurrentDb/* (current state)

Generate a feature spec that:
1. Introduces IEventStoreBuilder + EventSourcingAssert.Stream / Snapshot / Projection.
2. Upcaster registration and Upcasted assertion.
3. ProjectionAssert.Rebuild.From(0).IdempotentUnderRestart().
4. ≥ 6 RED-leading scenarios.

Constraints:
- Schema evolution scenarios cover both rename (V1.OldField → V2.NewField) and field-added cases.
- Idempotent rebuild: kill the rebuilder mid-flight, restart, end-state is identical.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
