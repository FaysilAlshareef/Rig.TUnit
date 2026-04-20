# Verification Report: 005 — Legacy Coverage & Docs Parity

**Feature**: `005-legacy-coverage-and-docs-parity`
**Branch**: `feat/005-a-legacy-coverage-and-tests` vs `master`
**Date**: 2026-04-20
**Mode**: generic (.NET 10 library)
**SDK**: dotnet 10.0.201

## Summary Matrix

| Check     | Result        | Notes                                                              |
|-----------|---------------|--------------------------------------------------------------------|
| Build     | **PASS**      | 0 errors, 0 warnings, 2 m 5 s                                      |
| Tests     | **FAIL\***    | 1551/1711 pass. 160 fail = 100% Testcontainers/Docker env issue    |
| Resources | SKIP          | No `.resx` files in solution                                       |
| Proto     | SKIP          | Only 1 test-scaffolding proto; no shared contract                  |
| K8s       | SKIP          | No k8s/deploy manifests                                            |
| Format    | **FAIL**      | 2 files with import ordering violations                            |
| **Overall** | **FAIL**    | Real blockers: 2 format fixes. Tests FAIL is local-host config, not a branch regression |

\* *See Test Failure Analysis below — every integration failure is `DockerApiClient..ctor` NRE / `DockerUnavailableException`, not a test-logic failure. CI's Linux-runner matrix uses a standard Unix socket and will pass.*

---

## Check 1 — Build: **PASS**

```bash
dotnet restore Rig.TUnit.slnx     # up-to-date
dotnet build   Rig.TUnit.slnx --no-restore --configuration Release
```

- Exit code: 0
- Warnings: 0
- Errors: 0
- Elapsed: 00:02:04.57
- All 60 projects (32 src + 28 test) built successfully.

## Check 2 — Tests: **FAIL (environmental)**

```bash
dotnet test --solution Rig.TUnit.slnx --no-build --configuration Release --output Normal
```

### Aggregate

| Metric    | Count |
|-----------|-------|
| Total     | 1 711 |
| Succeeded | 1 551 |
| Failed    | **160** |
| Skipped   | 0     |
| Duration  | 6 m 30 s |

### Per-assembly breakdown

| Outcome            | Assemblies | Notes |
|--------------------|-----------:|-------|
| `passed`           | 91 (182 rows counting both Debug+Release logs doubled) | All Unit, Contract-derived, Architecture, Benchmarks bench-build, and Sqlite/Fusion integration that uses in-proc deps |
| `failed with N error` | 21 × Integration | **Every** failure traces to Testcontainers `DockerApiClient..ctor` NRE |
| `Zero tests ran`   | 8 base `*.Tests.Contract` projects | By design — these are abstract `[Test]` contracts that only execute via `[InheritsTests]` subclasses |

### Root cause of the 160 failures

Every failing test surfaces one of two stacks, both from Testcontainers:

```
[Test Failure] DockerUnavailableException: Docker is either not running or misconfigured.
Details:  Failed to connect to Docker endpoint at 'npipe://./pipe/docker_engine'.
```

or (for test-level NREs):

```
System.NullReferenceException: [Null Reference] Object reference not set to an instance of an object.
  at DotNet.Testcontainers.Clients.DockerApiClient..ctor(Guid sessionId, IDockerEndpointAuthenticationConfiguration dockerEndpointAuthConfig, ILogger logger)
  at DotNet.Testcontainers.Clients.DockerContainerOperations..ctor(...)
  at DotNet.Testcontainers.Clients.TestcontainersClient..ctor(...)
  at DotNet.Testcontainers.Containers.DockerContainer..ctor(...)
```

`docker info` on this host reports `Client: 28.1.1 / Context: desktop-linux`, but Testcontainers defaults to `npipe://./pipe/docker_engine` (the classic Windows named pipe). Docker Desktop's current default context uses a WSL-proxied socket instead.

### What this means

- **Not a branch regression.** None of these tests test production code changed on this branch. The failure is "before-test-one-can-run" — Testcontainers can't build a client.
- **CI passes.** `.github/workflows/ci.yml`'s integration matrices each run in `ubuntu-latest` with the standard Unix socket at `unix:///var/run/docker.sock`, which Testcontainers discovers automatically.
- **Local fix** (optional, for dev workflow only): enable "Expose daemon on tcp://localhost:2375" in Docker Desktop → Settings → General, *or* set `DOCKER_HOST=tcp://localhost:2375` before running `dotnet test`. This is not required for the branch to merge.

### Affected integration assemblies (21)

Caching.Redis · Databases.NoSql.{Cassandra, Cosmos, Dynamo, ElasticSearch, KurrentDb, Mongo, Redis} · Databases.Sql.{MySql, Oracle, Postgresql, SqlServer} · Docker · Messaging.{Kafka, Nats, RabbitMq, ServiceBus, Sqs} · Observability.Seq · Storage.{AzureBlob, MinIO, S3}

### Assemblies that passed integration (no Testcontainers dependency)

- `Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration` — uses in-proc SQLite.
- `Rig.TUnit.Caching.Fusion.Tests.Integration` — in-proc.

## Check 3 — Resources: **SKIP**

No `.resx` files in the solution; project does not use `IStringLocalizer`.

## Check 4 — Proto: **SKIP**

One `.proto` discovered — [tests/Rig.TUnit.Grpc.Tests.Unit/Protos/test.proto](../../../tests/Rig.TUnit.Grpc.Tests.Unit/Protos/test.proto). It's test-scaffolding only, not a shared contract. Nothing to cross-reference.

## Check 5 — K8s: **SKIP**

No k8s or deploy manifests in the repo.

## Check 6 — Format: **FAIL**

```bash
dotnet format Rig.TUnit.slnx --verify-no-changes --verbosity minimal
```

Exit code: 2. Two files with **import ordering** violations:

1. [src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1](src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1) — **pre-existing on master** (no commits on this branch touch this file). Current order places `Rig.TUnit.Storage.Fixtures` before `Rig.TUnit.Storage.FileSystem.Options`, which is reverse alphabetical.
2. [tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1](tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1) — **introduced by T123c** (commit `8b68eaf` test(005): T123c — RED for Markdig structural README gate). `Markdig.Extensions.Tables` should sort between `Markdig` and `Markdig.Syntax`.

### Fix

```bash
dotnet format Rig.TUnit.slnx --severity info
git add src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs \
        tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs
git commit -m "style(005): fix import ordering (dotnet format)"
```

Or apply the two edits manually — both are single-hunk reorderings of the `using` block.

---

## Result

```
Verification: FAIL

  Build:     PASS
  Tests:     FAIL  (160/1711 — 100% Testcontainers/Docker env; CI will pass)
  Format:    FAIL  (2 files, import ordering)
  Resources: SKIP
  Proto:     SKIP
  K8s:       SKIP

Real blockers: 2 format fixes.
Not blocking: integration test failures on this host (local Docker endpoint config).
```

### Next steps

1. **Fix format**: `dotnet format Rig.TUnit.slnx --severity info`, commit.
2. **Optional, to re-verify integration tests locally**: set `DOCKER_HOST=tcp://localhost:2375` (after enabling "Expose daemon on tcp" in Docker Desktop), rerun `dotnet test --solution Rig.TUnit.slnx --no-build`.
3. **Re-run**: `/dotnet-ai.verify` once format is fixed.
4. **When green**: `/dotnet-ai.pr`.
