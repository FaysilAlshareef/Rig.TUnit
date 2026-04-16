# Build Prompt — Rig.TUnit Fluent Builder Expansion

Copy everything below this line and pass it to `/dai.spec` to generate the formal specification.

---

## Context

This is a **feature expansion** of the Rig.TUnit library (not a rewrite). The base library is fully implemented with 56 passing tests across 6 source packages and 7 test projects. This expansion adds new packages, a fluent builder API, and replaces MediatR with martinothamar/Mediator.

**Read before generating spec:**
- `planning/fluent-builder-expansion/Rig.TUnit-Library-Design.md` — complete API design with code examples for every component
- `planning/fluent-builder-expansion/Rig.TUnit-Session-Handoff.md` — exact files, dependencies, versions, implementation sequence
- `.dotnet-ai-kit/features/001-rig-tunit-library/spec.md` — base spec (for format reference)
- `src/` directory — scan existing source code for current APIs and patterns

## Feature Summary

### Feature Name: 002-rig-tunit-fluent-builder-expansion

### What to Build

9 major features grouped into 3 categories:

**Category A — Fluent Builder API (RigBuilder)**
1. `IRigConnectionSource` — abstraction for connection strings from any source
2. `RigConnect` — static factory (FromContainer, FromConfig, FromOptions, FromValue, Auto)
3. `RigBuilder` — fluent entry point via `services.AddRigTUnit(rig => ...)`
4. Package-specific sub-builders: `SqlServerRigBuilder`, `RedisRigBuilder`, `ServiceBusRigBuilder`, `GrpcRigBuilder<TProgram>`, `WebApiRigBuilder<TProgram>`
5. `ForceContainersInCi()` — CI/CD enforcement mode
6. Refactor existing extension methods to work alongside (NOT replace) the builder

**Category B — New Packages**
7. `Rig.TUnit.Mediator` — HandlerHelper extracted from Grpc, using martinothamar/Mediator (NOT MediatR)
   - Supports `IRequest<T>`, `ICommand<T>`, `IQuery<T>`, `INotification`
   - Returns `ValueTask<T>` (not `Task<T>`)
   - Library depends on `Mediator.Abstractions 3.0.2` only (NOT `Mediator.SourceGenerator`)
   - Source generator is installed by consumer's project
8. `Rig.TUnit.WebAPI` — HTTP client/server testing without gRPC
   - `HttpClientHelper<TProgram>` with typed GET/POST/PUT/DELETE helpers
   - `WebApiFactoryExtensions` for test configuration
   - `WebApiRigBuilder<TProgram>` for fluent integration

