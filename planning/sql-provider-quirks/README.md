# Planning — SQL provider quirks (F-019)

**Feature ID**: F-019
**Family**: SQL
**Status**: planned
**Depends on**: F-015 (schema topology)
**Target release**: v0.15
**Estimated tasks**: ~60 (Phase 0: 5 · 5 providers × 10 tasks · 5 docs)

---

## Why this feature exists

After F-015 / F-016 / F-017 / F-018, the SQL family has schema, isolation, bulk, and CDC. What remains are the **engine-specific** features that real codebases use heavily and the rig still cannot exercise:

- **Postgres**: JSONB operators (`@>`, `->>`), full-text (`tsvector`, `tsquery`, `pg_trgm`), Row-Level Security policies, partitioned tables (RANGE / LIST / HASH), generated columns, `LATERAL` joins, `WITH RECURSIVE`, `RETURNING`, advisory locks (`pg_advisory_lock`).
- **SqlServer**: filtered indexes, columnstore, In-Memory OLTP tables, `OUTPUT` clause, `MERGE`, `FOR JSON PATH`, Always Encrypted column-encryption, FILESTREAM, application locks (`sp_getapplock`).
- **MySql**: `JSON_TABLE`, generated invisible columns, GTID-based replication, `GET_LOCK` named locks.
- **Oracle**: hierarchical queries (`CONNECT BY`), package/PL-SQL bodies, `DBMS_SCHEDULER`, AQ.
- **Sqlite**: WAL mode, FTS5, `RETURNING` (3.35+), shared-cache mode.

Today, exercising any of these requires raw SQL strings glued together with `await connection.ExecuteAsync(...)`. There's no fluent surface, no assertion API.

## What we deliver

Per-provider extensions to F-015's `WithSchema` plus per-provider assertion surfaces. Compile-time scoped: only the operations the engine supports are exposed on each `I{Provider}SchemaBuilder`.

### Postgres example
```csharp
public interface IPostgresSchemaBuilder
{
    // ... already from F-015 ...
    IPostgresSchemaBuilder RowLevelSecurity(string table, string policyName, string usingClause);
    IPostgresSchemaBuilder JsonbColumn(string table, string column, string? defaultExpression = null);
    IPostgresSchemaBuilder TsVectorColumn(string table, string column, string sourceColumn);
    IPostgresSchemaBuilder PartitionedTable(string name, PartitionStrategy strategy, ...);
}

public static class PostgresAssert
{
    public static RlsAssert RowLevelSecurity(string table);
    public static FullTextAssert FullText(string table, string column);
    public static AdvisoryLockAssert AdvisoryLock(string name);
}
```

### SqlServer example
```csharp
public interface ISqlServerSchemaBuilder
{
    // ... already from F-015 ...
    ISqlServerSchemaBuilder ColumnstoreIndex(string table, params string[] columns);
    ISqlServerSchemaBuilder InMemoryTable(string name, ...);
    ISqlServerSchemaBuilder AlwaysEncryptedColumn(string table, string column, string cmkName, string cekName);
    ISqlServerSchemaBuilder FilteredIndex(string table, string filter, params string[] columns);
}
```

## Gaps closed (from SQL-9 in the gap analysis)

- Postgres-specific tests for JSONB, FTS, RLS, partitions, advisory locks.
- SqlServer-specific tests for columnstore, In-Memory OLTP, Always Encrypted, app locks.
- MySql-specific tests for JSON_TABLE, GTID, named locks.
- Oracle / Sqlite specifics where applicable.

## Providers in scope

5: all SQL providers, but each gets only its own quirks.

## Exit criteria

- Each provider package has ≥ 5 RED scenarios covering its highest-impact quirks.
- `docs/providers/*.md` updated with a "Engine-specific feature surface" section per provider.
- ADR-013 (planned) — "Quirk-on-quirk: each engine declares only what it natively supports" — formalises the per-provider sub-interface pattern.
- Coverage gates ≥ 90/85.

## Dependencies on other planned features

- Upstream: F-015.
- Downstream: F-038 (outbox can opt into engine-specific tricks like Postgres `SKIP LOCKED` for relay).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 019-sql-provider-quirks

Read first:
- planning/sql-provider-quirks/README.md
- planning/sql-schema-and-migrations/README.md (F-015 must be shipped)
- Postgres / SqlServer / MySql / Oracle / Sqlite engine-specific feature docs

Generate a feature spec that:
1. Extends F-015's I{Provider}SchemaBuilder with engine-specific operations.
2. Adds per-provider assertion namespaces (PostgresAssert, SqlServerAssert, etc.).
3. Phase 0 just adds parity-coverage tooling (no new contract); each provider phase delivers its own quirks.
4. Each provider phase ships ≥ 5 RED scenarios for its highest-impact quirks.
5. Phase 6 publishes ADR-013 documenting the per-provider scoping pattern.

Constraints:
- Each interface declares ONLY operations the engine natively supports.
- No shared "WithRls()" — RLS is Postgres-only and lives only on IPostgresSchemaBuilder.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, ADR-013 draft.
```
