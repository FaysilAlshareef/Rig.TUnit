# Rig.TUnit Performance Tuning

Guidance on when to use which provider for which test scenario. Per-provider
startup + per-test overhead callouts.

## Picking the right cache

| Scenario | Use |
|---|---|
| In-process unit test, single-node | `Caching.Memory` (no container, 0ms startup) |
| Integration test against real Redis protocol | `Caching.Redis` (~2s container start) |
| Testing backplane / invalidation | `Caching.Redis` + `RedisBackplaneCapture` |
| HybridCache (local + distributed) | `Caching.Hybrid` |
| FusionCache features (fail-safe, eager refresh, stampede) | `Caching.Fusion` |

## Picking the right storage

| Scenario | Use |
|---|---|
| Pure filesystem smoke test | `Storage.FileSystem` (System.IO.Abstractions) |
| S3 emulation | `Storage.S3` via LocalStack |
| MinIO-specific quirks | `Storage.MinIO` |
| Azure Blob | `Storage.AzureBlob` via Azurite |

## Picking the right database

| Scenario | Use |
|---|---|
| Fastest possible SQL test | `Databases.Sql.Sqlite` (in-memory, ~50ms) |
| ANSI SQL correctness | `Databases.Sql.Postgresql` |
| SQL Server–specific T-SQL | `Databases.Sql.SqlServer` |
| Pomelo/MySql-specific behaviour | `Databases.Sql.MySql` |
| Oracle-specific PL/SQL | `Databases.Sql.Oracle` (slow; serialise) |
| Document DB | `Databases.NoSql.Mongo` or `.Cosmos` |
| Key-value store | `Databases.NoSql.Redis` |
| Wide-column / CQL | `Databases.NoSql.Cassandra` |

## Per-provider startup costs (approximate, CI runners)

| Provider | Container start | First query |
|---|---|---|
| Sqlite | 0 (in-memory) | <1 ms |
| MemoryCache | 0 | <1 ms |
| Redis | ~1.5 s | <5 ms |
| Postgres | ~3 s | <10 ms |
| SqlServer | ~10 s | <15 ms |
| Kafka | ~6 s | <20 ms |
| MongoDB | ~3 s | <10 ms |
| Cassandra | ~25 s | <30 ms |
| Oracle Free | ~60 s | <20 ms |
| Cosmos (Linux) | ~30 s | <50 ms |
| Service Bus emulator | ~20 s | <50 ms |
| LocalStack (S3/Sqs) | ~4 s | <10 ms |
| Azurite | ~2 s | <10 ms |

Numbers are _approximate, per-cold-start_. Steady-state per-test overhead is much
smaller — see per-provider benchmark files in `tests/Rig.TUnit.Benchmarks/` and the
merged `benchmarks/baseline-005.json` for authoritative numbers.

## General tuning

### Parallelism
- TUnit runs test classes in parallel by default.
- Providers that cannot coexist at the process level (env-var mutation tests,
  Oracle sessions) use `[NotInParallel(\"key\")]`.
- Shared-fixture providers that use per-test isolation helpers scale parallel test
  execution cleanly.

### Cold-path vs warm-path
- First test of a class starts the container (cold path).
- Subsequent tests reuse the container (warm path).
- Dispose happens once per test project — not per test — when using
  `SharedXxxFixture`.

### CI cache warming
Pull heavy images in parallel with build steps:
```yaml
- name: Pull provider image (warm cache)
  if: matrix.provider == 'Oracle'
  run: docker pull gvenzl/oracle-free:23.5-slim-faststart
```
This halves matrix runtime when the image is big.

### Benchmark-regression budget
`benchmarks/baseline-005.json` records per-benchmark mean/stdDev. The
`benchmark-regression` CI job (T166/T167) fails any PR where a benchmark
regresses > 20% from baseline.

## When to add a benchmark

- Hot path changes (fixture startup, per-test isolation helper, common
  serialization path)
- Memory allocation reduction work — `[MemoryDiagnoser]` catches regressions
- Before/after comparisons for refactors that touch allocating paths

Skip benchmarks for:
- Pure configuration classes (no hot path)
- Test-only helper classes that run at setup-time
