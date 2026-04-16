# Tasks: Rig.TUnit Testing Infrastructure Library

**Feature**: 001-rig-tunit-library | **Mode**: Generic
**Generated**: 2026-04-16 | **Total Tasks**: 37

---

## Phase 0: Solution Scaffolding

- [ ] T001 Create solution scaffolding files
      Files: `global.json`, `Directory.Build.props`, `.gitignore`, `.editorconfig`, `Rig.TUnit.slnx`
      Notes: slnx starts with empty /src/ and /tests/ folders. Directory.Build.props centralizes net10.0, ImplicitUsings, Nullable, TreatWarningsAsErrors, LangVersion=latest. .editorconfig enforces file-scoped namespaces, var preferences, and C# conventions.

## Phase 1: Rig.TUnit.Core — TDD

### RED: Stubs + Tests

- [ ] T002 Create Rig.TUnit.Core csproj with stub classes
      Files: `src/Rig.TUnit.Core/Rig.TUnit.Core.csproj`, `src/Rig.TUnit.Core/Fakers/CustomConstructorFaker.cs`, `src/Rig.TUnit.Core/Extensions/ServiceRemovalExtensions.cs`, `src/Rig.TUnit.Core/Extensions/EnvironmentDetection.cs`
      Notes: Stubs = empty public classes/methods that compile but throw NotImplementedException. NuGet: TUnit.Core 1.33.0, Bogus 35.6.1, Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0. Add project to slnx.

- [ ] T003 Create Core.Tests.Unit csproj with test infrastructure
      Files: `tests/Rig.TUnit.Core.Tests.Unit/Rig.TUnit.Core.Tests.Unit.csproj`, `tests/Rig.TUnit.Core.Tests.Unit/TestInfrastructure/TestEntity.cs`
      Notes: NuGet: TUnit 1.33.0 (meta-package, NOT TUnit.Core), Microsoft.Extensions.DependencyInjection. ProjectRef to Core. TestEntity = sealed class with private ctor + private setters. Add project to slnx.

- [ ] T004 [P] Write CustomConstructorFakerTests.cs (3 tests)
      File: `tests/Rig.TUnit.Core.Tests.Unit/Fakers/CustomConstructorFakerTests.cs`
      Tests: Create_WithPrivateConstructor_CreatesInstance, Generate_WithPropertyRules_AppliesRules, Generate_CalledTwice_ProducesDistinctInstances

- [ ] T005 [P] Write ServiceRemovalExtensionsTests.cs (7 tests)
      File: `tests/Rig.TUnit.Core.Tests.Unit/Extensions/ServiceRemovalExtensionsTests.cs`
      Tests: RemoveService_WhenServiceExists_RemovesRegistration, RemoveService_WhenServiceMissing_ReturnsUnchanged, RemoveService_Always_ReturnsServiceCollectionForChaining, RemoveImplementation_WhenExists_RemovesDescriptor, RemoveImplementation_WhenMissing_ReturnsUnchanged, RemoveByName_WhenMultipleMatch_RemovesAll, RemoveByName_WhenNoneMatch_ReturnsUnchanged

- [ ] T006 [P] Write EnvironmentDetectionTests.cs (4 tests)
      File: `tests/Rig.TUnit.Core.Tests.Unit/Extensions/EnvironmentDetectionTests.cs`
      Tests: IsRunningInCiCd_GithubActionsSet_ReturnsTrue, IsRunningInCiCd_CiVariableSet_ReturnsTrue, IsRunningInCiCd_TfBuildSet_ReturnsTrue, IsRunningInCiCd_NoVariablesSet_ReturnsFalse
      Notes: Use [NotInParallel("EnvironmentVariables")]. Set env vars in [Before(Test)], restore in [After(Test)].

### GREEN: Implement + Verify

- [ ] T007 [depends: T004, T005, T006] Implement Core source classes and verify tests pass
      Files: `src/Rig.TUnit.Core/Fakers/CustomConstructorFaker.cs`, `src/Rig.TUnit.Core/Extensions/ServiceRemovalExtensions.cs`, `src/Rig.TUnit.Core/Extensions/EnvironmentDetection.cs`
      Notes: Implement from design doc. CustomConstructorFaker uses RuntimeHelpers.GetUninitializedObject. Verify: `dotnet test tests/Rig.TUnit.Core.Tests.Unit` — 14 tests pass.

