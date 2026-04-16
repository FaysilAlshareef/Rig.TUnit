# Tasks: Rig.TUnit Fluent Builder Expansion

**Feature**: 002-rig-tunit-fluent-builder-expansion | **Mode**: Generic
**Generated**: 2026-04-16 | **Total Tasks**: 75

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

- [x] T038 Create Rig.TUnit.WebAPI project with csproj (Core ref, Mediator ref, ASP.NET Core FrameworkRef, TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6) and add to slnx
      Files: `src/Rig.TUnit.WebAPI/Rig.TUnit.WebAPI.csproj`, `Rig.TUnit.slnx`

### HttpClientHelper + Extensions (TDD)

- [x] T039 Create WebAPI test project with csproj (TUnit 1.34.5, M.E.DI 10.0.0, ASP.NET Core FrameworkRef, ref to WebAPI) and add to slnx
      Files: `tests/Rig.TUnit.WebAPI.Tests.Unit/Rig.TUnit.WebAPI.Tests.Unit.csproj`, `Rig.TUnit.slnx`

- [x] T040 Create test infrastructure: TestProgram (minimal API), TestEndpoints (GET/POST/PUT/DELETE)
      Files: `tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestProgram.cs`, `tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestEndpoints.cs`
      Note: T041/T042 depend on these types — must complete before writing tests

- [x] T041 [RED] Write HttpClientHelperTests — GetAsync, PostAsync, PutAsync, DeleteAsync, CreateClient with options, lazy Client, DisposeAsync
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs`
      Tests: `GetAsync_ReturnsDeserializedResponse`, `PostAsync_SendsBodyAndReturnsResponse`, `PutAsync_SendsBodyAndReturnsResponse`, `DeleteAsync_ReturnsResponse`, `CreateClient_WithOptions_ReturnsConfiguredClient`, `Client_LazyCreation_CreateOnFirstAccess`, `DisposeAsync_DisposesClient`

- [x] T042 [RED] Write WebApiFactoryExtensionsTests — WithTestServices, WithTestServices+configuration
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Extensions/WebApiFactoryExtensionsTests.cs`

- [x] T043 [GREEN] Create HttpClientHelper<TProgram> class
      File: `src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs`

- [x] T044 [GREEN] Create WebApiFactoryExtensions class
      File: `src/Rig.TUnit.WebAPI/Extensions/WebApiFactoryExtensions.cs`

- [x] T045 [RED] Write WebApiRigBuilderTests — UseWebApi registers HttpClientHelper, AddHandlerHelper registers HandlerHelper
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Builder/WebApiRigBuilderTests.cs`
      Tests: UseWebApi_AddHttpClientHelper_RegistersInServiceCollection, UseWebApi_AddHandlerHelper_RegistersHandlerHelper

- [x] T046 [GREEN] Create WebApiRigBuilder<TProgram> + WebApiRigBuilderExtensions
      Files: `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilder.cs`, `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilderExtensions.cs`

- [x] T047 Verify WebAPI tests pass — 11/11 pass
      Command: `dotnet test --filter "FullyQualifiedName~WebAPI.Tests.Unit"`

---

## Phase 4: Package Builders + Extension Removal

### SqlServer Builder (TDD + atomic removal)

- [x] T048 [depends: T029] [RED] Write SqlServerRigBuilderTests (integration) — ReplaceDbContext isolation, multiple contexts
      File: `tests/Rig.TUnit.SqlServer.Tests.Integration/Builder/SqlServerRigBuilderTests.cs`

- [x] T049 [GREEN] Create SqlServerRigBuilder + SqlServerRigBuilderExtensions
      Files: `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilder.cs`, `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilderExtensions.cs`

- [x] T050 [ATOMIC] Delete SqlServerContainerExtensions.cs (source + test), migrate DbContextHelperTests to builder API
      Delete: `src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs`, `tests/Rig.TUnit.SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs`
      Migrate: `tests/Rig.TUnit.SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs`

### Redis Builder (TDD + atomic removal)

- [x] T051 [RED] Write RedisRigBuilderTests (integration) — ReplaceMultiplexer via builder, ReplaceClient<T> custom factory
      File: `tests/Rig.TUnit.Redis.Tests.Integration/Builder/RedisRigBuilderTests.cs`

- [x] T052 [GREEN] Create RedisRigBuilder + RedisRigBuilderExtensions
      Files: `src/Rig.TUnit.Redis/Builder/RedisRigBuilder.cs`, `src/Rig.TUnit.Redis/Builder/RedisRigBuilderExtensions.cs`

- [x] T053 [ATOMIC] Delete RedisContainerExtensions.cs (source + test)

### ServiceBus Builder (TDD + atomic removal)

- [x] T054 [RED] Write ServiceBusRigBuilderTests (integration) — ReplaceClient, custom wrapper
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Builder/ServiceBusRigBuilderTests.cs`

