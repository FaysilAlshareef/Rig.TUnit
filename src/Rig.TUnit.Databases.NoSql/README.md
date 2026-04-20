# Rig.TUnit.Databases.NoSql

> NoSQL family-base: `INoSqlRig`, `DocumentFixtureBase`, `NoSqlRigBuilder<TSelf>`, `JsonDocumentAssert`, `ChangeFeedCapture`.

## What this package is

The shared contract for every NoSQL / document / search / event-store
provider Rig.TUnit ships (`.Cassandra`, `.Cosmos`, `.Dynamo`,
`.ElasticSearch`, `.KurrentDb`, `.Mongo`, `.Redis`). Defines
`DocumentFixtureBase` (partition/collection create + teardown),
`NoSqlRigBuilder<TSelf>` (CRTP), `JsonDocumentAssert` (system-field-
scrubbing deep equality — ignores `_ts`, `_etag`, `_self`, `_rid` during
comparison), and `ChangeFeedCapture` (records emitted events for assertion).

Install one of the leaf packages directly — this base is useful only for
provider authors and cross-engine test harness code.

## When to use it

- Authoring a new document-store backend.
- Writing provider-agnostic NoSQL helpers.
- **Not for**: concrete NoSQL testing — install a leaf.

## Prerequisites

- .NET 10 SDK
- `Newtonsoft.Json` (transitive — used for partial-scrub comparison).

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
| `CollectionPrefix` | `string` | `$"test-{IsolationKey}"` | Applied to every container / table / keyspace name |
| `DropOnDispose` | `bool` | `true` | Teardown removes the per-test collection |
| `SystemFieldsToScrub` | `string[]` | `["_ts","_etag","_self","_rid","ETag"]` | Scrubbed by `JsonDocumentAssert` |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.INoSqlRig`
- `Rig.TUnit.Databases.NoSql.Fixtures.DocumentFixtureBase`
- `Rig.TUnit.Databases.NoSql.Builder.NoSqlRigBuilder<TSelf>`
- `Rig.TUnit.Databases.NoSql.Assertions.JsonDocumentAssert`
- `Rig.TUnit.Databases.NoSql.Helpers.ChangeFeedCapture`

## Per-test isolation

Each leaf provider names its container / collection / keyspace with the
`IsolationKey`. Teardown removes the named unit. The base package enforces
the naming contract but does not itself know how to create/delete — that
is the leaf's job.

## Parallelism + performance

## §9 — N/A: family-base; parallelism profile depends on the provider. See
each leaf for the cost model (Cosmos emulator is Linux-only, DynamoDB is
cheap, Cassandra's keyspace create is expensive).

## Troubleshooting

- **`JsonDocumentAssert` reports a diff on `_ts`** — the default scrub list
  missed a provider-specific system field. Override via
  `SystemFieldsToScrub`.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Every leaf has at least one system field that legitimately differs per
  write; the default scrub list covers the common ones but exotic
  providers (event-sourcing stores with version vectors) need extensions.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