## Phase 2: Rig.TUnit.Grpc — TDD

### RED: Stubs + Tests

- [ ] T008 Create Rig.TUnit.Grpc csproj with stub classes
      Files: `src/Rig.TUnit.Grpc/Rig.TUnit.Grpc.csproj`, `src/Rig.TUnit.Grpc/Helpers/GrpcClientHelper.cs`, `src/Rig.TUnit.Grpc/Helpers/HandlerHelper.cs`, `src/Rig.TUnit.Grpc/Helpers/MetadataHelper.cs`, `src/Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs`, `src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs`
      Notes: ProjectRef to Core. FrameworkReference Microsoft.AspNetCore.App. NuGet: TUnit.AspNetCore *, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Grpc.AspNetCore 2.71.0, Grpc.Net.Client 2.71.0, Grpc.Net.ClientFactory 2.71.0, Calzolari.Grpc.Net.Client.Validation 9.0.0, MediatR 12.4.1, Serilog 4.2.0, Serilog.Sinks.Console 6.0.0. GrpcClientHelper<TClient, TProgram> (C-002). HandlerHelper: only Send<TResult>, no HandleEvent (FR-003). Add to slnx.

- [ ] T009 Create Grpc.Tests.Unit csproj + proto + test infrastructure
      Files: `tests/Rig.TUnit.Grpc.Tests.Unit/Rig.TUnit.Grpc.Tests.Unit.csproj`, `tests/Rig.TUnit.Grpc.Tests.Unit/Protos/test.proto`, `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestProgram.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestGrpcService.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequest.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/TestRequestHandler.cs`
      Notes: NuGet: TUnit 1.33.0, Google.Protobuf, Grpc.Tools, MediatR 12.4.1. FrameworkReference ASP.NET Core. Proto item: <Protobuf Include="Protos\test.proto" GrpcServices="Both" />. TestProgram = minimal WebApplication registering MediatR + mapping TestGrpcService. TestRequestHandler returns predictable value. No NSubstitute (C-010). Add to slnx.

- [ ] T010 [P] Write MetadataHelperTests.cs (3 tests)
      File: `tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/MetadataHelperTests.cs`
      Tests: Build_WithClaimsDictionary_SetsAccessClaimsBinKey, Build_WithEmptyDictionary_ReturnsValidMetadata, Build_WithClaims_SerializesToProtobufBinary

- [ ] T011 [P] Write GrpcClientHelperTests.cs + HandlerHelperTests.cs (4 tests)
      Files: `tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/GrpcClientHelperTests.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs`
      Tests: GrpcClientHelper — Send_WithTestService_ReturnsResponse, SendAsync_WithTestService_ReturnsResponse. HandlerHelper — Send_WithTestRequest_DispatchesViaMediatR, Send_CreatesNewScopePerCall.

- [ ] T012 [P] Write WebApplicationFactoryExtensionsTests.cs + GrpcServiceReplacementExtensionsTests.cs (6 tests)
      Files: `tests/Rig.TUnit.Grpc.Tests.Unit/Extensions/WebApplicationFactoryExtensionsTests.cs`, `tests/Rig.TUnit.Grpc.Tests.Unit/Extensions/GrpcServiceReplacementExtensionsTests.cs`
      Tests: WithTestConfiguration_ConfiguresLogging, WithTestConfiguration_AppliesServiceOverrides, WithTestConfiguration_MapsEndpoints, WithTestConfiguration_WithNullCallbacks_DoesNotThrow, ReplaceGrpcClient_ReplacesExisting_RoutesThrough TestServer, CreateGrpcChannel_ReturnsValidChannel.

### GREEN: Implement + Verify

- [ ] T013 [depends: T010, T011, T012] Implement Grpc source classes and verify tests pass
      Files: `src/Rig.TUnit.Grpc/Helpers/GrpcClientHelper.cs`, `src/Rig.TUnit.Grpc/Helpers/HandlerHelper.cs`, `src/Rig.TUnit.Grpc/Helpers/MetadataHelper.cs`, `src/Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs`, `src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs`
      Notes: MetadataHelper uses Google.Protobuf.WellKnownTypes.Struct for generic dict serialization. GrpcServiceReplacementExtensions uses generic <TClient, TProgram> (C-005). Verify: `dotnet test tests/Rig.TUnit.Grpc.Tests.Unit` — 13 tests pass.

