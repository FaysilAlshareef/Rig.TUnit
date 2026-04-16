# Feature Specification: Rig.TUnit Fluent Builder Expansion

**Feature ID**: 002-rig-tunit-fluent-builder-expansion
**Created**: 2026-04-16
**Status**: Draft
**Input**: "Expand Rig.TUnit with fluent builder API, new Mediator and WebAPI packages, core utilities, and TDD-first development"

## User Stories

### User Story 1 - Fluent Builder API (Priority: P1)
As a .NET developer configuring test infrastructure, I need a fluent builder entry point (`services.AddRigTUnit(rig => ...)`) so that I can configure all test infrastructure (SqlServer, Redis, ServiceBus, Grpc, WebAPI) through a single chained API instead of scattered extension methods.

**Acceptance Scenarios**:
1. **Given** a `ServiceCollection`, **When** I call `services.AddRigTUnit(rig => rig.UseSqlServer(...).UseRedis(...).UseServiceBus(...))`, **Then** all infrastructure is configured through the fluent chain and the `IServiceCollection` is returned for further chaining.
2. **Given** a `RigBuilder`, **When** I call `.ForceContainersInCi()`, **Then** the builder records that CI/CD mode requires container fixtures.
3. **Given** a `RigBuilder` with `UseSqlServer(source, sql => ...)`, **When** I call `sql.ReplaceDbContext<TContext>()` multiple times, **Then** each DbContext type is replaced with an isolated test database on the container.
4. **Given** a `RigBuilder` with `UseRedis(source)`, **When** the builder executes, **Then** `IConnectionMultiplexer` is replaced with the test connection.
5. **Given** a `RigBuilder` with `UseServiceBus(source, sb => ...)`, **When** I call `sb.ReplaceClient()` and `sb.ReplaceClient<TCustomWrapper>(factory)`, **Then** both the base `ServiceBusClient` and the custom wrapper are replaced.
6. **Given** a `RigBuilder` with `UseGrpc<TProgram>(factory, grpc => ...)`, **When** I call `grpc.ReplaceClient<TClient>()`, **Then** the gRPC client routes through the in-memory test server.
7. **Given** a `RigBuilder` with `UseWebApi<TProgram>(factory, web => ...)`, **When** I call `web.AddHttpClientHelper()`, **Then** `HttpClientHelper<TProgram>` is registered in the service collection.

### User Story 2 - Dual-Mode Connection Sources (Priority: P1)
As a developer running tests locally and in CI/CD, I need connection sources that work with both Testcontainers and external services so that tests run against containers in CI and local services during development.

**Acceptance Scenarios**:
1. **Given** a container fixture implementing `IRigConnectionSource`, **When** I call `RigConnect.FromContainer(fixture)`, **Then** it returns the fixture itself as the connection source.
2. **Given** an `IConfiguration` with key `"ConnectionStrings:OrderDb"`, **When** I call `RigConnect.FromConfig(config, "ConnectionStrings:OrderDb")`, **Then** it returns a source that reads the value from configuration.
3. **Given** an `IOptions<T>` instance, **When** I call `RigConnect.FromOptions(options, o => o.ConnectionString)`, **Then** it returns a source that reads the value from the options selector.
4. **Given** a raw string `"Server=localhost;..."`, **When** I call `RigConnect.FromValue(connectionString)`, **Then** it returns a source wrapping that value.
5. **Given** a CI environment (`EnvironmentDetection.IsRunningInCiCd()` returns true), **When** I call `RigConnect.Auto(fixture, config, key)`, **Then** it returns the fixture's connection string (container mode).
6. **Given** a local environment with config key present, **When** I call `RigConnect.Auto(fixture, config, key)`, **Then** it returns the config value (external service mode).
7. **Given** a local environment with config key missing, **When** I call `RigConnect.Auto(fixture, config, key)`, **Then** it falls back to the fixture's connection string.
8. **Given** `RigConnect.FromConfig` with a missing key, **When** `ConnectionString` is accessed, **Then** it throws `InvalidOperationException` with the key name in the message.
9. **Given** `RigConnect.FromOptions` where the selector returns null, **When** `ConnectionString` is accessed, **Then** it throws `InvalidOperationException`.
10. **Given** `RigConnect.FromValue(null)`, **When** called, **Then** it throws `ArgumentNullException`.

### User Story 3 - Mediator Package (Priority: P1)
As a developer testing services that use martinothamar/Mediator, I need a `HandlerHelper` that dispatches `IRequest<T>`, `ICommand<T>`, `IQuery<T>`, and `INotification` within isolated DI scopes so that I can test handlers without gRPC dependency.

**Acceptance Scenarios**:
1. **Given** a `HandlerHelper` with Mediator registered, **When** I call `Send(IRequest<T>)`, **Then** the request is dispatched via `IMediator` in an isolated scope and the scope is disposed afterward.
2. **Given** a `HandlerHelper`, **When** I call `Send(ICommand<T>)`, **Then** the command is dispatched and returns `ValueTask<T>`.
3. **Given** a `HandlerHelper`, **When** I call `Send(IQuery<T>)`, **Then** the query is dispatched and returns `ValueTask<T>`.
4. **Given** a `HandlerHelper`, **When** I call `Publish(INotification)`, **Then** all notification handlers are invoked within an isolated scope.
5. **Given** a `CancellationToken`, **When** passed to any `HandlerHelper` method, **Then** the token is forwarded to the mediator pipeline.

### User Story 4 - WebAPI Testing Package (Priority: P1)
As a developer testing REST/HTTP APIs without gRPC, I need `HttpClientHelper<TProgram>` and `WebApiFactoryExtensions` so that I can send typed HTTP requests through an in-memory test server.

