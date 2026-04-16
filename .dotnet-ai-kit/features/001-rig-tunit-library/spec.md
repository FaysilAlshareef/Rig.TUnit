# Feature Specification: Rig.TUnit Testing Infrastructure Library

**Feature ID**: 001-rig-tunit-library
**Created**: 2026-04-16
**Status**: Draft
**Input**: "Build the Rig.TUnit library from scratch — a standalone .NET testing infrastructure library built on TUnit for integration testing gRPC microservices"

## User Stories

### User Story 1 - Core Testing Utilities (Priority: P1)
As a .NET developer writing TUnit-based tests, I need reusable fakers, service removal extensions, and CI/CD detection so that I can set up test infrastructure without boilerplate.

**Acceptance Scenarios**:
1. **Given** a domain entity with private setters, **When** I use `CustomConstructorFaker<T>`, **Then** it creates an instance via `RuntimeHelpers.GetUninitializedObject` bypassing the constructor.
2. **Given** a service collection with registered services, **When** I call `RemoveService<T>()`, **Then** the registration is removed by service type.
3. **Given** a CI/CD environment with `GITHUB_ACTIONS=true`, **When** I call `EnvironmentDetection.IsRunningInCiCd()`, **Then** it returns `true`.

### User Story 2 - gRPC Testing Helpers (Priority: P1)
As a developer testing gRPC microservices, I need generic helpers to create typed gRPC clients, dispatch MediatR commands, build metadata, and configure `WebApplicationFactory` so that I can write integration tests without manual channel/factory setup.

**Acceptance Scenarios**:
1. **Given** a `WebApplicationFactory<TProgram>` with a mapped gRPC service, **When** I use `GrpcClientHelper<TClient, TProgram>`, **Then** I can send gRPC requests through the in-memory test server.
2. **Given** a `WebApplicationFactory` with MediatR registered, **When** I use `HandlerHelper.Send<TResult>()`, **Then** the request is dispatched via MediatR within a scoped lifetime.
3. **Given** a factory, **When** I call `.WithTestConfiguration()`, **Then** Serilog Console logging is configured and optional service/endpoint overrides are applied.
4. **Given** a test needing gRPC metadata, **When** I use `MetadataHelper`, **Then** it builds properly formatted `access-claims-bin` headers.

### User Story 3 - SQL Server Container and Database Helpers (Priority: P1)
As a developer running integration tests, I need a SQL Server container fixture and generic `DbContextHelper<TContext>` so that I can run tests against real databases or in-memory databases with isolation.

**Acceptance Scenarios**:
1. **Given** a test session, **When** `SqlServerFixture` initializes via `IAsyncInitializer`, **Then** a SQL Server 2022 container starts and exposes a connection string.
2. **Given** a `DbContextHelper<TContext>`, **When** I call `Query()`, **Then** it executes within a fresh DI scope and returns the result.
3. **Given** a service collection, **When** I call `UseInMemoryDatabase<TContext>()`, **Then** the DbContext is replaced with an in-memory provider using a unique GUID name.
4. **Given** a running `SqlServerFixture`, **When** I call `UseSqlServerContainerIsolated<TContext>(fixture)`, **Then** a unique database is created on the shared container and migrations are applied.

### User Story 4 - Redis Container Fixture (Priority: P2)
As a developer testing gateway or caching services, I need a Redis container fixture so that I can test against a real Redis instance.

**Acceptance Scenarios**:
1. **Given** a test session, **When** `RedisFixture` initializes, **Then** a Redis 7 Alpine container starts and exposes a connection string.
2. **Given** a service collection and `RedisFixture`, **When** I call `RedisContainerExtensions`, **Then** the Redis connection is replaced with the container's connection string.

### User Story 5 - Service Bus Emulator Fixture and Listener (Priority: P2)
As a developer testing event-driven services, I need a Service Bus emulator fixture, a message listener, and an event sender so that I can test end-to-end messaging without Azure infrastructure.

**Acceptance Scenarios**:
1. **Given** a `service-bus-config.json` file, **When** `ServiceBusFixture` initializes, **Then** the Service Bus emulator starts with configured topics/subscriptions.
2. **Given** a running `ListenerHelper`, **When** messages arrive on a subscription, **Then** they are captured in `Messages` and I can call `WaitForMessagesAsync(expectedCount, timeout)` to poll without `Task.Delay`.
3. **Given** a `ServiceBusEventSender`, **When** I call it with event data, **Then** the message is published to the configured topic.

