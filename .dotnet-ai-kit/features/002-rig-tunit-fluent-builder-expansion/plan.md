# Implementation Plan: 002-rig-tunit-fluent-builder-expansion

**Feature ID**: 002-rig-tunit-fluent-builder-expansion
**Mode**: Generic (standalone library)
**Complexity**: Complex (8+ entities, multiple packages, extensive testing)
**Approach**: TDD — tests written first, then implementation, then refactor

## Constitution Check

No `.dotnet-ai-kit/memory/constitution.md` found — skipped. Run `/dai.learn` to generate.

## Complexity Tracking

| Indicator | Value | Rating |
|-----------|-------|--------|
| New packages | 2 (Mediator, WebAPI) | High |
| Modified packages | 5 (Core, Grpc, SqlServer, Redis, ServiceBus) | High |
| New entities/types | 17 public types | High |
| Functional requirements | 38 (FR-001 through FR-038) | High |
| Test coverage | 10 new unit test files + 4 integration + 1 migrated | High |
| Breaking changes | 5 deleted source files, 5 deleted test files, MediatR removal | High |

**Verdict**: Full artifacts required.

---

## Implementation Strategy

### Guiding Principles

1. **TDD Red-Green-Refactor**: Every new type gets a failing test BEFORE implementation
2. **Phase ordering prevents broken builds**: Non-breaking additions first, breaking changes last
3. **One compilable state per step**: After each step, `dotnet build` must pass
4. **Test isolation**: Unit tests never need Docker; integration tests always use containers

### Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| MediatR removal breaks Grpc tests | Phase 2 creates Mediator package AND migrates Grpc in one atomic step |
| Extension deletion breaks integration tests | Phase 4 deletes old extensions AND adds builder replacements together |
| Source generator placement error | FR-004 enforced: `Mediator.SourceGenerator` only in test projects |
| TUnit version mismatch | Phase 0 upgrades TUnit.Core 1.33.0 → 1.34.5 before any changes |

---

## Phase 0: Preparation (non-breaking)

**Goal**: Align versions, ensure clean baseline.

### Step 0.1: TUnit.Core version upgrade
- **Modify**: `src/Rig.TUnit.Core/Rig.TUnit.Core.csproj` — TUnit.Core 1.33.0 → 1.34.5
- **Modify**: `src/Rig.TUnit.SqlServer/Rig.TUnit.SqlServer.csproj` — TUnit.Core 1.33.0 → 1.34.5
- **Verify**: `dotnet build Rig.TUnit.slnx` — zero errors, zero warnings
- **Verify**: `dotnet test` on unit test projects — all 56 tests pass

---

## Phase 1: Core Infrastructure (non-breaking additions)

**Goal**: Add all new Core types. No existing code changes. Tests first.

### Step 1.1: IRigConnectionSource + Connection Sources

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Builder/ConnectionSourceTests.cs`
  - `ConfigConnectionSource_ValidKey_ReturnsValue`
  - `ConfigConnectionSource_MissingKey_ThrowsInvalidOperationException`
  - `OptionsConnectionSource_ValidSelector_ReturnsValue`
  - `OptionsConnectionSource_NullSelector_ThrowsInvalidOperationException`
  - `ValueConnectionSource_ValidString_ReturnsValue`
  - `ValueConnectionSource_NullString_ThrowsArgumentNullException`
  - `AutoConnectionSource_InCi_ReturnsFixtureConnectionString`
  - `AutoConnectionSource_LocalWithConfig_ReturnsConfigValue`
  - `AutoConnectionSource_LocalWithoutConfig_FallsBackToFixture`

**Add test dependency** to `Rig.TUnit.Core.Tests.Unit.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Memory" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="10.0.0" />
```

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Builder/IRigConnectionSource.cs`
- **Create**: `src/Rig.TUnit.Core/Builder/ConfigConnectionSource.cs` (internal sealed)
- **Create**: `src/Rig.TUnit.Core/Builder/OptionsConnectionSource.cs` (internal sealed)
- **Create**: `src/Rig.TUnit.Core/Builder/ValueConnectionSource.cs` (internal sealed)
- **Create**: `src/Rig.TUnit.Core/Builder/AutoConnectionSource.cs` (internal sealed)
- **Modify**: `src/Rig.TUnit.Core/Rig.TUnit.Core.csproj` — add Configuration + Options packages