**Acceptance Scenarios**:
1. **Given** an `HttpClientHelper<TProgram>`, **When** I call `GetAsync<TResponse>("/api/orders")`, **Then** it sends a GET request through the test server and deserializes the JSON response.
2. **Given** an `HttpClientHelper<TProgram>`, **When** I call `PostAsync<TRequest, TResponse>("/api/orders", body)`, **Then** it sends a POST request with JSON body and deserializes the response.
3. **Given** an `HttpClientHelper<TProgram>`, **When** I call `PutAsync<TRequest>("/api/orders/1", body)`, **Then** it sends a PUT request with JSON body.
4. **Given** an `HttpClientHelper<TProgram>`, **When** I call `DeleteAsync("/api/orders/1")`, **Then** it sends a DELETE request.
5. **Given** a `WebApplicationFactory<TProgram>`, **When** I call `.WithTestServices(configureServices, configuration)`, **Then** the factory is configured with test services and optional in-memory configuration.
6. **Given** an `HttpClientHelper<TProgram>`, **When** `DisposeAsync()` is called, **Then** the internal `HttpClient` is disposed.
7. **Given** an `HttpClientHelper<TProgram>`, **When** I call `.WithBearerToken("jwt")`, **Then** the default `Authorization` header is set to `Bearer jwt` and the helper is returned for chaining. Passing `null` clears the header.
8. **Given** an `HttpClientHelper<TProgram>`, **When** I call `.WithHeader("X-Trace", "value")`, **Then** the header is set (overwriting any prior value) and the helper is returned for chaining. Null or empty header name throws `ArgumentException`.

### User Story 4b - Test Authentication & Authorization (Priority: P1)
As a developer testing endpoints that require authentication/authorization, I need a `TestAuthenticationHandler` (with `TestAuthenticationOptions`) and `WithTestAuthentication` / `WithPermissiveAuthorization` extensions on `WebApplicationFactory<TProgram>` so that I can exercise protected endpoints without running a real identity provider.

**Acceptance Scenarios**:
1. **Given** a `WebApplicationFactory<TProgram>`, **When** I call `.WithTestAuthentication()`, **Then** a `"Test"` authentication scheme is registered and made the default authenticate/challenge scheme.
2. **Given** `WithTestAuthentication(options => options.Claims.Add(...))`, **When** a request is sent, **Then** the test handler produces a `ClaimsPrincipal` populated with the configured claims.
3. **Given** `WithTestAuthentication()` with no configured claims, **When** a request is sent, **Then** the principal has a single `ClaimTypes.Name` claim equal to `TestAuthenticationOptions.DefaultUserName` (`"test-user"`).
4. **Given** a factory chained with `.WithTestAuthentication().WithPermissiveAuthorization()`, **When** an `[Authorize]`-protected endpoint is called, **Then** it returns 200.
5. **Given** a null `WebApplicationFactory<TProgram>`, **When** `WithTestAuthentication` or `WithPermissiveAuthorization` is called, **Then** `ArgumentNullException` is thrown.
6. **Given** `WithPermissiveAuthorization`, **When** applied, **Then** only `DefaultPolicy` and `FallbackPolicy` are replaced — named policies (`[Authorize(Policy="...")]`) and role requirements (`[Authorize(Roles="...")]`) are NOT bypassed.

### User Story 5 - Core Utilities (Priority: P1)
As a test infrastructure author, I need `WaitHelper`, `TestConfigurationBuilder`, `RigFixtureBase`, and `CompositeFixture` so that I can build test setups with reusable polling, configuration, and fixture composition.

**Acceptance Scenarios**:
1. **Given** `WaitHelper.WaitForAsync(condition, timeout)`, **When** the condition becomes true before timeout, **Then** the method returns successfully.
2. **Given** `WaitHelper.WaitForAsync(condition, timeout)`, **When** the timeout is exceeded, **Then** it throws `TimeoutException`.
3. **Given** `WaitHelper.WaitForAsync` with a `CancellationToken`, **When** the token is cancelled, **Then** it throws `OperationCanceledException`.
4. **Given** `WaitHelper.WaitForResultAsync<T>(producer, timeout)`, **When** the producer returns a non-null value, **Then** the value is returned.
5. **Given** `WaitHelper.WaitForResultAsync<T>(producer, timeout)`, **When** the timeout is exceeded with null results, **Then** it throws `TimeoutException`.
6. **Given** `TestConfigurationBuilder`, **When** I call `.Set("Key", "Value").SetConnectionString("Db", "conn").Build()`, **Then** it returns a valid `IConfiguration` with the expected values.
7. **Given** `TestConfigurationBuilder`, **When** I call `.SetSection("Section", dict).Build()`, **Then** all section keys are prefixed correctly.
8. **Given** `TestConfigurationBuilder`, **When** I call `.BuildOptions<T>(sectionName)`, **Then** the configuration is bound to a strongly-typed options class.
9. **Given** `TestConfigurationBuilder.Create(c => c.Set(...))`, **When** called, **Then** it returns an `IConfiguration` using the static factory shorthand.
10. **Given** a `CompositeFixture` with 3 fixtures implementing `IAsyncInitializer`, **When** `InitializeAsync()` is called, **Then** all 3 fixtures initialize in parallel (via `Task.WhenAll`).
11. **Given** a `CompositeFixture` with 3 fixtures implementing `IAsyncDisposable`, **When** `DisposeAsync()` is called, **Then** fixtures are disposed in reverse (LIFO) order.
12. **Given** a `CompositeFixture`, **When** I call `Get<SqlServerFixture>()`, **Then** it returns the fixture of that type from the composition.
13. **Given** a `CompositeFixture`, **When** I call `Get<T>()` for a type not in the composition, **Then** it throws `InvalidOperationException`.

### User Story 6 - Existing Component Enhancements (Priority: P2)
As a developer using existing Rig.TUnit fixtures and helpers, I need seed support on `DbContextHelper`, a configurable config path on `ServiceBusFixture`, and `ListenerHelper` refactored to use `WaitHelper` so that test setup is easier and implementations share code.

**Acceptance Scenarios**:
1. **Given** `DbContextHelper<TContext>`, **When** I call `SeedAsync(async ctx => { ctx.Orders.Add(...); })`, **Then** data is inserted in an isolated scope and `SaveChangesAsync` is called automatically.
2. **Given** `DbContextHelper<TContext>`, **When** I call `SeedAsync(ctx => { ctx.Orders.Add(...); })` (synchronous overload), **Then** data is inserted with automatic save.
3. **Given** `ServiceBusFixture`, **When** I set `ConfigFilePath = "custom/path.json"` before `InitializeAsync()`, **Then** the emulator uses the custom config file path.
4. **Given** `ServiceBusFixture`, **When** I don't set `ConfigFilePath`, **Then** it defaults to `"TestInfrastructure/service-bus-config.json"`.
5. **Given** `ListenerHelper.WaitForMessagesAsync()`, **When** called, **Then** it internally delegates to `WaitHelper.WaitForAsync` with the same behavior as before.

