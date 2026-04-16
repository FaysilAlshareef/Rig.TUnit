# Tasks: Rig.TUnit Fluent Builder Expansion

**Feature**: 002-rig-tunit-fluent-builder-expansion | **Mode**: Generic
**Generated**: 2026-04-16 | **Total Tasks**: 69

---

## Phase 0: Preparation

- [x] T001 Upgrade TUnit.Core from 1.33.0 to 1.34.5 in Core and SqlServer csproj files
      Files: `src/Rig.TUnit.Core/Rig.TUnit.Core.csproj`, `src/Rig.TUnit.SqlServer/Rig.TUnit.SqlServer.csproj`
      Verify: `dotnet build Rig.TUnit.slnx` — zero errors; `dotnet test` — all 56 tests pass

---

## Phase 1: Core Infrastructure (non-breaking additions)

### Connection Sources (TDD)

- [x] T002 Add test dependencies to Core.Tests.Unit csproj (Configuration, Configuration.Memory, Options packages 10.0.0)
      File: `tests/Rig.TUnit.Core.Tests.Unit/Rig.TUnit.Core.Tests.Unit.csproj`

- [x] T003 Add Configuration + Options packages to Core csproj
      File: `src/Rig.TUnit.Core/Rig.TUnit.Core.csproj`
      Packages: `M.E.Configuration.Abstractions 10.0.0`, `M.E.Configuration 10.0.0`, `M.E.Configuration.Memory 10.0.0`, `M.E.Options 10.0.0`

- [x] T004 [RED] Write ConnectionSourceTests — tests for all 4 internal connection source types
      File: `tests/Rig.TUnit.Core.Tests.Unit/Builder/ConnectionSourceTests.cs`
      Tests: ConfigConnectionSource (valid, missing), OptionsConnectionSource (valid, null), ValueConnectionSource (valid, null), AutoConnectionSource (CI, local+config, local-no-config)

- [x] T005 [GREEN] Create IRigConnectionSource interface
      File: `src/Rig.TUnit.Core/Builder/IRigConnectionSource.cs`

- [x] T006 [P] [GREEN] Create ConfigConnectionSource (internal sealed)
      File: `src/Rig.TUnit.Core/Builder/ConfigConnectionSource.cs`

- [x] T007 [P] [GREEN] Create OptionsConnectionSource (internal sealed)
      File: `src/Rig.TUnit.Core/Builder/OptionsConnectionSource.cs`

- [x] T008 [P] [GREEN] Create ValueConnectionSource (internal sealed)
      File: `src/Rig.TUnit.Core/Builder/ValueConnectionSource.cs`

- [x] T009 [P] [GREEN] Create AutoConnectionSource (internal sealed, uses EnvironmentDetection)
      File: `src/Rig.TUnit.Core/Builder/AutoConnectionSource.cs`

- [x] T010 Verify ConnectionSourceTests pass
      Command: `dotnet test --filter "FullyQualifiedName~ConnectionSourceTests"`

### RigConnect (TDD)

- [x] T011 [RED] Write RigConnectTests — tests for all 5 static factory methods
      File: `tests/Rig.TUnit.Core.Tests.Unit/Builder/RigConnectTests.cs`
      Tests: FromContainer, FromConfig, FromOptions, FromValue, Auto

- [x] T012 [GREEN] Create RigConnect static factory class
      File: `src/Rig.TUnit.Core/Builder/RigConnect.cs`

- [x] T013 Verify RigConnectTests pass
      Command: `dotnet test --filter "FullyQualifiedName~RigConnectTests"`

### RigBuilder (TDD)

- [x] T014 [RED] Write RigBuilderTests — tests for AddRigTUnit, ForceContainersInCi, Services property
      File: `tests/Rig.TUnit.Core.Tests.Unit/Builder/RigBuilderTests.cs`

- [x] T015 [GREEN] Create RigBuilder class
      File: `src/Rig.TUnit.Core/Builder/RigBuilder.cs`

- [x] T016 [GREEN] Create RigBuilderExtensions (AddRigTUnit entry point)
      File: `src/Rig.TUnit.Core/Builder/RigBuilderExtensions.cs`