### Step 1.2: RigConnect Static Factory

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Builder/RigConnectTests.cs`
  - `FromContainer_ReturnsFixtureItself`
  - `FromConfig_ReturnsConfigConnectionSource`
  - `FromOptions_ReturnsOptionsConnectionSource`
  - `FromValue_ReturnsValueConnectionSource`
  - `Auto_ReturnsAutoConnectionSource`

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Builder/RigConnect.cs`

### Step 1.3: RigBuilder + Entry Point

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Builder/RigBuilderTests.cs`
  - `AddRigTUnit_InvokesConfigure_ReturnsServiceCollection`
  - `ForceContainersInCi_SetsFlag`
  - `Services_ExposesServiceCollection`

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Builder/RigBuilder.cs`
  - `Services` property MUST be `public` (FR-035) — cross-assembly extension methods need access
  - `IsForceContainersInCi` stays `internal` — metadata flag only (FR-036)
- **Create**: `src/Rig.TUnit.Core/Builder/RigBuilderExtensions.cs`

### Step 1.4: WaitHelper

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Helpers/WaitHelperTests.cs`
  - `WaitForAsync_ConditionTrue_ReturnsImmediately`
  - `WaitForAsync_ConditionBecomesTrue_ReturnsAfterPolling`
  - `WaitForAsync_Timeout_ThrowsTimeoutException`
  - `WaitForAsync_Cancelled_ThrowsOperationCanceledException`
  - `WaitForAsync_AsyncCondition_Works`
  - `WaitForResultAsync_NonNull_ReturnsResult`
  - `WaitForResultAsync_Timeout_ThrowsTimeoutException`
  - `WaitForResultAsync_CustomPollingInterval_Respected`

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Helpers/WaitHelper.cs`

### Step 1.5: TestConfigurationBuilder

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Configuration/TestConfigurationBuilderTests.cs`
  - `Set_SingleKey_BuildReturnsValue`
  - `SetConnectionString_BuildReturnsCorrectKey`
  - `SetSection_BuildReturnsPrefixedKeys`
  - `BuildOptions_BindsToTypedClass`
  - `Create_StaticFactory_ReturnsConfiguration`
  - `Set_MultipleKeys_AllRetrievable`

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Configuration/TestConfigurationBuilder.cs`