### User Story 6 - Meta-Package (Priority: P3)
As a developer who needs all Rig.TUnit capabilities, I can install the `Rig.TUnit` meta-package to get all sub-packages in one reference.

**Acceptance Scenarios**:
1. **Given** a test project, **When** I reference `Rig.TUnit`, **Then** all five sub-packages (Core, Grpc, SqlServer, Redis, ServiceBus) are transitively available.

### User Story 7 - Unit Tests (Priority: P1)
As a library maintainer, I need unit tests for all non-container logic so that I can verify correctness without Docker dependencies.

**Acceptance Scenarios**:
1. **Given** `CustomConstructorFaker<T>`, **When** tested with a class having private setters, **Then** the faker creates a valid instance bypassing the constructor.
2. **Given** `CustomConstructorFaker<T>`, **When** tested with Bogus property rules, **Then** rules are applied to the uninitialized instance.
3. **Given** `ServiceRemovalExtensions.RemoveService<T>()`, **When** the service type exists in the collection, **Then** the registration is removed and the collection is returned for chaining.
4. **Given** `ServiceRemovalExtensions.RemoveService<T>()`, **When** the service type does NOT exist, **Then** the collection is returned unchanged (no exception).
5. **Given** `ServiceRemovalExtensions.RemoveImplementation<T>()`, **When** tested with a registered implementation, **Then** only that specific implementation descriptor is removed.
6. **Given** `ServiceRemovalExtensions.RemoveByName()`, **When** multiple services match the name substring, **Then** all matching registrations are removed.
7. **Given** `EnvironmentDetection.IsRunningInCiCd()`, **When** a known CI variable (`GITHUB_ACTIONS`, `CI`, `TF_BUILD`, etc.) is set, **Then** it returns `true`.
8. **Given** `EnvironmentDetection.IsRunningInCiCd()`, **When** no CI variables are set, **Then** it returns `false`.
9. **Given** `MetadataHelper`, **When** called with a claims dictionary, **Then** it produces gRPC `Metadata` with the correct `access-claims-bin` key and protobuf-serialized binary value.
10. **Given** `MetadataHelper`, **When** called with an empty dictionary, **Then** it returns valid `Metadata` with an empty claims payload.
11. **Given** `InMemoryDbExtensions.UseInMemoryDatabase<TContext>()`, **When** called twice, **Then** each call produces a DbContext backed by a different in-memory database (unique GUID names).
12. **Given** `InMemoryDbExtensions.UseInMemoryDatabase<TContext>()`, **When** called on a service collection with an existing DbContext registration, **Then** the old registration is replaced.

### User Story 8 - Integration Tests (Priority: P1)
As a library maintainer, I need integration tests that run against real containers (SQL Server, Redis, Service Bus emulator) so that I can verify container fixtures, database helpers, and messaging helpers work end-to-end.