### User Story 7 - IRigConnectionSource on Existing Fixtures (Priority: P1)
As a developer using the fluent builder, I need existing fixtures (`SqlServerFixture`, `RedisFixture`, `ServiceBusFixture`) to implement `IRigConnectionSource` so that they can be passed directly to `RigConnect.FromContainer()` and builder methods.

**Acceptance Scenarios**:
1. **Given** `SqlServerFixture`, **When** it implements `IRigConnectionSource`, **Then** its `ConnectionString` property satisfies the interface.
2. **Given** `RedisFixture`, **When** it implements `IRigConnectionSource`, **Then** its `ConnectionString` property satisfies the interface.
3. **Given** `ServiceBusFixture`, **When** it implements `IRigConnectionSource`, **Then** its `ConnectionString` property satisfies the interface.

### User Story 8 - MediatR to Mediator Migration (Priority: P1)
As a library maintainer, I need to replace MediatR with martinothamar/Mediator (MIT licensed, source-generated, AOT-ready) so that the library is free for commercial use and more performant.

**Acceptance Scenarios**:
1. **Given** `Rig.TUnit.Grpc.csproj`, **When** the migration is complete, **Then** `MediatR 12.4.1` is removed and replaced with a project reference to `Rig.TUnit.Mediator`.
2. **Given** `Rig.TUnit.Grpc/Helpers/HandlerHelper.cs`, **When** the migration is complete, **Then** the file is deleted (HandlerHelper lives only in `Rig.TUnit.Mediator.Helpers`).
3. **Given** existing Grpc tests that use `HandlerHelper`, **When** they reference the new Mediator HandlerHelper, **Then** all tests pass with `ValueTask<T>` return types.

### User Story 9 - Old Extension Methods Removal (Priority: P1)
As a library maintainer, I need to remove old standalone extension methods and replace them with fluent builder equivalents so that there is a single API surface (no duplicates).

**Acceptance Scenarios**:
1. **Given** `SqlServerContainerExtensions.cs`, **When** the builder is implemented, **Then** the file is deleted and its logic lives in `SqlServerRigBuilder`.
2. **Given** `RedisContainerExtensions.cs`, **When** the builder is implemented, **Then** the file is deleted and its logic lives in `RedisRigBuilder`.
3. **Given** `ServiceBusContainerExtensions.cs`, **When** the builder is implemented, **Then** the file is deleted and its logic lives in `ServiceBusRigBuilder`.
4. **Given** `GrpcServiceReplacementExtensions.cs`, **When** the builder is implemented, **Then** the file is deleted and its logic lives in `GrpcRigBuilder`.
5. **Given** `InMemoryDbExtensions.cs`, **When** the builder is implemented, **Then** the file is **kept** (no container dependency, still useful standalone).

### User Story 10 - Unit Tests (TDD) (Priority: P1)
As a library maintainer following TDD, I need unit tests written before implementation for all new components so that every feature is verified without Docker dependencies.

**Acceptance Scenarios**:
1. **Given** `RigBuilderTests.cs`, **When** tests run, **Then** `AddRigTUnit` fluent chain, `ForceContainersInCi`, and sub-builder invocations are verified.
2. **Given** `RigConnectTests.cs`, **When** tests run, **Then** all 5 factory methods (`FromContainer`, `FromConfig`, `FromOptions`, `FromValue`, `Auto`) are verified.
3. **Given** `ConnectionSourceTests.cs`, **When** tests run, **Then** each internal connection source's `ConnectionString` getter is verified including error cases.
4. **Given** `WaitHelperTests.cs`, **When** tests run, **Then** success, timeout (`TimeoutException`), and cancellation (`OperationCanceledException`) paths are verified.
5. **Given** `TestConfigurationBuilderTests.cs`, **When** tests run, **Then** `Set`, `SetConnectionString`, `SetSection`, `Build`, `BuildOptions`, and `Create` are verified.
6. **Given** `CompositeFixtureTests.cs`, **When** tests run, **Then** parallel init, LIFO dispose, `Get<T>` success, and `Get<T>` missing type error are verified.
7. **Given** `HandlerHelperTests.cs` (Mediator), **When** tests run, **Then** `Send(IRequest)`, `Send(ICommand)`, `Send(IQuery)`, and `Publish(INotification)` are verified with real Mediator pipeline.
8. **Given** `HttpClientHelperTests.cs`, **When** tests run, **Then** GET, POST, PUT, DELETE through the in-memory test server are verified.
9. **Given** `WebApiFactoryExtensionsTests.cs`, **When** tests run, **Then** `WithTestServices` with service overrides and configuration is verified.
10. **Given** `DbContextHelperSeedTests.cs`, **When** tests run, **Then** `SeedAsync` (async and sync overloads) with data verification are verified.

### User Story 11 - Integration Tests (Priority: P2)
As a library maintainer, I need integration tests for fluent builders with real containers so that end-to-end builder+container scenarios are verified.

**Acceptance Scenarios**:
1. **Given** `SqlServerRigBuilderTests.cs`, **When** tests run with Docker, **Then** `ReplaceDbContext<T>()` via builder against a real SQL Server container is verified.
2. **Given** `RedisRigBuilderTests.cs`, **When** tests run with Docker, **Then** `ReplaceMultiplexer()` via builder against a real Redis container is verified.
3. **Given** `ServiceBusRigBuilderTests.cs`, **When** tests run with Docker, **Then** `ReplaceClient()` via builder against a real Service Bus emulator is verified.

### User Story 12 - Solution Structure Updates (Priority: P2)
As a library maintainer, I need the solution file, meta-package, and all project references updated so that the expanded library builds as a cohesive solution.