## Phase 3: Rig.TUnit.SqlServer — TDD

### RED: Stubs + Unit Tests

- [ ] T014 Create Rig.TUnit.SqlServer csproj with stub classes
      Files: `src/Rig.TUnit.SqlServer/Rig.TUnit.SqlServer.csproj`, `src/Rig.TUnit.SqlServer/Fixtures/SqlServerFixture.cs`, `src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs`, `src/Rig.TUnit.SqlServer/Extensions/InMemoryDbExtensions.cs`, `src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs`
      Notes: ProjectRef to Core. NuGet: Testcontainers.MsSql 4.6.0, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.InMemory 10.0.0. Add to slnx.

- [ ] T015 Create SqlServer.Tests.Unit csproj + test infrastructure
      Files: `tests/Rig.TUnit.SqlServer.Tests.Unit/Rig.TUnit.SqlServer.Tests.Unit.csproj`, `tests/Rig.TUnit.SqlServer.Tests.Unit/TestInfrastructure/TestDbContext.cs`, `tests/Rig.TUnit.SqlServer.Tests.Unit/TestInfrastructure/TestEntity.cs`
      Notes: NuGet: TUnit 1.33.0, Microsoft.Extensions.DependencyInjection. ProjectRef to SqlServer. TestDbContext with DbSet<TestEntity>. Add to slnx.

- [ ] T016 Write InMemoryDbExtensionsTests.cs (3 tests)
      File: `tests/Rig.TUnit.SqlServer.Tests.Unit/Extensions/InMemoryDbExtensionsTests.cs`
      Tests: UseInMemoryDatabase_ReplacesExistingRegistration, UseInMemoryDatabase_CalledTwice_UsesUniqueGuidNames, UseInMemoryDatabase_ParallelCalls_ProduceIsolatedDatabases

### GREEN: Unit Tests

- [ ] T017 [depends: T016] Implement InMemoryDbExtensions.cs and verify unit tests pass
      File: `src/Rig.TUnit.SqlServer/Extensions/InMemoryDbExtensions.cs`
      Notes: Uses Core's RemoveByName — ensure `using Rig.TUnit.Core.Extensions;`. Verify: `dotnet test tests/Rig.TUnit.SqlServer.Tests.Unit` — 3 tests pass.

### RED: Integration Tests

- [ ] T018 Create SqlServer.Tests.Integration csproj + test infrastructure
      Files: `tests/Rig.TUnit.SqlServer.Tests.Integration/Rig.TUnit.SqlServer.Tests.Integration.csproj`, `tests/Rig.TUnit.SqlServer.Tests.Integration/TestInfrastructure/TestDbContext.cs`, `tests/Rig.TUnit.SqlServer.Tests.Integration/TestInfrastructure/TestEntity.cs`
      Notes: NuGet: TUnit 1.33.0. ProjectRef to SqlServer. Own TestDbContext + TestEntity copies (FR-021). TestDbContext overrides OnModelCreating for schema. Add to slnx.

- [ ] T019 [P] Write SqlServerFixtureTests.cs (3 tests)
      File: `tests/Rig.TUnit.SqlServer.Tests.Integration/Fixtures/SqlServerFixtureTests.cs`
      Tests: InitializeAsync_StartsContainer, ConnectionString_AfterInit_IsValid, DisposeAsync_StopsContainer
      Notes: Use `[ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]` for fixture sharing (FR-020).

- [ ] T020 [P] Write DbContextHelperTests.cs (4 tests)
      File: `tests/Rig.TUnit.SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs`
      Tests: InsertAsync_ThenQuery_ReturnsEntity, InsertAsync_ClearsChangeTracker, Query_ExecutesInFreshScope, InsertAsync_ThenQuery_UseSeparateScopes

- [ ] T021 [P] Write SqlServerContainerExtensionsTests.cs (2 tests)
      File: `tests/Rig.TUnit.SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs`
      Tests: UseSqlServerContainerIsolated_CreatesUniqueDatabase, UseSqlServerContainerIsolated_TwoCalls_ProduceIsolatedDatabases

### GREEN: Integration Tests

