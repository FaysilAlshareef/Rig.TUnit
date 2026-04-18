# Review Report: 003-rig-tunit-ecosystem-expansion

**Date**: 2026-04-18 | **Mode**: generic (.NET 10 / TUnit test ecosystem) | **Reviewer**: dotnet-ai-kit reviewer

**Scope**: 183 changed files since `2901ee5` (Phase A merge gate). 47 new shipped packages under `src/`, 49 test projects under `tests/`, ~272/274 tasks complete (T403/T404 deferred).

---

## Repo: Rig.TUnit (NEEDS FIXES — non-blocking)

Build: GREEN (0 warnings, 0 errors via `dotnet build Rig.TUnit.slnx`)
Tests: 646/646 GREEN per implementation log.

### Standards Review

#### 1. Naming Conventions — 6 violations [MEDIUM]
Six fixtures call an isolation-key helper named for a *different* backing store. The helper itself just lowercases + truncates a string, so behaviour is correct, but the API reads as cross-store contamination and will mislead future maintainers.

| File | Line | Current | Recommended |
|---|---|---|---|
| `src/Rig.TUnit.Databases.NoSql.Mongo/Fixtures/MongoFixture.cs` | 28 | `IsolationKey.ForPostgresDatabase()` | `ForMongoDatabase()` (add to `IsolationKey`) |
| `src/Rig.TUnit.Databases.NoSql.EventStore/Fixtures/EventStoreFixture.cs` | 13 | `ForPostgresDatabase()` | `ForEventStoreDatabase()` |
| `src/Rig.TUnit.Databases.NoSql.ElasticSearch/Fixtures/ElasticSearchFixture.cs` | 13 | `ForPostgresDatabase()` | `ForElasticIndex()` |
| `src/Rig.TUnit.Databases.NoSql.Dynamo/Fixtures/DynamoFixture.cs` | 15 | `ForPostgresDatabase()` | `ForDynamoTable()` |
| `src/Rig.TUnit.Databases.NoSql.Cassandra/Fixtures/CassandraFixture.cs` | 13 | `ForPostgresDatabase()` | `ForCassandraKeyspace()` |
| `src/Rig.TUnit.Storage/Fixtures/StorageFixtureBase.cs` | 11 | `IsolationKey.ForRedisKeyPrefix()` for `ContainerName` | `ForStorageContainer()` or generic `ForBucket()` |

**Fix**: extend `Rig.TUnit.Core/IsolationKey.cs` (lines 58–81) with per-store helpers (most are 1-line wrappers around the existing truncate-and-lowercase logic). All callers update by find-replace. Zero behaviour change; purely a naming/API-clarity fix.

#### 2. Architecture Boundary Violations — none found [PASS]
Layered dependency tree intact. Meta-packages (`Rig.TUnit.All`, `Rig.TUnit`, `Rig.TUnit.Microservices`) contain only `<ProjectReference>` entries — no source leakage.

#### 3. Localization — N/A
Project does not use `Phrases.resx`. Skipped per rule.

#### 4. Error Handling — 1 finding [LOW]
- `src/Rig.TUnit.Resilience/Assertions/CircuitBreakerAssert.cs:54` — `catch (Exception ex)` is **justified** (the user-supplied `action` delegate can throw any exception type to drive the breaker), and the catch records into an `observedFailures` collection (not swallowed). Acceptable; comments at lines 56–58 document the intent. **No change required**, but consider adding `[SuppressMessage("Design", "CA1031")]` for clarity to static analyzers.
- `async void`: 0 occurrences ✅
- `DateTime.Now` / `DateTimeOffset.Now`: 0 occurrences ✅
- `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`: 0 occurrences ✅

#### 5. Testing — 2 findings [MEDIUM]
| Package | Issue | Severity |
|---|---|---|
| `src/Rig.TUnit.Docker/` | Has `Fixtures/` source, no `Rig.TUnit.Docker.Tests.*` project | MEDIUM |
| `src/Rig.TUnit.Security/` | Has Assertions/Builder/Contracts/Fixtures, no `Rig.TUnit.Security.Tests.*` project (provider-level Jwt/OAuth/Mtls/Policies are tested individually, but the base contract is not) | MEDIUM |