**Acceptance Scenarios**:
1. **Given** `Rig.TUnit.slnx`, **When** updated, **Then** it contains all 17 projects (8 source + 9 test).
2. **Given** `Rig.TUnit.csproj` (meta-package), **When** updated, **Then** it references all 7 sub-packages including Mediator and WebAPI.
3. **Given** `dotnet build Rig.TUnit.slnx`, **When** run, **Then** it completes with zero errors and zero warnings.

## Requirements

### Functional Requirements

- **FR-001**: All projects MUST target `net10.0`
- **FR-002**: All test projects MUST use TUnit 1.34.5 -- no xUnit, no `Microsoft.NET.Test.Sdk`, no `coverlet.collector`
- **FR-003**: `Rig.TUnit.Mediator` MUST depend on `Mediator.Abstractions 3.0.2` only -- NOT `Mediator.SourceGenerator`
- **FR-004**: `Mediator.SourceGenerator 3.0.2` MUST be installed in consumer/test projects only (the outermost project calling `AddMediator()`)
- **FR-005**: `HandlerHelper` MUST return `ValueTask<T>` (not `Task<T>`) to match martinothamar/Mediator API
- **FR-006**: `HandlerHelper` MUST support `IRequest<T>`, `ICommand<T>`, `IQuery<T>`, and `INotification`
- **FR-007**: All connection source implementations (`ConfigConnectionSource`, `OptionsConnectionSource`, `ValueConnectionSource`, `AutoConnectionSource`) MUST be `internal sealed`
- **FR-008**: Users MUST interact with connection sources only through `RigConnect` static factory
- **FR-009**: `AutoConnectionSource` MUST use `EnvironmentDetection.IsRunningInCiCd()` for CI/CD detection
- **FR-010**: `WaitHelper` MUST throw `TimeoutException` when timeout is exceeded (consistent with existing `ListenerHelper`)
- **FR-011**: `WaitHelper` default polling interval MUST be 250ms
- **FR-012**: `CompositeFixture` MUST initialize fixtures in parallel via `Task.WhenAll`
- **FR-013**: `CompositeFixture` MUST dispose fixtures in reverse (LIFO) order
- **FR-014**: `DbContextHelper.SeedAsync()` MUST use an isolated DI scope and call `SaveChangesAsync` automatically
- **FR-015**: `ServiceBusFixture.ConfigFilePath` MUST default to `"TestInfrastructure/service-bus-config.json"`
- **FR-016**: `ListenerHelper` MUST delegate to `WaitHelper` internally (no inline polling)
- **FR-017**: Old standalone extension methods (`SqlServerContainerExtensions`, `RedisContainerExtensions`, `ServiceBusContainerExtensions`, `GrpcServiceReplacementExtensions`) MUST be deleted -- not deprecated, not kept alongside
- **FR-018**: `InMemoryDbExtensions` MUST be kept (no container dependency, useful standalone)
- **FR-019**: `MediatR 12.4.1` MUST be removed from `Rig.TUnit.Grpc.csproj`
- **FR-020**: `Rig.TUnit.Grpc` MUST gain a project reference to `Rig.TUnit.Mediator`
- **FR-021**: Existing fixtures (`SqlServerFixture`, `RedisFixture`, `ServiceBusFixture`) MUST implement `IRigConnectionSource`
- **FR-022**: Development MUST follow TDD -- write failing tests first, then implement production code
- **FR-023**: Test method naming MUST follow `{Method}_{Scenario}_{ExpectedResult}` convention
- **FR-024**: Unit test projects MUST NOT require Docker or external infrastructure
- **FR-025**: All NuGet package versions MUST match the design document exactly
- **FR-026**: Namespaces MUST match folder structure (e.g., `Rig.TUnit.Core.Builder`, `Rig.TUnit.Mediator.Helpers`)
- **FR-027**: `RigBuilder` sub-builders MUST have `internal` constructors -- users access them through extension methods on `RigBuilder`
- **FR-028**: All existing test scenarios (56 tests) MUST continue to be covered after the expansion -- tests for deleted APIs are replaced by equivalent builder/mediator tests, tests using old APIs as setup are migrated to the new builder API
- **FR-029**: TUnit.Core in `Rig.TUnit.Core.csproj` MUST be upgraded from 1.33.0 to 1.34.5 to align with all other packages
- **FR-030**: Existing fixtures (`SqlServerFixture`, `RedisFixture`, `ServiceBusFixture`) MUST add `IRigConnectionSource` interface only -- they MUST NOT extend `RigFixtureBase` (which is for new consumer fixtures)
- **FR-031**: Old extension test files (`SqlServerContainerExtensionsTests.cs`, `RedisContainerExtensionsTests.cs`, `ServiceBusContainerExtensionsTests.cs`, `GrpcServiceReplacementExtensionsTests.cs`) MUST be deleted and their scenarios replaced by the new builder integration tests
- **FR-032**: `Rig.TUnit.Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs` MUST be deleted -- HandlerHelper tests move to `Rig.TUnit.Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs`
- **FR-033**: `GrpcServiceReplacementExtensionsTests.cs` MUST be migrated to `Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs` testing the new `GrpcRigBuilder` API
- **FR-034**: `DbContextHelperTests.cs` MUST be updated to use the fluent builder API instead of the deleted `UseSqlServerContainerIsolated` extension
- **FR-035**: `RigBuilder.Services` MUST be `public` (not `internal`) so that package-specific extension methods in other assemblies (SqlServer, Redis, ServiceBus, Grpc, WebAPI) can access the `IServiceCollection` to create sub-builders. `RigBuilder.IsForceContainersInCi` remains `internal`.
- **FR-036**: `ForceContainersInCi()` is a **metadata flag** on the builder — it does NOT override `AutoConnectionSource` behavior. Consumers can read `RigBuilder.IsForceContainersInCi` within the configure delegate to make their own decisions. `AutoConnectionSource` independently uses `EnvironmentDetection.IsRunningInCiCd()`.
- **FR-037**: `Grpc/Extensions/WebApplicationFactoryExtensions.cs` and its test MUST be intentionally retained alongside the new `WebAPI/Extensions/WebApiFactoryExtensions.cs` — they serve different purposes (`WithTestConfiguration` for gRPC vs `WithTestServices` for HTTP)
- **FR-038**: Naming convention note: `WebApiFactoryExtensions` (abbreviated) in WebAPI package vs `WebApplicationFactoryExtensions` (full name) in Grpc package — this inconsistency is accepted as the two classes serve different packages with different naming conventions
- **FR-039**: `Rig.TUnit.WebAPI` MUST ship a `TestAuthenticationHandler` (`public sealed`, scheme name `"Test"`) derived from `AuthenticationHandler<TestAuthenticationOptions>` that authenticates every request unconditionally using the configured claims, or a single `ClaimTypes.Name` claim matching `TestAuthenticationOptions.DefaultUserName` when `Claims` is empty
- **FR-040**: `TestAuthenticationOptions` MUST expose `DefaultUserName` (default `"test-user"`) and a mutable `IList<Claim> Claims` initialized empty
- **FR-041**: `WebApplicationFactory<TProgram>.WithTestAuthentication(Action<TestAuthenticationOptions>?)` MUST register the `"Test"` scheme and set it as both `DefaultAuthenticateScheme` and `DefaultChallengeScheme`; MUST throw `ArgumentNullException` when the factory is null
- **FR-042**: `WebApplicationFactory<TProgram>.WithPermissiveAuthorization()` MUST replace `AuthorizationOptions.DefaultPolicy` and `FallbackPolicy` with a policy that requires an authenticated user against the `"Test"` scheme; MUST throw `ArgumentNullException` when the factory is null; MUST NOT bypass named policies (`[Authorize(Policy=...)]`) or role requirements (`[Authorize(Roles=...)]`)
- **FR-043**: `HttpClientHelper<TProgram>.WithBearerToken(string?)` MUST set the default `Authorization` header to `Bearer <token>`; passing `null` MUST clear the header; MUST return `this` for fluent chaining
- **FR-044**: `HttpClientHelper<TProgram>.WithHeader(string name, string value)` MUST overwrite any prior value, MUST throw `ArgumentException` for null or empty `name`, and MUST return `this` for fluent chaining