- [ ] T022 [depends: T019, T020, T021] Implement SqlServerFixture, DbContextHelper, SqlServerContainerExtensions and verify integration tests pass
      Files: `src/Rig.TUnit.SqlServer/Fixtures/SqlServerFixture.cs`, `src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs`, `src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs`
      Notes: BuildServiceProvider() in SqlServerContainerExtensions is intentional — do not refactor. Verify: `dotnet test tests/Rig.TUnit.SqlServer.Tests.Integration` — 9 tests pass (Docker required).

## Phase 4: Rig.TUnit.Redis — TDD

### RED: Stubs + Integration Tests

- [ ] T023 Create Rig.TUnit.Redis csproj with stubs + Redis.Tests.Integration csproj
      Files: `src/Rig.TUnit.Redis/Rig.TUnit.Redis.csproj`, `src/Rig.TUnit.Redis/Fixtures/RedisFixture.cs`, `src/Rig.TUnit.Redis/Extensions/RedisContainerExtensions.cs`, `tests/Rig.TUnit.Redis.Tests.Integration/Rig.TUnit.Redis.Tests.Integration.csproj`
      Notes: Source NuGet: Testcontainers.Redis 4.6.0, StackExchange.Redis 2.8.16, ProjectRef to Core. Test NuGet: TUnit 1.33.0, ProjectRef to Redis. Add both to slnx.

- [ ] T024 [P] Write RedisFixtureTests.cs (3 tests)
      File: `tests/Rig.TUnit.Redis.Tests.Integration/Fixtures/RedisFixtureTests.cs`
      Tests: InitializeAsync_StartsContainer, ConnectionString_AfterInit_IsValid, DisposeAsync_StopsContainer
      Notes: Use `[ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]` for fixture sharing (FR-020).

- [ ] T025 [P] Write RedisContainerExtensionsTests.cs (2 tests)
      File: `tests/Rig.TUnit.Redis.Tests.Integration/Extensions/RedisContainerExtensionsTests.cs`
      Tests: UseRedisContainer_ReplacesMultiplexer, UseRedisContainer_CanSetAndGetKeys

### GREEN: Implement + Verify

- [ ] T026 [depends: T024, T025] Implement Redis source and verify integration tests pass
      Files: `src/Rig.TUnit.Redis/Fixtures/RedisFixture.cs`, `src/Rig.TUnit.Redis/Extensions/RedisContainerExtensions.cs`
      Notes: RedisContainerExtensions: RemoveService<IConnectionMultiplexer>, add singleton connected to fixture.ConnectionString. Verify: `dotnet test tests/Rig.TUnit.Redis.Tests.Integration` — 5 tests pass (Docker required).

## Phase 5: Rig.TUnit.ServiceBus — TDD

### RED: Stubs + Integration Tests

- [ ] T027 Create Rig.TUnit.ServiceBus csproj with stubs + ServiceBus.Tests.Integration csproj + config
      Files: `src/Rig.TUnit.ServiceBus/Rig.TUnit.ServiceBus.csproj`, `src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs`, `src/Rig.TUnit.ServiceBus/Helpers/ListenerHelper.cs`, `src/Rig.TUnit.ServiceBus/Helpers/ServiceBusEventSender.cs`, `src/Rig.TUnit.ServiceBus/Extensions/ServiceBusContainerExtensions.cs`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/Rig.TUnit.ServiceBus.Tests.Integration.csproj`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/TestInfrastructure/service-bus-config.json`
      Notes: Source NuGet: Testcontainers.ServiceBus 4.6.0, Azure.Messaging.ServiceBus 7.18.2, Newtonsoft.Json 13.0.3, ProjectRef to Core. Test csproj: CopyToOutputDirectory=PreserveNewest for service-bus-config.json. Add both to slnx.

- [ ] T028 [P] Write ServiceBusFixtureTests.cs (2 tests)
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Fixtures/ServiceBusFixtureTests.cs`
      Tests: InitializeAsync_StartsEmulator, DisposeAsync_StopsEmulator
      Notes: Use `[ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]` for fixture sharing (FR-020).

- [ ] T029 [P] Write ListenerHelperTests.cs (5 tests)
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Helpers/ListenerHelperTests.cs`
      Tests: WaitForMessages_CapturesMessage, WaitForMessages_TimeoutExceeded_ThrowsTimeoutException, WaitForMessages_ExpectedCountReached_Returns, StartAsync_ThenDispose_Lifecycle, Messages_AfterCapture_ContainsReceivedMessage