### Step 1.6: RigFixtureBase + CompositeFixture

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.Core.Tests.Unit/Fixtures/CompositeFixtureTests.cs`
  - `InitializeAsync_AllFixturesInitializedInParallel`
  - `DisposeAsync_FixturesDisposedInReverseOrder`
  - `Get_ExistingType_ReturnsFixture`
  - `Get_MissingType_ThrowsInvalidOperationException`
  - `InitializeAsync_NonInitializableFixtures_Ignored`

**Then implement**:
- **Create**: `src/Rig.TUnit.Core/Fixtures/RigFixtureBase.cs`
- **Create**: `src/Rig.TUnit.Core/Fixtures/CompositeFixture.cs`

### Step 1.7: Add IRigConnectionSource to Existing Fixtures

**No new tests needed** — existing fixture tests already verify ConnectionString works.

- **Modify**: `src/Rig.TUnit.SqlServer/Fixtures/SqlServerFixture.cs` — add `: IRigConnectionSource`
- **Modify**: `src/Rig.TUnit.Redis/Fixtures/RedisFixture.cs` — add `: IRigConnectionSource`
- **Modify**: `src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs` — add `: IRigConnectionSource`
- **Verify**: `dotnet build` — zero errors (binary-compatible change)

**Checkpoint**: `dotnet build Rig.TUnit.slnx` passes. All 56 existing tests pass. Phase 1 complete.

---

## Phase 2: Mediator Package (new package + MediatR removal)

**Goal**: Create Rig.TUnit.Mediator, migrate HandlerHelper, remove MediatR.

### Step 2.1: Create Mediator Project

- **Create**: `src/Rig.TUnit.Mediator/Rig.TUnit.Mediator.csproj` (with Mediator.Abstractions 3.0.2)
- **Add to solution**: `Rig.TUnit.slnx` → add under `/src/`

### Step 2.2: HandlerHelper (TDD)

**TDD: Create test project and tests first**
- **Create**: `tests/Rig.TUnit.Mediator.Tests.Unit/Rig.TUnit.Mediator.Tests.Unit.csproj`
  - References: TUnit 1.34.5, Mediator.SourceGenerator 3.0.2, Mediator.Abstractions 3.0.2, M.E.DI 10.0.0, project ref to Rig.TUnit.Mediator
- **Create**: Test infrastructure types:
  - `TestInfrastructure/TestRequest.cs` — `IRequest<string>`
  - `TestInfrastructure/TestRequestHandler.cs`
  - `TestInfrastructure/TestCommand.cs` — `ICommand<int>`
  - `TestInfrastructure/TestCommandHandler.cs`
  - `TestInfrastructure/TestQuery.cs` — `IQuery<string>`
  - `TestInfrastructure/TestQueryHandler.cs`
  - `TestInfrastructure/TestNotification.cs` — `INotification`
  - `TestInfrastructure/TestNotificationHandler.cs`
- **Create**: `tests/Rig.TUnit.Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs`
  - `Send_Request_DispatchesAndReturnsResult`
  - `Send_Command_DispatchesAndReturnsResult`
  - `Send_Query_DispatchesAndReturnsResult`
  - `Publish_Notification_InvokesHandler`
  - `Send_WithCancellationToken_ForwardsToken`
  - `Send_CreatesIsolatedScope_PerCall`
- **Add to solution**: `Rig.TUnit.slnx` → add under `/tests/`

**Then implement**:
- **Create**: `src/Rig.TUnit.Mediator/Helpers/HandlerHelper.cs`

### Step 2.3: Migrate Grpc Package

This is an atomic step — all changes together to keep build passing.

- **Modify**: `src/Rig.TUnit.Grpc/Rig.TUnit.Grpc.csproj`
  - Add: `<ProjectReference Include="..\Rig.TUnit.Mediator\Rig.TUnit.Mediator.csproj" />`
  - Remove: `<PackageReference Include="MediatR" Version="12.4.1" />`
- **Delete**: `src/Rig.TUnit.Grpc/Helpers/HandlerHelper.cs`
- **Modify**: `tests/Rig.TUnit.Grpc.Tests.Unit/Rig.TUnit.Grpc.Tests.Unit.csproj`
  - Remove: `<PackageReference Include="MediatR" Version="12.4.1" />`
  - Add: `<ProjectReference Include="..\..\src\Rig.TUnit.Mediator\Rig.TUnit.Mediator.csproj" />`
  - Add: `<PackageReference Include="Mediator.SourceGenerator" Version="3.0.2">` (PrivateAssets=all)
  - Add: `<PackageReference Include="Mediator.Abstractions" Version="3.0.2" />`
- **Delete**: `tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs` (replaced by Mediator tests)
- **Modify**: `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequest.cs` — change from `MediatR.IRequest<T>` to `Mediator.IRequest<T>`
- **Modify**: `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequestHandler.cs` — change from `MediatR.IRequestHandler<T>` to `Mediator.IRequestHandler<T, TResponse>`, return `ValueTask<T>`
- **Verify**: `dotnet build` + `dotnet test` — Grpc tests pass with new Mediator pipeline

**Checkpoint**: MediatR fully removed. Mediator package working. All tests pass.

---

## Phase 3: WebAPI Package (new package)

**Goal**: Create Rig.TUnit.WebAPI with HttpClientHelper and extensions.

### Step 3.1: Create WebAPI Project

- **Create**: `src/Rig.TUnit.WebAPI/Rig.TUnit.WebAPI.csproj`
  - Project refs: Core, Mediator
  - FrameworkReference: Microsoft.AspNetCore.App
  - Packages: TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6
- **Add to solution**: `Rig.TUnit.slnx` → add under `/src/`

### Step 3.2: HttpClientHelper + Extensions (TDD)

**TDD: Create test project and tests first**
- **Create**: `tests/Rig.TUnit.WebAPI.Tests.Unit/Rig.TUnit.WebAPI.Tests.Unit.csproj`
  - References: TUnit 1.34.5, M.E.DI 10.0.0, FrameworkReference ASP.NET Core, project ref to WebAPI
- **Create**: Test infrastructure:
  - `TestInfrastructure/TestProgram.cs` — minimal API program
  - `TestInfrastructure/TestEndpoints.cs` — GET/POST/PUT/DELETE endpoints
- **Create**: `tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs`
  - `GetAsync_ReturnsDeserializedResponse`
  - `PostAsync_SendsBodyAndReturnsResponse`
  - `PutAsync_SendsBodyAndReturnsResponse`
  - `DeleteAsync_ReturnsResponse`
  - `Client_LazyCreation_CreateOnFirstAccess`
  - `DisposeAsync_DisposesClient`
- **Create**: `tests/Rig.TUnit.WebAPI.Tests.Unit/Extensions/WebApiFactoryExtensionsTests.cs`
  - `WithTestServices_ConfiguresServices`
  - `WithTestServices_WithConfiguration_AddsInMemoryConfig`
- **Add to solution**: `Rig.TUnit.slnx` → add under `/tests/`

**Then implement**:
- **Create**: `src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs`
- **Create**: `src/Rig.TUnit.WebAPI/Extensions/WebApiFactoryExtensions.cs`

### Step 3.3: WebApiRigBuilder (TDD)

**TDD: Write tests first**
- **Create**: `tests/Rig.TUnit.WebAPI.Tests.Unit/Builder/WebApiRigBuilderTests.cs`
  - `UseWebApi_AddHttpClientHelper_RegistersInServiceCollection`
  - `UseWebApi_AddHandlerHelper_RegistersHandlerHelper`

**Then implement**:
- **Create**: `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilder.cs`
- **Create**: `src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilderExtensions.cs`

**Checkpoint**: WebAPI package complete with tests. Build passes.

---

## Phase 4: Package-Specific Builders + Extension Removal

**Goal**: Add fluent builders to each package, then delete old extensions.

**Strategy**: For each package, add the builder FIRST (non-breaking), then delete the old extension (breaking) and update affected tests in one step.

### Step 4.1: SqlServer Builder

**TDD: Integration test first**
- **Create**: `tests/Rig.TUnit.SqlServer.Tests.Integration/Builder/SqlServerRigBuilderTests.cs`
  - `ReplaceDbContext_ViaBuilder_CreatesIsolatedDatabase`
  - `ReplaceDbContext_MultipleCalls_ProduceIsolatedDatabases`

**Then implement**:
- **Create**: `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilder.cs`
- **Create**: `src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilderExtensions.cs`

**Then remove old API + migrate tests (atomic)**:
- **Delete**: `src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs`
- **Delete**: `tests/Rig.TUnit.SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs`
- **Migrate**: `tests/Rig.TUnit.SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs` — replace `UseSqlServerContainerIsolated` with builder API
- **Verify**: `dotnet build` + `dotnet test` on SqlServer projects

### Step 4.2: Redis Builder

**TDD: Integration test first**
- **Create**: `tests/Rig.TUnit.Redis.Tests.Integration/Builder/RedisRigBuilderTests.cs`
  - `ReplaceMultiplexer_ViaBuilder_ReplacesConnection`

**Then implement**:
- **Create**: `src/Rig.TUnit.Redis/Builder/RedisRigBuilder.cs`
- **Create**: `src/Rig.TUnit.Redis/Builder/RedisRigBuilderExtensions.cs`

**Then remove old API (atomic)**:
- **Delete**: `src/Rig.TUnit.Redis/Extensions/RedisContainerExtensions.cs`
- **Delete**: `tests/Rig.TUnit.Redis.Tests.Integration/Extensions/RedisContainerExtensionsTests.cs`
- **Verify**: `dotnet build` + `dotnet test` on Redis projects

### Step 4.3: ServiceBus Builder

**TDD: Integration test first**
- **Create**: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Builder/ServiceBusRigBuilderTests.cs`
  - `ReplaceClient_ViaBuilder_ReplacesServiceBusClient`
  - `ReplaceClient_CustomWrapper_ReceivesConnectionString`