**Category C — Core Utilities & Enhancements**
9. `WaitHelper` — generic async polling (extracted from ListenerHelper's pattern)
10. `TestConfigurationBuilder` — in-memory IConfiguration builder for tests
11. `RigFixtureBase` — abstract base implementing IAsyncInitializer + IAsyncDisposable + IRigConnectionSource
12. `CompositeFixture` — compose multiple fixtures, parallel init, LIFO dispose
13. `DbContextHelper.SeedAsync()` — seed test data in isolated scope
14. `ServiceBusFixture` custom config path — settable `ConfigFilePath` property
15. `ListenerHelper` refactor — delegate to shared `WaitHelper`

### Critical Constraints

1. **Clean replacement** — old standalone extension methods are removed, fluent builder is the single API (no duplicates)
2. **net10.0** — all projects target .NET 10
3. **TUnit 1.34.5** — all packages aligned
4. **Mediator not MediatR** — `Mediator.Abstractions 3.0.2` (MIT licensed, source-generated, AOT-ready)
5. **Source generator in consumer** — `Rig.TUnit.Mediator` has abstractions only; `Mediator.SourceGenerator` goes in test projects
6. **Fluent-first (FB methodology)** — all public APIs chainable
7. **TDD** — tests written first, then implementation
8. **Internal connection sources** — users interact via `RigConnect` static factory only
9. **Existing fixtures gain IRigConnectionSource** — binary-compatible interface addition
10. **HandlerHelper moved** — deleted from Grpc, lives only in new Rig.TUnit.Mediator package

### Dual-Mode Connection Support

The RigBuilder supports infrastructure from both containers AND external services:

| Mode | Source | Use Case |
|------|--------|----------|
| Container | Testcontainers fixture | CI/CD (forced), local (optional) |
| Config | `IConfiguration` key | Local dev with running services |
| Options | `IOptions<T>` selector | Strongly-typed options classes |
| Value | Raw connection string | Simple/hardcoded cases |
| Auto | Container in CI, Config locally | Smart switching via `EnvironmentDetection` |

`RigConnect.Auto(fixture, config, key)` logic:
- CI detected → always use fixture (container)
- Local + config key exists → use config
- Local + no config → fallback to fixture

### Custom ServiceBus Wrapper Support

Users wrap `ServiceBusClient` in custom classes:
```csharp
public class CompetitionServiceBus(ServiceBusOptions opts)
{
    public ServiceBusClient Client { get; } = new ServiceBusClient(opts.CompetitionServiceBus);
}
```

The builder handles this with factory delegates:
```csharp
.UseServiceBus(sbFixture, sb => sb
    .ReplaceClient()  // base ServiceBusClient
    .ReplaceClient<CompetitionServiceBus>(conn => 
        new CompetitionServiceBus(new ServiceBusOptions { CompetitionServiceBus = conn }))
)
```

### Package Dependencies

| Package | New Dependencies |
|---------|-----------------|
| Core | +Configuration.Abstractions, +Configuration, +Configuration.Memory, +Options (all 10.0.0) |
| Mediator [NEW] | Mediator.Abstractions 3.0.2, M.E.DI.Abstractions 10.0.0 |
| WebAPI [NEW] | TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6, FrameworkRef ASP.NET Core |
| Grpc | +ProjectRef to Mediator, -MediatR 12.4.1 |
| SqlServer | (unchanged) |
| Redis | (unchanged) |
| ServiceBus | (unchanged) |

### Success Criteria

1. `dotnet build Rig.TUnit.slnx` — zero errors, zero warnings
2. All existing tests still pass (zero regressions)
3. New unit tests pass without Docker
4. New integration tests pass with Docker
5. RigBuilder configures SqlServer, Redis, ServiceBus, Grpc, WebAPI via fluent API
6. RigConnect.Auto switches between container and config based on environment
7. HandlerHelper works with martinothamar/Mediator (IRequest, ICommand, IQuery, INotification)
8. HttpClientHelper sends/receives HTTP requests through in-memory test server
9. WaitHelper times out with TimeoutException (consistent with existing ListenerHelper)
10. CompositeFixture initializes in parallel, disposes in reverse order
11. DbContextHelper.SeedAsync inserts data in isolated scope
12. TestConfigurationBuilder produces valid IConfiguration
13. Benchmarks include new components
14. Total: 8 source projects + 9 test projects in solution

### Testing Requirements (TDD)

All tests use TUnit (not xUnit). Test method naming: `{Method}_{Scenario}_{ExpectedResult}`.

**New Unit Test Files:**
- `Core.Tests.Unit/Builder/RigBuilderTests.cs` — AddRigTUnit fluent chain
- `Core.Tests.Unit/Builder/RigConnectTests.cs` — all connection source modes
- `Core.Tests.Unit/Builder/ConnectionSourceTests.cs` — Config/Options/Value/Auto sources
- `Core.Tests.Unit/Helpers/WaitHelperTests.cs` — success, timeout, cancellation
- `Core.Tests.Unit/Configuration/TestConfigurationBuilderTests.cs` — Set, SetConnectionString, SetSection, Build, BuildOptions
- `Core.Tests.Unit/Fixtures/CompositeFixtureTests.cs` — parallel init, LIFO dispose, Get<T>
- `Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs` — Send request, command, query; Publish notification
- `WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs` — GET, POST, PUT, DELETE through test server
- `WebAPI.Tests.Unit/Extensions/WebApiFactoryExtensionsTests.cs` — WithTestServices
- `SqlServer.Tests.Unit/Helpers/DbContextHelperSeedTests.cs` — SeedAsync with data verification

**New Integration Test Files:**
- `SqlServer.Tests.Integration/Builder/SqlServerRigBuilderTests.cs` — ReplaceDbContext via builder
- `Redis.Tests.Integration/Builder/RedisRigBuilderTests.cs` — ReplaceMultiplexer via builder
- `ServiceBus.Tests.Integration/Builder/ServiceBusRigBuilderTests.cs` — ReplaceClient via builder

### Do NOT

- Do NOT keep old extension methods alongside new builders — delete them, no duplicates
- Do NOT install `Mediator.SourceGenerator` in library projects (only in test/consumer projects)
- Do NOT add `Microsoft.NET.Test.Sdk` or `coverlet.collector` (conflicts with TUnit)
- Do NOT create abstract base classes for fixtures except `RigFixtureBase`
- Do NOT make connection source classes public (keep internal, expose via RigConnect)
- Do NOT add README or docs files
- Do NOT add NuGet packaging config
- Do NOT add CI/CD workflows
