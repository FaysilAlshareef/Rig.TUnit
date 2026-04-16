# Research: 002-rig-tunit-fluent-builder-expansion

## Codebase Scan Results

### Project Mode
- **Mode**: Generic (standalone library)
- **Architecture**: Modular packages with dependency graph (Core -> packages -> meta)
- **.NET Version**: net10.0 (set in `Directory.Build.props`)
- **C# Version**: `latest` (C# 14 features available)

### Directory.Build.props (shared settings)
- `TargetFramework`: net10.0
- `ImplicitUsings`: enable
- `Nullable`: enable
- `TreatWarningsAsErrors`: true
- `LangVersion`: latest
- `TestingPlatformDotnetTestSupport`: true

### NuGet Version Inventory (current)

| Package | Version | Used In | Notes |
|---------|---------|---------|-------|
| TUnit.Core | 1.33.0 | Core, SqlServer | **Needs upgrade to 1.34.5** |
| TUnit | 1.34.5 | Core.Tests.Unit, Grpc.Tests.Unit | Already at target |
| TUnit.AspNetCore | 1.34.5 | Grpc | Already at target |
| Bogus | 35.6.1 | Core | Unchanged |
| M.E.DI.Abstractions | 10.0.0 | Core | Unchanged |
| M.E.DI | 10.0.0 | Core.Tests.Unit | Unchanged |
| Testcontainers.MsSql | 4.6.0 | SqlServer | Unchanged |
| Testcontainers.Redis | 4.6.0 | Redis | Unchanged |
| Testcontainers.ServiceBus | 4.6.0 | ServiceBus | Unchanged |
| EF Core SqlServer | 10.0.0 | SqlServer | Unchanged |
| EF Core InMemory | 10.0.0 | SqlServer | Unchanged |
| StackExchange.Redis | 2.8.16 | Redis | Unchanged |
| Azure.Messaging.ServiceBus | 7.18.2 | ServiceBus | Unchanged |
| Newtonsoft.Json | 13.0.3 | ServiceBus | Unchanged |
| MediatR | 12.4.1 | Grpc, Grpc.Tests.Unit | **To be removed** |
| Grpc.AspNetCore | 2.71.0 | Grpc | Unchanged |
| Grpc.Net.Client | 2.71.0 | Grpc | Unchanged |
| Grpc.Net.ClientFactory | 2.71.0 | Grpc | Unchanged |
| Calzolari.Grpc.Net.Client.Validation | 9.0.0 | Grpc | Unchanged |
| Serilog | 4.2.0 | Grpc | Unchanged |
| Serilog.Sinks.Console | 6.0.0 | Grpc | Unchanged |
| M.AspNetCore.Mvc.Testing | 10.0.6 | Grpc, Grpc.Tests.Unit | Unchanged |
| Google.Protobuf | 3.30.2 | Grpc.Tests.Unit | Unchanged |
| Grpc.Tools | 2.71.0 | Grpc.Tests.Unit | Unchanged |

### Coding Patterns Detected

1. **File-scoped namespaces** — all files use `namespace X;`
2. **Primary constructors** (C# 12) — `HandlerHelper(IServiceScopeFactory scopeFactory)`, `DbContextHelper<TContext>(IServiceProvider provider)`
3. **Sealed classes** — all fixtures and helpers are `sealed`
4. **Extension method style** — static class per concern, returns `IServiceCollection` for chaining
5. **No XML doc on extension classes** — some helpers have XML docs, extensions don't consistently
6. **Collection expressions** — `private static readonly string[] CiVariables = [...]`
7. **Expression-bodied members** — used for single-line methods/properties
8. **No explicit TargetFramework in csproj** — inherited from Directory.Build.props
9. **No TreatWarningsAsErrors in csproj** — inherited from Directory.Build.props

### DI Patterns

- Fixtures: Use constructor-initialized containers, no DI
- Helpers: Constructor injection with `IServiceScopeFactory` or `IServiceProvider`
- Extensions: Static methods operating on `IServiceCollection`
- Test setup: `new ServiceCollection()` + manual registration + `BuildServiceProvider()`

### Test Patterns

- Framework: TUnit (not xUnit)
- Assertions: `await Assert.That(x).IsEqualTo(y)` — all assertions must be awaited
- Naming: `{Method}_{Scenario}_{ExpectedResult}`
- Structure: Arrange-Act-Assert with blank line separation
- Fixtures: `[ClassDataSource<TFixture>(Shared = SharedType.PerTestSession)]`
- No mocking framework used — real pipelines (MediatR, WebApplicationFactory)
- Test infrastructure: Internal types per test project (TestDbContext, TestEntity, TestRequest, etc.)

### Existing Fixture Pattern (to preserve)

```csharp
public sealed class XFixture : IAsyncInitializer, IAsyncDisposable
{
    public XContainer Container { get; } = new XBuilder()...Build();
    public string ConnectionString => Container.GetConnectionString();
    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

ServiceBusFixture differs: uses `{ get; init; }` for ConfigFilePath, lazy container init.

### Files That Will Be Affected by Extension Deletion

Tests using deleted APIs directly:
- `tests/SqlServer.Tests.Integration/Extensions/SqlServerContainerExtensionsTests.cs` → DELETE
- `tests/Redis.Tests.Integration/Extensions/RedisContainerExtensionsTests.cs` → DELETE
- `tests/ServiceBus.Tests.Integration/Extensions/ServiceBusContainerExtensionsTests.cs` → DELETE
- `tests/Grpc.Tests.Unit/Extensions/GrpcServiceReplacementExtensionsTests.cs` → DELETE
- `tests/Grpc.Tests.Unit/Helpers/HandlerHelperTests.cs` → DELETE (moves to Mediator tests)

Tests using deleted APIs as setup:
- `tests/SqlServer.Tests.Integration/Helpers/DbContextHelperTests.cs` → MIGRATE to builder API
