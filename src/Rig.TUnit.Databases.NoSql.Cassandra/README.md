# Rig.TUnit.Databases.NoSql.Cassandra

> Testcontainers-backed Apache Cassandra fixture with `KeyspacePerTestHelper` for injection-safe per-test keyspaces.

## What this package is

The Rig.TUnit Cassandra provider. `CassandraFixture` spins Apache
Cassandra via Testcontainers and exposes a `Session` ready for CQL.
`KeyspacePerTestHelper` is the novel piece — it validates keyspace names
via a CQL-identifier whitelist (48-char cap, `[a-z_][a-z0-9_]*`
alphabet, no `"` injection possible) and issues `CREATE KEYSPACE` /
`DROP KEYSPACE` per test inside an `IAsyncDisposable` scope.

## When to use it

- Integration tests hitting a real Cassandra cluster (single-node dev
  replica is sufficient for most cases).
- Verifying CQL against the server's actual parser (string comparison of
  CQL against the driver's parser is not enough).
- **Not for**: unit tests — the startup cost is significant.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (first pull ~600 MB)
- `CassandraCSharpDriver` 3.x (transitive)

## Quick start

```csharp
using Cassandra;
using Rig.TUnit.Core.Helpers;
using Rig.TUnit.Databases.NoSql.Cassandra.Fixtures;
using Rig.TUnit.Databases.NoSql.Cassandra.Helpers;

await using var fx = new CassandraFixture();
await fx.InitializeAsync();

var key = IsolationKey.FromExecutionContext();
await using var scope = await KeyspacePerTestHelper.CreateAsync(
    fx.Session, key, prefix: "orders");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"cassandra:5.0"` | Container image |
| `StartupTimeoutSeconds` | `int` | `180` | Cassandra boot is slow |
| `ReplicationStrategy` | `string` | `"SimpleStrategy"` | Keyspace default |
| `ReplicationFactor` | `int` | `1` | For single-node test cluster |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.Cassandra.Fixtures.CassandraFixture`
- `Rig.TUnit.Databases.NoSql.Cassandra.Options.CassandraFixtureOptions`
- `Rig.TUnit.Databases.NoSql.Cassandra.Builder.CassandraRigBuilder`
- `Rig.TUnit.Databases.NoSql.Cassandra.Helpers.KeyspacePerTestHelper`

## Per-test isolation

`KeyspacePerTestHelper.CreateAsync(session, isolationKey, prefix)` returns
an `IAsyncDisposable` scope owning a keyspace named
`{prefix}_{IsolationKey:short}`. `BuildSafeKeyspace` enforces the CQL
identifier whitelist (no `"`, `;`, `--`, whitespace; 48-char cap; lowercase
start; underscore-alphanumeric continuations). On dispose: `DROP KEYSPACE`.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~45–90 s (Cassandra is slow to boot).
- Per-test keyspace create + drop: ~300 ms.
- Parallelism: 4–8 tests concurrently is typical; each keyspace is
  isolated so contention is minimal.

## Troubleshooting

- **`Unconfigured table`** — the session's keyspace is still the default
  `system`; either call `USE {keyspace}` or fully qualify table names.
- **`Unable to connect to any servers`** — the container is still warming
  up. Fixture waits for `SELECT release_version FROM system.local` but
  under heavy parallel startup the wait may time out; raise
  `StartupTimeoutSeconds`.

See [docs/troubleshooting.md#cassandra](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Cassandra is eventually consistent by default (`ONE` consistency
  level). Tests asserting after a write must use `CL.LOCAL_QUORUM` or
  poll; never assume read-your-writes.
- Identifiers are lowercased unless quoted — but the quote form is not
  safe against SQL injection, which is why `KeyspacePerTestHelper`
  enforces the strict whitelist instead of quoting.

## Benchmarks

See [`CassandraBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/CassandraBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
