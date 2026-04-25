# Planning — SQL schema & migration topology (F-015)

**Feature ID**: F-015
**Family**: SQL
**Status**: planned
**Depends on**: —
**Target release**: v0.12
**Estimated tasks**: ~84 (Phase 0: 7 · 5 SQL providers × 14 tasks · 7 docs/bench)

---

## Why this feature exists

The SQL provider builders today wire only EF Core's `DbContext` (`src/Rig.TUnit.Databases.Sql.SqlServer/Builder/SqlServerRigBuilder.cs`). There is no fluent surface to declare:

- Schemas, tables, columns, primary / foreign keys.
- Indexes (incl. filtered, columnstore, full-text, expression).
- Stored procedures, functions, triggers.
- Users / roles / permissions.
- Provider extensions (Postgres `CREATE EXTENSION`, MSSQL linked server, MySql plugins).
- Seed-statement bundles wired to F-010.

Today users either let EF Core run `EnsureCreatedAsync` (limited) or attach a raw `.sql` file at startup. Neither is fluent or composable.

This is the **direct SQL analogue of Feature 007's `WithTopology`** — the rig owns provider-native admin operations behind a fluent, idempotent, provider-scoped API.

## What we deliver

A `WithSchema(Action<ISqlSchemaBuilder>)` builder method on every SQL provider's RigBuilder, plus per-provider sub-interfaces (`ISqlServerSchemaBuilder`, `IPostgresSchemaBuilder`, etc.) holding only operations the engine supports — compile-time safety, mirroring 007's `IServiceBusTopologyBuilder` pattern.

## Public API surface (sketch)

```csharp
public interface ISqlSchemaBuilder // marker
{
    Task ApplyAsync(CancellationToken ct);
}

public interface IPostgresSchemaBuilder : ISqlSchemaBuilder
{
    IPostgresSchemaBuilder Schema(string name);
    IPostgresSchemaBuilder Table(string name, Action<IPostgresTableConfig> configure);
    IPostgresSchemaBuilder Extension(string name);                 // CREATE EXTENSION
    IPostgresSchemaBuilder Role(string name, string password, params string[] grants);
    IPostgresSchemaBuilder Function(string name, string body);
    IPostgresSchemaBuilder Trigger(string name, string table, string body);
    IPostgresSchemaBuilder PartitionedTable(string name, PartitionStrategy strategy, ...);
    IPostgresSchemaBuilder Procedure(string name, string body);
}

public interface ISqlServerSchemaBuilder : ISqlSchemaBuilder
{
    ISqlServerSchemaBuilder Schema(string name);
    ISqlServerSchemaBuilder Table(string name, Action<ISqlServerTableConfig> configure);
    ISqlServerSchemaBuilder LinkedServer(string name, string product, string source);
    ISqlServerSchemaBuilder ColumnstoreIndex(string table, params string[] columns);
    ISqlServerSchemaBuilder InMemoryTable(string name, Action<ISqlServerTableConfig> configure);
    ISqlServerSchemaBuilder Procedure(string name, string body);
    ISqlServerSchemaBuilder Trigger(string name, string table, string body);
    // ServiceBroker, FILESTREAM intentionally absent here (cross-deps in F-018)
}
```

Each provider declares only what it natively supports — no shared `WithFifo()`-style noise.

## Gaps closed

- SQL-1 from the gap analysis: schema/migration topology missing.
- Eliminates external `.sql` seed file pattern.
- Procedure / function / trigger declarations now first-class.

## Providers in scope

5: SqlServer, Postgresql, MySql, Oracle, Sqlite.

## Exit criteria

- `ISqlSchemaBuilder` marker + 5 provider-scoped interfaces ship with 100 % line coverage in introducing PR.
- Each provider has ≥ 4 RED-leading scenarios (table+PK, index, procedure, role).
- `ProviderCompletenessTests` extended with `SqlProviders_Declare_WithSchema` rule, parity coverage file, progressive enforcement (Phase 0 empty file, each provider phase appends).
- `docs/providers/{sqlserver,postgresql,mysql,oracle,sqlite}.md` updated with full schema example.
- `WithSchema` re-applied yields no error (idempotent).
- Migration tools (Flyway / DbUp / Liquibase) explicitly out-of-scope; this is for the **rig**, not for prod-app migrations.

## Dependencies on other planned features

- Upstream: none.
- Downstream: F-016 (transaction isolation matrix), F-017 (bulk + fast-restore), F-018 (CDC / temporal — needs CDC-enabled tables declared via WithSchema), F-019 (provider quirks: RLS / JSONB / FTS), F-038 (outbox/inbox schema needs WithSchema).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 015-sql-schema-and-migrations

Read first:
- planning/sql-schema-and-migrations/README.md
- planning/messaging-topology-and-sessions/Topology-Builder-Design.md (analogue pattern)
- src/Rig.TUnit.Databases.Sql/Builder/* (existing shape)
- src/Rig.TUnit.Messaging.ServiceBus/Topology/* (provider-scoped fluent reference)

Generate a feature spec that:
1. Introduces ISqlSchemaBuilder marker + 5 provider-scoped sub-interfaces (no shared fluent — per memory rule "compile-time over runtime").
2. WithSchema(Action<I{Provider}SchemaBuilder>) on each provider's RigBuilder.
3. Phase 0 lands marker + ProviderCompletenessTests parity rule + empty .schema-coverage.txt.
4. Phases 1..5 are the 5 SQL providers (parallel-eligible after Phase 0), each appending one line to the coverage file.
5. Phase 6 ships docs + a benchmark of WithSchema + WithSeedData (F-010) end-to-end.

Constraints:
- Each interface declares ONLY operations the engine natively supports (no no-ops, no throws).
- ApplyAsync is idempotent (re-running same declaration succeeds with no-op).
- Pre-release library — no [Obsolete].
- File-scoped namespaces, sealed concrete types, TUnit AAA.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
