# Rig.TUnit.Databases.NoSql.Mongo

> Testcontainers-backed MongoDB fixture with `CollectionPerTestHelper` and `BsonDiff`.

## What this package is

The Rig.TUnit MongoDB provider. `MongoFixture` spins MongoDB via
Testcontainers and exposes an `IMongoClient` + default-database. Two
helpers solve the common pain points: `CollectionPerTestHelper` owns a
per-test collection scoped by `IsolationKey` and drops it on disposal,
and `BsonDiff` performs a structural diff between expected / actual
documents with the usual system-field scrub list.

## When to use it

- Integration tests targeting MongoDB-specific features (change streams,
  `$lookup`, text indexes, transactions with replica set).
- Asserting document shape with tolerance for `_id` / `ts` drift.
- **Not for**: unit tests — MongoDB's in-memory driver `Mongo2Go` is
  faster but produces different wire errors from the real server.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (MongoDB image ~200 MB)
- `MongoDB.Driver` 3.x (transitive)

## Quick start

```csharp
using MongoDB.Bson;
using Rig.TUnit.Core.Helpers;
using Rig.TUnit.Databases.NoSql.Mongo.Fixtures;
using Rig.TUnit.Databases.NoSql.Mongo.Helpers;

await using var fx = new MongoFixture();
await fx.InitializeAsync();

await using var scope = new CollectionPerTestHelper(
    fx.Database, IsolationKey.FromExecutionContext());
var orders = scope.GetCollection<BsonDocument>();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"mongo:7"` | Image |
| `StartupTimeoutSeconds` | `int` | `60` | Mongo boots fast |
| `ReplicaSetName` | `string?` | `null` | Set for transactions; off by default |
| `AuthDatabase` | `string?` | `null` | Off by default in dev mode |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.Mongo.Fixtures.MongoFixture`
- `Rig.TUnit.Databases.NoSql.Mongo.Options.MongoFixtureOptions`
- `Rig.TUnit.Databases.NoSql.Mongo.Builder.MongoRigBuilder`
- `Rig.TUnit.Databases.NoSql.Mongo.Helpers.CollectionPerTestHelper`
- `Rig.TUnit.Databases.NoSql.Mongo.Assertions.BsonDiff`

## Per-test isolation

`CollectionPerTestHelper` names collections `{logical}_{IsolationKey:short}`
and calls `Database.DropCollectionAsync` on `DisposeAsync`. Fully
parallel-safe.

## Parallelism + performance

- First-run pull: ~15 s.
- Warm startup: ~2 s.
- Per-test collection create + drop: ~30 ms.
- Parallelism: 8+ concurrent tests — collection-level isolation works well.

## Troubleshooting

- **Transactions fail with `Standalone servers do not support`** — set
  `ReplicaSetName = "rs0"`; the fixture wires the replicaset init script.
- **`BsonDiff` reports diff on `_id`** — default scrub list includes
  `_id`, `_etag`, `_ts`. If your domain exposes a domain-level `Id`
  alongside `_id`, scrub it too.

See [docs/troubleshooting.md#mongo](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- MongoDB's default write-concern is `w: 1` (acknowledged by primary) —
  tests asserting durability across a restart must use `w: "majority"`.
- Index creation is synchronous for small data, async for large. Test
  helpers assume small.
- Driver 3.x uses `MongoClient` which is thread-safe; do not wrap in
  `using` — dispose is process-level.

## Benchmarks

See [`MongoBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MongoBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
