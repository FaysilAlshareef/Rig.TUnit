# Planning — NoSQL collection & index topology (F-020)

**Feature ID**: F-020
**Family**: NoSQL
**Status**: planned
**Depends on**: —
**Target release**: v0.12
**Estimated tasks**: ~98 (Phase 0: 7 · 7 NoSQL providers × 12 tasks · 7 docs/bench)

---

## Why this feature exists

The NoSQL provider builders today are skeletal:
- `src/Rig.TUnit.Databases.NoSql.Mongo/Builder/MongoRigBuilder.cs` is essentially empty.
- `src/Rig.TUnit.Databases.NoSql.Cosmos/Builder/CosmosRigBuilder.cs` exposes only `AccountEndpoint`.
- `src/Rig.TUnit.Databases.NoSql.Cassandra/Builder/CassandraRigBuilder.cs` is empty.
- Dynamo / Elasticsearch / KurrentDb / Redis-as-NoSql have no fluent topology.

Every NoSQL test today either uses raw SDK calls to set up collections / indexes / keyspaces, or assumes the engine has them already (broken on a fresh container).

This is the **NoSQL analogue of Feature 007's `WithTopology`** for messaging and F-015's `WithSchema` for SQL.

## What we deliver

A `WithTopology(Action<I{Provider}TopologyBuilder>)` builder method per NoSQL provider. Per-provider sub-interfaces hold only operations the engine supports — same compile-time scoping pattern as 007.

### Per-provider surface
| Provider | Key declarations |
|----------|-------------------|
| Mongo | `Database`, `Collection`, `Index` (unique / TTL / text / 2dsphere / partial), `ChangeStream` enable |
| Cosmos | `Database`, `Container` (PK / hierarchical PK), `IndexingPolicy`, `Throughput` (manual / autoscale) |
| Cassandra | `Keyspace` (replication strategy), `Table`, `MaterializedView`, `SecondaryIndex` |
| Dynamo | `Table` (PK / SK), `GlobalSecondaryIndex`, `LocalSecondaryIndex`, `Stream`, `Ttl` |
| Elasticsearch | `Index` (mapping / settings / shards / replicas), `Alias`, `IndexTemplate`, `IlmPolicy` |
| KurrentDb | `Stream` (with metadata), `PersistentSubscription`, `Projection` |
| Redis (NoSQL role) | `Module` (RedisJSON / RediSearch), `JsonDocument`, `SearchIndex`, `User` (ACL) |

## Public API surface (sketch — Mongo example)

```csharp
public interface IMongoTopologyBuilder : ITopologyBuilder
{
    IMongoTopologyBuilder Database(string name, Action<IMongoDatabaseConfig>? configure = null);
}

public interface IMongoDatabaseConfig
{
    IMongoDatabaseConfig Collection(string name, Action<IMongoCollectionConfig>? configure = null);
}

public interface IMongoCollectionConfig
{
    IMongoCollectionConfig WithIndex(IndexKeysDefinition<BsonDocument> keys, Action<CreateIndexOptions>? options = null);
    IMongoCollectionConfig WithUniqueIndex(string field);
    IMongoCollectionConfig WithTtlIndex(string field, TimeSpan expiry);
    IMongoCollectionConfig WithTextIndex(params string[] fields);
    IMongoCollectionConfig WithChangeStream(); // enables, doesn't subscribe
}
```

Each provider follows the same shape with engine-appropriate operations.

## Gaps closed

- Mongo / Cosmos / Cassandra / Dynamo / Elasticsearch / KurrentDb / Redis topology not declarable in the rig.
- External JSON mappings / `cqlsh` scripts / CloudFormation templates currently required.

## Providers in scope

7: Mongo, Cosmos, Cassandra, Dynamo, Elasticsearch, KurrentDb, Redis.

## Exit criteria

- `ITopologyBuilder` (already a base marker from Feature 007) extended with NoSQL-friendly base ops if needed.
- 7 provider sub-interfaces ship with 100 % line coverage in their introducing PRs.
- `ProviderCompletenessTests` extended with `NoSqlProviders_Declare_WithTopology` rule, parity coverage file.
- Each provider has ≥ 4 RED scenarios (collection/table create, index, change-stream-or-equivalent enable, idempotent re-apply).
- `docs/providers/*.md` updated with `WithTopology` example per provider.

## Dependencies on other planned features

- Upstream: none (Feature 007 already provides the marker interface).
- Downstream: F-021 (consistency / conflict tests), F-022 (change feed), F-023 (RU / cost), F-024 (provider quirks).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 020-nosql-collection-and-index-topology

Read first:
- planning/nosql-collection-and-index-topology/README.md
- .dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md (analogue pattern)
- .dotnet-ai-kit/features/007-messaging-topology-sessions/data-model.md (per-provider sub-interface pattern)
- Mongo / Cosmos / Cassandra / Dynamo / Elasticsearch / KurrentDb / Redis admin SDK docs

Generate a feature spec that:
1. Reuses ITopologyBuilder marker from Feature 007 (no duplicate).
2. Adds 7 provider-scoped I{Provider}TopologyBuilder sub-interfaces, each declaring only what its engine supports.
3. WithTopology on each NoSQL RigBuilder.
4. Phase 0 lands ProviderCompletenessTests parity rule (.parity-coverage.txt re-used or sibling .nosql-parity-coverage.txt).
5. Phases 1..7 are the 7 providers (parallel-eligible after Phase 0).
6. Phase 6 ships docs + benchmarks for "topology setup time".

Constraints:
- Compile-time over runtime: no shared WithFifo()-style noise across engines.
- ApplyAsync idempotent on every provider.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
