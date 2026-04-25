# Planning — Snapshot / restore between tests (F-011)

**Feature ID**: F-011
**Family**: Cross-cutting
**Status**: planned
**Depends on**: F-010 (seed data — populate snapshot baseline)
**Target release**: v0.10
**Estimated tasks**: ~84 (Phase 0: 7 · 12 providers × 6 wiring tasks · 5 docs)

---

## Why this feature exists

A 2 k-row seed is fine to load **once**, brutal to load **per test**. CI runtimes for integration suites today are dominated by repeated seed work because there is no rig-wide snapshot/restore primitive. Real-world impact:

- A SqlServer integration suite of 80 tests reseeds 80 × 25 s = 33 min of CI per build.
- Postgres `pg_basebackup` and template-database cloning would cut that to seconds; the rig does not expose either.
- Mongo doesn't ship a built-in snapshot, but `mongodump` round-trip via test-fixture cache works.
- Cosmos emulator cannot snapshot natively; the rig must export+import containers.
- Cassandra `nodetool snapshot` is per-keyspace; unused today.
- S3 / MinIO snapshots are bucket-copy; trivial but unsupported.

`tests/Rig.TUnit.*.Tests.Integration` projects today either reseed (slow) or share state across tests (forbidden by `.claude/rules/testing.md`).

## What we deliver

Two new fixture-level methods on every storage-capable provider:

```csharp
ValueTask SnapshotAsync(string name, CancellationToken ct = default);
ValueTask RestoreSnapshotAsync(string name, CancellationToken ct = default);
```

Implementation strategy is per-provider (no one-size-fits-all):

| Provider | Mechanism |
|----------|-----------|
| Postgres | template-database clone (`CREATE DATABASE … TEMPLATE …`) |
| SqlServer | DB snapshot (`CREATE DATABASE … AS SNAPSHOT OF`), or restore-from-backup |
| MySql | `mysqldump` / `xtrabackup` on small data, schema+seed-replay on small fixtures |
| Sqlite | `.backup` API |
| Oracle | flashback or expdp/impdp |
| Mongo | collection clone via aggregation `$out` |
| Cosmos | container-export → container-import via SDK bulk |
| Cassandra | `nodetool snapshot` + restore |
| Elasticsearch | snapshot repository (`_snapshot` API) |
| Redis | `BGSAVE` + RDB swap |
| S3 / MinIO | `CopyObject` to snapshot prefix; restore via reverse copy |
| AzureBlob | blob snapshot API |
| FileSystem | directory-copy with `XCopy`/`robocopy`-style atomicity |

## Public API surface (sketch)

```csharp
public abstract partial class StorageCapableFixture
{
    public ValueTask SnapshotAsync(string name, CancellationToken ct = default);
    public ValueTask RestoreSnapshotAsync(string name, CancellationToken ct = default);
    public ValueTask DropSnapshotAsync(string name, CancellationToken ct = default);
}

public abstract partial class StorageCapableRigBuilder<TSelf>
{
    public TSelf WithSnapshotPolicy(SnapshotPolicy policy);
}

public enum SnapshotPolicy { OncePerSession, OncePerTestClass, Disabled }
```

## Gaps closed (from CC-4 in the gap analysis)

- Slow integration tests dominated by reseed cost.
- No native snapshot/restore primitive in any provider.
- No standard policy for "seed once, restore between tests".

## Providers in scope (wiring)

12 storage-capable providers across `src/Rig.TUnit.Databases.*` and `src/Rig.TUnit.Storage.*`.

## Exit criteria

- `SnapshotAsync` / `RestoreSnapshotAsync` ship on every storage-capable fixture.
- Each provider package has a RED scenario asserting `restore < seed` time delta (provider-tuned threshold).
- Benchmark deltas appended to `benchmarks/baseline-011.json`.
- `ProviderCompletenessTests` extended with `StorageProviders_Declare_SnapshotRestore` rule.
- `SnapshotPolicy.OncePerTestClass` documented as the recommended default in `docs/providers/*.md`.

## Dependencies on other planned features

- Upstream: **F-010** (seed-data factories — `WithSeedData` populates the baseline that gets snapshotted).
- Downstream: F-017 (SQL bulk + fast-restore deepens this for SQL family).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 011-snapshot-and-restore

Read first:
- planning/snapshot-and-restore/README.md
- planning/seed-data-factories/README.md (F-010 must be shipped first)
- Postgres template-database docs, SqlServer DB-snapshot docs
- Cosmos container-export/import sample
- planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md (parity matrix style)

Generate a feature spec that:
1. Introduces SnapshotAsync / RestoreSnapshotAsync on every storage-capable fixture.
2. Per-provider mechanism table (template DB, BGSAVE, blob snapshot, etc.).
3. Phase 0 lands the base contract + parity coverage file + SnapshotPolicy enum.
4. Each provider phase ships a RED benchmark proving restore beats reseed.
5. Phase 6 publishes benchmarks under benchmarks/baseline-011.json.

Constraints:
- Snapshots are per-fixture, not per-rig.
- RestoreSnapshotAsync MUST be idempotent (re-running same restore yields same state).
- DropSnapshotAsync MUST be invoked by fixture teardown — no leaked DBs / blobs.
- File-scoped namespaces, sealed concrete types, TUnit AAA, no Thread.Sleep.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, benchmarks scaffolding.
```