- [x] T055 [GREEN] Create ServiceBusRigBuilder + ServiceBusRigBuilderExtensions
      Files: `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilder.cs`, `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs`

- [x] T056 [ATOMIC] Delete ServiceBusContainerExtensions.cs (source + test)

### Grpc Builder (TDD + atomic removal)

- [x] T057 [RED] Write GrpcRigBuilderTests — ReplaceClient via builder routes through test server
      File: `tests/Rig.TUnit.Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs`

- [x] T058 [GREEN] Create GrpcRigBuilder<TProgram> + GrpcRigBuilderExtensions
      Files: `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilder.cs`, `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilderExtensions.cs`

- [x] T059 [ATOMIC] Delete GrpcServiceReplacementExtensions.cs (source + test)

- [x] T060 **CHECKPOINT** — All builders work, all old extensions removed; `dotnet build` passes; all unit tests pass (81 pass: 51 Core + 3 SqlServer.Unit + 6 Mediator + 10 Grpc + 11 WebAPI); `InMemoryDbExtensions.cs` retained.

---

## Phase 5: Enhancements

### DbContextHelper.SeedAsync (TDD)

- [x] T061 [RED] Write DbContextHelperSeedTests — async SeedAsync, sync SeedAsync, auto SaveChangesAsync
      File: `tests/Rig.TUnit.SqlServer.Tests.Unit/Helpers/DbContextHelperSeedTests.cs`

- [x] T062 [GREEN] Add SeedAsync overloads to DbContextHelper
      File: `src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs`
      Also: stabilized `InMemoryDbExtensions.UseInMemoryDatabase` to capture the db name outside the options delegate so every scope hits the same in-memory store.

- [x] T063 Verify DbContextHelperSeedTests pass

### ServiceBusFixture ConfigFilePath Verification

- [x] T064 Verify ServiceBusFixture.ConfigFilePath — covered by `ServiceBusFixtureConfigPathTests`

### ListenerHelper → WaitHelper Refactor

- [x] T065 Refactor ListenerHelper.WaitForMessagesAsync to delegate to WaitHelper.WaitForAsync (FR-016)

---

## Phase 6: Solution Finalization + Verification

- [x] T066 Verify Rig.TUnit.slnx has all 17 projects (8 src + 9 test)

- [x] T067 Update meta-package Rig.TUnit.csproj — add project refs to Mediator and WebAPI

- [x] T068 Update Benchmarks project — WaitHelperBenchmarks, TestConfigurationBuilderBenchmarks, CompositeFixtureBenchmarks, HttpClientHelperBenchmarks

- [x] T069 **FINAL VERIFICATION** — full solution builds 0 errors/0 warnings; all 84 unit tests pass (51 Core + 6 SqlServer + 6 Mediator + 11 WebAPI + 10 Grpc); no MediatR references in src/ or tests/; `InMemoryDbExtensions.cs` retained; 17 projects total.

---

## Phase 7: Test Authentication & HttpClientHelper Headers (retroactive — C-018)

These tasks formalize work added during implementation to round out the WebAPI testing story, and the follow-up test coverage added after code review (C-018).

### Authentication Surface (production code, retroactively documented)

