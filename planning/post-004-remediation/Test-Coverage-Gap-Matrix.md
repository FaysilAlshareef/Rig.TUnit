# Test Coverage Gap Matrix — FR-030 Audit

**Date:** 2026-04-19 (post-merge of Feature 004)
**Mandate:** FR-030…FR-036 from [spec.md:262-275](../../.dotnet-ai-kit/features/004-provider-consistency-remediation/spec.md) — every provider MUST ship **Unit + Integration + Contract + Benchmark** test categories.

A provider is NOT "canonical" until all four test artefacts exist. Feature 004 applied this to new/remediated providers. Older projects (features 001–003 era) fall short.

## Legend

- ✓ — present and covered
- ✗ — missing (FR-030 violation)
- N/A — not applicable (meta/abstract/analyzer package)
- Arch — covered by a rule in `Rig.TUnit.Architecture.Tests/Rules/*`

## Full matrix (63 src projects)

| Project | Unit | Integration | Contract | Arch | Benchmark |
|---|---|---|---|---|---|
| Rig.TUnit | ✗ | ✗ | ✗ | N/A | N/A |
| Rig.TUnit.All | ✗ | ✗ | ✗ | N/A | N/A |
| Rig.TUnit.Caching (base) | ✗ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Caching.Fusion | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Caching.Hybrid | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Caching.Memory | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Caching.Redis | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Ci | ✓ | ✗ | ✗ | ✓ | ✗ |
| Rig.TUnit.Concurrency | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Core | ✓ | ✗ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases (base) | ✓ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Databases.NoSql (base) | ✗ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Databases.NoSql.Cassandra | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.Cosmos | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.Dynamo | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.ElasticSearch | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.KurrentDb | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.Mongo | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.NoSql.Redis | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Databases.Sql (base) | ✓ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Databases.Sql.MySql | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.Sql.Oracle | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.Sql.Postgresql | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Databases.Sql.SqlServer | ✓ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Databases.Sql.Sqlite | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Docker | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Grpc | ✓ | ✗ | ✗ | ✓ | ✓ |
| Rig.TUnit.HealthChecks | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Http | ✓ | ✗ | ✗ | ✓ | ✓ |
| Rig.TUnit.Mediator | ✓ | ✗ | ✗ | ✓ | ✗ |
| Rig.TUnit.Messaging (base) | ✗ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Messaging.Kafka | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Messaging.Nats | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Messaging.RabbitMq | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Messaging.ServiceBus | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Messaging.Sqs | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Microservices (base) | ✗ | ✗ | ✗ | N/A | N/A |
| Rig.TUnit.Microservices.Contracts | ✓ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Microservices.EventSourcing | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Microservices.Inbox | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Microservices.Outbox | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Microservices.Saga | ✓ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Microservices.Snapshots | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Observability (base) | ✗ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Observability.AppInsights | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Observability.Logging | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Observability.Logging.Analyzers | ✓ | ✗ | ✗ | ✓ | ✗ |
| Rig.TUnit.Observability.Metrics | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Observability.Seq | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Observability.Tracing | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Parallelism | ✗ | ✓ | ✓ | ✓ | ✗ |
| Rig.TUnit.Resilience | ✗ | ✓ | ✗ | ✓ | ✗ |
| Rig.TUnit.Security (base) | ✗ | ✗ | ✗ | N/A | N/A |
| Rig.TUnit.Security.Jwt | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Security.Mtls | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Security.OAuth | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Security.Policies | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Storage (base) | ✗ | ✗ | ✓ | N/A | N/A |
| Rig.TUnit.Storage.AzureBlob | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Storage.FileSystem | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Storage.MinIO | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.Storage.S3 | ✓ | ✓ | ✗ | ✓ | ✓ |
| Rig.TUnit.WebAPI | ✓ | ✗ | ✗ | ✓ | ✗ |

> Note: the "Contract" column reports whether the leaf provider has its own contract file. Many providers inherit the **family-level** contract (e.g., `SqlRigContract`, `NoSqlRigContract`, `MessagingRigContract`, `StorageRigContract`) from the family-base test project, which is the 004-approved pattern. The ✗ in that column therefore reflects a bookkeeping gap more than a coverage gap.

## Projects failing FR-030 (prioritized)

### Priority P0 — foundation modules (block the whole stack)

| Project | Missing | Notes |
|---|---|---|
| `Rig.TUnit.Core` | Integration, Contract | Every other project depends on Core; must be maximally covered |
| `Rig.TUnit.Mediator` | Integration, Contract, Benchmark | Pipeline primitive; pure abstraction, easy to benchmark |
| `Rig.TUnit.Grpc` | Integration, Contract, Benchmark | Cross-cutting |
| `Rig.TUnit.WebAPI` | Integration, Contract, Benchmark | Cross-cutting |
| `Rig.TUnit.Http` | Integration, Contract, Benchmark | Request-mock primitive |