**Acceptance Scenarios**:
1. **Given** `SqlServerFixture`, **When** `InitializeAsync()` completes, **Then** a SQL Server 2022 container is running and `ConnectionString` returns a valid connection string.
2. **Given** `SqlServerFixture`, **When** `DisposeAsync()` completes, **Then** the container is stopped and cleaned up.
3. **Given** `DbContextHelper<TContext>` with a real SQL Server container, **When** `InsertAsync()` is called followed by `Query()`, **Then** the inserted entity is retrievable and each operation runs in a separate DI scope.
4. **Given** `DbContextHelper<TContext>`, **When** `InsertAsync()` completes, **Then** the change tracker is cleared (no tracked entities remain).
5. **Given** `UseSqlServerContainerIsolated<TContext>()` called twice on the same fixture, **When** entities are inserted in each, **Then** each database is fully isolated — data in one is not visible from the other.
6. **Given** `RedisFixture`, **When** `InitializeAsync()` completes, **Then** a Redis 7 container is running and `ConnectionString` connects successfully.
7. **Given** `RedisContainerExtensions`, **When** applied to a service collection with a `RedisFixture`, **Then** the Redis connection resolves to the container instance.
8. **Given** `ServiceBusFixture` with a valid `service-bus-config.json`, **When** `InitializeAsync()` completes, **Then** the emulator is running with configured topics and subscriptions.
9. **Given** `ListenerHelper` connected to a running emulator, **When** a message is published, **Then** `WaitForMessagesAsync()` returns after the message is captured and `Messages` contains the message.
10. **Given** `ListenerHelper.WaitForMessagesAsync(expectedCount: 3, timeout: 10s)`, **When** only 2 messages arrive within the timeout, **Then** the method throws `TimeoutException` with a message indicating expected vs actual count.
11. **Given** `ServiceBusEventSender`, **When** called with event data, **Then** the message is published to the correct topic and is receivable by a `ListenerHelper`.
12. **Given** `GrpcClientHelper<TClient, TProgram>` with a `WebApplicationFactory` hosting a test gRPC service, **When** `Send()` is called, **Then** the request is routed through the in-memory test server and the response is returned.
13. **Given** `HandlerHelper` with MediatR registered, **When** `Send<TResult>()` is called, **Then** the request is dispatched via MediatR within a scoped lifetime and the scope is disposed afterward.
14. **Given** `WebApplicationFactoryExtensions.WithTestConfiguration()`, **When** `configureServices` and `mapEndpoints` callbacks are provided, **Then** the factory is configured with Serilog console logging, service overrides, and endpoint mapping.
15. **Given** `GrpcServiceReplacementExtensions.ReplaceGrpcClient<TClient, TProgram>()`, **When** the service under test resolves the replaced client, **Then** it routes through the fake gRPC server hosted in the test.

### User Story 9 - Benchmark Tests (Priority: P3)
As a library maintainer, I need benchmark tests for performance-sensitive operations so that I can track allocation and throughput regressions across releases.

**Acceptance Scenarios**:
1. **Given** `CustomConstructorFaker<T>`, **When** benchmarked for object creation, **Then** allocation and throughput baselines are recorded.
2. **Given** `DbContextHelper<TContext>`, **When** benchmarked for scope creation overhead, **Then** per-operation DI scope cost is measurable.
3. **Given** `GrpcClientHelper<TClient, TProgram>`, **When** benchmarked for channel creation, **Then** the factory-to-channel pipeline overhead is measurable.
4. **Given** `ServiceRemovalExtensions`, **When** benchmarked on collections of varying sizes (10, 100, 1000 registrations), **Then** linear scan performance is documented.

## Requirements

### Functional Requirements

- **FR-001**: All projects MUST target `net10.0`
- **FR-002**: All projects MUST use TUnit 1.33.0+ as the test framework — no xUnit, no `Microsoft.NET.Test.Sdk`, no `coverlet.collector`
- **FR-003**: All types MUST be generic — no references to `Program`, `ApplicationDbContext`, or any service-specific types
- **FR-004**: Container fixtures MUST implement `IAsyncInitializer` + `IAsyncDisposable` — no abstract base classes, no factory pattern
- **FR-005**: Logging MUST use `Serilog.WriteTo.Console()` — not `Serilog.Sinks.XUnit` (incompatible with TUnit)
- **FR-006**: Solution MUST use XML-based `.slnx` format (`Rig.TUnit.slnx`)
- **FR-007**: Namespaces MUST match folder structure (e.g., `Rig.TUnit.Core.Fakers`, `Rig.TUnit.Grpc.Helpers`)
- **FR-008**: NuGet package versions MUST be exact as specified in the handoff document
- **FR-009**: `DbContextHelper<TContext>` MUST create a fresh DI scope per operation to prevent cross-test DbContext sharing
- **FR-010**: `ListenerHelper.WaitForMessagesAsync()` MUST poll every 250ms using `Task.Delay(250, ct)` — no arbitrary long waits like `Task.Delay(10000)` or `Task.Delay(20000)`
- **FR-011**: `UseInMemoryDatabase<TContext>()` MUST use a unique `Guid` name per call for parallel isolation
- **FR-012**: `UseSqlServerContainerIsolated<TContext>()` MUST create a unique database name on the shared container

### Testing Requirements