### Key Entities

- **IRigConnectionSource**: Interface providing `string ConnectionString` from any source (container, config, options, value, auto)
- **RigConnect**: Static factory with `FromContainer`, `FromConfig`, `FromOptions`, `FromValue`, `Auto` methods
- **RigBuilder**: Fluent entry point created by `services.AddRigTUnit(rig => ...)` with package-specific extension methods
- **SqlServerRigBuilder**: Sub-builder for SQL Server with `ReplaceDbContext<TContext>()` methods
- **RedisRigBuilder**: Sub-builder for Redis with `ReplaceMultiplexer()` and `ReplaceClient<T>()` methods
- **ServiceBusRigBuilder**: Sub-builder for Service Bus with `ReplaceClient()` and `ReplaceClient<T>(factory)` methods
- **GrpcRigBuilder\<TProgram\>**: Sub-builder for gRPC with `ReplaceClient<TClient>()` methods
- **WebApiRigBuilder\<TProgram\>**: Sub-builder for WebAPI with `AddHttpClientHelper()` and `AddHandlerHelper()` methods
- **HandlerHelper**: Dispatches Mediator requests/commands/queries/notifications within isolated DI scopes (ValueTask-based)
- **HttpClientHelper\<TProgram\>**: Creates typed HttpClient instances routed through in-memory test server with GET/POST/PUT/DELETE helpers plus `WithBearerToken(string?)` and `WithHeader(string, string)` for default-header configuration
- **TestAuthenticationHandler**: `AuthenticationHandler<TestAuthenticationOptions>` that unconditionally authenticates every request with the `"Test"` scheme, ignoring incoming `Authorization` headers
- **TestAuthenticationOptions**: `AuthenticationSchemeOptions` exposing `DefaultUserName` (default `"test-user"`) and a mutable `IList<Claim>` for configured principal claims
- **TestAuthenticationExtensions**: `WithTestAuthentication(Action<TestAuthenticationOptions>?)` and `WithPermissiveAuthorization()` extensions on `WebApplicationFactory<TProgram>` for test-only auth pipelines
- **WaitHelper**: Static generic async polling utility with sync, async, and result-producing overloads
- **TestConfigurationBuilder**: Fluent in-memory `IConfiguration` builder with `Set`, `SetConnectionString`, `SetSection`, `Build`, `BuildOptions`
- **RigFixtureBase**: Abstract base implementing `IAsyncInitializer` + `IAsyncDisposable` + `IRigConnectionSource`
- **CompositeFixture**: Composes multiple fixtures with parallel init and LIFO dispose
- **ConfigConnectionSource**: Internal -- reads connection string from `IConfiguration` key
- **OptionsConnectionSource\<T\>**: Internal -- reads connection string from `IOptions<T>` via selector
- **ValueConnectionSource**: Internal -- wraps a raw connection string value
- **AutoConnectionSource**: Internal -- switches between container (CI) and config (local) using environment detection

## Architecture Scope

This is a **standalone library** expansion (not a microservice). The architecture adds 2 new packages and extends 5 existing packages.

### Affected Layers

| Layer | Change Type | Details |
|-------|-------------|---------|
| `Rig.TUnit.Core` | **Extended** | New `Builder/`, `Helpers/`, `Configuration/`, `Fixtures/` folders; new package deps |
| `Rig.TUnit.Mediator` | **New package** | `HandlerHelper` extracted from Grpc, uses martinothamar/Mediator |
| `Rig.TUnit.WebAPI` | **New package** | `HttpClientHelper`, `WebApiFactoryExtensions`, `WebApiRigBuilder` |
| `Rig.TUnit.Grpc` | **Modified** | Gains Mediator ref, removes MediatR, deletes HandlerHelper + old extensions, adds GrpcRigBuilder |
| `Rig.TUnit.SqlServer` | **Modified** | Fixture gets `IRigConnectionSource`, deletes old extensions, adds SqlServerRigBuilder, SeedAsync on DbContextHelper |
| `Rig.TUnit.Redis` | **Modified** | Fixture gets `IRigConnectionSource`, deletes old extensions, adds RedisRigBuilder |
| `Rig.TUnit.ServiceBus` | **Modified** | Fixture gets `IRigConnectionSource` + configurable config path, deletes old extensions, adds ServiceBusRigBuilder, ListenerHelper uses WaitHelper |
| `Rig.TUnit` (meta) | **Modified** | Adds references to Mediator and WebAPI |

