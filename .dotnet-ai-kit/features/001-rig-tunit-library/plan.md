# Implementation Plan: 001-rig-tunit-library

**Mode**: Generic (standalone library) | **Complexity**: Complex | **Constitution**: N/A (not generated)

## Context

Build the Rig.TUnit library from scratch — a standalone .NET 10 testing infrastructure library built on TUnit for integration testing gRPC microservices. The repo is greenfield (zero source code). All design decisions are finalized in planning docs with complete code examples. Development follows TDD (FR-013).

**Authoritative sources** (priority order when conflicts):
1. `spec.md` clarifications C-001 to C-010
2. `planning/Rig.TUnit-Session-Handoff.md` (exact files, versions)
3. `planning/Rig.TUnit-Library-Design.md` (code examples)

## Critical Spec Overrides (Design Doc Deviations)

| # | Design Doc Says | Spec Says (Follow This) |
|---|----------------|------------------------|
| 1 | `GrpcClientHelper<TClient>` with `WebApplicationFactory<Program>` | `GrpcClientHelper<TClient, TProgram>` — generic TProgram (C-002) |
| 2 | `ReplaceGrpcClient<TClient>` with concrete Program | `ReplaceGrpcClient<TClient, TProgram>` (C-005) |
| 3 | `WaitForMessagesAsync` returns silently on timeout | Throws `TimeoutException` (C-008) |
| 4 | `HandlerHelper.HandleEvent<T>(Event<T>)` | Remove — only `Send<TResult>(IRequest<TResult>)` exists (FR-003) |
| 5 | NSubstitute for Grpc tests | Real MediatR pipeline + test handlers (C-010) |

---

## Phase 0: Solution Scaffolding

**Files:**
- `Rig.TUnit.slnx` — XML solution (add projects incrementally per phase)
- `global.json` — pin SDK 10.0.100, rollForward: latestFeature
- `Directory.Build.props` — net10.0, ImplicitUsings, Nullable, TreatWarningsAsErrors, LangVersion=latest
- `.gitignore` — standard .NET template

**Verify:** `dotnet --version` confirms .NET 10 SDK

---

## Phase 1: Rig.TUnit.Core (TDD)

### 1a. Source stub + test project (RED)

**Source** (`src/Rig.TUnit.Core/`):
- `Rig.TUnit.Core.csproj` — TUnit.Core 1.33.0, Bogus 35.6.1, Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0
- `Fakers/CustomConstructorFaker.cs` — stub
- `Extensions/ServiceRemovalExtensions.cs` — stub
- `Extensions/EnvironmentDetection.cs` — stub

**Tests** (`tests/Rig.TUnit.Core.Tests.Unit/`):
- `Rig.TUnit.Core.Tests.Unit.csproj` — TUnit 1.33.0 (meta-package, NOT TUnit.Core), Microsoft.Extensions.DependencyInjection
- `Fakers/CustomConstructorFakerTests.cs` — 3 tests (private ctor, rule application, distinct instances)
- `Extensions/ServiceRemovalExtensionsTests.cs` — 7 tests (RemoveService exists/missing, RemoveImplementation exists/missing, RemoveByName multi/none, chaining)
- `Extensions/EnvironmentDetectionTests.cs` — 4 tests (GITHUB_ACTIONS, CI, TF_BUILD, no vars) — use `[NotInParallel]`
- `TestInfrastructure/TestEntity.cs` — sealed class with private ctor + private setters

### 1b. Implement source (GREEN)
Implement from design doc code. Verify: `dotnet test` on Core.Tests.Unit passes.

---

## Phase 2: Rig.TUnit.Grpc (TDD)

### 2a. Source stub + test project (RED)

