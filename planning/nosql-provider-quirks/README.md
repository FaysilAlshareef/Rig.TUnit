# Planning — NoSQL provider quirks (F-024)

**Feature ID**: F-024
**Family**: NoSQL
**Status**: planned
**Depends on**: F-020 (collection/index topology)
**Target release**: v0.15
**Estimated tasks**: ~80 (Phase 0: 5 · 7 providers × 10 tasks · 5 docs)

---

## Why this feature exists

After F-020 (topology) / F-021 (consistency) / F-022 (change feed) / F-023 (cost), each NoSQL engine still has high-impact features that have no rig surface:

- **Cosmos** — hierarchical PK (2024+), multi-region writes, conflict-resolution policies (LWW / custom merge proc), patch-API, transactional batch within a logical PK.
- **Mongo** — aggregation-pipeline assertions, multi-doc transactions across replica set, GridFS, Atlas Search, change-stream resume after collection drop.
- **Dynamo** — transactions (`TransactWriteItems`, 100-item / 4 MB limits), conditional `attribute_exists`, GSI propagation lag.
- **Cassandra** — LWT (`IF NOT EXISTS`), tunable CL (deepens F-021), tombstone accumulation, `nodetool repair`, hinted handoff.
- **Elasticsearch** — relevance-score assertions, scroll vs `search_after`, ILM hot→warm→cold transitions, snapshot/restore, painless scripts, percolator.
- **KurrentDb** — catch-up subscription state, persistent-subscription ack/nack, `$by_category` / `$by_event_type`, optimistic concurrency by `expectedVersion`, scavenge run.
- **Redis (NoSQL role)** — Lua atomic scripts, MULTI/EXEC abort, pub/sub fan-out, keyspace notifications, cluster failover, Sentinel master switch, RedisJSON/RediSearch query results, slow-log capture.

## What we deliver

Per-provider extension of F-020's `I{Provider}TopologyBuilder` plus engine-specific assertion namespaces (`CosmosAssert`, `MongoAssert`, `DynamoAssert`, `CassandraAssert`, `ElasticAssert`, `KurrentAssert`, `RedisAssert`).

Selected examples:

```csharp
public static class CosmosAssert
{
    public static HierarchicalPkAssertion HierarchicalPk(string container);
    public static ConflictResolutionAssertion Conflict(string itemId);
    public static TransactionalBatchAssertion Batch();
}

public static class CassandraAssert
{
    public static LwtAssertion Lwt();
    public static TombstoneAssertion Tombstones(string keyspace, string table);
}

public static class ElasticAssert
{
    public static SearchAssertion Search(string index, QueryContainer query);
    public static IlmAssertion Index(string indexName);
}

public static class RedisAssert
{
    public static LuaAssertion Lua(string script);
    public static PubSubAssertion Channel(string name);
    public static SlowLogAssertion SlowLog();
}
```

## Gaps closed (from NOSQL-5..NOSQL-11 in the gap analysis)

All remaining engine-specific gaps not closed by F-020/021/022/023.

## Providers in scope

7: Cosmos, Mongo, Dynamo, Cassandra, Elasticsearch, KurrentDb, Redis.

## Exit criteria

- Each provider package ships ≥ 5 RED scenarios for its highest-impact quirks.
- `docs/providers/*.md` updated with engine-specific assertion idioms.
- ADR-013 (planned, shared with F-019) — "quirk-on-quirk: each engine declares only what it natively supports".

## Dependencies on other planned features

- Upstream: F-020, F-021, F-022.
- Downstream: F-040 (event-store schema evolution can leverage KurrentAssert).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 024-nosql-provider-quirks

Read first:
- planning/nosql-provider-quirks/README.md
- planning/nosql-collection-and-index-topology/README.md (F-020 must be shipped)
- planning/nosql-consistency-and-conflicts/README.md (F-021 must be shipped)
- Engine-specific docs (Cosmos hierarchical PK, Cassandra LWT, Elastic ILM, Redis Lua, etc.)

Generate a feature spec that:
1. Extends F-020's per-provider topology builders with engine-specific operations.
2. Adds per-provider assertion namespaces (CosmosAssert, CassandraAssert, etc.).
3. Each provider phase ships ≥ 5 RED scenarios.

Constraints:
- Each interface declares ONLY operations the engine natively supports.
- No shared "WithLwt()" — LWT is Cassandra-only.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
