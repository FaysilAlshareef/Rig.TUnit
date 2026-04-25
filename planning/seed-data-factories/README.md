# Planning — Seed-data factories (F-010)

**Feature ID**: F-010
**Family**: Cross-cutting
**Status**: planned
**Depends on**: —
**Target release**: v0.9
**Estimated tasks**: ~84 (Phase 0: 7 · 12 storage-capable providers × 6 wiring tasks · 5 docs)

---

## Why this feature exists

Every test in the rig that needs > 5 rows reinvents its own seed code. There is no shared `Faker<T>` / `Bogus` / `AutoFixture` integration, no bulk-loader fast path. Today users hand-write `for (var i = 0; i < 1000; i++) await ctx.AddAsync(...)`, then `SaveChangesAsync` chokes on row-by-row inserts. Real-world consequences:

- A 10 k-row seed for a Postgres integration test takes 25–60 s — CI burn.
- A 1 k-document Mongo seed runs as 1 000 individual inserts when `InsertManyAsync` would batch.
- Cosmos seeds skip the bulk-executor SDK and pay RU for every individual write.
- Dynamo seeds blow through 25-item `BatchWriteItem` limits silently.
- S3 seeds upload one file at a time when concurrent uploads are trivially parallel.

There is also no standard way to **regenerate** seeds (for randomised property tests) — `Bogus` is the obvious .NET answer but the rig has no integration.

## What we deliver

- A `WithSeedData<T>(Action<ISeedBuilder<T>>)` builder method on every storage-capable RigBuilder.
- `ISeedBuilder<T>` accepts a `Faker<T>` (Bogus) instance or a hand-rolled `IEnumerable<T>` / `IAsyncEnumerable<T>`.
- Per-provider **bulk adapters** mapped to native fast paths:
  - SqlServer → `SqlBulkCopy`
  - Postgres → `COPY FROM STDIN` (Npgsql binary import)
  - MySql → multi-row `INSERT` or `LOAD DATA INFILE`
  - Mongo → `InsertManyAsync` with `Ordered = false`
  - Cosmos → bulk-executor / `AllowBulkExecution`
  - Dynamo → batched `BatchWriteItemRequest` (chunked at 25)
  - Cassandra → unlogged batch
  - Elasticsearch → `_bulk` API
  - Redis → pipelined `MSET`
  - S3 / MinIO / AzureBlob → parallel upload with bounded concurrency
  - FileSystem → parallel writes

## Public API surface (sketch)

```csharp
public interface ISeedBuilder<T>
{
    ISeedBuilder<T> WithFaker(Func<Faker<T>, Faker<T>> configure);
    ISeedBuilder<T> WithExplicit(IEnumerable<T> rows);
    ISeedBuilder<T> WithCount(int count);
    ISeedBuilder<T> WithSeed(int randomSeed); // reproducible
    ISeedBuilder<T> WithBatchSize(int size);
}

public abstract partial class StorageCapableRigBuilder<TSelf>
{
    public TSelf WithSeedData<T>(string targetName, Action<ISeedBuilder<T>> configure);
}
```

## Gaps closed (from CC-3 in the gap analysis)

- Slow seed loops in every storage-capable provider.
- No standard `Bogus` `Faker<T>` integration in the rig.
- No reproducible-randomness (`WithSeed(int)`).
- Native bulk-load APIs unused.

## Providers in scope (wiring)

| Package | Bulk adapter |
|---------|--------------|
| `src/Rig.TUnit.Databases.Sql.SqlServer` | `SqlBulkCopy` |
| `src/Rig.TUnit.Databases.Sql.Postgresql` | `COPY FROM STDIN` |
| `src/Rig.TUnit.Databases.Sql.MySql` | multi-row `INSERT` / `LOAD DATA` |
| `src/Rig.TUnit.Databases.Sql.Oracle` | `OracleBulkCopy` |
| `src/Rig.TUnit.Databases.Sql.Sqlite` | transaction-scoped multi-insert |
| `src/Rig.TUnit.Databases.NoSql.Mongo` | `InsertManyAsync` |
| `src/Rig.TUnit.Databases.NoSql.Cosmos` | bulk-executor |
| `src/Rig.TUnit.Databases.NoSql.Dynamo` | `BatchWriteItem` chunked |
| `src/Rig.TUnit.Databases.NoSql.Cassandra` | unlogged batch |
| `src/Rig.TUnit.Databases.NoSql.ElasticSearch` | `_bulk` |
| `src/Rig.TUnit.Databases.NoSql.Redis` | pipelined `MSET` |
| `src/Rig.TUnit.Storage.*` | bounded parallel upload |

## Exit criteria

- `ISeedBuilder<T>` + `WithSeedData` ship in base; 100 % line coverage.
- Each provider's bulk adapter benchmarked vs naive insert; benchmark deltas appended to `benchmarks/baseline-010.json`.
- Each provider package adds 1 RED scenario per fast path (`Seed_10k_Rows_CompletesUnder(span)`).
- `ProviderCompletenessTests` extended with `StorageProviders_Declare_WithSeedData` rule.
- `docs/providers/*.md` updated per touched provider with seed example.
- `Bogus` package added once to `Directory.Packages.props`; referenced only from base seed library.

## Dependencies on other planned features

- Upstream: none.
- Downstream: F-011 (snapshot/restore — uses seeds to populate a snapshot baseline once), F-017 (SQL bulk + fast restore is a per-provider deepening of the SQL bulk adapters started here).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 010-seed-data-factories

Read first:
- planning/seed-data-factories/README.md
- https://github.com/bchavez/Bogus README + Faker<T> usage
- src/Rig.TUnit.Databases.Sql.SqlServer/* (existing fixture shape)
- planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md (parity matrix style)

Generate a feature spec that:
1. Introduces ISeedBuilder<T> + WithSeedData on every storage-capable RigBuilder.
2. Per-provider bulk adapters mapped to native fast paths (SqlBulkCopy, COPY FROM STDIN, BatchWriteItem, etc.).
3. Phase 0 lands base contract + Bogus integration + parity coverage file.
4. Each provider phase delivers a RED scenario asserting "10 k rows under 2 s" (or provider-tuned threshold).
5. Phase 6 publishes benchmarks under benchmarks/baseline-010.json.

Constraints:
- Reproducible randomness via WithSeed(int).
- Bulk adapters honour CancellationToken throughout.
- Pre-release library — no [Obsolete] aliases.
- Honour the parity-coverage progressive enforcement pattern from Feature 007.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, benchmarks scaffolding.
```