### New Package Dependencies

| Package | New Dependencies |
|---------|-----------------|
| Core | +Configuration.Abstractions 10.0.0, +Configuration 10.0.0, +Configuration.Memory 10.0.0, +Options 10.0.0 |
| Mediator [NEW] | Mediator.Abstractions 3.0.2, M.E.DI.Abstractions 10.0.0 |
| WebAPI [NEW] | TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6, FrameworkRef ASP.NET Core |
| Grpc | +ProjectRef to Mediator, -MediatR 12.4.1 |

### Dependency Graph (Post-Expansion)

```
                    +-------------------+
                    |  Rig.TUnit.Core   |
                    +--------+----------+
           +--------+--------+--------+---------+
           |        |                 |         |
     +-----v---+ +--v--------+  +----v---+ +---v--------+
     | SqlServer| |  Mediator |  | Redis  | | ServiceBus |
     +---------+ +--+--------+  +--------+ +------------+
                +---+------+
          +-----v---+ +---v-----+
          |   Grpc  | |  WebAPI |
          +---------+ +---------+
                |           |
                +-----+-----+
                +-----v-----+
                | Rig.TUnit |  (meta-package)
                +-----------+
```

### File Inventory

**New Source Files (30 files):**
- `Core/Builder/IRigConnectionSource.cs`
- `Core/Builder/RigBuilder.cs`
- `Core/Builder/RigBuilderExtensions.cs`
- `Core/Builder/RigConnect.cs`
- `Core/Builder/ConfigConnectionSource.cs`
- `Core/Builder/OptionsConnectionSource.cs`
- `Core/Builder/ValueConnectionSource.cs`
- `Core/Builder/AutoConnectionSource.cs`
- `Core/Helpers/WaitHelper.cs`
- `Core/Configuration/TestConfigurationBuilder.cs`
- `Core/Fixtures/RigFixtureBase.cs`
- `Core/Fixtures/CompositeFixture.cs`
- `Mediator/Rig.TUnit.Mediator.csproj`
- `Mediator/Helpers/HandlerHelper.cs`
- `WebAPI/Rig.TUnit.WebAPI.csproj`
- `WebAPI/Helpers/HttpClientHelper.cs`
- `WebAPI/Extensions/WebApiFactoryExtensions.cs`
- `WebAPI/Builder/WebApiRigBuilder.cs`
- `WebAPI/Builder/WebApiRigBuilderExtensions.cs`
- `WebAPI/Authentication/TestAuthenticationHandler.cs`
- `WebAPI/Authentication/TestAuthenticationOptions.cs`
- `WebAPI/Authentication/TestAuthenticationExtensions.cs`
- `SqlServer/Builder/SqlServerRigBuilder.cs`
- `SqlServer/Builder/SqlServerRigBuilderExtensions.cs`
- `Redis/Builder/RedisRigBuilder.cs`
- `Redis/Builder/RedisRigBuilderExtensions.cs`
- `ServiceBus/Builder/ServiceBusRigBuilder.cs`
- `ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs`
- `Grpc/Builder/GrpcRigBuilder.cs`
- `Grpc/Builder/GrpcRigBuilderExtensions.cs`

**Deleted Files (5 files):**
- `SqlServer/Extensions/SqlServerContainerExtensions.cs`
- `Redis/Extensions/RedisContainerExtensions.cs`
- `ServiceBus/Extensions/ServiceBusContainerExtensions.cs`
- `Grpc/Extensions/GrpcServiceReplacementExtensions.cs`
- `Grpc/Helpers/HandlerHelper.cs`

**Modified Files (9 files):**
- `Core/Rig.TUnit.Core.csproj` (new package refs)
- `SqlServer/Fixtures/SqlServerFixture.cs` (add IRigConnectionSource)
- `SqlServer/Helpers/DbContextHelper.cs` (add SeedAsync)
- `Redis/Fixtures/RedisFixture.cs` (add IRigConnectionSource)
- `ServiceBus/Fixtures/ServiceBusFixture.cs` (add IRigConnectionSource, ConfigFilePath)
- `ServiceBus/Helpers/ListenerHelper.cs` (delegate to WaitHelper)
- `Grpc/Rig.TUnit.Grpc.csproj` (add Mediator ref, remove MediatR)
- `Rig.TUnit/Rig.TUnit.csproj` (add Mediator + WebAPI refs)
- `Rig.TUnit.slnx` (add Mediator, WebAPI, and their test projects)

**Retained Files (explicitly kept):**
- `Grpc/Extensions/WebApplicationFactoryExtensions.cs` — intentionally retained alongside new WebAPI extensions; different namespace and methods (`WithTestConfiguration` vs `WithTestServices`)
- `SqlServer/Extensions/InMemoryDbExtensions.cs` — intentionally retained per FR-018 (no container dependency, useful standalone)

**New Test Projects (2 projects):**
- `tests/Rig.TUnit.Mediator.Tests.Unit/`
- `tests/Rig.TUnit.WebAPI.Tests.Unit/`

**New Test Files in Existing Projects:**
- `Core.Tests.Unit/Builder/RigBuilderTests.cs`
- `Core.Tests.Unit/Builder/RigConnectTests.cs`
- `Core.Tests.Unit/Builder/ConnectionSourceTests.cs`
- `Core.Tests.Unit/Helpers/WaitHelperTests.cs`
- `Core.Tests.Unit/Configuration/TestConfigurationBuilderTests.cs`
- `Core.Tests.Unit/Fixtures/CompositeFixtureTests.cs`
- `SqlServer.Tests.Unit/Helpers/DbContextHelperSeedTests.cs`
- `SqlServer.Tests.Integration/Builder/SqlServerRigBuilderTests.cs`
- `Redis.Tests.Integration/Builder/RedisRigBuilderTests.cs`
- `ServiceBus.Tests.Integration/Builder/ServiceBusRigBuilderTests.cs`
- `Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs`
- `WebAPI.Tests.Unit/Authentication/TestAuthenticationHandlerTests.cs`
- `WebAPI.Tests.Unit/Authentication/TestAuthenticationOptionsTests.cs`
- `WebAPI.Tests.Unit/Authentication/TestAuthenticationExtensionsTests.cs`