- **FR-013**: Development MUST follow TDD — write failing tests first, then implement production code to make them pass, then refactor
- **FR-014**: Every public type and public method in the library MUST have corresponding test coverage
- **FR-015**: Unit test projects (`*.Tests.Unit`) MUST NOT require Docker or any external infrastructure — use in-memory fakes, mocks, service collections, and `WebApplicationFactory` (in-memory server) only
- **FR-016**: Integration test projects (`*.Tests.Integration`) MUST use real containers via Testcontainers — no in-memory substitutes for container-dependent behavior
- **FR-023**: Unit and integration tests MUST be in separate projects — `*.Tests.Unit` for no-Docker tests, `*.Tests.Integration` for container-dependent tests. Packages with only one type get just the applicable suffix.
- **FR-017**: Benchmark project MUST use `BenchmarkDotNet` with `[MemoryDiagnoser]` for allocation tracking
- **FR-018**: All test projects MUST use TUnit as the test framework — no xUnit, NUnit, or MSTest
- **FR-019**: Test method naming MUST follow `{Method}_{Scenario}_{ExpectedResult}` convention (e.g., `RemoveService_WhenServiceExists_RemovesRegistration`)
- **FR-020**: Integration tests requiring containers MUST use `[ClassDataSource<TFixture>(Shared = SharedType.PerTestSession)]` for fixture sharing — one container per test session, not per test
- **FR-021**: Each test project MUST include test infrastructure (test DbContext, test gRPC service, test entities) as internal types within the test project — not shared across test projects
- **FR-022**: Tests MUST use Arrange-Act-Assert structure with clear visual separation (blank lines between sections)

### Key Entities

- **CustomConstructorFaker\<T\>**: Bogus faker that bypasses constructors via `RuntimeHelpers.GetUninitializedObject` for domain objects with private setters
- **GrpcClientHelper\<TClient, TProgram\>**: Creates typed gRPC clients routed through in-memory test server (generic over both client and program entry point)
- **HandlerHelper**: Dispatches events/commands via MediatR within scoped lifetimes (accepts `IServiceProvider`, not `WebApplicationFactory`)
- **MetadataHelper**: Builds gRPC `Metadata` with `access-claims-bin` headers — accepts `Dictionary<string, string>` of claims, serializes to protobuf binary format
- **DbContextHelper\<TContext\>**: Provides scoped database operations (Query, InsertAsync) for test assertions and seeding
- **SqlServerFixture**: TUnit `IAsyncInitializer` fixture wrapping `MsSqlContainer` (SQL Server 2022)
- **RedisFixture**: TUnit `IAsyncInitializer` fixture wrapping `RedisContainer` (Redis 7 Alpine)
- **ServiceBusFixture**: TUnit `IAsyncInitializer` fixture wrapping `ServiceBusContainer` (Azure emulator)
- **ListenerHelper**: Captures Service Bus messages via session processor with polling-based wait
- **ServiceBusContainerExtensions**: Replaces Service Bus connection string in the service collection — removes existing `ServiceBusClient` registration and re-registers it with the fixture's emulator connection string
- **ServiceBusEventSender**: Publishes events to Service Bus topics for testing query/processor handlers

## Architecture Scope

This is a **standalone library** (not a microservice). Architecture is modular packages with a dependency graph:

### Package Dependency Graph

```
Rig.TUnit.Core (TUnit.Core, Bogus)
    ^
    |--- Rig.TUnit.Grpc (+ TUnit.AspNetCore, Grpc.AspNetCore, Grpc.Net.Client, MediatR, Serilog)
    |--- Rig.TUnit.SqlServer (+ Testcontainers.MsSql, EF Core SqlServer, EF Core InMemory)
    |--- Rig.TUnit.Redis (+ Testcontainers.Redis, StackExchange.Redis)
    |--- Rig.TUnit.ServiceBus (+ Testcontainers.ServiceBus, Azure.Messaging.ServiceBus, Newtonsoft.Json)
         ^
         |--- Rig.TUnit (meta-package referencing all above)
```

### Test Project Dependency Graph

