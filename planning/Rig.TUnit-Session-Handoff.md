# Rig.TUnit — Session Handoff

## What This Is

A complete handoff document for implementing the `Rig.TUnit` standalone NuGet library.
Use this + `Rig.TUnit-Library-Design.md` in a new session to implement the library.

## Naming Convention

- **Rig** = the library brand (short for "test rig")
- **TUnit** = the test framework scope
- Future expansion: `Rig.XUnit.*`, `Rig.NUnit.*`

## Target Repo

Create a new repo: `Ecom-LTD/rig.tunit` (or work locally at a chosen path).

## .NET Version

- **net10.0** (matching MeterCharger services)

## Framework

- **TUnit 1.33.0+** (NOT xUnit)
- **TUnit.AspNetCore** for WebApplicationFactory integration
- No `Microsoft.NET.Test.Sdk`, no `coverlet.collector` (conflict with TUnit)

---

## Files To Create

### Solution

```
Rig.TUnit.slnx
```

### Rig.TUnit.Core

```
src/Rig.TUnit.Core/Rig.TUnit.Core.csproj
src/Rig.TUnit.Core/Fakers/CustomConstructorFaker.cs
src/Rig.TUnit.Core/Extensions/ServiceRemovalExtensions.cs
src/Rig.TUnit.Core/Extensions/EnvironmentDetection.cs
```

**csproj dependencies:**
```xml
<PackageReference Include="TUnit.Core" Version="1.33.0" />
<PackageReference Include="Bogus" Version="35.6.1" />
```

### Rig.TUnit.Grpc

```
src/Rig.TUnit.Grpc/Rig.TUnit.Grpc.csproj
src/Rig.TUnit.Grpc/Helpers/GrpcClientHelper.cs
src/Rig.TUnit.Grpc/Helpers/HandlerHelper.cs
src/Rig.TUnit.Grpc/Helpers/MetadataHelper.cs
src/Rig.TUnit.Grpc/Extensions/WebApplicationFactoryExtensions.cs
src/Rig.TUnit.Grpc/Extensions/GrpcServiceReplacementExtensions.cs
```

**csproj dependencies:**
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="TUnit.AspNetCore" Version="*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
<PackageReference Include="Grpc.AspNetCore" Version="2.71.0" />
<PackageReference Include="Grpc.Net.Client" Version="2.71.0" />
<PackageReference Include="Grpc.Net.ClientFactory" Version="2.71.0" />
<PackageReference Include="Calzolari.Grpc.Net.Client.Validation" Version="9.0.0" />
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="Serilog" Version="4.2.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
```

### Rig.TUnit.SqlServer

```
src/Rig.TUnit.SqlServer/Rig.TUnit.SqlServer.csproj
src/Rig.TUnit.SqlServer/Fixtures/SqlServerFixture.cs
src/Rig.TUnit.SqlServer/Helpers/DbContextHelper.cs
src/Rig.TUnit.SqlServer/Extensions/InMemoryDbExtensions.cs
src/Rig.TUnit.SqlServer/Extensions/SqlServerContainerExtensions.cs
```

**csproj dependencies:**
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.MsSql" Version="4.6.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

### Rig.TUnit.Redis

```
src/Rig.TUnit.Redis/Rig.TUnit.Redis.csproj
src/Rig.TUnit.Redis/Fixtures/RedisFixture.cs
src/Rig.TUnit.Redis/Extensions/RedisContainerExtensions.cs
```

**csproj dependencies:**
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.Redis" Version="4.6.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

### Rig.TUnit.ServiceBus

```
src/Rig.TUnit.ServiceBus/Rig.TUnit.ServiceBus.csproj
src/Rig.TUnit.ServiceBus/Fixtures/ServiceBusFixture.cs
src/Rig.TUnit.ServiceBus/Helpers/ListenerHelper.cs
src/Rig.TUnit.ServiceBus/Helpers/ServiceBusEventSender.cs
src/Rig.TUnit.ServiceBus/Extensions/ServiceBusContainerExtensions.cs
```

**csproj dependencies:**
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.ServiceBus" Version="4.6.0" />
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Rig.TUnit (meta-package)

```
src/Rig.TUnit/Rig.TUnit.csproj
```

**csproj — references all:**
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<ProjectReference Include="..\Rig.TUnit.Grpc\Rig.TUnit.Grpc.csproj" />
<ProjectReference Include="..\Rig.TUnit.SqlServer\Rig.TUnit.SqlServer.csproj" />
<ProjectReference Include="..\Rig.TUnit.Redis\Rig.TUnit.Redis.csproj" />
<ProjectReference Include="..\Rig.TUnit.ServiceBus\Rig.TUnit.ServiceBus.csproj" />
```