**Deleted Test Files (replaced by new builder/mediator tests):**
- `SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs` (replaced by `SqlServerRigBuilderTests.cs`)
- `Redis.Tests.Integration/Extensions/RedisContainerExtensionsTests.cs` (replaced by `RedisRigBuilderTests.cs`)
- `ServiceBus.Tests.Integration/Extensions/ServiceBusContainerExtensionsTests.cs` (replaced by `ServiceBusRigBuilderTests.cs`)
- `Grpc.Tests.Unit/Extensions/GrpcServiceReplacementExtensionsTests.cs` (replaced by `GrpcRigBuilderTests.cs`)
- `Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs` (replaced by `Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs`)

**Migrated Test Files (updated to use new APIs):**
- `SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs` (migrated from `UseSqlServerContainerIsolated` to fluent builder)

## Implementation Phases

### Phase 1: Core Infrastructure (no breaking changes)
1. `IRigConnectionSource` + all connection source implementations
2. `RigConnect` static factory
3. `RigBuilder` + `AddRigTUnit` entry point
4. `WaitHelper`
5. `TestConfigurationBuilder`
6. `RigFixtureBase`
7. `CompositeFixture`
8. Update `Core.csproj` with new dependencies
9. Existing fixtures: add `IRigConnectionSource` interface
10. Unit tests for all above

### Phase 2: Mediator Package (new package)
1. Create `Rig.TUnit.Mediator` project + csproj
2. `HandlerHelper` with Mediator interfaces
3. Test project with source generator
4. Unit tests (Request, Command, Query, Notification)
5. Update Grpc: reference Mediator, delete old HandlerHelper, remove MediatR

### Phase 3: WebAPI Package (new package)
1. Create `Rig.TUnit.WebAPI` project + csproj
2. `HttpClientHelper<TProgram>`
3. `WebApiFactoryExtensions`
4. `WebApiRigBuilder` + extensions
5. Test project with TestProgram + TestEndpoints
6. Unit tests

### Phase 4: Package-Specific Builders (fluent extensions)
1. `SqlServerRigBuilder` + extensions + delete old extensions
2. `RedisRigBuilder` + extensions + delete old extensions
3. `ServiceBusRigBuilder` + extensions + delete old extensions
4. `GrpcRigBuilder` + extensions + delete old extensions
5. Integration tests for each builder

### Phase 5: Enhancements
1. `DbContextHelper.SeedAsync()`
2. `ServiceBusFixture` custom config path
3. `ListenerHelper` refactor to use `WaitHelper`
4. Tests for enhancements

### Phase 6: Solution + Verification
1. Update `Rig.TUnit.slnx` with all new projects
2. Update meta-package references
3. `dotnet build` -- zero errors, zero warnings
4. `dotnet test` (unit tests) -- all pass
5. Regression verification -- all 56 existing tests pass
6. Benchmarks updated for new components

## Edge Cases

- **Null connection string in `FromValue`**: Throws `ArgumentNullException` at construction time (fail-fast)
- **Missing config key in `FromConfig`**: Throws `InvalidOperationException` with key name when `ConnectionString` is accessed (not at construction time -- lazy)
- **Null options selector in `FromOptions`**: Throws `InvalidOperationException` with type name when `ConnectionString` is accessed
- **`Auto` mode with no CI and no config**: Falls back to container fixture (never throws unless fixture is uninitialized)
- **`CompositeFixture.Get<T>` for missing type**: Throws `InvalidOperationException` with the type name
- **`WaitHelper` cancellation**: Uses `CancellationTokenSource.CreateLinkedTokenSource` so both external cancellation and timeout are honored
- **`WaitHelper` immediate condition**: If condition is true on first check, returns immediately without any `Task.Delay`
- **`SeedAsync` with exceptions**: If seed action throws, the scope is still disposed (using `await using`)
- **Multiple `ReplaceDbContext<T>` calls**: Each creates a uniquely named database on the shared container
- **`ServiceBusRigBuilder.ReplaceClient<T>` custom wrapper**: Factory receives the connection string to construct custom wrappers
- **`HttpClientHelper` lazy client creation**: `Client` property creates the `HttpClient` on first access
- **Mediator source generator placement**: Only in outermost project (test project), not in library -- prevents build errors from duplicate generators

## Constraints (DO NOT)

- Do NOT keep old extension methods alongside new builders -- delete them, no duplicates
- Do NOT install `Mediator.SourceGenerator` in library projects (only in test/consumer projects)
- Do NOT add `Microsoft.NET.Test.Sdk` or `coverlet.collector` (conflicts with TUnit)
- Do NOT create abstract base classes for fixtures except `RigFixtureBase`
- Do NOT make connection source classes public (keep internal, expose via `RigConnect`)
- Do NOT add README or docs files
- Do NOT add NuGet packaging config
- Do NOT add CI/CD workflows
- Do NOT write production code before its corresponding test is written and failing (TDD)
- Do NOT deviate from the exact file paths and API design in the design document
- Do NOT guess package/framework APIs -- check official documentation first

## Reference Documents

- **Build Prompt**: `planning/fluent-builder-expansion/Rig.TUnit-Build-Prompt.md` -- feature summary, constraints, success criteria
- **Design**: `planning/fluent-builder-expansion/Rig.TUnit-Library-Design.md` -- complete API design with code examples
- **Handoff**: `planning/fluent-builder-expansion/Rig.TUnit-Session-Handoff.md` -- exact files, dependencies, implementation sequence
- **Base Spec**: `.dotnet-ai-kit/features/001-rig-tunit-library/spec.md` -- base library specification (for context)

## Success Criteria

