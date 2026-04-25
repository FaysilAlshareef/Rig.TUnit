# Planning — Outbox / Inbox correctness (F-038)

**Feature ID**: F-038
**Family**: Microservices
**Status**: planned
**Depends on**: F-015 (SQL schema topology — outbox/inbox tables), F-008 (clock — relay intervals)
**Target release**: v0.13
**Estimated tasks**: ~62 (Phase 0: 7 · 5 SQL stores × 10 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Microservices.Outbox` and `Rig.TUnit.Microservices.Inbox` provide fixtures and basic assertions but no fluent **schema** topology builder, no per-test isolation of tables, and — critically — no correctness scenarios for the production-bug surface:

- Duplicate publish on relay restart (at-least-once → assert idempotency).
- Strict per-aggregate ordering (out-of-order publish breaks downstream).
- Transactional consistency (no orphan messages — message exists ↔ aggregate row exists).
- Poison-message quarantine after N retries.
- Relay backpressure under broker outage (queue grows, doesn't lose).
- Inbox idempotency under retry storms (100 deliveries × same message id → handler runs once).

## What we deliver

```csharp
public interface IOutboxSchemaBuilder
{
    IOutboxSchemaBuilder Table(string name);
    IOutboxSchemaBuilder WithPartitioning(string partitionKey);
    IOutboxSchemaBuilder WithRetryPolicy(int maxRetries, TimeSpan backoff);
    IOutboxSchemaBuilder WithAcknowledgment(bool required);
    IOutboxSchemaBuilder WithRelayInterval(TimeSpan interval);
}

public abstract partial class OutboxFixture
{
    public OutboxFixture WithSchema(Action<IOutboxSchemaBuilder> configure);
    public Task AwaitProcessingAsync(TimeSpan timeout, CancellationToken ct);
}

public static class OutboxAssert
{
    public static OutboxAssertion Outbox(OutboxFixture fixture);
}

public sealed class OutboxAssertion
{
    public OutboxAssertion NoOrphans();
    public OutboxAssertion PerAggregateOrdered();
    public OutboxAssertion PoisonAfter(int retries);
    public OutboxAssertion AwaitProcessing(TimeSpan timeout);
    public OutboxAssertion BackpressureQueue(int maxDepth);
}

public static class InboxAssert
{
    public static InboxAssertion Inbox(InboxFixture fixture);
}

public sealed class InboxAssertion
{
    public InboxAssertion MessageId(string id).HandledExactly(int times).UnderConcurrency(int n);
}
```

## Gaps closed (from MS-1 + MS-2 in the gap analysis)

- Outbox correctness: orphans, ordering, poison messages, backpressure.
- Inbox idempotency under retry storms.
- Relay restart duplication assertion.

## Providers in scope

5 SQL stores: SqlServer, Postgres, MySql, Oracle, Sqlite (each as Outbox/Inbox backing store).

## Exit criteria

- `IOutboxSchemaBuilder`, `OutboxAssert`, `InboxAssert` ship with 100 % line coverage.
- Each SQL provider has ≥ 4 RED-leading correctness scenarios.
- F-008 fake-clock used for relay-interval assertions.
- Schema is idempotent — re-applying the same builder declaration is a no-op.
- `docs/providers/outbox-inbox.md` (new) covers the correctness contract.

## Dependencies on other planned features

- Upstream: F-008, F-015.
- Downstream: F-039 (saga uses outbox semantics for compensator dispatch), F-040 (event sourcing pairs with outbox for projections).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 038-outbox-inbox-correctness

Read first:
- planning/outbox-inbox-correctness/README.md
- planning/sql-schema-and-migrations/README.md (F-015 must be shipped)
- planning/deterministic-clock/README.md (F-008 must be shipped)
- src/Rig.TUnit.Microservices.Outbox/* and Inbox/* (current state)

Generate a feature spec that:
1. Introduces IOutboxSchemaBuilder + WithSchema on OutboxFixture / InboxFixture.
2. OutboxAssert.NoOrphans / PerAggregateOrdered / PoisonAfter / BackpressureQueue.
3. InboxAssert.MessageId(id).HandledExactly(n).UnderConcurrency(c).
4. Each SQL provider phase ships ≥ 4 RED-leading correctness scenarios.

Constraints:
- F-008 IFakeClock for relay-interval assertions.
- Each scenario asserts behaviour, not implementation; design supports both polling-relay and CDC-relay (F-018) variants.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