```
Unit tests (no Docker required):
  Rig.TUnit.Core.Tests.Unit ---------> Rig.TUnit.Core
  Rig.TUnit.Grpc.Tests.Unit ---------> Rig.TUnit.Grpc (+ test gRPC service, test MediatR handler)
  Rig.TUnit.SqlServer.Tests.Unit ----> Rig.TUnit.SqlServer (InMemoryDb tests only)

Integration tests (Docker required):
  Rig.TUnit.SqlServer.Tests.Integration -> Rig.TUnit.SqlServer (real SQL Server 2022 container)
  Rig.TUnit.Redis.Tests.Integration ----> Rig.TUnit.Redis (real Redis 7 container)
  Rig.TUnit.ServiceBus.Tests.Integration -> Rig.TUnit.ServiceBus (real Service Bus emulator)

Benchmarks:
  Rig.TUnit.Benchmarks -------> Rig.TUnit.Core + Rig.TUnit.Grpc + Rig.TUnit.SqlServer (BenchmarkDotNet)
```

### Source Projects and Files

| Project | Files |
|---------|-------|
| `Rig.TUnit.Core` | `Fakers/CustomConstructorFaker.cs`, `Extensions/ServiceRemovalExtensions.cs`, `Extensions/EnvironmentDetection.cs` |
| `Rig.TUnit.Grpc` | `Helpers/GrpcClientHelper.cs`, `Helpers/HandlerHelper.cs`, `Helpers/MetadataHelper.cs`, `Extensions/WebApplicationFactoryExtensions.cs`, `Extensions/GrpcServiceReplacementExtensions.cs` |
| `Rig.TUnit.SqlServer` | `Fixtures/SqlServerFixture.cs`, `Helpers/DbContextHelper.cs`, `Extensions/InMemoryDbExtensions.cs`, `Extensions/SqlServerContainerExtensions.cs` |
| `Rig.TUnit.Redis` | `Fixtures/RedisFixture.cs`, `Extensions/RedisContainerExtensions.cs` |
| `Rig.TUnit.ServiceBus` | `Fixtures/ServiceBusFixture.cs`, `Helpers/ListenerHelper.cs`, `Helpers/ServiceBusEventSender.cs`, `Extensions/ServiceBusContainerExtensions.cs` |
| `Rig.TUnit` | `Rig.TUnit.csproj` (meta-package only) |

### Test Projects and Files

**Unit Test Projects (no Docker):**

| Project | Location | Files | Infrastructure |
|---------|----------|-------|----------------|
| `Rig.TUnit.Core.Tests.Unit` | `tests/` | `Fakers/CustomConstructorFakerTests.cs`, `Extensions/ServiceRemovalExtensionsTests.cs`, `Extensions/EnvironmentDetectionTests.cs` | None (pure unit tests) |
| `Rig.TUnit.Grpc.Tests.Unit` | `tests/` | `Helpers/GrpcClientHelperTests.cs`, `Helpers/HandlerHelperTests.cs`, `Helpers/MetadataHelperTests.cs`, `Extensions/WebApplicationFactoryExtensionsTests.cs`, `Extensions/GrpcServiceReplacementExtensionsTests.cs`, `TestInfrastructure/TestGrpcService.cs`, `TestInfrastructure/TestProgram.cs`, `TestInfrastructure/TestRequest.cs`, `TestInfrastructure/TestRequestHandler.cs` | Test gRPC service, real MediatR pipeline with test handler (no mocking), minimal Program entry point (in-memory server, no Docker) |
| `Rig.TUnit.SqlServer.Tests.Unit` | `tests/` | `Extensions/InMemoryDbExtensionsTests.cs`, `TestInfrastructure/TestDbContext.cs`, `TestInfrastructure/TestEntity.cs` | Test DbContext + entity (InMemory provider, no Docker) |

**Integration Test Projects (Docker required):**

| Project | Location | Files | Infrastructure |
|---------|----------|-------|----------------|
| `Rig.TUnit.SqlServer.Tests.Integration` | `tests/` | `Fixtures/SqlServerFixtureTests.cs`, `Helpers/DbContextHelperTests.cs`, `Extensions/SqlServerContainerExtensionsTests.cs`, `TestInfrastructure/TestDbContext.cs`, `TestInfrastructure/TestEntity.cs` | Docker (SQL Server 2022), test DbContext + entity |
| `Rig.TUnit.Redis.Tests.Integration` | `tests/` | `Fixtures/RedisFixtureTests.cs`, `Extensions/RedisContainerExtensionsTests.cs` | Docker (Redis 7) |
| `Rig.TUnit.ServiceBus.Tests.Integration` | `tests/` | `Fixtures/ServiceBusFixtureTests.cs`, `Helpers/ListenerHelperTests.cs`, `Helpers/ServiceBusEventSenderTests.cs`, `Extensions/ServiceBusContainerExtensionsTests.cs`, `TestInfrastructure/service-bus-config.json` | Docker (Service Bus emulator) |