---

## Key Design Decisions

### 1. TUnit, NOT xUnit
- `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]` replaces `ICollectionFixture`
- `IAsyncInitializer` + `IAsyncDisposable` replaces `IAsyncLifetime`
- Nested data source dependency resolution (auto-ordered initialization)
- Tests parallel by default
- `await Assert.That(x).IsEqualTo(y)` — must await!

### 2. TUnit.AspNetCore
- `TestWebApplicationFactory<TProgram>` — auto-configures logging, per-test isolation
- `WebApplicationTest<TFactory, TProgram>` — base class with `Factory` property
- `GetIsolatedName("table")` → `"Test_42_table"` unique per test
- Lifecycle: SetupAsync → ConfigureWebHost → ConfigureTestServices → test → dispose

### 3. Generic Types (decoupled from any service)
- `DbContextHelper<TContext>` — works with any DbContext
- `GrpcClientHelper<TClient>` — works with any gRPC client
- `HandlerHelper` — dispatches via MediatR (works with any Event<T>)
- No references to `Program`, `ApplicationDbContext`, or any service-specific types

### 4. Container Fixtures are Simple
```csharp
// That's it. No IContainerFactory, no IContainerManager, no ContainerType enum.
public sealed class SqlServerFixture : IAsyncInitializer, IAsyncDisposable
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();
    public string ConnectionString => Container.GetConnectionString();
    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

### 5. DB Isolation Strategy
- **Unit tests**: `UseInMemoryDatabase<TContext>()` with unique `Guid` name per class
- **Live tests**: `UseSqlServerContainerIsolated<TContext>(fixture)` — unique DB name on shared container

### 6. No Task.Delay
- `ListenerHelper.WaitForMessagesAsync(expectedCount, timeout)` — polls every 250ms
- TUnit's `await Assert.That(async () => ...).IsTrue().Within(TimeSpan.FromSeconds(15))`

### 7. Serilog in TUnit
- `Serilog.Sinks.XUnit` does NOT work with TUnit
- Use `WriteTo.Console()` — TUnit captures stdout per test
- Or `TestContext.Current!.Output.WriteLine()` for manual output

---

## Reference: Existing Patterns to Port

### From Competition Base (xUnit, `Compititions/Base/`)

| File | What to port | Target package |
|------|-------------|----------------|
| `CustomConstructorFaker.cs` | RuntimeHelpers.GetUninitializedObject pattern | Core |
| `DbContextHelper.cs` | Query<T>(), InsertAsync<T>() with scope + ChangeTracker.Clear | SqlServer |
| `GrpcClientHelper.cs` | Typed gRPC client via factory channel | Grpc |
| `HandlerHelper.cs` | MediatR Send for Event<T> : IRequest<bool> | Grpc |
| `MetadataHelper.cs` | gRPC Metadata with `access-claims-bin` header | Grpc |
| `ServiceCollectionExtensions.cs` | RemoveServiceBusLogic, ReplaceService<TGrpcClient>, UseDb | Core + Grpc + SqlServer |
| `WebApplicationExtensions.cs` | WithDefaultConfigurations, CreateGrpcChannel | Grpc |
| `FakeServiceBusPublisher.cs` | No-op IServiceBusPublisher | Core |
| `Listener.cs` / `ListenerHelper.cs` | Service Bus session processor for live tests | ServiceBus |
| Fake gRPC services pattern | Proto as Server + C# impl + FakeServicesData | Grpc (documented pattern, not lib code) |
| Per-entity Asserts | `request.AssertEquality(@event)` | Per-service (not in lib) |

### From anis.catalogue TestContainers (xUnit)

| File | What changed | Target package |
|------|-------------|----------------|
| `SqlServerTestContainer.cs` | Simplified to `SqlServerFixture` | SqlServer |
| `RedisTestContainer.cs` | Simplified to `RedisFixture` | Redis |
| `ServiceBusTestContainer.cs` | Simplified to `ServiceBusFixture` | ServiceBus |
| `SharedTestContainersFixture.cs` | Eliminated — TUnit ClassDataSource handles it | — |
| `ContainerManager.cs` | Eliminated — no needed abstraction | — |
| `ContainerFactory.cs` | Eliminated | — |
| `RequiresContainersAttribute.cs` | Eliminated — install the NuGet package you need | — |
| `IntegrationTestFixture.cs` | Replaced by `WebApplicationTest<TFactory, TProgram>` | Grpc (via TUnit.AspNetCore) |
| `IntegrationTestCollection.cs` | Eliminated — no xUnit collections | — |
| `TestEnvironmentExtensions.cs` | Kept EnvironmentDetection, removed dual-mode | Core |
| `ServiceCollectionExtensions.cs` | Split into per-package extensions | SqlServer, Redis, ServiceBus |
| `ServiceBusEventSender.cs` | Ported to ServiceBus package | ServiceBus |
| `ServiceBusDemoManager.cs` | Simplified — no dual-mode, direct sender | ServiceBus |

---

## Consumer Example (What a developer writes)

### Command Unit Test
```csharp
// Install: Rig.TUnit.Core + Rig.TUnit.Grpc + Rig.TUnit.SqlServer
public class CreateMeterTest
{
    private readonly DbContextHelper<MeterChargerCommandsDbContext> _db;
    private readonly GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient> _grpc;