### Build & Structure
- **SC-001**: `dotnet build Rig.TUnit.slnx` completes with zero errors and zero warnings
- **SC-002**: All 17 projects present in solution (8 source + 9 test) with correct project references
- **SC-003**: All NuGet package versions match the design document exactly
- **SC-004**: All namespaces match folder structure
- **SC-005**: Connection source classes are `internal sealed`
- **SC-006**: `MediatR` package is fully removed from the solution

### Functionality
- **SC-007**: `RigBuilder` configures SqlServer, Redis, ServiceBus, Grpc, WebAPI via fluent API
- **SC-008**: `RigConnect.Auto` switches between container and config based on environment
- **SC-009**: `HandlerHelper` works with martinothamar/Mediator (`IRequest`, `ICommand`, `IQuery`, `INotification`)
- **SC-010**: `HttpClientHelper` sends/receives HTTP requests through in-memory test server
- **SC-011**: `WaitHelper` times out with `TimeoutException` (consistent with existing `ListenerHelper`)
- **SC-012**: `CompositeFixture` initializes in parallel, disposes in reverse order
- **SC-013**: `DbContextHelper.SeedAsync` inserts data in isolated scope
- **SC-014**: `TestConfigurationBuilder` produces valid `IConfiguration`

### Testing
- **SC-015**: All existing 56 tests pass (zero regressions)
- **SC-016**: New unit tests pass without Docker
- **SC-017**: New integration tests pass with Docker
- **SC-018**: All tests follow `{Method}_{Scenario}_{ExpectedResult}` naming
- **SC-019**: TDD methodology followed -- tests written before implementation

### Benchmarks
- **SC-020**: Benchmarks include new components (WaitHelper, TestConfigurationBuilder, CompositeFixture, HttpClientHelper)

## Clarifications

- **C-001** [Domain & Data Model]: FR-028 "existing tests pass" vs FR-017 "delete old extensions" -- FR-028 means test *scenarios* are preserved, not exact test code. 5 extension/handler test files are deleted and replaced by new builder/mediator tests. 1 test file (`DbContextHelperTests.cs`) is migrated to use the builder API. Added FR-028 rewording, FR-031, FR-032, FR-033, FR-034.
- **C-002** [Domain & Data Model]: TUnit.Core version alignment -- `Core.csproj` has TUnit.Core 1.33.0, design docs require 1.34.5. Auto-resolved: upgrade included in expansion scope. Added FR-029.
- **C-003** [Domain & Data Model]: Existing fixtures inheritance approach -- design doc section 8.2 shows `ServiceBusFixture : RigFixtureBase`, but handoff specifies interface-only addition. Auto-resolved: handoff is the implementation spec; existing fixtures add `IRigConnectionSource` only, `RigFixtureBase` is for new consumer fixtures. Added FR-030.
- **C-004** [Edge Cases]: Missing GrpcRigBuilder test -- no test file was planned despite GrpcRigBuilder being new code. Auto-resolved: old `GrpcServiceReplacementExtensionsTests.cs` is migrated to `GrpcRigBuilderTests.cs`. Added FR-033 and new test file to inventory.
- **C-005** [Domain & Data Model]: `RigBuilder.Services` must be `public` not `internal` -- package-specific extension methods in other assemblies (SqlServer, Redis, ServiceBus, Grpc, WebAPI) need access to `IServiceCollection`. Added FR-035.
- **C-006** [Domain & Data Model]: `ForceContainersInCi()` semantics clarified -- it is a metadata flag consumers can read within the configure delegate; it does NOT override `AutoConnectionSource` which independently uses `EnvironmentDetection`. Added FR-036.
- **C-007** [Domain & Data Model]: `Grpc/Extensions/WebApplicationFactoryExtensions.cs` explicitly retained -- not deleted, serves different purpose than new `WebAPI/Extensions/WebApiFactoryExtensions.cs`. Added FR-037.
- **C-008** [Naming]: `WebApiFactoryExtensions` vs `WebApplicationFactoryExtensions` naming inconsistency accepted -- different packages, different conventions. Added FR-038.
- **C-009** [Domain & Data Model]: File count corrected -- spec header said 26, actual count is 27 source files. Modified files increased from 8 to 9 (includes `Rig.TUnit.slnx`). Retained files section added.
- **C-010** [Coverage Gap]: `HttpClientHelper.CreateClient` method had no test -- added to test scope (T041).
- **C-011** [Coverage Gap]: `WaitHelper` default 250ms polling interval (FR-011) had no explicit test -- added `WaitForAsync_DefaultPollingInterval_Is250ms` to test scope (T018).
- **C-012** [Coverage Gap]: `RedisRigBuilder.ReplaceClient<T>` had no integration test -- added `ReplaceClient_CustomFactory_ReturnsCustomType` to test scope (T051).
- **C-013** [Coverage Gap]: `ServiceBusFixture.ConfigFilePath` enhancement had no task -- added T064 for verification.
- **C-014** [Coverage Gap]: `WebApiRigBuilder` had no [RED] test task before implementation -- added T045 for TDD compliance.
- **C-015** [Task Dependencies]: T032/T040 incorrectly marked [P] when subsequent test tasks depend on them -- removed [P] markers.
- **C-016** [Task Dependencies]: Phase 4 implicit dependency on Phase 1 Core types -- added `[depends: T029]` to first Phase 4 task.
- **C-017** [Edge Cases]: T050 (SqlServer atomic deletion) must not accidentally delete `InMemoryDbExtensions.cs` -- added explicit guard note to task.
- **C-018** [Scope Addition]: Test authentication/authorization surface (`TestAuthenticationHandler`, `TestAuthenticationOptions`, `TestAuthenticationExtensions.WithTestAuthentication`, `TestAuthenticationExtensions.WithPermissiveAuthorization`) and `HttpClientHelper.WithBearerToken` / `WithHeader` were added during implementation to round out the WebAPI test story. Retroactively formalized via User Story 4b, FR-039..FR-044, tasks T070..T075, and file inventory updates (27 → 30 source files, new test files under `WebAPI.Tests.Unit/Authentication/`).
- **C-019** [Semantics]: `WithPermissiveAuthorization` only replaces `DefaultPolicy`/`FallbackPolicy`. Named policies and role requirements still apply. XML doc on the method and FR-042 make this explicit.