- [x] T070 Add `TestAuthenticationOptions` (`public sealed`, inherits `AuthenticationSchemeOptions`) with `DefaultUserName` (default `"test-user"`) and mutable `IList<Claim> Claims` (FR-040).
      File: `src/Rig.TUnit.WebAPI/Authentication/TestAuthenticationOptions.cs`

- [x] T071 Add `TestAuthenticationHandler` (`public sealed`, scheme name `"Test"`) that returns `AuthenticateResult.Success` with configured claims or default-name claim when `Claims` empty (FR-039).
      File: `src/Rig.TUnit.WebAPI/Authentication/TestAuthenticationHandler.cs`

- [x] T072 Add `TestAuthenticationExtensions` with `WithTestAuthentication` and `WithPermissiveAuthorization` extension methods on `WebApplicationFactory<TProgram>`; both null-check the factory (FR-041, FR-042).
      File: `src/Rig.TUnit.WebAPI/Authentication/TestAuthenticationExtensions.cs`
      Note: `WithPermissiveAuthorization` overrides only `DefaultPolicy` / `FallbackPolicy` — named policies and role requirements still apply. Documented in XML docs per C-019.

- [x] T073 Add `HttpClientHelper<TProgram>.WithBearerToken(string?)` and `.WithHeader(string, string)` fluent methods (FR-043, FR-044).
      File: `src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs`

### Tests (TDD retroactively — filled after code review)

- [x] T074 [GREEN] Add tests for `TestAuthenticationOptions`, `TestAuthenticationHandler`, and `TestAuthenticationExtensions`.
      Files:
        - `tests/Rig.TUnit.WebAPI.Tests.Unit/Authentication/TestAuthenticationOptionsTests.cs`
        - `tests/Rig.TUnit.WebAPI.Tests.Unit/Authentication/TestAuthenticationHandlerTests.cs`
        - `tests/Rig.TUnit.WebAPI.Tests.Unit/Authentication/TestAuthenticationExtensionsTests.cs`
      Scenarios covered: default options, custom `DefaultUserName`, claims mutation, handler default-name fallback, handler custom-claims application, handler no-name-injection when claims provided, `WithTestAuthentication_NullFactory_Throws`, `WithPermissiveAuthorization_NullFactory_Throws`, end-to-end `/secure/me` returns 200 with default name, end-to-end `/secure/me` returns 200 with custom claims echoing `alice`, anonymous endpoint remains accessible when auth is registered.
      Also: added `[Authorize]` endpoint (`/secure/me`) and header-echo endpoints (`/headers/authorization`, `/headers/{name}`) to `TestEndpoints.cs`, and wired `UseAuthentication`/`UseAuthorization` into `TestWebApplicationFactory`.

- [x] T075 [GREEN] Add tests for `HttpClientHelper.WithBearerToken` and `WithHeader` (append to existing `HttpClientHelperTests.cs`).
      File: `tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs`
      Scenarios: `WithBearerToken_SetsAuthorizationHeader`, `WithBearerToken_Null_ClearsAuthorizationHeader`, `WithBearerToken_ReturnsSelf_EnablesChaining`, `WithBearerToken_RoundTripsThroughTestServer`, `WithHeader_SetsHeader`, `WithHeader_OverwritesExistingValue`, `WithHeader_NullOrEmptyName_Throws`, `WithHeader_ReturnsSelf_EnablesChaining`, `WithHeader_RoundTripsThroughTestServer`.

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
| 7 | T070-T075 (6) | 0 | Test authentication + header helpers (retroactive) |
| **Total** | **75** | **4** | |

## Legend

- `[RED]` — TDD: write failing test (test exists, implementation doesn't)
- `[GREEN]` — TDD: implement to make test pass
- `[P]` — Can run in parallel with adjacent `[P]` tasks
- `[ATOMIC]` — All changes in this task must be applied together (build would break if partial)
- `[depends: T{N}]` — Blocked until specified task completes
- **CHECKPOINT** — Stop and verify build + tests before proceeding