    public CreateMeterTest()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithTestConfiguration(
                configureServices: services =>
                {
                    services.UseInMemoryDatabase<MeterChargerCommandsDbContext>();
                    services.RemoveService<IServiceBusPublisher>();
                    services.AddSingleton<IServiceBusPublisher, FakeServiceBusPublisher>();
                },
                mapEndpoints: e => e.MapGrpcService<MeterChargerCommandsService>());

        _db = new DbContextHelper<MeterChargerCommandsDbContext>(factory.Services);
        _grpc = new GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient>(factory);
    }

    [Test]
    public async Task CreateMeter_ValidData_ReturnsSuccess()
    {
        var request = new CreateMeterRequestFaker().Generate();
        var response = await _grpc.Send(c => c.CreateMeterAsync(request));
        var @event = await _db.Query(db => db.Events.OfType<MeterCreated>().SingleOrDefaultAsync());

        await Assert.That(@event).IsNotNull();
        await Assert.That(response.Message).IsEqualTo(Phrases.MeterCreated);
    }
}
```

### Command Live Test
```csharp
// Additionally install: Rig.TUnit.SqlServer (container fixture)
public class CreateMeterLiveTest
{
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public required SqlServerFixture Sql { get; init; }

    private DbContextHelper<MeterChargerCommandsDbContext> _db = null!;
    private GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient> _grpc = null!;

    [Before(Test)]
    public void Setup()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithTestConfiguration(configureServices: services =>
            {
                services.UseSqlServerContainerIsolated<MeterChargerCommandsDbContext>(Sql);
            });
        _db = new DbContextHelper<MeterChargerCommandsDbContext>(factory.Services);
        _grpc = new GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient>(factory);
    }

    [Test]
    public async Task CreateMeter_ValidData_PersistsToDb()
    {
        var request = new CreateMeterRequestFaker().Generate();
        await _grpc.Send(c => c.CreateMeterAsync(request));
        var @event = await _db.Query(db => db.Events.OfType<MeterCreated>().SingleOrDefaultAsync());
        await Assert.That(@event).IsNotNull();
    }
}
```

---

## Memory References

These memory files contain additional context (stored in Claude's memory system):
- `testing_existing_patterns.md` — Competition Base xUnit patterns
- `testing_testcontainers.md` — anis.catalogue architecture + issues
- `testing_architecture_plan.md` — MeterCharger recommended architecture
- `testing_tunit_reference.md` — TUnit features, lifecycle, assertions, migration guide
