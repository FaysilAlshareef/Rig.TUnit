# Rig.TUnit — Standalone Test Library Design

## 1. Vision

A **standalone NuGet package** that replaces `Anis.TestContainers.Common` with a modern TUnit-based library. Developers install a package, write their test class, and everything else (containers, factories, service replacement, cleanup) is handled automatically.

### Key Discovery: TUnit.AspNetCore

TUnit has an official `TUnit.AspNetCore` package that provides:
- `TestWebApplicationFactory<TEntryPoint>` — auto-configures logging, OpenTelemetry, per-test isolation
- `WebApplicationTest<TFactory, TEntryPoint>` — base class for tests with `Factory` property
- `GetIsolatedName("table")` → `"Test_42_table"` for per-test DB schema isolation
- `GetIsolatedPrefix()` → `"test_42_"` for unique resource naming
- Nested `[ClassDataSource]` with automatic dependency graph resolution (containers → factory → tests)
- **No `TUnit.TestContainers` package needed** — TUnit's `ClassDataSource` + `IAsyncInitializer` handles it natively

Sources:
- [TUnit ASP.NET Core docs](https://tunit.dev/docs/examples/aspnet/)
- [TUnit Complex Infrastructure](https://tunit.dev/docs/examples/complex-test-infrastructure/)
- [TUnit GitHub Issue #708](https://github.com/thomhurst/TUnit/issues/708)
- [Per-Test Isolation Guide (Tom Longhurst)](https://medium.com/@thomhurst/per-test-isolation-in-asp-net-core-a-tunit-aspnetcore-guide-ce09f7d4a05f)

**Before (current — anis.catalogue):**
```csharp
// Developer must understand: ICollectionFixture, SharedTestContainersFixture, 
// ContainerManager, IContainerFactory, RequiresContainersAttribute, 
// IntegrationTestCollection, WebApplicationFactory<Program> lifecycle,
// ServiceCollectionExtensions, TestEnvironmentExtensions, DbTruncate...

[Collection(nameof(IntegrationTestCollection))]
[RequiresContainers(ContainerType.SqlServer, ContainerType.ServiceBus)]
public class MyTest : IntegrationTestFixture, IClassFixture<WebApplicationFactory<Program>>
{
    public MyTest(
        WebApplicationFactory<Program> factory,
        SharedTestContainersFixture sharedFixture,
        ITestOutputHelper helper)
    {
        sharedFixture.EnsureContainersStartedAsync(GetType()).GetAwaiter().GetResult();
        
        factory = factory.WithDefaultConfigurations(helper, services =>
        {
            services.SetUnitTestsDefaultEnvironment(factory, sharedFixture);
            services.SetUnitTestsApplicationEnvironment(factory);
        });
        // ... initialize helpers
    }
}
```

**After (new — Rig.TUnit):**
```csharp
// Developer only needs to know: inherit base class, write tests

public class MyTest : CommandIntegrationTest
{
    [Test]
    public async Task CreateOrder_ValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateOrderRequestFaker().Generate();

        // Act
        var response = await Grpc.Send(c => c.CreateOrderAsync(request));
        var @event = await Db.Query(db => db.Events.OfType<OrderCreated>().SingleOrDefaultAsync());

        // Assert
        await Assert.That(@event).IsNotNull();
        await Assert.That(response.Message).IsEqualTo(Phrases.OrderCreated);
    }
}
```

---

## 2. Problems with Current Library (Anis.TestContainers.Common)

| # | Problem | Impact |
|---|---------|--------|
| 1 | **xUnit-only** — `IClassFixture`, `ICollectionFixture`, `[Collection]` | Can't use with TUnit |
| 2 | **Too many abstractions** — IContainerFactory, IContainerManager, ITestContainer, ContainerType enum | Developers confused by indirection |
| 3 | **Coupled to Catalogue project** — references `Anis.Catalogue.Grpc`, `Program`, `ApplicationDbContext` | Can't reuse across services |
| 4 | **Manual container orchestration** — `EnsureContainersStartedAsync().GetAwaiter().GetResult()` | Blocking async anti-pattern |
| 5 | **No per-test isolation** — shared DB with truncation | Parallel tests interfere |
| 6 | **Dual-mode complexity** — TestContainers vs DirectServices switching | Extra code paths to maintain |
| 7 | **Sequential execution** — all classes in same xUnit collection | Slow test runs |
| 8 | **`Task.Delay()` for async ops** | Slow + flaky |
| 9 | **`BuildServiceProvider()` in setup** | Anti-pattern, creates orphan DI container |
| 10 | **WebApplicationFactory not shared properly** | Service Bus connection quota issues |

---

## 3. Package Structure

### Option A: Modular Packages (Recommended)

```
Rig.TUnit/
├── Rig.TUnit.Core/              → Base helpers, fakers, assertions, no dependencies on containers
├── Rig.TUnit.Grpc/              → gRPC test helpers (GrpcClientHelper, MetadataHelper, fake service pattern)
├── Rig.TUnit.SqlServer/         → SQL Server container fixture + DbContextHelper + migration
├── Rig.TUnit.Redis/             → Redis container fixture + cache helpers  
├── Rig.TUnit.ServiceBus/        → Service Bus emulator fixture + listener helpers
└── Rig.TUnit/                   → Meta-package that references all above
```

### Dependency Graph

```
Rig.TUnit.Core ← (no container dependencies, just TUnit + Bogus)
    ↑
    ├── Rig.TUnit.Grpc ← (adds Grpc.AspNetCore, Grpc.Net.Client, Calzolari)
    │       ↑
    ├── Rig.TUnit.SqlServer ← (adds Testcontainers.MsSql, EF Core SqlServer)
    │       ↑
    ├── Rig.TUnit.Redis ← (adds Testcontainers.Redis, StackExchange.Redis)
    │       ↑
    └── Rig.TUnit.ServiceBus ← (adds Testcontainers.ServiceBus, Azure.Messaging.ServiceBus)
            ↑
        Rig.TUnit ← meta-package (references all)
```

### What Each Package Provides

| Package | Contains | NuGet Dependencies |
|---------|----------|--------------------|
| `Rig.TUnit.Core` | `CustomConstructorFaker<T>`, base test patterns, `FakeServiceBusPublisher`, assertion base classes | TUnit, Bogus |
| `Rig.TUnit.Grpc` | `GrpcClientHelper<T>`, `MetadataHelper`, `WebApplicationFactory` extensions, fake gRPC service base, service replacement | Rig.TUnit.Core, Grpc.AspNetCore, Grpc.Net.Client, Grpc.Net.ClientFactory, Calzolari.Grpc.Net.Client.Validation, Microsoft.AspNetCore.Mvc.Testing |
| `Rig.TUnit.SqlServer` | `SqlServerFixture`, `DbContextHelper<T>`, EF InMemory + real SQL extensions, migration helper | Rig.TUnit.Core, Testcontainers.MsSql, Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.InMemory |
| `Rig.TUnit.Redis` | `RedisFixture`, `CacheHelper`, Redis service replacement | Rig.TUnit.Core, Testcontainers.Redis, StackExchange.Redis |
| `Rig.TUnit.ServiceBus` | `ServiceBusFixture`, `ListenerHelper`, `ServiceBusEventSender`, `MessageAssert` base | Rig.TUnit.Core, Testcontainers.ServiceBus, Azure.Messaging.ServiceBus |
| `Rig.TUnit` | Meta-package | All above |

### Why Modular?

- **Command service** needs: `Core` + `Grpc` + `SqlServer` (unit tests), + `ServiceBus` (live tests)
- **Query service** needs: `Core` + `Grpc` + `SqlServer` + `ServiceBus`
- **Processor service** needs: `Core` + `Grpc` only (no DB)
- **Gateway service** needs: `Core` + `Grpc` + `Redis`
- **Any service** can install `Rig.TUnit` meta-package for everything

---

## 4. Solution Structure

```
Rig.TUnit.sln
│
├── src/
│   ├── Rig.TUnit.Core/
│   │   ├── Rig.TUnit.Core.csproj
│   │   ├── Fakers/
│   │   │   └── CustomConstructorFaker.cs
│   │   ├── FakeServices/
│   │   │   └── FakeServiceBusPublisher.cs      ← generic no-op for IServiceBusPublisher
│   │   ├── Assertions/
│   │   │   └── AssertionExtensions.cs           ← shared assertion helpers
│   │   └── Extensions/
│   │       ├── ServiceRemovalExtensions.cs       ← RemoveService<T>, RemoveImplementation<T>
│   │       └── EnvironmentDetection.cs           ← CI/CD auto-detection
│   │
│   ├── Rig.TUnit.Grpc/
│   │   ├── Rig.TUnit.Grpc.csproj
│   │   ├── Helpers/
│   │   │   ├── GrpcClientHelper.cs              ← generic GrpcClientHelper<TClient>
│   │   │   ├── MetadataHelper.cs                ← gRPC metadata builder
│   │   │   └── HandlerHelper.cs                 ← MediatR dispatch helper
│   │   ├── Extensions/
│   │   │   ├── WebApplicationFactoryExtensions.cs ← WithDefaultConfigurations()
│   │   │   └── GrpcServiceReplacementExtensions.cs ← ReplaceGrpcClient<T>()
│   │   └── FakeServices/
│   │       └── FakeGrpcServiceBase.cs            ← base class for fake gRPC servers
│   │
│   ├── Rig.TUnit.SqlServer/
│   │   ├── Rig.TUnit.SqlServer.csproj
│   │   ├── Fixtures/
│   │   │   └── SqlServerFixture.cs              ← IAsyncInitializer, ClassDataSource ready
│   │   ├── Helpers/
│   │   │   └── DbContextHelper.cs               ← generic DbContextHelper<TContext>
│   │   └── Extensions/
│   │       ├── InMemoryDbExtensions.cs           ← UseInMemoryDatabase<TContext>() for unit tests
│   │       ├── SqlServerContainerExtensions.cs   ← UseSqlServerContainer<TContext>() for live tests
│   │       └── DatabaseMigrationExtensions.cs    ← MigrateAndClean<TContext>()
│   │
│   ├── Rig.TUnit.Redis/
│   │   ├── Rig.TUnit.Redis.csproj
│   │   ├── Fixtures/
│   │   │   └── RedisFixture.cs
│   │   ├── Helpers/
│   │   │   └── CacheHelper.cs
│   │   └── Extensions/
│   │       └── RedisContainerExtensions.cs
│   │
│   ├── Rig.TUnit.ServiceBus/
│   │   ├── Rig.TUnit.ServiceBus.csproj
│   │   ├── Fixtures/
│   │   │   └── ServiceBusFixture.cs
│   │   ├── Helpers/
│   │   │   ├── ListenerHelper.cs                ← ServiceBus session listener
│   │   │   └── ServiceBusEventSender.cs         ← send events for query/processor tests
│   │   ├── Assertions/
│   │   │   └── MessageAssertBase.cs             ← base class for message assertions
│   │   └── Extensions/
│   │       └── ServiceBusContainerExtensions.cs
│   │
│   └── Rig.TUnit/
│       └── Rig.TUnit.csproj                  ← meta-package, references all above
│
└── tests/
    └── Rig.TUnit.Tests/                      ← tests for the library itself
```

---

## 5. Core API Design

### 5.1 Rig.TUnit.Core

#### CustomConstructorFaker<T>
```csharp
namespace Rig.TUnit.Core.Fakers;

/// <summary>
/// Bogus faker that bypasses constructors using RuntimeHelpers.GetUninitializedObject.
/// Essential for domain objects with private/protected setters.
/// </summary>
public class CustomConstructorFaker<T> : Faker<T> where T : class
{
    public CustomConstructorFaker()
    {
        CustomInstantiator(_ => 
            RuntimeHelpers.GetUninitializedObject(typeof(T)) as T 
            ?? throw new TypeLoadException($"Cannot create instance of {typeof(T).Name}"));
    }
}
```

#### Service Removal Extensions
```csharp
namespace Rig.TUnit.Core.Extensions;

public static class ServiceRemovalExtensions
{
    /// <summary>Removes a service registration by service type.</summary>
    public static IServiceCollection RemoveService<TService>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor != null) services.Remove(descriptor);
        return services;
    }

    /// <summary>Removes a service registration by implementation type.</summary>
    public static IServiceCollection RemoveImplementation<TImpl>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(TImpl));
        if (descriptor != null) services.Remove(descriptor);
        return services;
    }

    /// <summary>Removes all registrations whose ServiceType.FullName contains the given name.</summary>
    public static IServiceCollection RemoveByName(this IServiceCollection services, string typeName)
    {
        var toRemove = services.Where(d => d.ServiceType.FullName?.Contains(typeName) == true).ToList();
        foreach (var d in toRemove) services.Remove(d);
        return services;
    }
}
```

#### CI/CD Environment Detection
```csharp
namespace Rig.TUnit.Core.Extensions;

public static class EnvironmentDetection
{
    private static readonly string[] CiVariables =
    [
        "CI", "CONTINUOUS_INTEGRATION", "TF_BUILD", "GITHUB_ACTIONS",
        "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION", "CIRCLECI",
        "TRAVIS", "APPVEYOR", "CODEBUILD_BUILD_ID", "BUILD_BUILDID"
    ];

    /// <summary>Returns true if running in a CI/CD environment.</summary>
    public static bool IsRunningInCiCd() =>
        CiVariables.Any(v => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)));
}
```

### 5.2 Rig.TUnit.Grpc

#### GrpcClientHelper<TClient> (Generic)
```csharp
namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Creates typed gRPC clients that route through the in-memory test server.
/// </summary>
public sealed class GrpcClientHelper<TClient> where TClient : ClientBase<TClient>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GrpcClientHelper(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>Sends a gRPC request and returns the result.</summary>
    public TResult Send<TResult>(Func<TClient, TResult> action)
    {
        var client = CreateClient();
        return action(client);
    }

    /// <summary>Sends an async gRPC request.</summary>
    public async Task<TResult> SendAsync<TResult>(Func<TClient, AsyncUnaryCall<TResult>> action)
    {
        var client = CreateClient();
        return await action(client);
    }

    private TClient CreateClient()
    {
        var channel = _factory.CreateGrpcChannel();
        return (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
    }
}
```

#### WebApplicationFactory Extensions
```csharp
namespace Rig.TUnit.Grpc.Extensions;

public static class WebApplicationFactoryExtensions
{
    /// <summary>
    /// Configures WebApplicationFactory with Serilog console output (TUnit captures stdout),
    /// optional service configuration, and optional endpoint mapping.
    /// </summary>
    public static WebApplicationFactory<TProgram> WithTestConfiguration<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null,
        Action<IEndpointRouteBuilder>? mapEndpoints = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();
            });

            if (configureServices != null)
                builder.ConfigureTestServices(configureServices);

            if (mapEndpoints != null)
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(mapEndpoints);
                });
            }
        });
    }

    /// <summary>Creates a GrpcChannel backed by the in-memory test server.</summary>
    public static GrpcChannel CreateGrpcChannel<TProgram>(
        this WebApplicationFactory<TProgram> factory) where TProgram : class
    {
        var client = factory.CreateDefaultClient();
        return GrpcChannel.ForAddress(
            client.BaseAddress ?? throw new InvalidOperationException("BaseAddress is null"),
            new GrpcChannelOptions { HttpClient = client });
    }
}
```

#### gRPC Service Replacement
```csharp
namespace Rig.TUnit.Grpc.Extensions;

public static class GrpcServiceReplacementExtensions
{
    /// <summary>
    /// Replaces a gRPC client registration to route through the test server.
    /// The test server must have a fake implementation mapped.
    /// </summary>
    public static IServiceCollection ReplaceGrpcClient<TClient>(
        this IServiceCollection services,
        WebApplicationFactory<Program> factory) where TClient : ClientBase<TClient>
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TClient));
        if (descriptor != null) services.Remove(descriptor);

        services.AddGrpcClient<TClient>(options =>
        {
            options.Creator = _ =>
                Activator.CreateInstance(typeof(TClient), factory.CreateGrpcChannel()) as TClient
                ?? throw new InvalidOperationException($"Cannot create {typeof(TClient).FullName}");
        });

        return services;
    }
}
```

#### HandlerHelper (Generic)
```csharp
namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Dispatches events/commands directly via MediatR, bypassing the Service Bus.
/// Used for Query and Processor handler testing.
/// </summary>
public sealed class HandlerHelper(IServiceProvider provider)
{
    public async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    public async Task<bool> HandleEvent<T>(Event<T> @event)
    {
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(@event);
    }
}
```

### 5.3 Rig.TUnit.SqlServer

#### SqlServerFixture
```csharp
namespace Rig.TUnit.SqlServer.Fixtures;

/// <summary>
/// TUnit-compatible SQL Server container fixture.
/// Use with [ClassDataSource&lt;SqlServerFixture&gt;(Shared = SharedType.PerTestSession)]
/// for a single container shared across all tests.
/// </summary>
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

#### DbContextHelper<TContext> (Generic)
```csharp
namespace Rig.TUnit.SqlServer.Helpers;

/// <summary>
/// Provides scoped database operations for test assertions and data seeding.
/// Creates a new DI scope per operation to prevent DbContext sharing issues.
/// </summary>
public sealed class DbContextHelper<TContext>(IServiceProvider provider) where TContext : DbContext
{
    /// <summary>Executes a query against the DbContext within a fresh scope.</summary>
    public async Task<TResult> Query<TResult>(Func<TContext, Task<TResult>> query)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        return await query(context);
    }

    /// <summary>Inserts an entity and clears the change tracker.</summary>
    public async Task<T> InsertAsync<T>(T entity) where T : class
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return entity;
    }

    /// <summary>Inserts multiple entities and clears the change tracker.</summary>
    public async Task<List<T>> InsertAsync<T>(List<T> entities) where T : class
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Set<T>().AddRangeAsync(entities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return entities;
    }
}
```

#### InMemory DB Extensions (for unit tests — no containers)
```csharp
namespace Rig.TUnit.SqlServer.Extensions;

public static class InMemoryDbExtensions
{
    /// <summary>
    /// Replaces DbContext with an InMemory database.
    /// Each call uses a unique database name for parallel test isolation.
    /// </summary>
    public static IServiceCollection UseInMemoryDatabase<TContext>(
        this IServiceCollection services) where TContext : DbContext
    {
        services.RemoveByName(typeof(TContext).Name);

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase($"test_{Guid.NewGuid():N}"));

        return services;
    }
}
```

#### SQL Server Container Extensions (for live tests)
```csharp
namespace Rig.TUnit.SqlServer.Extensions;

public static class SqlServerContainerExtensions
{
    /// <summary>
    /// Replaces DbContext with a real SQL Server backed by a TestContainers instance.
    /// Automatically runs EF Core migrations.
    /// </summary>
    public static IServiceCollection UseSqlServerContainer<TContext>(
        this IServiceCollection services,
        SqlServerFixture fixture) where TContext : DbContext
    {
        services.RemoveByName(typeof(TContext).Name);

        services.AddDbContext<TContext>(options =>
            options.UseSqlServer(fixture.ConnectionString));

        // Migrate on first use
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Database.Migrate();

        return services;
    }

    /// <summary>
    /// Uses a unique database on the shared SQL Server container.
    /// Provides true per-class isolation for parallel execution.
    /// </summary>
    public static IServiceCollection UseSqlServerContainerIsolated<TContext>(
        this IServiceCollection services,
        SqlServerFixture fixture) where TContext : DbContext
    {
        services.RemoveByName(typeof(TContext).Name);

        var dbName = $"test_{Guid.NewGuid():N}";
        var connectionString = $"{fixture.ConnectionString};Database={dbName}";

        services.AddDbContext<TContext>(options =>
            options.UseSqlServer(connectionString));

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Database.Migrate();

        return services;
    }
}
```

### 5.4 Rig.TUnit.ServiceBus

#### ServiceBusFixture
```csharp
namespace Rig.TUnit.ServiceBus.Fixtures;

/// <summary>
/// TUnit-compatible Azure Service Bus emulator fixture.
/// Requires a service-bus-config.json in the test project for topic/subscription setup.
/// </summary>
public sealed class ServiceBusFixture : IAsyncInitializer, IAsyncDisposable
{
    private ServiceBusContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Service Bus container not started");

    /// <summary>Path to service-bus-config.json (defaults to project root).</summary>
    public string ConfigFilePath { get; init; } = "service-bus-config.json";

    public async Task InitializeAsync()
    {
        _container = new ServiceBusBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithConfig(configFilePath: ConfigFilePath)
            .WithAcceptLicenseAgreement(true)
            .Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }
}
```

#### ListenerHelper
```csharp
namespace Rig.TUnit.ServiceBus.Helpers;

/// <summary>
/// Captures messages from an Azure Service Bus topic subscription during live tests.
/// </summary>
public sealed class ListenerHelper : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSessionProcessor _processor;
    private readonly List<ServiceBusReceivedMessage> _messages = [];

    public IReadOnlyList<ServiceBusReceivedMessage> Messages => _messages;

    public ListenerHelper(string connectionString, string topicName, string subscriptionName)
    {
        _client = new ServiceBusClient(connectionString);

        var options = new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = 1,
            MaxConcurrentCallsPerSession = 1,
            MaxConcurrentSessions = 100,
        };

        _processor = _client.CreateSessionProcessor(topicName, subscriptionName, options);
        _processor.ProcessMessageAsync += OnMessage;
        _processor.ProcessErrorAsync += OnError;
    }

    public async Task StartAsync() => await _processor.StartProcessingAsync();

    public async Task StopAsync() => await _processor.StopProcessingAsync();

    public async ValueTask DisposeAsync()
    {
        await _processor.CloseAsync();
        await _client.DisposeAsync();
    }

    /// <summary>
    /// Waits until at least one message is received, with timeout.
    /// Replaces Task.Delay() pattern.
    /// </summary>
    public async Task WaitForMessagesAsync(
        int expectedCount = 1,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (_messages.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(250, ct);
        }
    }

    private async Task OnMessage(ProcessSessionMessageEventArgs args)
    {
        _messages.Add(args.Message);
        await args.CompleteMessageAsync(args.Message);
    }

    private Task OnError(ProcessErrorEventArgs args) =>
        Task.FromResult(args.Exception); // Log but don't throw
}
```

### 5.5 Rig.TUnit.Redis

#### RedisFixture
```csharp
namespace Rig.TUnit.Redis.Fixtures;