Stale folders (no `.csproj`, only `obj/`): `src/Rig.TUnit.ServiceBus/`, `src/Rig.TUnit.SqlServer/`. **Recommend deletion** [LOW].

`Task.Delay(...)` is used in 7 test files for time-based scenarios (cache-expiry, rate-limit windows, retry back-off). Per `rules/testing.md` these *should* migrate to `TimeProvider.Testing`/`FakeTimeProvider`, but for testing **real wall-clock providers** (Polly, FusionCache, RabbitMQ broker delivery) the delays are unavoidable. Acceptable [LOW].

#### 6. Security — 0 critical findings [PASS]
All "password"/"secret" hits are **test-container default credentials** (Postgres `postgres`, Mongo `mongo`, MinIO `minioadmin`, RabbitMQ `guest`, LocalStack `test`/`test`). These run inside ephemeral Docker containers that never reach a network — by design and unavoidable for Testcontainers integration. Not a finding.

`SaPassword = "Your_password123!"` in `SqlServerFixtureOptions` is the SQL Server image's documented dev default. Acceptable.

#### 7. Event Structure — N/A
Generic library, not microservice services. Skipped.

#### 8. Performance — 1 finding [MEDIUM]
- `src/Rig.TUnit.WebAPI/Authentication/TestAuthenticationOptions.cs:12` — `public string DefaultUserName { get; set; }` uses `set` instead of `init`. Per `rules/coding-style.md` and `architecture-profile.md` ("MUST use `private set` … NEVER expose public setters on domain objects"), DTO-style options classes should use `init`-only properties for immutability (this is the pattern used by every other `*Options` class in the solution — see `MongoFixtureOptions`, `PostgresFixtureOptions`, `SqlServerFixtureOptions`). Trivial 1-line fix.

`AsNoTracking()` not applicable in this code base (test-fixture library, not a query layer).

#### 9. Brief Compliance — N/A
Single-repo (no microservice secondary repos for this feature).

### CodeRabbit
Not run — `coderabbit` CLI not detected on PATH. Standards review only. To enable, install from <https://coderabbit.ai/cli>.

### Auto-Fixed
None applied (no `--auto-fix` flag passed).

---

## Summary

- **Total findings**: 11
- **CRITICAL**: 0
- **HIGH**: 0
- **MEDIUM**: 9 (6× isolation-key naming, 2× missing tests, 1× public setter)
- **LOW**: 2 (justified `catch (Exception)` with comment, orphan `obj/` folders)
- **Auto-fixed**: 0
- **Remaining**: 11

### Recommended Fix Order

1. **Naming (6×)** — add per-store helpers to `IsolationKey`, find-replace fixtures. ~10 min, zero behaviour change. Submit as one chore commit.
2. **`init` setter** — `TestAuthenticationOptions.DefaultUserName`. 1 line, 1 min.
3. **Missing tests** — add `Rig.TUnit.Docker.Tests.Integration` (smoke-test container start/stop) and `Rig.TUnit.Security.Tests.Unit` (assertions on the base `SecurityFixtureBase`). ~30 min each.
4. **Orphan folders** — `git rm -r src/Rig.TUnit.ServiceBus src/Rig.TUnit.SqlServer`. 1 min.

None of these block the merge gate (build + tests both green). All are non-functional cleanups improving API clarity and test coverage.

### Deferred from implement phase (already documented)
- T403/T404 — MySql provider (blocked: Pomelo.EntityFrameworkCore.MySql 10.0 not on NuGet)
- T703 — `MetaPackages_HaveZeroSourceFiles` architecture test (manually verified for `Rig.TUnit.All`, automation pending)
- T801/T803/T807 — coverage gate, benchmark baseline, full CI matrix YAML

### Next
- `/dotnet-ai.verify` — run integration test suite end-to-end with Docker.
- Apply the 6 naming fixes + `init` setter fix as a follow-up cleanup commit before merge.