- [x] T017 Verify RigBuilderTests pass
      Command: `dotnet test --filter "FullyQualifiedName~RigBuilderTests"`

### WaitHelper (TDD)

- [x] T018 [RED] Write WaitHelperTests — success, timeout, cancellation, async condition, result, default 250ms polling (FR-011)
      File: `tests/Rig.TUnit.Core.Tests.Unit/Helpers/WaitHelperTests.cs`
      Tests: `WaitForAsync_ConditionTrue_ReturnsImmediately`, `WaitForAsync_ConditionBecomesTrue_ReturnsAfterPolling`, `WaitForAsync_Timeout_ThrowsTimeoutException`, `WaitForAsync_Cancelled_ThrowsOperationCanceledException`, `WaitForAsync_AsyncCondition_Works`, `WaitForAsync_DefaultPollingInterval_Is250ms`, `WaitForResultAsync_NonNull_ReturnsResult`, `WaitForResultAsync_Timeout_ThrowsTimeoutException`, `WaitForResultAsync_CustomPollingInterval_Respected`

- [x] T019 [GREEN] Create WaitHelper static class with 3 overloads
      File: `src/Rig.TUnit.Core/Helpers/WaitHelper.cs`

- [x] T020 Verify WaitHelperTests pass
      Command: `dotnet test --filter "FullyQualifiedName~WaitHelperTests"`

### TestConfigurationBuilder (TDD)

- [x] T021 [RED] Write TestConfigurationBuilderTests — Set, SetConnectionString, SetSection, Build, BuildOptions, Create
      File: `tests/Rig.TUnit.Core.Tests.Unit/Configuration/TestConfigurationBuilderTests.cs`

- [x] T022 [GREEN] Create TestConfigurationBuilder class
      File: `src/Rig.TUnit.Core/Configuration/TestConfigurationBuilder.cs`

- [x] T023 Verify TestConfigurationBuilderTests pass
      Command: `dotnet test --filter "FullyQualifiedName~TestConfigurationBuilderTests"`

### Fixtures (TDD)

- [x] T024 [RED] Write CompositeFixtureTests — parallel init, LIFO dispose, Get<T> success/failure
      File: `tests/Rig.TUnit.Core.Tests.Unit/Fixtures/CompositeFixtureTests.cs`

- [x] T025 [GREEN] Create RigFixtureBase abstract class
      File: `src/Rig.TUnit.Core/Fixtures/RigFixtureBase.cs`

- [x] T026 [GREEN] Create CompositeFixture class
      File: `src/Rig.TUnit.Core/Fixtures/CompositeFixture.cs`

- [x] T027 Verify CompositeFixtureTests pass
      Command: `dotnet test --filter "FullyQualifiedName~CompositeFixtureTests"`

### IRigConnectionSource on Existing Fixtures

- [x] T028 Add IRigConnectionSource interface to SqlServerFixture, RedisFixture, ServiceBusFixture
      Files: `src/Rig.TUnit.SqlServer/Fixtures/SqlServerFixture.cs`, `src/Rig.TUnit.Redis/Fixtures/RedisFixture.cs`, `src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs`

- [x] T029 **CHECKPOINT** — `dotnet build Rig.TUnit.slnx` passes; all 56 existing tests pass; Phase 1 complete
      Command: `dotnet build Rig.TUnit.slnx` then `dotnet test`

---

## Phase 2: Mediator Package + MediatR Removal

### Create Mediator Project

- [x] T030 Create Rig.TUnit.Mediator project with csproj (Mediator.Abstractions 3.0.2, M.E.DI.Abstractions 10.0.0, ref to Core) and add to slnx
      Files: `src/Rig.TUnit.Mediator/Rig.TUnit.Mediator.csproj`, `Rig.TUnit.slnx`

### HandlerHelper (TDD)

- [x] T031 Create Mediator test project with csproj (TUnit 1.34.5, Mediator.SourceGenerator 3.0.2, Mediator.Abstractions 3.0.2, M.E.DI 10.0.0) and add to slnx
      Files: `tests/Rig.TUnit.Mediator.Tests.Unit/Rig.TUnit.Mediator.Tests.Unit.csproj`, `Rig.TUnit.slnx`