public sealed class RedisFixture : IAsyncInitializer, IAsyncDisposable
{
    public RedisContainer Container { get; } = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

---

## 6. Developer Experience — Usage Examples

### 6.1 Command Service — Unit Test (No Containers)

```xml
<!-- Anis.MeterCharger.Commands.Tests.csproj -->
<ItemGroup>
    <PackageReference Include="Rig.TUnit.Core" Version="1.0.0" />
    <PackageReference Include="Rig.TUnit.Grpc" Version="1.0.0" />
    <PackageReference Include="Rig.TUnit.SqlServer" Version="1.0.0" />
</ItemGroup>
```

```csharp
using Rig.TUnit.Core.Fakers;
using Rig.TUnit.Grpc.Helpers;
using Rig.TUnit.Grpc.Extensions;
using Rig.TUnit.SqlServer.Helpers;
using Rig.TUnit.SqlServer.Extensions;

namespace Anis.MeterCharger.Commands.Tests.Tests.Meter;

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
                mapEndpoints: endpoints =>
                {
                    endpoints.MapGrpcService<MeterChargerCommandsService>();
                });

        _db = new DbContextHelper<MeterChargerCommandsDbContext>(factory.Services);
        _grpc = new GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient>(factory);
    }

    [Test]
    public async Task CreateMeter_ValidData_ReturnsMeterCreated()
    {
        var request = new CreateMeterRequestFaker().Generate();

        var response = await _grpc.Send(c => c.CreateMeterAsync(request));
        var @event = await _db.Query(db => db.Events.OfType<MeterCreated>().SingleOrDefaultAsync());

        await Assert.That(@event).IsNotNull();
        await Assert.That(response.Message).IsEqualTo(Phrases.MeterCreated);
    }
}
```

### 6.2 Command Service — Live Test (With Containers)

```xml
<!-- Anis.MeterCharger.Commands.Tests.Live.csproj -->
<ItemGroup>
    <PackageReference Include="Rig.TUnit.Core" Version="1.0.0" />
    <PackageReference Include="Rig.TUnit.Grpc" Version="1.0.0" />
    <PackageReference Include="Rig.TUnit.SqlServer" Version="1.0.0" />
    <PackageReference Include="Rig.TUnit.ServiceBus" Version="1.0.0" />
</ItemGroup>
```

```csharp
using Rig.TUnit.SqlServer.Fixtures;
using Rig.TUnit.SqlServer.Extensions;
using Rig.TUnit.ServiceBus.Helpers;
using Rig.TUnit.Grpc.Extensions;

namespace Anis.MeterCharger.Commands.Tests.Live.Tests.Meter;

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
            .WithTestConfiguration(
                configureServices: services =>
                {
                    services.UseSqlServerContainerIsolated<MeterChargerCommandsDbContext>(Sql);
                    // Keep real ServiceBusPublisher for live tests
                },
                mapEndpoints: endpoints =>
                {
                    endpoints.MapGrpcService<MeterChargerCommandsService>();
                });

        _db = new DbContextHelper<MeterChargerCommandsDbContext>(factory.Services);
        _grpc = new GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient>(factory);
    }

    [Test]
    public async Task CreateMeter_ValidData_PersistsToRealDatabase()
    {
        var request = new CreateMeterRequestFaker().Generate();

        var response = await _grpc.Send(c => c.CreateMeterAsync(request));
        var @event = await _db.Query(db => db.Events.OfType<MeterCreated>().SingleOrDefaultAsync());
        var outbox = await _db.Query(db => db.OutboxMessages.SingleOrDefaultAsync());

        await Assert.That(@event).IsNotNull();
        await Assert.That(outbox).IsNotNull();
    }
}
```

### 6.3 Query Service — Handler Test (No Containers)

```csharp
using Rig.TUnit.Grpc.Helpers;
using Rig.TUnit.SqlServer.Helpers;
using Rig.TUnit.SqlServer.Extensions;

namespace Anis.MeterCharger.Queries.Tests.Tests.Meter;

public class MeterCreatedHandlerTest
{
    private readonly DbContextHelper<MeterChargerQueriesDbContext> _db;
    private readonly HandlerHelper _handler;

    public MeterCreatedHandlerTest()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithTestConfiguration(configureServices: services =>
            {
                services.UseInMemoryDatabase<MeterChargerQueriesDbContext>();
                services.RemoveImplementation<MeterChargerListener>(); // remove SB listener
            });

        _db = new DbContextHelper<MeterChargerQueriesDbContext>(factory.Services);
        _handler = new HandlerHelper(factory.Services);
    }

    [Test]
    public async Task MeterCreated_NewMeter_ProjectsToReadModel()
    {
        var @event = new MeterCreatedEventFaker().Generate();

        var result = await _handler.HandleEvent(@event);
        var meter = await _db.Query(db => db.Meters.FirstOrDefaultAsync());

        await Assert.That(result).IsTrue();
        await Assert.That(meter).IsNotNull();
    }
}
```

### 6.4 Processor Service — Handler Test (No DB, No Containers)

```csharp
namespace Anis.MeterCharger.Processor.Tests.Tests;

public class MeterCreatedProcessorTest
{
    private readonly HandlerHelper _handler;
    private readonly FakeServicesData _fakeData;

    public MeterCreatedProcessorTest()
    {
        _fakeData = new FakeServicesDataFaker().Generate();

        var factory = new WebApplicationFactory<Program>()
            .WithTestConfiguration(
                configureServices: services =>
                {
                    services.RemoveImplementation<MeterChargerListener>();
                    services.AddSingleton(_fakeData);
                },
                mapEndpoints: endpoints =>
                {
                    endpoints.MapGrpcService<FakeExternalCommandsService>();
                });

        _handler = new HandlerHelper(factory.Services);
    }

    [Test]
    public async Task MeterCreated_ValidEvent_CallsExternalService()
    {
        var @event = new MeterCreatedEventFaker().Generate();

        var result = await _handler.HandleEvent(@event);

        await Assert.That(result).IsTrue();
        await Assert.That(_fakeData.CapturedRequest).IsNotNull();
    }
}
```

---

## 7. Comparison: Old vs New

| Aspect | Old (Anis.TestContainers.Common) | New (Rig.TUnit.*) |
|--------|----------------------------------|----------------------|
| **Framework** | xUnit only | TUnit only |
| **Container management** | IContainerFactory → IContainerManager → ITestContainer → SharedTestContainersFixture → ICollectionFixture | `SqlServerFixture` + `[ClassDataSource(PerTestSession)]` |
| **Container selection** | `[RequiresContainers]` attribute → `EnsureContainersStartedAsync()` | Install the package you need (SqlServer, Redis, ServiceBus) |
| **Fixture sharing** | `ICollectionFixture<SharedTestContainersFixture>` + `[Collection]` | `[ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]` |
| **WebApplicationFactory** | `IClassFixture<WebApplicationFactory<Program>>` + custom extension | `new WebApplicationFactory<Program>().WithTestConfiguration()` |
| **DB for unit tests** | Real SQL Server via user secrets | `services.UseInMemoryDatabase<TContext>()` (zero infrastructure) |
| **DB for live tests** | Container + shared DB + truncation | `services.UseSqlServerContainerIsolated<TContext>()` (unique DB per class) |
| **Parallelism** | `DisableTestParallelization = true` or xUnit collection serial | Full parallel by default, `[NotInParallel]` where needed |
| **Async verification** | `Task.Delay(10000-20000)` | `listener.WaitForMessagesAsync(timeout: 15s)` or `Assert.That().Within()` |
| **Logging** | `Serilog.Sinks.XUnit` + `ITestOutputHelper` | `WriteTo.Console()` (TUnit captures stdout) |
| **Coupling** | References `Anis.Catalogue.Grpc`, `Program`, `ApplicationDbContext` | Generic types: `DbContextHelper<TContext>`, `GrpcClientHelper<TClient>` |
| **Reusability** | Single project, hardcoded to catalogue | Standalone NuGet packages, works with any service |
| **DbContextHelper** | Hardcoded `ApplicationDbContext` | Generic `DbContextHelper<TContext>` |
| **GrpcClientHelper** | Hardcoded client types with per-method Send | Generic `GrpcClientHelper<TClient>` with `Send<TResult>()` |
| **Lines of setup per test class** | ~15 (constructor boilerplate) | ~10 (constructor + WithTestConfiguration) |
| **Abstractions to learn** | 10+ types | 3-4 types (Fixture, DbHelper, GrpcHelper, HandlerHelper) |

---

## 8. Package Versioning Strategy

| Package | Initial Version | Follows SemVer? |
|---------|----------------|-----------------|
| `Rig.TUnit.Core` | 1.0.0 | Yes |
| `Rig.TUnit.Grpc` | 1.0.0 | Yes |
| `Rig.TUnit.SqlServer` | 1.0.0 | Yes |
| `Rig.TUnit.Redis` | 1.0.0 | Yes |
| `Rig.TUnit.ServiceBus` | 1.0.0 | Yes |
| `Rig.TUnit` | 1.0.0 | Yes — version matches highest dependency |

All packages version together (same version number across all packages per release).

---

## 9. NuGet Dependencies Per Package

### Rig.TUnit.Core
```xml
<PackageReference Include="TUnit.Core" Version="1.33.0" />
<PackageReference Include="Bogus" Version="35.6.1" />
```

### Rig.TUnit.Grpc
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
> `TUnit.AspNetCore` brings `TestWebApplicationFactory<T>` and `WebApplicationTest<TFactory, T>` with per-test isolation.

### Rig.TUnit.SqlServer
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.MsSql" Version="4.6.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

### Rig.TUnit.Redis
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.Redis" Version="4.6.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

### Rig.TUnit.ServiceBus
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<PackageReference Include="Testcontainers.ServiceBus" Version="4.6.0" />
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Rig.TUnit (meta-package)
```xml
<ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
<ProjectReference Include="..\Rig.TUnit.Grpc\Rig.TUnit.Grpc.csproj" />
<ProjectReference Include="..\Rig.TUnit.SqlServer\Rig.TUnit.SqlServer.csproj" />
<ProjectReference Include="..\Rig.TUnit.Redis\Rig.TUnit.Redis.csproj" />
<ProjectReference Include="..\Rig.TUnit.ServiceBus\Rig.TUnit.ServiceBus.csproj" />
```

---

## 10. Implementation Order

### Phase 1: Core + Grpc (Week 1)
1. Create `Rig.TUnit.Core` — CustomConstructorFaker, ServiceRemoval, EnvironmentDetection
2. Create `Rig.TUnit.Grpc` — GrpcClientHelper<T>, HandlerHelper, WebApplicationFactory extensions, MetadataHelper
3. Write tests for both packages
4. Apply to MeterCharger.Commands.Tests (unit tests, no containers)

### Phase 2: SqlServer (Week 1-2)
1. Create `Rig.TUnit.SqlServer` — SqlServerFixture, DbContextHelper<T>, InMemory + Container extensions
2. Write tests
3. Apply to MeterCharger.Commands.Tests.Live

### Phase 3: ServiceBus + Redis (Week 2)
1. Create `Rig.TUnit.ServiceBus` — ServiceBusFixture, ListenerHelper, MessageAssert base
2. Create `Rig.TUnit.Redis` — RedisFixture, CacheHelper
3. Write tests
4. Apply to live test projects

### Phase 4: Meta-package + NuGet (Week 2-3)
1. Create `Rig.TUnit` meta-package
2. Set up NuGet packaging (private feed or GitHub Packages)
3. Apply to Query and Processor services
4. Update MeterCharger test projects to use NuGet references instead of project references

---

## 11. Repo Structure

```
Ecom-LTD/
  anis.testing/                    ← NEW REPO
    src/
      Rig.TUnit.Core/
      Rig.TUnit.Grpc/
      Rig.TUnit.SqlServer/
      Rig.TUnit.Redis/
      Rig.TUnit.ServiceBus/
      Rig.TUnit/               ← meta-package
    tests/
      Rig.TUnit.Core.Tests/
      Rig.TUnit.Grpc.Tests/
      Rig.TUnit.SqlServer.Tests/
    .github/
      workflows/
        ci.yaml                   ← build + test + publish NuGet
    Rig.TUnit.slnx
    README.md
    nuget.config                  ← private feed configuration
```

---

## 12. What Developers Need to Know (Cheat Sheet)

### For Unit Tests (*.Tests)
```
1. Install: Rig.TUnit.Core + Rig.TUnit.Grpc + Rig.TUnit.SqlServer (if DB)
2. Create WebApplicationFactory with .WithTestConfiguration()
3. Use services.UseInMemoryDatabase<TContext>() — no SQL Server needed
4. Use services.RemoveService<IServiceBusPublisher>() + FakeServiceBusPublisher
5. Create helpers: DbContextHelper<TContext>, GrpcClientHelper<TClient>, HandlerHelper
6. Write [Test] methods with await Assert.That()
7. Everything runs in parallel automatically
```

### For Live Tests (*.Tests.Live)
```
1. Additionally install: Rig.TUnit.SqlServer and/or Rig.TUnit.ServiceBus
2. Add [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)] property
3. Use services.UseSqlServerContainerIsolated<TContext>(fixture)
4. For Service Bus: add service-bus-config.json, use ListenerHelper
5. Use [NotInParallel("ServiceBus")] for SB tests
6. Use listener.WaitForMessagesAsync() instead of Task.Delay()
```

### What NOT to Do
```
- Do NOT install Microsoft.NET.Test.Sdk (conflicts with TUnit)
- Do NOT install coverlet.collector (conflicts with TUnit)
- Do NOT use [assembly: CollectionBehavior(...)] (xUnit concept)
- Do NOT use IClassFixture or ICollectionFixture (xUnit concept)
- Do NOT use Task.Delay() for waiting — use WaitForMessagesAsync or Assert.That().Within()
- Do NOT share WebApplicationFactory across test classes
- Do NOT forget to await Assert.That() — silent pass!
```

---

## 13. TUnit.AspNetCore Integration (Critical Pattern)

TUnit has an official `TUnit.AspNetCore` package that provides per-test WebApplicationFactory isolation.
This is the foundation our library should build on.

### How TUnit.AspNetCore Works

```csharp
// 1. Container fixture — shared across ALL tests in session
public class SqlContainer : IAsyncInitializer, IAsyncDisposable
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();
    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

// 2. Factory — gets container injected via nested ClassDataSource
//    TUnit resolves: SqlContainer → AppFactory (automatic dependency order)
public class AppFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<SqlContainer>(Shared = SharedType.PerTestSession)]
    public required SqlContainer Sql { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", Sql.Container.GetConnectionString() }
            });
        });
    }
}

// 3. Test base — provides Factory property, per-test isolation
public abstract class IntegrationTestBase : WebApplicationTest<AppFactory, Program>
{
    [ClassDataSource<SqlContainer>(Shared = SharedType.PerTestSession)]
    public required SqlContainer Sql { get; init; }

    protected string SchemaName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        SchemaName = GetIsolatedName("schema");  // "Test_42_schema" unique per test
        // Create per-test schema and run migrations
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:Schema", SchemaName }
        });
    }
}

// 4. Actual test — inherits base, gets Factory automatically
public class OrderTests : IntegrationTestBase
{
    [Test]
    public async Task CreateOrder_ReturnsCreated()
    {
        var client = Factory.CreateClient();  // Per-test isolated factory instance
        var response = await client.PostAsync("/orders", content);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }
}
```

### WebApplicationTest Lifecycle Order

1. `ConfigureTestOptions` — enable features (logging, OpenTelemetry, HTTP capture)
2. `SetupAsync` — async per-test initialization (create schemas, seed data)
3. Factory `ConfigureWebHost` / `ConfigureStartupConfiguration`
4. `ConfigureWebHostBuilder` — low-level WebHost access
5. `ConfigureTestConfiguration` — per-test config overrides (overrides factory)
6. `ConfigureTestServices` — per-test DI overrides
7. Application startup
8. **Test execution**
9. Factory disposal

### How This Changes Our Library Design

The `TUnit.AspNetCore` package handles most of what `Rig.TUnit.Grpc` was going to do:
- WebApplicationFactory lifecycle → handled by `TestWebApplicationFactory`
- Per-test isolation → handled by `WebApplicationTest` + `GetIsolatedName()`
- Logging → auto-configured to test output
- Factory property → auto-injected via base class

**What our library still provides:**
- Container fixtures (SqlServerFixture, RedisFixture, ServiceBusFixture)
- Generic helpers (DbContextHelper<T>, GrpcClientHelper<T>, HandlerHelper)
- CustomConstructorFaker<T>
- Service replacement extensions (gRPC client replacement, ServiceBus removal)
- ListenerHelper for Service Bus message capture
- Per-service base classes that combine everything

### Revised Package Dependency Graph

```
TUnit.AspNetCore (official, from NuGet)
    ↑
Rig.TUnit.Core ← CustomConstructorFaker, service removal, env detection
    ↑
    ├── Rig.TUnit.Grpc ← GrpcClientHelper<T>, HandlerHelper, gRPC replacement, MetadataHelper
    │
    ├── Rig.TUnit.SqlServer ← SqlServerFixture, DbContextHelper<T>, InMemory + Container extensions
    │
    ├── Rig.TUnit.Redis ← RedisFixture, CacheHelper
    │
    └── Rig.TUnit.ServiceBus ← ServiceBusFixture, ListenerHelper, MessageAssert base
```

### Updated Rig.TUnit.Grpc (uses TUnit.AspNetCore)

```xml
<PackageReference Include="TUnit.AspNetCore" Version="*" />
```

```csharp
namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Creates typed gRPC clients routed through the test server.
/// Works with both raw WebApplicationFactory and TUnit.AspNetCore TestWebApplicationFactory.
/// </summary>
public sealed class GrpcClientHelper<TClient>(IHttpClientFactory factory) 
    where TClient : ClientBase<TClient>
{
    // Alternative: takes WebApplicationFactory directly
    public GrpcClientHelper(WebApplicationFactory<Program> factory)
        : this(CreateChannel(factory)) { }

    private readonly GrpcChannel _channel;
    
    private GrpcClientHelper(GrpcChannel channel) => _channel = channel;

    public TResult Send<TResult>(Func<TClient, TResult> action)
    {
        var client = (TClient)Activator.CreateInstance(typeof(TClient), _channel)!;
        return action(client);
    }

    private static GrpcChannel CreateChannel(WebApplicationFactory<Program> factory)
    {
        var httpClient = factory.CreateDefaultClient();
        return GrpcChannel.ForAddress(
            httpClient.BaseAddress ?? throw new InvalidOperationException(),
            new GrpcChannelOptions { HttpClient = httpClient });
    }
}
```

### Complete Live Test Example with TUnit.AspNetCore

```csharp
// AppFactory.cs — shared, configures containers
public class MeterChargerTestFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public required SqlServerFixture Sql { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.UseSqlServerContainer<MeterChargerCommandsDbContext>(Sql);
        });
    }
}

// Base class — per-test schema isolation
public abstract class MeterChargerLiveTestBase : WebApplicationTest<MeterChargerTestFactory, Program>
{
    protected DbContextHelper<MeterChargerCommandsDbContext> Db { get; private set; } = null!;
    protected GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient> Grpc { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        Db = new DbContextHelper<MeterChargerCommandsDbContext>(Factory.Services);
        Grpc = new GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient>(Factory);
    }
}

// Actual test — clean, minimal, focused
public class CreateMeterLiveTest : MeterChargerLiveTestBase
{
    [Test]
    public async Task CreateMeter_ValidData_PersistsEventAndOutbox()
    {
        var request = new CreateMeterRequestFaker().Generate();

        var response = await Grpc.Send(c => c.CreateMeterAsync(request));
        var @event = await Db.Query(db => db.Events.OfType<MeterCreated>().SingleOrDefaultAsync());
        var outbox = await Db.Query(db => db.OutboxMessages.SingleOrDefaultAsync());

        await Assert.That(@event).IsNotNull();
        await Assert.That(outbox).IsNotNull();
        await Assert.That(response.Message).IsEqualTo(Phrases.MeterCreated);
    }
}
```

### Complete Unit Test Example (No Containers, No TUnit.AspNetCore)

```csharp
// For unit tests, we don't need TUnit.AspNetCore — raw WebApplicationFactory is fine
public class CreateMeterTest
{
    private readonly DbContextHelper<MeterChargerCommandsDbContext> _db;
    private readonly GrpcClientHelper<MeterChargerCommands.MeterChargerCommandsClient> _grpc;

    public CreateMeterTest()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.UseInMemoryDatabase<MeterChargerCommandsDbContext>();
                    services.RemoveService<IServiceBusPublisher>();
                    services.AddSingleton<IServiceBusPublisher, FakeServiceBusPublisher>();
                });
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapGrpcService<MeterChargerCommandsService>());
                });
            });

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
    }
}
```
