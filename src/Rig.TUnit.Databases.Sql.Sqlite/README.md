# Rig.TUnit.Databases.Sql.Sqlite

> In-memory shared-cache SQLite provider — the zero-container fast path for tests that need real SQL semantics.

## What this package is

The fast-path SQL provider. `SqliteFixture` opens a connection to
`Data Source=file::memory:?cache=shared` (or a file on disk when isolation
demands it) and holds it open for the fixture's lifetime so the in-memory
database persists across `DbContext` disposals. No container, no Docker,
no pull — tests using SQLite finish in tens of milliseconds.

Still participates in `Rig.TUnit.Databases.Sql`'s family contract so the
same parity assertions run against SQLite as against the container-backed
providers.

## When to use it

- Fast feedback loops where container startup cost dominates.
- Running the family contract on a machine without Docker.
- Verifying EF Core LINQ translation is legal across providers.
- **Not for**: features SQLite does not support — `DbType.RowVersion`,
  schema-aware queries, stored procedures, `FOR JSON`, full-text search
  beyond the FTS5 extension. Use a real Postgres/SqlServer for those.

## Prerequisites

- .NET 10 SDK
- `Microsoft.EntityFrameworkCore.Sqlite` 10.x (transitive)

## Quick start

```csharp
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;

await using var fx = new SqliteFixture();
await fx.InitializeAsync();

var opts = new DbContextOptionsBuilder<TestDb>()
    .UseSqlite(fx.Connection)
    .Options;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Mode` | `SqliteMode` | `SharedCacheMemory` | `SharedCacheMemory` / `FilePath` / `PrivateCacheMemory` |
| `FilePath` | `string?` | `null` | Only used when `Mode == FilePath` |
| `EnableForeignKeys` | `bool` | `true` | `PRAGMA foreign_keys = ON` |
| `JournalMode` | `string` | `"WAL"` | `WAL` / `DELETE` / `MEMORY` |

## Fixture + helper APIs

- `Rig.TUnit.Databases.Sql.Sqlite.Fixtures.SqliteFixture`
- `Rig.TUnit.Databases.Sql.Sqlite.Options.SqliteFixtureOptions`
- `Rig.TUnit.Databases.Sql.Sqlite.Builder.SqliteRigBuilder`
- `UseSqlite(RigBuilder, …)` extension

## Per-test isolation

SQLite in-memory is isolated by the fixture owning its open connection —
drop the fixture, the DB is gone. For file-mode, the file name includes
`IsolationKey` so parallel tests do not collide.

## Parallelism + performance

- Zero startup (no container).
- Per-test DbContext: ~5 ms.
- Safe under full parallelism — each fixture owns its own connection; no
  cross-test state.

## Troubleshooting

- **`SQLite Error 19: 'FOREIGN KEY constraint failed'`** — defaults to ON
  in the fixture; older codebases assuming OFF will see new failures. Set
  `EnableForeignKeys = false` if intentional.
- **Data disappears between tests** — the connection was closed; ensure
  the fixture's connection is the one `DbContext` uses, not a new one.

See [docs/troubleshooting.md#sqlite](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- SQLite is dynamically typed — `decimal` round-trip precision is lossy;
  use `TEXT` storage (`HasConversion<string>()`) for money.
- No schemas; `[Table("dbo.Orders")]` translates to `"dbo.Orders"` (literal
  identifier with dot), not a schema-qualified name.
- `DATETIME` is stored as TEXT ISO-8601 by default; timezone round-trip
  loses tzinfo unless you use `DateTimeOffset`.

## Benchmarks

See [`SqliteBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SqliteBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. SQLite is the family's
speed-of-light reference.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.Sql`](../Rig.TUnit.Databases.Sql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