- [x] T032 Create test infrastructure: TestRequest, TestRequestHandler, TestCommand, TestCommandHandler, TestQuery, TestQueryHandler, TestNotification, TestNotificationHandler
      Files: `tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/*.cs` (8 files)
      Note: T033 depends on these types — must complete before writing tests

- [x] T033 [RED] Write HandlerHelperTests — Send(Request), Send(Command), Send(Query), Publish(Notification), CancellationToken, scope isolation
      File: `tests/Rig.TUnit.Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs`

- [x] T034 [GREEN] Create HandlerHelper class using martinothamar/Mediator interfaces (ValueTask<T>)
      File: `src/Rig.TUnit.Mediator/Helpers/HandlerHelper.cs`

- [x] T035 Verify HandlerHelperTests pass
      Command: `dotnet test --filter "FullyQualifiedName~Mediator.Tests.Unit"`

### Migrate Grpc (atomic)

- [x] T036 [ATOMIC] Migrate Grpc package: add Mediator project ref, remove MediatR, delete old HandlerHelper, update Grpc test csproj (add Mediator refs, remove MediatR), delete old HandlerHelperTests, update TestRequest/TestRequestHandler to Mediator types. **Search comprehensively** for ALL remaining MediatR usages in both `src/Rig.TUnit.Grpc/` and `tests/Rig.TUnit.Grpc.Tests.Unit/` (`grep -r "MediatR"`) and update all references.
      Files: `src/Rig.TUnit.Grpc/Rig.TUnit.Grpc.csproj`, `src/Rig.TUnit.Grpc/Helpers/HandlerHelper.cs` (DELETE), `tests/Rig.TUnit.Grpc.Tests.Unit/Rig.TUnit.Grpc.Tests.Unit.csproj`, `tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs` (DELETE), `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequest.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequestHandler.cs`, plus any other files containing `MediatR` references

- [x] T037 **CHECKPOINT** — MediatR fully removed; `dotnet build` passes; all tests pass
      Command: `dotnet build Rig.TUnit.slnx` then `dotnet test --filter "Category!=Integration"`

---

## Phase 3: WebAPI Package

### Create WebAPI Project

- [ ] T038 Create Rig.TUnit.WebAPI project with csproj (Core ref, Mediator ref, ASP.NET Core FrameworkRef, TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6) and add to slnx
      Files: `src/Rig.TUnit.WebAPI/Rig.TUnit.WebAPI.csproj`, `Rig.TUnit.slnx`

### HttpClientHelper + Extensions (TDD)

- [ ] T039 Create WebAPI test project with csproj (TUnit 1.34.5, M.E.DI 10.0.0, ASP.NET Core FrameworkRef, ref to WebAPI) and add to slnx
      Files: `tests/Rig.TUnit.WebAPI.Tests.Unit/Rig.TUnit.WebAPI.Tests.Unit.csproj`, `Rig.TUnit.slnx`

- [ ] T040 Create test infrastructure: TestProgram (minimal API), TestEndpoints (GET/POST/PUT/DELETE)
      Files: `tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestProgram.cs`, `tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestEndpoints.cs`
      Note: T041/T042 depend on these types — must complete before writing tests

- [ ] T041 [RED] Write HttpClientHelperTests — GetAsync, PostAsync, PutAsync, DeleteAsync, CreateClient with options, lazy Client, DisposeAsync
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs`
      Tests: `GetAsync_ReturnsDeserializedResponse`, `PostAsync_SendsBodyAndReturnsResponse`, `PutAsync_SendsBodyAndReturnsResponse`, `DeleteAsync_ReturnsResponse`, `CreateClient_WithOptions_ReturnsConfiguredClient`, `Client_LazyCreation_CreateOnFirstAccess`, `DisposeAsync_DisposesClient`

- [ ] T042 [RED] Write WebApiFactoryExtensionsTests — WithTestServices, WithTestServices+configuration
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Extensions/WebApiFactoryExtensionsTests.cs`

- [ ] T043 [GREEN] Create HttpClientHelper<TProgram> class
      File: `src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs`

- [ ] T044 [GREEN] Create WebApiFactoryExtensions class
      File: `src/Rig.TUnit.WebAPI/Extensions/WebApiFactoryExtensions.cs`

