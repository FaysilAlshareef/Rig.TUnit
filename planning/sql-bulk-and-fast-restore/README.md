# Planning — SQL bulk + fast-restore (F-017)

**Feature ID**: F-017
**Family**: SQL
**Status**: planned
**Depends on**: F-010 (seed-data factories — F-017 deepens the SQL bulk adapters), F-011 (snapshot/restore — F-017 deepens the SQL restore mechanisms)
**Target release**: v0.13
**Estimated tasks**: ~50 (Phase 0: 5 · 5 providers × 8 tasks · 5 docs/bench)

---

## Why this feature exists

F-010 introduces a generic `WithSeedData` fast path; F-011 introduces a generic `SnapshotAsync` / `RestoreSnapshotAsync`. F-017 is the **SQL-specific deepening** of both: per-provider native bulk APIs and per-provider fast-restore strategies that hit single-digit-second timings even for 100 k+ rows.

Why a separate feature instead of folding into F-010/F-011? Because the SQL-specific work is large enough to merit its own RED+GREEN scenarios:
- `SqlBulkCopy` with `KeepIdentity`, `CheckConstraints=false`, custom column mapping.
- Postgres `COPY FROM STDIN BINARY` with full type-fidelity (`NpgsqlBinaryImporter`).
- Oracle `OracleBulkCopy` with array binding.
- MySql server-side `LOAD DATA LOCAL INFILE` over a memory stream.
- Sqlite multi-row `INSERT` inside a single transaction with `journal_mode=WAL`.

And the restore side:
- Postgres template database (`CREATE DATABASE x TEMPLATE seed_db`) — the fastest reset known.
- SqlServer DB snapshot (`CREATE DATABASE x AS SNAPSHOT OF seed_db`) — copy-on-write.
- MySql `xtrabackup` if available, fall back to schema-replay.
- Oracle flashback database.
- Sqlite `.backup` API + file-copy.

## What we deliver

Per-provider extension methods on the existing `WithSeedData` and `SnapshotAsync` surfaces:

```csharp
// For SqlServer:
public static class SqlServerSeedExtensions
{
    public static ISeedBuilder<T> WithSqlBulkCopy<T>(
        this ISeedBuilder<T> builder,
        Action<SqlBulkCopyOptions>? options = null);
}

// For Postgres:
public static class PostgresSeedExtensions
{
    public static ISeedBuilder<T> WithCopyFromBinary<T>(
        this ISeedBuilder<T> builder,
        Action<NpgsqlBinaryImporterOptions>? options = null);
}

// Snapshot fast paths:
public static class PostgresSnapshotExtensions
{
    public static SqlFixture WithTemplateDatabaseSnapshot(this PostgresFixture fixture);
}
```

The base `WithSeedData` / `SnapshotAsync` shape introduced in F-010 / F-011 remains unchanged; F-017 adds provider-scoped fast-path opt-ins.

## Gaps closed (from SQL-2 in the gap analysis)

- Slow seed loops dominated by per-row inserts.
- Slow integration suites dominated by reseed cost.
- No `SqlBulkCopy` / `COPY FROM STDIN` / `OracleBulkCopy` integration.
- No template-database / DB-snapshot fast paths.

## Providers in scope

5: SqlServer, Postgresql, MySql, Oracle, Sqlite.

## Exit criteria

- Each provider package has 1 RED-leading benchmark scenario asserting "100 k rows seeded under (provider-tuned) threshold".
- Each provider package has 1 RED-leading benchmark asserting "restore < (seed time × 0.2)".
- Benchmark deltas appended to `benchmarks/baseline-017.json`.
- `docs/providers/*.md` updated with the fast-path opt-in API and a `seed × restore` performance table.
- Architecture test asserts `Microsoft.Data.SqlClient`, `Npgsql`, `Oracle.ManagedDataAccess.Core`, `MySqlConnector` are referenced **only** by their respective providers.

## Dependencies on other planned features

- Upstream: F-010, F-011.
- Downstream: F-038 (outbox correctness uses fast-restore between scenarios).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 017-sql-bulk-and-fast-restore

Read first:
- planning/sql-bulk-and-fast-restore/README.md
- planning/seed-data-factories/README.md (F-010 must be shipped)
- planning/snapshot-and-restore/README.md (F-011 must be shipped)
- SqlBulkCopy / NpgsqlBinaryImporter / OracleBulkCopy / LOAD DATA INFILE docs

Generate a feature spec that:
1. Adds per-provider seed fast-path extension methods on F-010's ISeedBuilder<T>.
2. Adds per-provider snapshot fast-path extension methods on F-011's SnapshotAsync (template DB, DB snapshot, etc.).
3. Phase 0 only registers benchmarks scaffolding — no new contract types.
4. Each provider phase ships ≥ 2 benchmark RED scenarios (seed throughput + restore-vs-reseed delta).

Constraints:
- Architecture test guards: each provider's native client package referenced ONLY by its provider package.
- Benchmark seeds run with WithSeed(int) so results are reproducible.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, benchmarks/baseline-017.json scaffolding.
```