**Source** (`src/Rig.TUnit.Grpc/`):
- `Rig.TUnit.Grpc.csproj` — ProjectRef to Core, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, NuGet: TUnit.AspNetCore *, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Grpc.AspNetCore 2.71.0, Grpc.Net.Client 2.71.0, Grpc.Net.ClientFactory 2.71.0, Calzolari.Grpc.Net.Client.Validation 9.0.0, MediatR 12.4.1, Serilog 4.2.0, Serilog.Sinks.Console 6.0.0
- `Helpers/GrpcClientHelper.cs` — `<TClient, TProgram>` (C-002)
- `Helpers/HandlerHelper.cs` — only `Send<TResult>`, no `HandleEvent<T>` (FR-003)
- `Helpers/MetadataHelper.cs` — Dict<string,string> to protobuf binary (C-004)
- `Extensions/WebApplicationFactoryExtensions.cs` — `WithTestConfiguration<TProgram>`, `CreateGrpcChannel<TProgram>`
- `Extensions/GrpcServiceReplacementExtensions.cs` — `ReplaceGrpcClient<TClient, TProgram>` (C-005)

**Tests** (`tests/Rig.TUnit.Grpc.Tests.Unit/`):
- `Rig.TUnit.Grpc.Tests.Unit.csproj` — TUnit 1.33.0, Google.Protobuf, Grpc.Tools, MediatR, FrameworkReference ASP.NET Core
- `Helpers/GrpcClientHelperTests.cs` — 2 tests
- `Helpers/HandlerHelperTests.cs` — 2 tests (uses real MediatR, C-010)
- `Helpers/MetadataHelperTests.cs` — 3 tests (claims dict, empty dict, binary format)
- `Extensions/WebApplicationFactoryExtensionsTests.cs` — 4 tests
- `Extensions/GrpcServiceReplacementExtensionsTests.cs` — 2 tests
- `TestInfrastructure/TestProgram.cs` — minimal WebApplication with MediatR + gRPC
- `TestInfrastructure/TestGrpcService.cs` — implements test proto service
- `TestInfrastructure/TestRequest.cs` — IRequest<string>
- `TestInfrastructure/TestRequestHandler.cs` — IRequestHandler returning predictable value
- `Protos/test.proto` — simple gRPC service definition

### 2b. Implement source (GREEN)
Verify: `dotnet test` on Grpc.Tests.Unit passes.

---

## Phase 3: Rig.TUnit.SqlServer (TDD)

### 3a. Source stub + unit tests (RED)

**Source** (`src/Rig.TUnit.SqlServer/`):
- `Rig.TUnit.SqlServer.csproj` — ProjectRef to Core, Testcontainers.MsSql 4.6.0, EF Core SqlServer 10.0.0, EF Core InMemory 10.0.0
- `Fixtures/SqlServerFixture.cs`
- `Helpers/DbContextHelper.cs`
- `Extensions/InMemoryDbExtensions.cs` — calls Core's `RemoveByName`
- `Extensions/SqlServerContainerExtensions.cs`

**Unit Tests** (`tests/Rig.TUnit.SqlServer.Tests.Unit/`):
- `Extensions/InMemoryDbExtensionsTests.cs` — 3 tests (replaces registration, unique GUID names, parallel isolation)
- `TestInfrastructure/TestDbContext.cs`, `TestEntity.cs`

### 3b. Implement InMemoryDbExtensions (GREEN for unit tests)

### 3c. Integration tests (RED)

**Integration Tests** (`tests/Rig.TUnit.SqlServer.Tests.Integration/`):
- `Fixtures/SqlServerFixtureTests.cs` — lifecycle (init/dispose/connect)
- `Helpers/DbContextHelperTests.cs` — insert/query/scope isolation/change tracker clearing
- `Extensions/SqlServerContainerExtensionsTests.cs` — isolated database creation
- `TestInfrastructure/TestDbContext.cs`, `TestEntity.cs` (own copies per FR-021)

### 3d. Implement remaining source (GREEN for integration tests)
Note: `BuildServiceProvider()` in SqlServerContainerExtensions is intentional — do not refactor.

Verify: unit tests pass without Docker, integration tests pass with Docker.

---

## Phase 4: Rig.TUnit.Redis (TDD)

### 4a. Source stub + integration tests (RED)

**Source** (`src/Rig.TUnit.Redis/`):
- `Rig.TUnit.Redis.csproj` — ProjectRef to Core, Testcontainers.Redis 4.6.0, StackExchange.Redis 2.8.16
- `Fixtures/RedisFixture.cs`
- `Extensions/RedisContainerExtensions.cs` — replaces IConnectionMultiplexer with container connection

