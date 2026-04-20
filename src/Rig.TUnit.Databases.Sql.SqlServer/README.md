# Rig.TUnit.Databases.Sql.SqlServer

> Testcontainers-backed SQL Server fixture using the `mcr.microsoft.com/mssql/server:2022-latest` image, integrated with `Microsoft.EntityFrameworkCore.SqlServer`.

## What this package is

The Rig.TUnit SQL Server provider. `SqlServerFixture` spins the Microsoft
2022 Developer-edition container via Testcontainers and exposes a ready
`ConnectionString`. Integrates with the `Rig.TUnit.Databases.Sql` family
base for parity assertions and with EF Core via the standard
`UseSqlServer(connectionString)` wire.

## When to use it

- Integration tests targeting SqlServer-specific features (`OUTPUT INTO`,
  temporal tables, `MERGE`, rowversion).
- Multi-engine tests where SqlServer is a required matrix cell.
- Verifying migration scripts before deploy.
- **Not for**: unit tests — prefer SQLite in-memory.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (first pull ~1.5 GB)
- Linux or Windows-containers-enabled Docker daemon.

## Quick start

```csharp
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.SqlServer.Fixtures;

await using var fx = new SqlServerFixture();
await fx.InitializeAsync();

var opts = new DbContextOptionsBuilder<TestDb>()
    .UseSqlServer(fx.ConnectionString)
    .Options;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"mcr.microsoft.com/mssql/server:2022-latest"` | Container image |
| `StartupTimeoutSeconds` | `int` | `180` | SqlServer takes ~30-60s to warm |
| `SaPassword` | `string` | `"RigTUnit_P@ss1"` | SA password — must satisfy policy |
| `AcceptEula` | `bool` | `true` | Sets `ACCEPT_EULA=Y` |
| `Edition` | `string` | `"Developer"` | `Express` / `Developer` / `Standard` / `Enterprise` |

## Fixture + helper APIs

- `Rig.TUnit.Databases.Sql.SqlServer.Fixtures.SqlServerFixture`
- `Rig.TUnit.Databases.Sql.SqlServer.Options.SqlServerFixtureOptions`
- `Rig.TUnit.Databases.Sql.SqlServer.Builder.SqlServerRigBuilder`
- `UseSqlServer(RigBuilder, …)` extension

## Per-test isolation

Default is one container per fixture class; per-test DB isolation via
`CREATE DATABASE test_{IsolationKey}` + `DROP DATABASE` in the Arrange /
teardown pair. For heavy use, one fixture per test-class is typical — full
per-test containers are cost-prohibitive for SqlServer's warm-up time.

## Parallelism + performance

- First-run pull: ~60–90 s (~1.5 GB).
- Warm startup: ~30–60 s.
- Per-test DB create + drop: ~150 ms.
- Parallelism: cap at 4 unless your host has >16 GB RAM allocated to
  Docker — each container reserves ~2 GB.

## Troubleshooting

- **`The SA password does not meet SQL Server password policy`** — password
  must have 8+ chars + upper + lower + digit + symbol. The default
  `RigTUnit_P@ss1` satisfies this; override carefully.
- **Container OOM** — SqlServer's default memory target is 80 % of host;
  limit via `-e MSSQL_MEMORY_LIMIT_MB=2048` in the Testcontainers config.
- **`Login failed for user 'sa'`** — ephemeral container health-check
  reports ready before `sa` login is enabled. Fixture waits for a
  successful `SELECT 1` before returning, so normal calls are safe.

See [docs/troubleshooting.md#sqlserver](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `rowversion` / `timestamp` is a binary concurrency token — EF maps it to
  `byte[]` but reports `DataType=Binary`; use `IsConcurrencyToken()` in
  `OnModelCreating`.
- Max identifier length is 128 chars — longest in the SQL family.
- Case folding: default collation is `SQL_Latin1_General_CP1_CI_AS` (case
  INsensitive). Tests relying on exact case must use a case-sensitive
  collation.

## Benchmarks

See [`SqlServerBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SqlServerBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.Sql`](../Rig.TUnit.Databases.Sql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