- [ ] T045 [RED] Write WebApiRigBuilderTests — UseWebApi registers HttpClientHelper, AddHandlerHelper registers HandlerHelper
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Builder/WebApiRigBuilderTests.cs`
      Tests: UseWebApi_AddHttpClientHelper_RegistersInServiceCollection, UseWebApi_AddHandlerHelper_RegistersHandlerHelper

- [ ] T046 [GREEN] Create WebApiRigBuilder<TProgram> + WebApiRigBuilderExtensions
      Files: `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilder.cs`, `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilderExtensions.cs`

- [ ] T047 Verify WebAPI tests pass
      Command: `dotnet test --filter "FullyQualifiedName~WebAPI.Tests.Unit"`

---

## Phase 4: Package Builders + Extension Removal

### SqlServer Builder (TDD + atomic removal)

- [ ] T048 [depends: T029] [RED] Write SqlServerRigBuilderTests (integration) — ReplaceDbContext isolation, multiple contexts
      File: `tests/Rig.TUnit.SqlServer.Tests.Integration/Builder/SqlServerRigBuilderTests.cs`

- [ ] T049 [GREEN] Create SqlServerRigBuilder + SqlServerRigBuilderExtensions
      Files: `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilder.cs`, `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilderExtensions.cs`

- [ ] T050 [ATOMIC] Delete SqlServerContainerExtensions.cs (source + test), migrate DbContextHelperTests to builder API
      Delete: `src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs`, `tests/Rig.TUnit.SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs`
      Migrate: `tests/Rig.TUnit.SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs`
      **IMPORTANT**: Do NOT delete `InMemoryDbExtensions.cs` — it is kept per FR-018.

### Redis Builder (TDD + atomic removal)

- [ ] T051 [RED] Write RedisRigBuilderTests (integration) — ReplaceMultiplexer via builder, ReplaceClient<T> custom factory
      File: `tests/Rig.TUnit.Redis.Tests.Integration/Builder/RedisRigBuilderTests.cs`
      Tests: `ReplaceMultiplexer_ViaBuilder_ReplacesConnection`, `ReplaceClient_CustomFactory_ReturnsCustomType`

- [ ] T052 [GREEN] Create RedisRigBuilder + RedisRigBuilderExtensions
      Files: `src/Rig.TUnit.Redis/Builder/RedisRigBuilder.cs`, `src/Rig.TUnit.Redis/Builder/RedisRigBuilderExtensions.cs`

- [ ] T053 [ATOMIC] Delete RedisContainerExtensions.cs (source + test)
      Delete: `src/Rig.TUnit.Redis/Extensions/RedisContainerExtensions.cs`, `tests/Rig.TUnit.Redis.Tests.Integration/Extensions/RedisContainerExtensionsTests.cs`

### ServiceBus Builder (TDD + atomic removal)

- [ ] T054 [RED] Write ServiceBusRigBuilderTests (integration) — ReplaceClient, custom wrapper
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Builder/ServiceBusRigBuilderTests.cs`

- [ ] T055 [GREEN] Create ServiceBusRigBuilder + ServiceBusRigBuilderExtensions
      Files: `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilder.cs`, `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs`

