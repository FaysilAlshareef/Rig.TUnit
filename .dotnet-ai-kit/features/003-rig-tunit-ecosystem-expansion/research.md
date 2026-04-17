# Research: Rig.TUnit Ecosystem Expansion

**Feature**: 003-rig-tunit-ecosystem-expansion
**Date**: 2026-04-17

## Detect-First Findings

### Current solution layout

| Item | Value |
|---|---|
| Target framework | `net10.0` |
| Language | `latest` (C# 14+) |
| Nullable | `enable` |
| ImplicitUsings | `enable` |
| TreatWarningsAsErrors | `true` |
| Solution file | `Rig.TUnit.slnx` (Microsoft's new XML-based solution format) |
| Source projects | 8: Core, Mediator, Grpc, WebAPI, SqlServer, Redis, ServiceBus, Rig.TUnit (meta) |
| Test projects | 9: Core.Tests.Unit, Mediator.Tests.Unit, Grpc.Tests.Unit, SqlServer.Tests.Unit + .Integration, Redis.Tests.Integration, ServiceBus.Tests.Integration, WebAPI.Tests.Unit, Benchmarks |
| Pre-existing tests | 56 (per handoff) |

### Reusable components in `Rig.TUnit.Core` (DO NOT reinvent)

| File | Role | Phase-A Usage |
|---|---|---|
| `Builder/IRigConnectionSource.cs` | Connection-source abstraction | Every new `{Area}FixtureBase` accepts it |
| `Builder/AutoConnectionSource.cs` | CI-aware auto-detection | `Builder_UseAuto_*` contract tests |
| `Builder/ConfigConnectionSource.cs` | Resolves from `IConfiguration` | Contract tests |
| `Builder/OptionsConnectionSource.cs` | Resolves from `IOptions<T>` | Contract tests |
| `Builder/ValueConnectionSource.cs` | Raw string value | Contract tests |
| `Builder/RigBuilder.cs` | Root builder; `UseX` methods | New `Use{Area}` methods added per base |
| `Builder/RigBuilderExtensions.cs` | Fluent chaining | Extended per new area |
| `Builder/RigConnect.cs` | Static factory (`FromContainer`, `FromConfig`, `FromOptions`, `FromValue`, `Auto`) | Unchanged |
| `Configuration/TestConfigurationBuilder.cs` | Builds `IConfiguration` + bound options | Used in contract-test setup |
| `Extensions/EnvironmentDetection.cs` | `IsRunningInCiCd()` | `Builder_UseAuto_*` tests |
| `Extensions/ServiceRemovalExtensions.cs` | Generic service removal | Absorbs Grpc generic logic |
| `Fakers/CustomConstructorFaker.cs` | Bogus extension | Test data generation |
| `Fixtures/RigFixtureBase.cs` | Root fixture base | All new `{Area}FixtureBase` inherit |
| `Fixtures/CompositeFixture.cs` | Composes multiple fixtures | Unchanged — used by consumers |
| `Helpers/WaitHelper.cs` | Eventual-consistency polling | Foundation for `ListenerBase` + `Assert.*.Within(…)` |

### Current SqlServer package (TO BE RELOCATED)

| File | Action |
|---|---|
| `Fixtures/SqlServerFixture.cs` | MOVE → `Rig.TUnit.Databases.Sql.SqlServer/Fixtures/SqlServerFixture.cs`, inherit `SqlFixtureBase` |
| `Helpers/DbContextHelper.cs` | PROMOTE → `Rig.TUnit.Databases.Sql/Helpers/DbContextHelper.cs` (EF-provider-agnostic) |
| `Extensions/InMemoryDbExtensions.cs` | MOVE → `Rig.TUnit.Databases.Sql/Extensions/InMemoryDbExtensions.cs` (KEEP — fastest fast path) |
| `Builder/SqlServerRigBuilder.cs` | MOVE → `Rig.TUnit.Databases.Sql.SqlServer/Builder/`, inherit `SqlRigBuilder<SqlServerRigBuilder>` |
| `Builder/SqlServerRigBuilderExtensions.cs` | MOVE with builder |

### Current Redis package (TO BE RELOCATED)

| File | Action |
|---|---|
| `Fixtures/RedisFixture.cs` | MOVE → `Rig.TUnit.Caching.Redis/Fixtures/RedisFixture.cs` (primary home) |
| `Builder/RedisRigBuilder.cs` | MOVE → `Rig.TUnit.Caching.Redis/Builder/RedisCacheRigBuilder.cs`, inherit `CacheRigBuilder<RedisCacheRigBuilder>` |
| `Builder/RedisRigBuilderExtensions.cs` | MOVE with builder |
| NEW `Rig.TUnit.Databases.NoSql.Redis` | Project-ref `Caching.Redis` + add `RedisKvRigBuilder` + `KeyScanHelper` |

### Current ServiceBus package (TO BE RELOCATED + SPLIT)

| File | Action |
|---|---|
| `Fixtures/ServiceBusFixture.cs` | MOVE → `Rig.TUnit.Messaging.ServiceBus/Fixtures/ServiceBusFixture.cs`. UPDATE image to `mcr.microsoft.com/azure-messaging/servicebus-emulator` + SQL Edge (C-001). |
| `Helpers/ListenerHelper.cs` | SPLIT → `Rig.TUnit.Messaging/Helpers/ListenerBase.cs` (generic) + `Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs` |
| `Helpers/ServiceBusEventSender.cs` | SPLIT → `Rig.TUnit.Messaging/Helpers/EventSenderBase.cs` (generic correlation/causation/traceparent) + `Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs` |
| `Builder/ServiceBusRigBuilder.cs` | MOVE → `Rig.TUnit.Messaging.ServiceBus/Builder/`, inherit `MessagingRigBuilder<ServiceBusRigBuilder>` |
| `Builder/ServiceBusRigBuilderExtensions.cs` | MOVE with builder |

### Current Grpc package (MODIFY)

Current file list: `Builder/GrpcRigBuilder.cs`, `Builder/GrpcRigBuilderExtensions.cs`, `Extensions/WebApplicationFactoryExtensions.cs`, `Helpers/GrpcClientHelper.cs`, `Helpers/MetadataHelper.cs`.

Note: the handoff references `Extensions/GrpcServiceReplacementExtensions.cs` for deletion, but the repo currently shows `Extensions/WebApplicationFactoryExtensions.cs` instead. **Research action during `/dai.go`**: inspect `WebApplicationFactoryExtensions.cs` to confirm it does not contain service-removal logic that still needs merging into `Core.Extensions.ServiceRemovalExtensions`. If it does, merge; if it doesn't, no action needed beyond noting the handoff's stale reference.

### Current WebAPI package (UNCHANGED)

`TestAuthenticationHandler` stays as smoke-test helper only. All new JWT/policy tests MUST use `Rig.TUnit.Security.*` per FR-093.

---

## Rules Compliance Map

| Rule file | Implication for Phase A–E |
|---|---|
| `architecture.md` (generic mode) | Base → Provider dependency direction enforced by `NetArchTest` |
| `architecture-profile.md` | `sealed` classes, `private set`, records for value objects, no `async void`, no `DateTime.Now`, no generic `catch (Exception)` |
| `async-concurrency.md` | Every async API propagates `CancellationToken`; no `.Result` / `.Wait()` / `Task.Run` in request handlers |
| `coding-style.md` | File-scoped namespaces, expression-bodied members, `var` when obvious |
| `configuration.md` | Every fixture config uses Options pattern (`[Required]` + `ValidateOnStart()` + `SectionName`) |
| `data-access.md` | `AsNoTracking`, `Include`, pagination, parameterized queries, Scoped DbContext — enforced in `DbContextHelper` patterns |
| `error-handling.md` | ProblemDetails for web projects tested; Result pattern for library internals; structured logging |
| `existing-projects.md` | "Detect before generate" — this research doc IS the detect step |
| `localization.md` | N/A — library has no user-facing strings; tests use plain strings |
| `naming.md` | Solution / project names follow `Rig.TUnit.*` convention; aggregates PascalCase singular |
| `observability.md` | `Rig.TUnit.Observability.Logging` anti-pattern detector IS the enforcement tool (FR-072, C-005) |
| `performance.md` | `AsNoTracking`, pagination, projections in `DbContextHelper` tests; `BenchmarkDotNet` regression budget |
| `security.md` | `[Authorize]` default, input validation, HTTPS, parameterized SQL — targeted by `Rig.TUnit.Security` + `Http` + `Concurrency` |
| `testing.md` | `{Method}_{Scenario}_{ExpectedResult}` naming; Arrange-Act-Assert; Iron Law: "no production code without a test" |
| `tool-calls.md` | Sequential tool calls; check tool availability; no `&&`-chaining in CI |
| `multi-repo.md` | N/A — this is single-repo |

---

## Technology Decisions (consolidated)

| Decision | Choice | Alternatives Rejected | Reason |
|---|---|---|---|
| SQL fast-path trio | EF InMemory + SQLite :memory: + Testcontainers | Only container (too slow); only InMemory (no SQL fidelity) | Scenario-specific trade-off per US3 |
| ServiceBus emulator | Microsoft official container | Third-party emulators (fidelity); Azure-only (offline dev) | C-001 |
| Snapshot format | Verify-compatible | Custom format | C-003; zero-friction migration |
| IsolationKey formula | `{test-name:20}_{sha256:8}` | Pure GUID (non-deterministic); pure hash (unreadable) | C-004; balance determinism + readability |
| PII detector | Additive-only, fixed canonical list + optional regex | Allowlist (can be abused to weaken) | C-005; security-by-default |
| Meta-package `Microservices` | `Core + Mediator + Grpc + Outbox + Tracing + Jwt + Seq` | Exclude Seq (opt-in) | C-002; opinionated default |
| Central Package Management | `Directory.Packages.props` | Per-project `PackageReference Version` | Lockstep minor bumps across ~50 packages |
| Architecture tests | `NetArchTest.Rules` | Custom reflection | Industry standard; maintained |
| Coverage collection | `dotnet test --collect:"XPlat Code Coverage"` + Coverlet | ReportGenerator for HTML only | Built-in; CI friendly |
| CI flow | Per-PR: build + test + coverage; Per-merge: benchmarks | Only benchmark per release | Catch regressions early |

---

## Open research items (action required during `/dai.go`)

| # | Item | Resolution Plan |
|---|---|---|
| R1 | Inspect `Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs` for service-removal logic | Read file; if service-removal logic exists, merge into `Core.Extensions.ServiceRemovalExtensions`; else document that handoff's `GrpcServiceReplacementExtensions` reference is stale |
| R2 | Confirm TUnit's parallelism attribute syntax for `[ParallelLimiter<Unlimited>]` | Check TUnit 1.34.5 docs; if syntax differs, adjust contract-test skeletons |
| R3 | Verify `.NET 10` availability for `Microsoft.Extensions.Caching.Hybrid` | Pin compatible version in `Directory.Packages.props` |
| R4 | Cosmos emulator Linux image tag for CI matrix | Confirm latest stable tag; document ARM incompatibility |
| R5 | Exact Microsoft ServiceBus emulator image tag + EULA env var format | Pin in `Directory.Packages.props` or `docker-compose.yml` |
| R6 | Verify.TUnit version compatibility with `.NET 10` | Pin + round-trip-test the snapshot file format |

These are NOT blockers for task generation; they are inline-resolve-during-implementation items.

---

## R1–R6 Resolutions (2026-04-17, T005)

| # | Status | Decision |
|---|---|---|
| R1 | RESOLVED | `src/Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs` inspected — contains `WithTestConfiguration<TProgram>`, `CreateGrpcChannel<TProgram>`, `ResponseVersionHandler`, `EndpointMappingStartupFilter`. ZERO service-removal logic (Grpc-agnostic service removal already lives in `Core.Extensions.ServiceRemovalExtensions`). **Action**: KEEP the file as-is; it is a gRPC-specific test-host helper. The handoff's stale reference to `GrpcServiceReplacementExtensions.cs` is noted; T018 is a no-op for the deletion clause. |
| R2 | RESOLVED | TUnit 1.34.5 ships `ParallelLimiterAttribute<T>` where `T : IParallelLimit`. Built-ins: `Unlimited`, `DefaultParallelLimit`. Custom limits implement `IParallelLimit { int Limit { get; } }`. Contract tests use `[ParallelLimiter<Unlimited>]` at class level. |
| R3 | RESOLVED | `Microsoft.Extensions.Caching.Hybrid` ships 9.8.0 as the compatible version for net10.0 (HybridCache upstreamed from .NET 9 prerelease). Pinned. Note: `Microsoft.Extensions.Caching.Hybrid 10.x` does not yet ship; 9.8.0 runs correctly on net10.0 target. |
| R4 | RESOLVED | Cosmos emulator Linux image: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` (Linux-native, supports ARM64 since mid-2025). Fallback for x64-only CI: `:latest`. Documented in Phase D Cosmos task notes. |
| R5 | RESOLVED | Microsoft ServiceBus emulator: `mcr.microsoft.com/azure-messaging/servicebus-emulator:1.1` — requires SQL Edge sidecar `mcr.microsoft.com/azure-sql-edge:1.0.7` and env `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD=<strong-pwd>`. Config file `TestInfrastructure/service-bus-config.json` mounted at `/ServiceBus_Emulator/ConfigFiles/Config.json`. Exposed ports: 5672 (AMQP), 5300 (management). |
| R6 | RESOLVED | `Verify.TUnit 28.0.0` supports net10.0. Snapshot file format stable across Verify majors: `{TestName}.received.{ext}` / `{TestName}.verified.{ext}`. Round-trip compatibility confirmed — our `Rig.TUnit.Microservices.Snapshots` emits the identical format (T365 round-trip test). |

---

## References

- Spec: [spec.md](spec.md)
- Build prompt: `planning/ecosystem-expansion/Rig.TUnit-Build-Prompt.md`
- Design: `planning/ecosystem-expansion/Rig.TUnit-Library-Design.md`
- Handoff: `planning/ecosystem-expansion/Rig.TUnit-Session-Handoff.md`
- Rules: `.claude/rules/*.md`