**Integration Tests** (`tests/Rig.TUnit.Redis.Tests.Integration/`):
- `Fixtures/RedisFixtureTests.cs` — init/dispose/connect
- `Extensions/RedisContainerExtensionsTests.cs` — replaces multiplexer, can set/get keys

### 4b. Implement source (GREEN)
Verify: integration tests pass with Docker.

---

## Phase 5: Rig.TUnit.ServiceBus (TDD)

### 5a. Source stub + integration tests (RED)

**Source** (`src/Rig.TUnit.ServiceBus/`):
- `Rig.TUnit.ServiceBus.csproj` — ProjectRef to Core, Testcontainers.ServiceBus 4.6.0, Azure.Messaging.ServiceBus 7.18.2, Newtonsoft.Json 13.0.3
- `Fixtures/ServiceBusFixture.cs` — ConfigFilePath property
- `Helpers/ListenerHelper.cs` — throws TimeoutException (C-008), polls every 250ms, uses `ConcurrentBag<ServiceBusReceivedMessage>` (thread-safe with MaxConcurrentSessions=100)
- `Helpers/ServiceBusEventSender.cs` — JSON serialization via Newtonsoft, IAsyncDisposable
- `Extensions/ServiceBusContainerExtensions.cs`

**Integration Tests** (`tests/Rig.TUnit.ServiceBus.Tests.Integration/`):
- `Fixtures/ServiceBusFixtureTests.cs` — 2 tests
- `Helpers/ListenerHelperTests.cs` — 5 tests (capture, timeout throws, expected count, lifecycle)
- `Helpers/ServiceBusEventSenderTests.cs` — 3 tests (publish, session ID, JSON format)
- `Extensions/ServiceBusContainerExtensionsTests.cs`
- `TestInfrastructure/service-bus-config.json` — CopyToOutputDirectory=PreserveNewest

### 5b. Implement source (GREEN)
Verify: integration tests pass with Docker.

---

## Phase 6: Meta-Package + Benchmarks + Final

### Meta-package
- `src/Rig.TUnit/Rig.TUnit.csproj` — ProjectReferences to all 5 source projects, no .cs files

### Benchmarks
- `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj` — `<OutputType>Exe</OutputType>`, BenchmarkDotNet, refs to Core + Grpc + SqlServer
- `tests/Rig.TUnit.Benchmarks/Program.cs` — BenchmarkSwitcher entry point
- `tests/Rig.TUnit.Benchmarks/CoreBenchmarks.cs` — faker + service removal (10/100/1000)
- `tests/Rig.TUnit.Benchmarks/SqlServerBenchmarks.cs` — DI scope creation
- `tests/Rig.TUnit.Benchmarks/GrpcBenchmarks.cs` — channel creation
- All benchmark classes: `[MemoryDiagnoser]` (SC-016)

### Final slnx
Update `Rig.TUnit.slnx` to contain all 13 projects (6 src + 7 tests).

### Verification (Success Criteria)
1. `dotnet build Rig.TUnit.slnx` — zero errors, zero warnings (SC-001)
2. `dotnet test --filter "Tests.Unit"` — all unit tests pass, no Docker (SC-008, SC-010)
3. `dotnet test --filter "Tests.Integration"` — all integration tests pass with Docker (SC-011)
4. `dotnet run --project tests/Rig.TUnit.Benchmarks/ -- --list flat` — benchmarks listed (SC-015)

---

## Known Pitfalls

| Pitfall | Mitigation |
|---------|------------|
| Test csproj uses TUnit.Core instead of TUnit | Always reference `TUnit` meta-package (includes runner) |
| Grpc library missing ASP.NET Core types | Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` |
| Core.csproj missing DI Abstractions | Add Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0 |
| Proto not compiling in Grpc.Tests.Unit | Add Grpc.Tools + `<Protobuf>` item |
| service-bus-config.json not in output | CopyToOutputDirectory=PreserveNewest |
| Env var tests flaky in parallel | `[NotInParallel("EnvironmentVariables")]` |
| Benchmark project not runnable | Must be `<OutputType>Exe</OutputType>` |