### Priority P1 — platform utilities

| Project | Missing | Notes |
|---|---|---|
| `Rig.TUnit.Ci` | Integration, Benchmark | CI helper module |
| `Rig.TUnit.Concurrency` | Unit, Contract, Benchmark | |
| `Rig.TUnit.HealthChecks` | Unit, Benchmark | |
| `Rig.TUnit.Parallelism` | Unit, Benchmark | |
| `Rig.TUnit.Resilience` | Unit, Benchmark | |

### Priority P1 — legacy providers with gaps

| Project | Missing | Notes |
|---|---|---|
| `Rig.TUnit.Caching.Memory` | Unit, Benchmark | In-process; contract via Caching family |
| `Rig.TUnit.Caching.Redis` | Unit, Benchmark | `TestCompletenessTests.SkipUntilFixed` flags it |
| `Rig.TUnit.Databases.Sql.Sqlite` | Unit, Benchmark | |
| `Rig.TUnit.Databases.Sql.SqlServer` | Benchmark | |
| `Rig.TUnit.Databases.NoSql.Redis` | Unit, Benchmark | Shares Caching.Redis suite |

### Priority P1 — Observability leaves

| Project | Missing | Notes |
|---|---|---|
| `Rig.TUnit.Observability.Logging` | Unit, Benchmark | |
| `Rig.TUnit.Observability.Seq` | Unit, Benchmark | |
| `Rig.TUnit.Observability.Tracing` | Unit, Benchmark | Large `TraceAssertTests` already exists — only missing Unit |

### Priority P1 — Microservices

| Project | Missing | Notes |
|---|---|---|
| `Rig.TUnit.Microservices.Contracts` | Benchmark | |
| `Rig.TUnit.Microservices.Saga` | Benchmark | |
| `Rig.TUnit.Microservices.EventSourcing` | (complete) | |
| `Rig.TUnit.Microservices.Inbox` | Unit, Benchmark | |
| `Rig.TUnit.Microservices.Outbox` | Unit, Benchmark | |
| `Rig.TUnit.Microservices.Snapshots` | Unit, Benchmark | |

## Benchmark gap (FR-033) — 21 missing

Projects with **no BenchmarkDotNet class** in `tests/Rig.TUnit.Benchmarks/`:

Caching.Memory, Caching.Redis, Ci, Concurrency, Databases.Sql.SqlServer, Databases.Sql.Sqlite, Databases.NoSql.Redis, HealthChecks, Mediator, Microservices.{Contracts, Inbox, Outbox, Saga, Snapshots}, Observability.{Logging, Logging.Analyzers, Seq, Tracing}, Parallelism, Resilience, WebAPI.

## Coverage gate status (FR-035 / FR-036)

- **Defined:** ≥ 90% line / ≥ 85% branch per package via TUnit-native `dotnet run --coverage --coverage-output-format cobertura`.
- **Enforced in CI:** No. `.github/workflows/ci.yml` runs `dotnet test` with no coverage flags, no cobertura artifact, no gate step.
- **Gap:** The gate lives only in the spec. Even completed providers have no evidence that they meet 90/85 — whatever current coverage is, nothing is checking.

## Architecture rule enforcement status

`tests/Rig.TUnit.Architecture.Tests/Rules/`:

| Rule | Exists | Skip markers | Enforced uniformly |
|---|---|---|---|
| `CodeOrganizationTests` | Yes | Unknown | To verify |
| `CoverageRuleTests` | Yes | Unknown | To verify |
| `DependencyDirectionTests` | Yes | Unknown | To verify |
| `ForbiddenApiTests` | Yes | Unknown | To verify |
| `ProviderCompletenessTests` | Yes | Yes (per 004 spec) | **No — SkipUntilFixed active** |
| `ReadmeCompletenessTests` | Yes | Yes | **No — SkipUntilFixed active** |
| `TestCompletenessTests` | Yes | Yes (explicit list at lines 22-53) | **No** |
| `TestFileOrganizationTests` | Yes | Unknown | To verify |

Feature 004 Phase 6 (`/clarify` + `/plan` + `/tasks`) is meant to retire these skips, but the merge landed before Phase 6 completed.

## Empty orphan artefacts

These folders exist on disk but are not referenced by `Rig.TUnit.slnx` and contain only `bin/obj/`:

- `src/Rig.TUnit.ServiceBus/` — stale (renamed to `Rig.TUnit.Messaging.ServiceBus`)
- `tests/Rig.TUnit.ServiceBus.Tests.Integration/` — stale (renamed)
- `tests/Rig.TUnit.SqlServer.Tests.Integration/` — stale (renamed to `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration`)

Action: `git rm -r` all three as a single cleanup commit.
