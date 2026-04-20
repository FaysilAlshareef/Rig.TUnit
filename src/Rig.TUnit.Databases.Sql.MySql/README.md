# Rig.TUnit.Databases.Sql.MySql

> Testcontainers-backed MySQL fixture using the official `mysql:8.4` image, with `MySqlConnector` raw access and Pomelo EF Core extensions.

## What this package is

A production-shape MySQL test fixture. `MySqlFixture` spins the official
`mysql` image via Testcontainers, waits for readiness via socket-connect,
and exposes `ConnectionString` / `Database` for raw `MySqlConnector` use or
any EF Core provider the caller installs. Integrates with the
`Rig.TUnit.Databases.Sql` family contract for cross-engine parity testing.

## When to use it

- Integration tests targeting MySQL-specific features (full-text search,
  spatial indexes, `JSON_TABLE`).
- Multi-engine tests where one matrix cell runs against MySQL.
- Verifying Pomelo EF Core behaviour against a real server.
- **Not for**: pure domain unit tests — use SQLite-in-memory instead.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (pulls `mysql:8.4`, ~600 MB first pull)
- If testing EF Core: Pomelo.EntityFrameworkCore.MySql (caller installs)

## Quick start

```csharp
using MySqlConnector;
using Rig.TUnit.Databases.Sql.MySql.Fixtures;

await using var fx = new MySqlFixture();
await fx.InitializeAsync();

await using var conn = new MySqlConnection(fx.ConnectionString);
await conn.OpenAsync();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ImageTag` | `string` | `"8.4"` | MySQL Docker image tag |
| `StartupTimeoutSeconds` | `int` | `180` | MySQL init can be slow on first pull |
| `Username` | `string` | `"root"` | Default container user |
| `Password` | `string` | `"rigtunit"` | Root password |
| `Database` | `string` | `"rigtunit"` | Default database created on startup |

## Fixture + helper APIs

- `Rig.TUnit.Databases.Sql.MySql.Fixtures.MySqlFixture`
- `Rig.TUnit.Databases.Sql.MySql.Options.MySqlFixtureOptions`
- `Rig.TUnit.Databases.Sql.MySql.Builder.MySqlRigBuilder`
- `UseMySql(RigBuilder, …)` extension

## Per-test isolation

Default strategy is one container per fixture (per-class isolation via
TUnit's default lifecycle). For per-test schema isolation, use
`IsolationKey.FromExecutionContext()` as the schema prefix and create it
in the test's Arrange step via `CREATE SCHEMA IF NOT EXISTS`.

## Parallelism + performance

- First-run container pull: ~20–40 s (one-time per CI cache warmup).
- Warm startup: ~4–8 s (MySQL init is slower than Postgres).
- Per-test DbContext open: ~20 ms.
- Safe under full test parallelism at class granularity.

## Troubleshooting

- **`ReplaceDbContext<T>()` throws `NotSupportedException`** — Pomelo
  EF Core 10 stable is pending (PR #2019). Install the prerelease or
  register your `DbContext` manually without the helper.
- **Port conflicts under heavy parallelism** — Testcontainers allocates
  ephemeral host ports by default; ensure you are not pinning a fixed port.

See [docs/troubleshooting.md#mysql](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- MySQL identifiers are case-insensitive by default on Windows containers;
  case-sensitive on Linux (most Testcontainers runs). Tests that depend on
  table-name casing must pin to Linux.
- `utf8mb4` is the default charset; legacy `utf8` is an alias for
  `utf8mb3` and does not store full Unicode.
- Pomelo's `ServerVersion.AutoDetect` opens a connection at model-building
  time — provide `ServerVersion.Create(8, 4, 0, ServerType.MySql)` in tests
  to keep build synchronous.

## Benchmarks

See [`MySqlBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MySqlBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.Sql`](../Rig.TUnit.Databases.Sql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