**Then implement**:
- **Create**: `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilder.cs`
- **Create**: `src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs`

**Then remove old API (atomic)**:
- **Delete**: `src/Rig.TUnit.ServiceBus/Extensions/ServiceBusContainerExtensions.cs`
- **Delete**: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Extensions/ServiceBusContainerExtensionsTests.cs`
- **Verify**: `dotnet build` + `dotnet test` on ServiceBus projects

### Step 4.4: Grpc Builder

**TDD: Unit test first**
- **Create**: `tests/Rig.TUnit.Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs`
  - `ReplaceClient_ViaBuilder_RouteThroughTestServer`

**Then implement**:
- **Create**: `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilder.cs`
- **Create**: `src/Rig.TUnit.Grpc/Builder/GrpcRigBuilderExtensions.cs`

**Then remove old API (atomic)**:
- **Delete**: `src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs`
- **Delete**: `tests/Rig.TUnit.Grpc.Tests.Unit/Extensions/GrpcServiceReplacementExtensionsTests.cs`
- **Verify**: `dotnet build` + `dotnet test` on Grpc projects

**Checkpoint**: All builders implemented, all old extensions removed. Build passes. All tests pass.

---

## Phase 5: Enhancements

**Goal**: Add SeedAsync, configurable ConfigFilePath, ListenerHelper refactor.

### Step 5.1: DbContextHelper.SeedAsync (TDD)

**TDD: Test first**
- **Create**: `tests/Rig.TUnit.SqlServer.Tests.Unit/Helpers/DbContextHelperSeedTests.cs`
  - `SeedAsync_AsyncAction_InsertsDataInIsolatedScope`
  - `SeedAsync_SyncAction_InsertsDataAndSaves`
  - `SeedAsync_AutoCallsSaveChangesAsync`

**Then implement**:
- **Modify**: `src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs` — add `SeedAsync` overloads

### Step 5.2: ServiceBusFixture ConfigFilePath

Already has `{ get; init; }` — verify existing tests cover custom path usage.
If not covered, add test. Change `init` to `set` if design requires it (minor).

### Step 5.3: ListenerHelper → WaitHelper Refactor

- **Modify**: `src/Rig.TUnit.ServiceBus/Helpers/ListenerHelper.cs`
  - Replace inline polling loop with `WaitHelper.WaitForAsync()` call
  - Add project reference from ServiceBus to Core if not already present (it is)
- **Verify**: Existing `ListenerHelperTests.cs` still passes

**Checkpoint**: All enhancements done. Build passes.

---

## Phase 6: Solution Finalization + Verification

### Step 6.1: Update Solution File

- **Modify**: `Rig.TUnit.slnx` — ensure all 17 projects listed:
  - `/src/`: Core, Mediator (new), Grpc, WebAPI (new), SqlServer, Redis, ServiceBus, Rig.TUnit
  - `/tests/`: Core.Tests.Unit, Mediator.Tests.Unit (new), Grpc.Tests.Unit, WebAPI.Tests.Unit (new), SqlServer.Tests.Unit, SqlServer.Tests.Integration, Redis.Tests.Integration, ServiceBus.Tests.Integration, Benchmarks

### Step 6.2: Update Meta-Package

- **Modify**: `src/Rig.TUnit/Rig.TUnit.csproj` — add references to Mediator and WebAPI

### Step 6.3: Update Benchmarks

- **Modify**: `tests/Rig.TUnit.Benchmarks/` — add benchmarks for new components

### Step 6.4: Full Verification

1. `dotnet build Rig.TUnit.slnx` — zero errors, zero warnings
2. `dotnet test` (unit tests) — all pass without Docker
3. `dotnet test` (integration tests) — all pass with Docker
4. Verify no MediatR references remain: `grep -r "MediatR" src/ tests/` → zero results
5. Verify no old extension files remain
6. Verify project count: 8 source + 9 test = 17 total

---

## Dependency Order Summary

```
Phase 0: Version alignment (no code changes)
    ↓
