# Rig.TUnit.Databases.Sql.Oracle

> Testcontainers-backed Oracle fixture using `gvenzl/oracle-free:23.5-slim-faststart` and `Oracle.EntityFrameworkCore`.

## What this package is

An Oracle Free integration fixture. `OracleFixture` spins the
`gvenzl/oracle-free:23.5-slim-faststart` image — the fastest-to-boot Oracle
container variant (~60–90 s warm, considerably more on first pull) — and
exposes a working connection string plus an EF Core extension
(`UseOracle`) for the `Oracle.EntityFrameworkCore` provider.

Integrates with `Rig.TUnit.Databases.Sql`'s family contract so the same
semantic assertions run across MySql / Postgres / Oracle / SqlServer /
Sqlite.

## When to use it

- Integration tests targeting Oracle-specific features (sequences, PL/SQL,
  `MERGE`, `RETURNING INTO`).
- Multi-engine parity testing where Oracle is a required cell.
- Verifying behaviour under the OCP licence — Oracle EF Core is free.
- **Not for**: unit tests. Oracle's session setup is the slowest in the
  family; prefer SQLite in-memory for fast feedback loops.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (first pull ~2 GB)
- `Oracle.EntityFrameworkCore` transitively included via this package.

## Quick start

```csharp
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Oracle.Extensions;
using Rig.TUnit.Databases.Sql.Oracle.Fixtures;

await using var fx = new OracleFixture();
await fx.InitializeAsync();

var opts = new DbContextOptionsBuilder<TestDb>()
    .UseOracle(fx.ConnectionString)
    .Options;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"gvenzl/oracle-free:23.5-slim-faststart"` | Container image |
| `StartupTimeoutSeconds` | `int` | `300` | First pull can exceed 3 min |
| `Username` | `string` | `"rigtunit"` | Test schema user |
| `Password` | `string` | `"rigtunit"` | Test schema password |

## Fixture + helper APIs

- `Rig.TUnit.Databases.Sql.Oracle.Fixtures.OracleFixture`
- `Rig.TUnit.Databases.Sql.Oracle.Options.OracleFixtureOptions`
- `Rig.TUnit.Databases.Sql.Oracle.Builder.OracleRigBuilder`
- `UseOracle(RigBuilder, …)` extension
- `UseOracle(DbContextOptionsBuilder, string)` — EF wiring

## Per-test isolation

Oracle uses schema-per-test via `IsolationKey`-derived user names. Tests
create `CREATE USER {iso} IDENTIFIED BY …` in Arrange and `DROP USER
{iso} CASCADE` in teardown. Because `CREATE USER` requires session privs,
the fixture pre-provisions a test-DBA schema.

## Parallelism + performance

- First-run container pull: ~3–5 min (large image).
- Warm startup: ~60–90 s.
- Per-test schema create: ~1–2 s (Oracle's session setup is the slowest in
  the SQL family).
- Under parallel execution: cap `Iterations` at 2–4 — more will exhaust
  Oracle's default `processes` limit (150).

## Troubleshooting

- **`ORA-00020: maximum number of processes exceeded`** — reduce
  `Iterations` in your `ParallelIsolationContract` subclass, or configure
  the container with `-e ORACLE_PROCESSES=500`.
- **Startup timeout** — `gvenzl/oracle-free:23.5-slim-faststart` is the
  fastest variant; `oracle-free:23.5-slim` is 2× slower; the full
  `oracle-free:23.5` is 5× slower.

See [docs/troubleshooting.md#oracle](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Oracle identifiers are case-sensitive ONLY when quoted. EF Core quotes by
  default — expect `"Orders"` vs `orders` distinctness.
- Max identifier length 30 chars (23c extends to 128 but only when
  `COMPATIBLE=12.2` is disabled; Free ships with it enabled).
- `NUMBER` with no precision maps to `decimal(38,0)` by default — specify
  `HasPrecision(…)` in `OnModelCreating` to avoid silent truncation.
- `TIMESTAMP WITH LOCAL TIME ZONE` behaviour differs from `WITH TIME ZONE`
  — Oracle stores UTC for the former, offset for the latter.

## Benchmarks

See [`OracleBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/OracleBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. Oracle's per-test cost is the
largest in the SQL family — tracked closely.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.Sql`](../Rig.TUnit.Databases.Sql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