**Benchmarks:**

| Project | Location | Files | Infrastructure |
|---------|----------|-------|----------------|
| `Rig.TUnit.Benchmarks` | `tests/` | `CoreBenchmarks.cs`, `SqlServerBenchmarks.cs`, `GrpcBenchmarks.cs` | BenchmarkDotNet |

### Test NuGet Dependencies

| Test Project | Additional NuGet Packages |
|-------------|--------------------------|
| All unit + integration test projects | `TUnit` (test runner + assertions) |
| `Rig.TUnit.Core.Tests.Unit` | `Microsoft.Extensions.DependencyInjection` |
| `Rig.TUnit.Grpc.Tests.Unit` | `Google.Protobuf` (for claims serialization verification) — uses real MediatR pipeline with test handlers, no mocking framework |
| `Rig.TUnit.SqlServer.Tests.Unit` | (transitive from SqlServer project) |
| `Rig.TUnit.SqlServer.Tests.Integration` | (transitive from SqlServer project) |
| `Rig.TUnit.Redis.Tests.Integration` | (transitive from Redis project) |
| `Rig.TUnit.ServiceBus.Tests.Integration` | (transitive from ServiceBus project) |
| `Rig.TUnit.Benchmarks` | `BenchmarkDotNet` |

### Solution File

`Rig.TUnit.slnx` — XML-based slnx format containing all 13 projects: 6 under `src/`, 7 under `tests/`.

## Edge Cases

- **Parallel test isolation**: `UseInMemoryDatabase` and `UseSqlServerContainerIsolated` each generate unique names to prevent cross-test interference
- **Container startup failure**: Fixtures throw on `InitializeAsync` — TUnit surfaces the error per test session
- **Missing `service-bus-config.json`**: `ServiceBusFixture` will fail with a clear error if the config file is not found at the specified path
- **`DbContext` scope leaks**: `DbContextHelper` creates and disposes a fresh scope per operation to prevent change tracker pollution
- **TUnit assertion awaiting**: All `Assert.That()` calls must be `await`ed — forgetting `await` causes silent passes (documented in design)
- **`Serilog.Sinks.XUnit` incompatibility**: Must use `WriteTo.Console()` — TUnit captures stdout per test, not `ITestOutputHelper`
- **`ListenerHelper` thread safety**: `_messages` collection MUST use `ConcurrentBag<ServiceBusReceivedMessage>` (not `List<>`) because `MaxConcurrentSessions=100` means `OnMessage` is invoked from multiple threads concurrently

## Constraints (DO NOT)

- Do NOT add README or docs files
- Do NOT add NuGet packaging config yet
- Do NOT add CI/CD workflows yet
- Do NOT create abstract base classes for fixtures — keep them simple sealed classes
- Do NOT add classes not listed in the handoff doc (`FakeServiceBusPublisher`, `FakeGrpcServiceBase`, `AssertionExtensions`, `CacheHelper`, `MessageAssertBase` are in the design but NOT in the handoff file list) — exception: test infrastructure types (test DbContext, test entities, test gRPC services) are allowed inside test projects only
- Do NOT deviate from the exact file paths in the handoff doc for source projects
- Do NOT write production code before its corresponding test is written and failing (TDD red-green-refactor)
- Do NOT use mocking frameworks for container-based tests — integration tests MUST use real containers
- Do NOT share test infrastructure types across test projects — each test project owns its own test DbContext, test entities, etc.
- Do NOT skip edge case tests — empty collections, null inputs, timeout scenarios, concurrent access, and disposal must all be covered

## Reference Documents

- **Handoff**: `planning/Rig.TUnit-Session-Handoff.md` — exact NuGet versions, file paths, and implementation scope
- **Design**: `planning/Rig.TUnit-Library-Design.md` — full API design, code examples, and architecture decisions

## Success Criteria