Phase 1: Core additions (non-breaking, all new files)
    ↓
Phase 2: Mediator package + MediatR removal (breaking but atomic)
    ↓
Phase 3: WebAPI package (non-breaking, all new files)
    ↓
Phase 4: Package builders + extension deletion (breaking but atomic per package)
    ↓
Phase 5: Enhancements (non-breaking modifications)
    ↓
Phase 6: Solution finalization + verification
```

## File Change Summary

| Action | Count | Details |
|--------|-------|---------|
| Create (source) | 27 | 12 Core, 2 Mediator, 5 WebAPI, 2 SqlServer, 2 Redis, 2 ServiceBus, 2 Grpc |
| Create (test projects) | 2 | Mediator.Tests.Unit, WebAPI.Tests.Unit |
| Create (test files) | 12 | 6 Core, 1 SqlServer, 3 Integration builders, 1 Grpc builder, 1 WebAPI builder |
| Delete (source) | 5 | 4 extension files + Grpc HandlerHelper |
| Delete (test) | 5 | 4 extension tests + Grpc HandlerHelper test |
| Modify (source) | 9 | Core.csproj, 3 fixtures, DbContextHelper, ListenerHelper, Grpc.csproj, meta.csproj, slnx |
| Migrate (test) | 1 | DbContextHelperTests.cs |
| Retain (source) | 2 | InMemoryDbExtensions.cs (FR-018), Grpc WebApplicationFactoryExtensions.cs (FR-037) |