- [ ] T056 [ATOMIC] Delete ServiceBusContainerExtensions.cs (source + test)
      Delete: `src/Rig.TUnit.ServiceBus/Extensions/ServiceBusContainerExtensions.cs`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/Extensions/ServiceBusContainerExtensionsTests.cs`

### Grpc Builder (TDD + atomic removal)

- [ ] T057 [RED] Write GrpcRigBuilderTests — ReplaceClient via builder routes through test server
      File: `tests/Rig.TUnit.Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs`

- [ ] T058 [GREEN] Create GrpcRigBuilder<TProgram> + GrpcRigBuilderExtensions
      Files: `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilder.cs`, `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilderExtensions.cs`

- [ ] T059 [ATOMIC] Delete GrpcServiceReplacementExtensions.cs (source + test)
      Delete: `src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/Extensions/GrpcServiceReplacementExtensionsTests.cs`

- [ ] T060 **CHECKPOINT** — All builders work, all old extensions removed; `dotnet build` passes; all unit tests pass; verify `InMemoryDbExtensions.cs` still exists and its tests pass
      Command: `dotnet build Rig.TUnit.slnx` then `dotnet test --filter "Category!=Integration"`

---

## Phase 5: Enhancements

### DbContextHelper.SeedAsync (TDD)

- [ ] T061 [RED] Write DbContextHelperSeedTests — async SeedAsync, sync SeedAsync, auto SaveChangesAsync
      File: `tests/Rig.TUnit.SqlServer.Tests.Unit/Helpers/DbContextHelperSeedTests.cs`

- [ ] T062 [GREEN] Add SeedAsync overloads to DbContextHelper
      File: `src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs`

- [ ] T063 Verify DbContextHelperSeedTests pass
      Command: `dotnet test --filter "FullyQualifiedName~DbContextHelperSeedTests"`

### ServiceBusFixture ConfigFilePath Verification

- [ ] T064 Verify ServiceBusFixture.ConfigFilePath defaults to `"TestInfrastructure/service-bus-config.json"` and is settable before InitializeAsync; add unit test if not already covered (FR-015)
      File: `src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs`
      Note: Existing code uses `{ get; init; }` which satisfies the design. Verify with a test that custom path is accepted.

### ListenerHelper → WaitHelper Refactor

- [ ] T065 Refactor ListenerHelper.WaitForMessagesAsync to delegate to WaitHelper.WaitForAsync (FR-016)
      File: `src/Rig.TUnit.ServiceBus/Helpers/ListenerHelper.cs`
      Verify: Existing ListenerHelper integration tests still pass

---

## Phase 6: Solution Finalization + Verification

- [ ] T066 Verify Rig.TUnit.slnx has all 17 projects (8 src + 9 test)
      File: `Rig.TUnit.slnx`

- [ ] T067 Update meta-package Rig.TUnit.csproj — add project refs to Mediator and WebAPI
      File: `src/Rig.TUnit/Rig.TUnit.csproj`

- [ ] T068 Update Benchmarks project — add specific benchmark files:
      Files: `tests/Rig.TUnit.Benchmarks/WaitHelperBenchmarks.cs`, `tests/Rig.TUnit.Benchmarks/TestConfigurationBuilderBenchmarks.cs`, `tests/Rig.TUnit.Benchmarks/CompositeFixtureBenchmarks.cs`, `tests/Rig.TUnit.Benchmarks/HttpClientHelperBenchmarks.cs`
      All benchmark classes MUST include `[MemoryDiagnoser]` (SC-020)

- [ ] T069 **FINAL VERIFICATION** — `dotnet build Rig.TUnit.slnx` zero errors zero warnings; all unit tests pass without Docker; no MediatR references remain (`grep -r "MediatR" src/ tests/` → zero); no old extension files remain; `InMemoryDbExtensions.cs` exists; 8 source + 9 test = 17 projects
      Commands: `dotnet build Rig.TUnit.slnx`, `dotnet test --filter "Category!=Integration"`, verify no MediatR references

---

## Task Summary

| Phase | Tasks | Parallel | Description |
|-------|-------|----------|-------------|
| 0 | T001 (1) | 0 | Version alignment |
| 1 | T002-T029 (28) | 4 | Core infrastructure (TDD) |
| 2 | T030-T037 (8) | 0 | Mediator package + MediatR removal |
| 3 | T038-T047 (10) | 0 | WebAPI package (TDD) |
| 4 | T048-T060 (13) | 0 | Package builders + extension deletion |
| 5 | T061-T065 (5) | 0 | Enhancements (TDD) |
| 6 | T066-T069 (4) | 0 | Finalization + verification |
| **Total** | **69** | **4** | |

## Legend

- `[RED]` — TDD: write failing test (test exists, implementation doesn't)
- `[GREEN]` — TDD: implement to make test pass
- `[P]` — Can run in parallel with adjacent `[P]` tasks
- `[ATOMIC]` — All changes in this task must be applied together (build would break if partial)
- `[depends: T{N}]` — Blocked until specified task completes
- **CHECKPOINT** — Stop and verify build + tests before proceeding
