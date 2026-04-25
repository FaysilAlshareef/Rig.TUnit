# Planning — SQL CDC / temporal / pubsub (F-018)

**Feature ID**: F-018
**Family**: SQL
**Status**: planned
**Depends on**: F-008 (deterministic clock — temporal queries), F-015 (schema topology — declare CDC-enabled tables)
**Target release**: v0.14
**Estimated tasks**: ~54 (Phase 0: 7 · 3 providers × 14 tasks · 5 docs)

---

## Why this feature exists

Modern SQL stacks ship change-capture, temporal queries, and engine-native pubsub. None are testable in the rig today.

- **SqlServer CDC** (`sys.fn_cdc_get_all_changes_*`) and **Change Tracking** (`CHANGETABLE`) — the basis of every event-sourcing/projection scenario in MS shops.
- **System-versioned temporal tables** (`FOR SYSTEM_TIME AS OF`) — every audit-log scenario.
- **Postgres logical replication slots** + `pgoutput` — the basis of Debezium / Wal2Json pipelines.
- **Postgres `LISTEN` / `NOTIFY`** — lightweight pubsub used for cache invalidation.
- **MySql binlog** — same role as Postgres logical replication.
- **SqlServer Service Broker** — engine-native message queues (legacy but live).

Real-world tests this enables:
- "After table change, CDC capture sees the row within 5 s; payload includes both before/after images."
- "Subscriber to `pg_logical_emit_message` receives the message with metadata."
- "Temporal query `FOR SYSTEM_TIME AS OF '2026-04-01'` returns the row as it was on that date."
- "`LISTEN orders` receives a payload after `NOTIFY orders, '{...}'`."

## What we deliver

A `WithCdc(Action<ICdcBuilder>)` builder method per provider, plus assertion surfaces:

```csharp
public interface IPostgresCdcBuilder : ICdcBuilder
{
    IPostgresCdcBuilder LogicalReplicationSlot(string name, string plugin = "pgoutput");
    IPostgresCdcBuilder Listen(string channel, Action<NotificationPayload> handler);
}

public interface ISqlServerCdcBuilder : ICdcBuilder
{
    ISqlServerCdcBuilder EnableCdcOnTable(string schema, string table);
    ISqlServerCdcBuilder EnableChangeTrackingOnTable(string schema, string table);
    ISqlServerCdcBuilder TemporalTable(string schema, string table);
    ISqlServerCdcBuilder ServiceBrokerQueue(string name);
}

public static class CdcAssert
{
    public static CdcStreamAssert Stream(string slotOrTable);
    public static NotifyAssert Channel(string channel);
    public static TemporalAssert TemporalTable(string schema, string table);
}
```

## Gaps closed (from SQL-3 + SQL-4 in the gap analysis)

- CDC / Change Tracking / temporal tables / Service Broker not testable.
- Postgres logical replication and `LISTEN/NOTIFY` not testable.
- MySql binlog not testable.

## Providers in scope

3: SqlServer, Postgresql, MySql. Oracle has Streams / AQ but is deferred to a later feature; Sqlite has no analogue.

## Exit criteria

- `CdcAssert` and per-provider sub-builders ship with 100 % line coverage.
- Each in-scope provider has ≥ 4 RED scenarios (CDC capture, temporal query, pubsub roundtrip, replication-lag).
- `docs/providers/{sqlserver,postgresql,mysql}.md` updated with CDC sections.
- F-008 fake-clock used for "as-of" temporal assertions — no real Task.Delay.

## Dependencies on other planned features

- Upstream: F-008, F-015.
- Downstream: F-038 (outbox/inbox can opt to use CDC for reliable publishing — F-018 is the test surface).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 018-sql-cdc-and-pubsub

Read first:
- planning/sql-cdc-and-pubsub/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- planning/sql-schema-and-migrations/README.md (F-015 must be shipped)
- SqlServer CDC + Change Tracking + temporal-table docs
- Postgres logical-replication + LISTEN/NOTIFY docs
- MySql binlog reader (MySqlConnector or Debezium-style)

Generate a feature spec that:
1. Introduces ICdcBuilder marker + 3 provider sub-interfaces (SqlServer, Postgres, MySql).
2. WithCdc(Action<I{Provider}CdcBuilder>) on each provider's RigBuilder.
3. CdcAssert.Stream / .Channel / .TemporalTable surfaces.
4. Phase 0 lands marker + parity coverage file (no Oracle / Sqlite — explicitly N/A documented).
5. Each provider phase ships ≥ 4 RED scenarios.
6. Phase 6 documents which CDC features each engine ships and which are deferred.

Constraints:
- Logical replication slots cleaned up by fixture teardown — no leaked WAL.
- F-008 IFakeClock advanced for temporal "AS OF" assertions.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
