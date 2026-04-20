# Rig.TUnit.Databases.Sql

> Family-base package for SQL-database test fixtures: EF Core provider contract, `SqlContract` suite, transaction helpers, `DbContextHelper<TContext>`.

## What this package is

The shared foundation for every SQL database provider Rig.TUnit ships
(MySql, Oracle, Postgresql, SqlServer, Sqlite). It defines
`SqlRigBuilder<TSelf>` with `ReplaceDbContext<T>`, the EF-agnostic
`DbContextHelper<TContext>`, `InMemoryDbExtensions`, and SQL-specific
assertions (`RawSqlAssert`). Also ships the `SqlContract` TUnit suite
(`[InheritsTests]`) that leaf integration projects run to prove parity of
semantics across engines: transactions, concurrency tokens, migration
apply/rollback, and streaming result sets.

Install this one directly only when you are writing a new SQL provider or
consuming provider-agnostic assertions.

## When to use it

- Authoring a new SQL backend (DuckDB, SingleStore, …).
- Sharing test fixtures across multi-engine integration tests.
- **Not for**: concrete SQL testing — install one of the five leaf packages.

## Prerequisites

- .NET 10 SDK
- Microsoft.EntityFrameworkCore 10.x
- The leaf provider's native client (Npgsql, Pomelo.MySql, Oracle.EFCore, etc.).

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `MigrationStrategy` | `MigrationStrategy` | `ApplyOnInit` | `ApplyOnInit` / `SkipMigrations` / `EnsureCreated`. |
| `CommandTimeoutSeconds` | `int` | `30` | EF `Database.SetCommandTimeout`. |
| `EnableDetailedErrors` | `bool` | `true` | Turn on `EnableDetailedErrors()`. |
| `EnableSensitiveDataLogging` | `bool` | `true` | Turn on `EnableSensitiveDataLogging()`. |

## Fixture + helper APIs

- `Rig.TUnit.Databases.Sql.Builder.SqlRigBuilder<TSelf>` — CRTP builder
- `Rig.TUnit.Databases.Sql.Helpers.DbContextHelper<TContext>`
- `Rig.TUnit.Databases.Sql.Extensions.InMemoryDbExtensions`
- `Rig.TUnit.Databases.Sql.Assertions.RawSqlAssert`
- `Rig.TUnit.Databases.Sql.Contracts.SqlContract` — family TUnit suite

## Per-test isolation

Each leaf provider materialises a fresh database/schema per test; strategy
varies (ephemeral DB, schema-per-test, or `IsolationKey` prefix). The base
contract suite relies on the per-test guarantee without assuming one
strategy.

## Parallelism + performance

## §9 — N/A: family-base; parallelism profile depends on the concrete
engine. Sqlite and Postgres are fully parallel; Oracle's session-setup cost
forces a lower `Iterations` value.

## Troubleshooting

- **`ModelBuilding` errors differ across engines** — use provider-specific
  `HasColumnType` calls inside `OnModelCreating` or guard with
  `Database.IsSqlServer()` / `Database.IsNpgsql()`.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- SQL engines differ on: identity columns, RETURNING clauses, case folding,
  max identifier length (30 Oracle, 64 MySql, 63 Postgres, 128 SqlServer),
  and default isolation level. Contract tests normalise these.

## Benchmarks

## §12 — N/A: family-base. Concrete leaves have individual
`Rig.TUnit.Benchmarks/*SqlBenchmarks.cs` entries tracked in
`benchmarks/baseline-005.json`.

## Related docs

- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