### Build & Structure
- **SC-001**: `dotnet build Rig.TUnit.slnx` completes with zero errors and zero warnings
- **SC-002**: All 13 projects are present in the solution (6 source + 7 test) with correct project references
- **SC-003**: All NuGet package versions match the handoff document exactly (source projects)
- **SC-004**: All namespaces match folder structure (`Rig.TUnit.{Package}.{Folder}`)
- **SC-005**: No references to service-specific types (`Program`, `ApplicationDbContext`, etc.) in source projects — test infrastructure types are allowed in test projects only
- **SC-006**: Container fixtures are `sealed` classes implementing `IAsyncInitializer` + `IAsyncDisposable`
- **SC-007**: All source file paths match the handoff document exactly (25 files: 18 .cs + 6 .csproj + 1 .slnx)

### Unit Tests
- **SC-008**: `dotnet test` passes with zero failures for all `*.Tests.Unit` projects (Core.Tests.Unit, Grpc.Tests.Unit, SqlServer.Tests.Unit)
- **SC-009**: Every public method and public type in source projects has at least one corresponding test
- **SC-010**: Unit test projects run without Docker — all `*.Tests.Unit` projects complete with no container dependencies

### Integration Tests
- **SC-011**: `dotnet test` passes with zero failures for all `*.Tests.Integration` projects when Docker is available (SqlServer.Tests.Integration, Redis.Tests.Integration, ServiceBus.Tests.Integration)
- **SC-012**: Container fixture tests verify both `InitializeAsync` (start) and `DisposeAsync` (cleanup) lifecycle
- **SC-013**: `DbContextHelper` integration tests verify scope isolation — operations in separate calls do not share change tracker state
- **SC-014**: Parallel isolation tests verify that `UseInMemoryDatabase` (unit) and `UseSqlServerContainerIsolated` (integration) produce independently isolated databases

### Benchmark Tests
- **SC-015**: `Rig.TUnit.Benchmarks` project builds and runs with `BenchmarkDotNet` — benchmarks execute and produce a summary report
- **SC-016**: Benchmarks include `[MemoryDiagnoser]` for allocation tracking on all benchmark classes

## Clarifications

- **C-001** [Domain & Data Model]: Handoff document location → `planning/Rig.TUnit-Session-Handoff.md` and `planning/Rig.TUnit-Library-Design.md` — added Reference Documents section
- **C-002** [Domain & Data Model]: `GrpcClientHelper`, `GrpcServiceReplacementExtensions`, and `WebApplicationFactoryExtensions` must use generic `TProgram` parameter instead of concrete `Program` to satisfy FR-003. `GrpcClientHelper<TClient, TProgram>`, `ReplaceGrpcClient<TClient, TProgram>`, `WithTestConfiguration<TProgram>`
- **C-003** [Domain & Data Model]: SC-007 file count corrected from 22 to 25 (18 .cs + 6 .csproj + 1 .slnx)
- **C-004** [Edge Cases]: `MetadataHelper` accepts `Dictionary<string, string>` of claims, serializes to protobuf binary format, and sets the `access-claims-bin` gRPC metadata key
- **C-005** [Edge Cases]: `GrpcServiceReplacementExtensions.ReplaceGrpcClient<TClient, TProgram>()` replaces outgoing gRPC client dependencies so the service under test calls a fake gRPC server hosted in the test — generic over both client and program entry point
- **C-006** [Scope Change]: Removed "Do NOT add test projects" constraint. Testing is now in scope with TDD methodology. Added User Stories 7-9, FR-013 to FR-022, and SC-008 to SC-016.
- **C-007** [Domain & Data Model]: Test projects split into separate `*.Tests.Unit` (no Docker) and `*.Tests.Integration` (Docker required) projects. FR-016 naming corrected. FR-023 added. Solution expanded from 6 to 13 projects (6 source + 3 unit + 3 integration + 1 benchmark).
- **C-008** [Edge Cases]: `ListenerHelper.WaitForMessagesAsync()` throws `TimeoutException` when expected message count is not reached within the timeout — fail-fast behavior, no silent pass.
- **C-009** [Edge Cases]: Benchmarks include container benchmarks (fixture startup, real DB operations). Requires Docker. SqlServer dependency kept.
- **C-010** [Domain & Data Model]: Grpc.Tests.Unit uses real MediatR pipeline with test handlers instead of NSubstitute mocking. NSubstitute dependency removed. Test infrastructure includes `TestRequestHandler.cs`.
