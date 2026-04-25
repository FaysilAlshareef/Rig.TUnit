# Planning — NoSQL consistency & ETag conflicts (F-021)

**Feature ID**: F-021
**Family**: NoSQL
**Status**: planned
**Depends on**: F-020 (collection/index topology)
**Target release**: v0.13
**Estimated tasks**: ~62 (Phase 0: 7 · 5 providers × 10 tasks · 5 docs)

---

## Why this feature exists

NoSQL engines have **tunable** consistency. Tests that don't pin the level (or don't know they're not pinned) ship as "works on my machine":

- **Cosmos** — Strong / Bounded Staleness / Session / Consistent Prefix / Eventual. Default is Session.
- **Mongo** — read concerns `local` / `majority` / `linearizable`. Default is `local`.
- **Cassandra** — `CL=ONE / QUORUM / ALL` (and `EACH_QUORUM` for multi-DC).
- **Dynamo** — strongly-consistent vs eventually-consistent reads.
- **Elasticsearch** — `refresh=true` / `wait_for` / `false` semantics.

Optimistic concurrency / ETag conflicts are also untestable today:
- Cosmos `_etag` mismatch returns `412 PreconditionFailed`.
- Mongo `_id` + version field with `$set` + filter.
- Dynamo conditional writes (`attribute_exists` / `attribute_not_exists`).
- Cassandra LWT (`IF NOT EXISTS`).
- Elasticsearch `if_seq_no` / `if_primary_term`.

Real-world bugs the rig should catch:
- A repository that doesn't re-read after a 412 → silent overwrite.
- A "session-consistent" read that returns stale data because the session token was lost on retry.
- An LWT that fails because someone else won the race; the app retries blindly without backoff.

## What we deliver

A consistency scope and a conflict-injection assertion API:

```csharp
public abstract partial class NoSqlFixture
{
    public IDisposable WithConsistency(ConsistencyLevel level);
    public IDisposable WithReadStaleness(TimeSpan max);
}

public static class ConsistencyAssert
{
    public static ReadAssertion Read(ConsistencyLevel level);
    public static ConflictAssertion Conflict<T>();
}

public sealed class ConflictAssertion
{
    public ConflictAssertion ETagMismatch();
    public ConflictAssertion ConditionalWriteFailed();
    public ConflictAssertion LwtAppliedFalse();
    public ConflictAssertion VersionConflict();
}
```

Plus injection helpers:

```csharp
public sealed class ConflictInjector
{
    public IDisposable ForceEtagMismatchOn(string id);
    public IDisposable ForceConditionalWriteFailureOn(string pk, string sk);
}
```

## Gaps closed (from NOSQL-1 + NOSQL-2 in the gap analysis)

- Tunable-consistency assertions across 5 providers.
- Optimistic-concurrency / ETag conflict reproduction.
- LWT applied-false reproduction.

## Providers in scope

5: Cosmos, Mongo, Cassandra, Dynamo, Elasticsearch.
(KurrentDb has streamed-consistency semantics handled in F-024; Redis is single-node consistency primarily.)

## Exit criteria

- `ConsistencyAssert` and `ConflictAssertion` ship with 100 % line coverage.
- Each provider has ≥ 3 RED scenarios (consistency-level matrix, ETag mismatch, conditional-write failure).
- `docs/providers/*.md` updated with a "Consistency matrix" table per provider listing actually-honoured levels.

## Dependencies on other planned features

- Upstream: F-020.
- Downstream: F-038 (outbox correctness asserts conditional-write semantics), F-040 (event-store optimistic concurrency).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 021-nosql-consistency-and-conflicts

Read first:
- planning/nosql-consistency-and-conflicts/README.md
- planning/nosql-collection-and-index-topology/README.md (F-020 must be shipped)
- Cosmos consistency-level docs, Mongo read concern, Cassandra CL, Dynamo consistent reads, Elastic refresh semantics

Generate a feature spec that:
1. Introduces ConsistencyLevel enum + WithConsistency scope on NoSQL fixtures.
2. ConsistencyAssert.Read + ConflictAssertion + ConflictInjector.
3. Each provider phase delivers ≥ 3 RED scenarios.
4. Document each engine's actual honoured levels.

Constraints:
- ConsistencyLevel is normalised; per-engine mapping documented in research.md.
- ConflictInjector deterministic (no random races).
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
