# Planning — Change feed / change streams (F-022)

**Feature ID**: F-022
**Family**: NoSQL
**Status**: planned
**Depends on**: F-020 (collection/index topology)
**Target release**: v0.13
**Estimated tasks**: ~62 (Phase 0: 7 · 5 providers × 10 tasks · 5 docs)

---

## Why this feature exists

Every modern NoSQL engine emits a change stream — the basis for projections, search-index syncs, cache-invalidation fan-out, audit logs, and replication-bridge tools. None of these are testable in the rig today:

- **Cosmos change feed** — pull / push, lease management, start-from-now / start-from-beginning, full-fidelity vs latest-version mode.
- **Mongo change streams** — resume tokens, full-document, multi-collection, with `$match` filters.
- **Dynamo Streams** — KEYS_ONLY / NEW_IMAGE / OLD_IMAGE / NEW_AND_OLD_IMAGES.
- **KurrentDb subscriptions** — catch-up, persistent, all-streams.
- **Elasticsearch Watcher** (or equivalent ILM transitions) — change notifications.

Real-world bugs the rig should catch:
- A change-feed processor that loses its lease and reprocesses 10 k events.
- A resume-token round-trip that drops events on consumer restart.
- A change stream missing a delete event because the consumer was filtering on `update` only.
- A Cosmos lease container with an unbounded growth rate (rare bug — manifests under sustained load).

## What we deliver

A `WithChangeFeed(Action<I{Provider}ChangeFeedConfig>)` scope on each in-scope provider, plus an assertion API.

```csharp
public interface ICosmosChangeFeedConfig
{
    ICosmosChangeFeedConfig FromBeginning();
    ICosmosChangeFeedConfig FromNow();
    ICosmosChangeFeedConfig FromContinuationToken(string token);
    ICosmosChangeFeedConfig FullFidelity();
    ICosmosChangeFeedConfig WithLeaseContainer(string containerName);
}

public static class ChangeFeedAssert
{
    public static ChangeFeedScope Stream(string source);
}

public sealed class ChangeFeedScope
{
    public ChangeFeedScope ReceivedExactly(int count, TimeSpan within);
    public ChangeFeedScope ResumedFrom(string token).ContinuesWithoutGap();
    public ChangeFeedScope NoDuplicates();
    public ChangeFeedScope Filter(Func<ChangeEvent, bool> predicate);
}
```

## Gaps closed (from NOSQL-3 in the gap analysis)

- Change-feed lease management not testable.
- Resume-token round-trip not testable.
- Stream filter / event-type assertions missing.

## Providers in scope

5: Cosmos, Mongo, Dynamo, KurrentDb, Elasticsearch (Watcher / ILM).

## Exit criteria

- Per-provider sub-interface ships with 100 % line coverage.
- `ChangeFeedAssert.Stream` ships with `ReceivedExactly`, `ResumedFrom`, `NoDuplicates`, `Filter` operators.
- Each in-scope provider has ≥ 4 RED scenarios (start-from-now, resume-from-token, no-duplicates, multi-collection-or-equivalent).
- `docs/providers/*.md` updated.

## Dependencies on other planned features

- Upstream: F-020.
- Downstream: F-038 (outbox can be implemented via change feed), F-040 (event-sourcing projections).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 022-nosql-change-feed-and-streams

Read first:
- planning/nosql-change-feed-and-streams/README.md
- planning/nosql-collection-and-index-topology/README.md (F-020 must be shipped)
- Cosmos change-feed processor docs, Mongo change-streams docs, DynamoDB Streams docs, KurrentDb subscription docs

Generate a feature spec that:
1. Introduces I{Provider}ChangeFeedConfig + WithChangeFeed scope per in-scope provider.
2. ChangeFeedAssert.Stream + scope operators.
3. Each provider phase ships ≥ 4 RED scenarios.
4. Cosmos lease container lifecycle managed by the rig fixture (no leaked leases).

Constraints:
- Resume tokens round-tripped through tests must be deterministic.
- F-008 IFakeClock used for any "received within N seconds" assertion.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