- [ ] T030 [P] Write ServiceBusEventSenderTests.cs (3 tests)
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Helpers/ServiceBusEventSenderTests.cs`
      Tests: SendAsync_PublishesReceivableMessage, SendAsync_SetsSessionId, SendAsync_SerializesAsJson

- [ ] T031 [P] Write ServiceBusContainerExtensionsTests.cs (2 tests)
      File: `tests/Rig.TUnit.ServiceBus.Tests.Integration/Extensions/ServiceBusContainerExtensionsTests.cs`
      Tests: UseServiceBusContainer_ReplacesConnectionString, UseServiceBusContainer_CanCreateClientFromReplacedConnection

### GREEN: Implement + Verify

- [ ] T032 [depends: T028, T029, T030, T031] Implement ServiceBus source and verify integration tests pass
      Files: `src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs`, `src/Rig.TUnit.ServiceBus/Helpers/ListenerHelper.cs`, `src/Rig.TUnit.ServiceBus/Helpers/ServiceBusEventSender.cs`, `src/Rig.TUnit.ServiceBus/Extensions/ServiceBusContainerExtensions.cs`
      Notes: ListenerHelper.WaitForMessagesAsync MUST throw TimeoutException (C-008). ListenerHelper MUST use `ConcurrentBag<ServiceBusReceivedMessage>` instead of `List<>` — MaxConcurrentSessions=100 means OnMessage is called from multiple threads. ServiceBusContainerExtensions removes existing ServiceBusClient and re-registers with fixture connection string. ServiceBusEventSender serializes with Newtonsoft.Json, implements IAsyncDisposable. Verify: `dotnet test tests/Rig.TUnit.ServiceBus.Tests.Integration` — 12 tests pass (Docker required).

## Phase 6: Meta-Package + Benchmarks + Final Verification

- [ ] T033 Create Rig.TUnit meta-package csproj
      File: `src/Rig.TUnit/Rig.TUnit.csproj`
      Notes: ProjectReferences to all 5 source projects, no .cs files. Add to slnx.

- [ ] T034 Create Rig.TUnit.Benchmarks csproj + Program.cs
      Files: `tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj`, `tests/Rig.TUnit.Benchmarks/Program.cs`
      Notes: OutputType=Exe (NOT a test project). NuGet: BenchmarkDotNet. ProjectRefs to Core, Grpc, SqlServer. Program.cs = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args). Add to slnx.

- [ ] T035 [P] Write CoreBenchmarks.cs, SqlServerBenchmarks.cs, GrpcBenchmarks.cs
      Files: `tests/Rig.TUnit.Benchmarks/CoreBenchmarks.cs`, `tests/Rig.TUnit.Benchmarks/SqlServerBenchmarks.cs`, `tests/Rig.TUnit.Benchmarks/GrpcBenchmarks.cs`
      Notes: All classes MUST have [MemoryDiagnoser] (SC-016). CoreBenchmarks: faker creation, service removal at 10/100/1000 registrations. SqlServerBenchmarks: DI scope creation overhead. GrpcBenchmarks: channel/client creation.

- [ ] T036 Finalize Rig.TUnit.slnx with all 13 projects
      File: `Rig.TUnit.slnx`
      Notes: Verify all 13 projects present: 6 under /src/, 7 under /tests/. Ensure XML structure is correct.

- [ ] T037 [depends: T001-T036] Final verification against all success criteria
      Verify:
        1. `dotnet build Rig.TUnit.slnx` — zero errors, zero warnings (SC-001)
        2. `dotnet test Rig.TUnit.slnx --filter "Tests.Unit"` — all unit tests pass, no Docker (SC-008, SC-010)
        3. `dotnet test Rig.TUnit.slnx --filter "Tests.Integration"` — all integration tests pass, Docker required (SC-011)
        4. `dotnet run --project tests/Rig.TUnit.Benchmarks/ -- --list flat` — benchmarks discoverable (SC-015)
        5. Verify namespace matching (SC-004), no service-specific types in source (SC-005), fixtures are sealed (SC-006)
