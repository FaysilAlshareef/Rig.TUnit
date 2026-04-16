# Rig.TUnit — Fluent Builder, Mediator, WebAPI & Configuration Design

## 1. Vision

Expand Rig.TUnit from a container-fixture library into a **complete test infrastructure platform** with:

- **Fluent Builder API** (`RigBuilder`) — single entry point to configure all test infrastructure
- **Dual-mode connections** — Testcontainers OR external services via `IConfiguration`/`IOptions<T>`
- **Mediator package** — extract HandlerHelper from Grpc, replace MediatR with martinothamar/Mediator (source-generated, free, AOT-ready)
- **WebAPI package** — HTTP client/server testing without gRPC dependency
- **Reusable utilities** — WaitHelper, TestConfigurationBuilder, seed support, fixture composition

### Design Principles

1. **Fluent-first (FB methodology)** — every public API returns a builder or `IServiceCollection` for chaining
2. **Connection-source agnostic** — container fixtures AND manual connection strings through same API
3. **CI/CD enforcement** — `Auto()` mode uses containers in CI, external locally
4. **Clean replacement** — old standalone extension methods are removed; fluent builder is the single API
5. **Minimal dependencies per package** — user installs only what they need

---

## 1b. Existing Library (What's Already Built)

The base Rig.TUnit library is complete with 56 tests. This is what exists today:

| Package | Contents |
|---------|----------|
| `Rig.TUnit.Core` | `CustomConstructorFaker<T>`, `ServiceRemovalExtensions` (RemoveService, RemoveImplementation, RemoveByName), `EnvironmentDetection` |
| `Rig.TUnit.Grpc` | `GrpcClientHelper<TClient, TProgram>`, `HandlerHelper` (MediatR), `MetadataHelper`, `WebApplicationFactoryExtensions`, `GrpcServiceReplacementExtensions` |
| `Rig.TUnit.SqlServer` | `SqlServerFixture`, `DbContextHelper<TContext>`, `InMemoryDbExtensions`, `SqlServerContainerExtensions` |
| `Rig.TUnit.Redis` | `RedisFixture`, `RedisContainerExtensions` |
| `Rig.TUnit.ServiceBus` | `ServiceBusFixture`, `ListenerHelper`, `ServiceBusEventSender`, `ServiceBusContainerExtensions` |
| `Rig.TUnit` | Meta-package referencing all above |

**Current extension method APIs** (these get replaced by the fluent builder — remove after builder is implemented):
- `services.UseSqlServerContainerIsolated<TContext>(fixture)`
- `services.UseInMemoryDatabase<TContext>()`
- `services.UseRedisContainer(fixture)`
- `services.UseServiceBusContainer(fixture)`
- `services.ReplaceGrpcClient<TClient, TProgram>(fixture, factory)`

**Key versions**: net10.0, TUnit 1.34.5, Testcontainers 4.6.0, MediatR 12.4.1 (to be replaced), EF Core 10.0.0

---

## 2. Package Structure

```
Rig.TUnit/
├── Rig.TUnit.Core/              → RigBuilder, RigConnect, WaitHelper, TestConfigBuilder,
│                                   CompositeFixture, Fakers, ServiceRemoval, EnvDetection
├── Rig.TUnit.Mediator/    [NEW] → HandlerHelper (extracted from Grpc), uses martinothamar/Mediator
├── Rig.TUnit.Grpc/              → GrpcClientHelper, MetadataHelper, WebAppFactory exts
│                                   (depends on Rig.TUnit.Mediator for HandlerHelper re-export)
├── Rig.TUnit.WebAPI/      [NEW] → HttpClientHelper, WebAPI testing without gRPC
├── Rig.TUnit.SqlServer/         → SqlServerFixture, DbContextHelper (with seed), InMemoryDb,
│                                   SqlServerContainerExts, SqlServerBuilder (fluent)
├── Rig.TUnit.Redis/             → RedisFixture, RedisContainerExts, RedisBuilder (fluent)
├── Rig.TUnit.ServiceBus/        → ServiceBusFixture (custom config), ListenerHelper,
│                                   ServiceBusEventSender, ServiceBusBuilder (fluent)
├── Rig.TUnit/                   → Meta-package referencing all above
```

### Dependency Graph

```
                    ┌─────────────────┐
                    │  Rig.TUnit.Core │
                    └───────┬─────────┘
              ┌─────────────┼──────────────┬──────────────┐
              │             │              │              │
    ┌─────────▼───┐  ┌──────▼──────┐  ┌───▼────┐  ┌─────▼──────┐
    │  SqlServer  │  │  Mediator   │  │ Redis  │  │ ServiceBus │
    └─────────────┘  └──────┬──────┘  └────────┘  └────────────┘
                     ┌──────┴──────┐
               ┌─────▼─────┐ ┌────▼────┐
               │    Grpc   │ │  WebAPI  │
               └───────────┘ └─────────┘
                     │             │
                     └──────┬──────┘
                      ┌─────▼─────┐
                      │ Rig.TUnit │  (meta-package)
                      └───────────┘
```

---

## 3. Core Infrastructure — RigBuilder & RigConnect

### 3.1 IRigConnectionSource

The fundamental abstraction: all infrastructure needs a connection string, regardless of where it comes from.

```csharp
namespace Rig.TUnit.Core.Builder;

/// <summary>
/// Provides a connection string from any source — container fixture, configuration, or raw value.
/// </summary>
public interface IRigConnectionSource
{
    string ConnectionString { get; }
}
```

All existing fixtures (`SqlServerFixture`, `RedisFixture`, `ServiceBusFixture`) implement this interface. This is the only change to existing fixture classes.

### 3.2 RigConnect — Static Factory

```csharp
namespace Rig.TUnit.Core.Builder;

public static class RigConnect
{
    /// <summary>From a container fixture (Testcontainers).</summary>
    public static IRigConnectionSource FromContainer(IRigConnectionSource fixture)
        => fixture;

    /// <summary>From an IConfiguration key (e.g., "ConnectionStrings:OrderDb").</summary>
    public static IRigConnectionSource FromConfig(IConfiguration configuration, string key)
        => new ConfigConnectionSource(configuration, key);

    /// <summary>From an IOptions&lt;T&gt; property selector.</summary>
    public static IRigConnectionSource FromOptions<TOptions>(
        IOptions<TOptions> options,
        Func<TOptions, string> selector) where TOptions : class
        => new OptionsConnectionSource<TOptions>(options, selector);

    /// <summary>From a raw connection string value.</summary>
    public static IRigConnectionSource FromValue(string connectionString)
        => new ValueConnectionSource(connectionString);

    /// <summary>
    /// Smart mode: uses container in CI/CD, falls back to configuration locally.
    /// CI detection uses EnvironmentDetection.IsRunningInCiCd().
    /// </summary>
    public static IRigConnectionSource Auto(
        IRigConnectionSource fixture,
        IConfiguration configuration,
        string configKey)
        => new AutoConnectionSource(fixture, configuration, configKey);
}
```

### 3.3 Connection Source Implementations

```csharp
// Internal — users interact via RigConnect static factory
internal sealed class ConfigConnectionSource(IConfiguration configuration, string key) 
    : IRigConnectionSource
{
    public string ConnectionString => configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration key '{key}' not found. Ensure it exists in appsettings or user secrets.");
}

internal sealed class OptionsConnectionSource<TOptions>(
    IOptions<TOptions> options, Func<TOptions, string> selector) 
    : IRigConnectionSource where TOptions : class
{
    public string ConnectionString => selector(options.Value)
        ?? throw new InvalidOperationException(
            $"Options selector for {typeof(TOptions).Name} returned null.");
}

internal sealed class ValueConnectionSource(string connectionString) : IRigConnectionSource
{
    public string ConnectionString { get; } = connectionString
        ?? throw new ArgumentNullException(nameof(connectionString));
}

internal sealed class AutoConnectionSource(
    IRigConnectionSource fixture,
    IConfiguration configuration,
    string configKey) : IRigConnectionSource
{
    public string ConnectionString
    {
        get
        {
            // CI/CD: always use container for reproducibility
            if (EnvironmentDetection.IsRunningInCiCd())
                return fixture.ConnectionString;

            // Local: prefer config if available, fallback to container
            var configValue = configuration[configKey];
            return !string.IsNullOrEmpty(configValue) ? configValue : fixture.ConnectionString;
        }
    }
}
```

### 3.4 RigBuilder — Fluent Entry Point

```csharp
namespace Rig.TUnit.Core.Builder;

public sealed class RigBuilder
{
    private readonly IServiceCollection _services;
    private bool _forceContainersInCi;

    internal RigBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// When CI is detected, throws if any infrastructure is configured without a container fixture.
    /// </summary>
    public RigBuilder ForceContainersInCi()
    {
        _forceContainersInCi = true;
        return this;
    }

    internal IServiceCollection Services => _services;
    internal bool IsForceContainersInCi => _forceContainersInCi;
}

// Entry point extension method
public static class RigBuilderExtensions
{
    public static IServiceCollection AddRigTUnit(
        this IServiceCollection services,
        Action<RigBuilder> configure)
    {
        var builder = new RigBuilder(services);
        configure(builder);
        return services;
    }
}
```

Each package adds its own extension methods on `RigBuilder`. This is the **extension point pattern** — packages extend the builder without Core knowing about them.

---

## 4. Package-Specific Fluent Builders

### 4.1 SqlServer Builder

Lives in `Rig.TUnit.SqlServer`, extends `RigBuilder`:

```csharp
namespace Rig.TUnit.SqlServer.Builder;

public sealed class SqlServerRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _connectionSource;

    internal SqlServerRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _connectionSource = source;
    }

    /// <summary>Replace a DbContext registration with an isolated test database.</summary>
    public SqlServerRigBuilder ReplaceDbContext<TContext>() where TContext : DbContext
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        _services.RemoveByName(typeof(TContext).Name);
        _services.AddDbContext<TContext>(opts =>
            opts.UseSqlServer($"{_connectionSource.ConnectionString};Database={dbName}"));

        // Ensure database exists
        using var sp = _services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();

        return this;
    }

    /// <summary>Replace a DbContext with a custom connection string builder.</summary>
    public SqlServerRigBuilder ReplaceDbContext<TContext>(
        Func<string, string> connectionStringTransform) where TContext : DbContext
    {
        var transformed = connectionStringTransform(_connectionSource.ConnectionString);
        _services.RemoveByName(typeof(TContext).Name);
        _services.AddDbContext<TContext>(opts => opts.UseSqlServer(transformed));
        return this;
    }
}

// Extension on RigBuilder
public static class SqlServerRigBuilderExtensions
{
    /// <summary>Configure SQL Server with a container fixture.</summary>
    public static RigBuilder UseSqlServer(
        this RigBuilder builder,
        IRigConnectionSource connectionSource,
        Action<SqlServerRigBuilder> configure)
    {
        var sqlBuilder = new SqlServerRigBuilder(builder.Services, connectionSource);
        configure(sqlBuilder);
        return builder;
    }

    /// <summary>Shorthand: container fixture, no sub-builder needed for single DbContext.</summary>
    public static RigBuilder UseSqlServer<TContext>(
        this RigBuilder builder,
        IRigConnectionSource connectionSource) where TContext : DbContext
    {
        return builder.UseSqlServer(connectionSource, sql => sql.ReplaceDbContext<TContext>());
    }
}
```

### 4.2 Redis Builder

```csharp
namespace Rig.TUnit.Redis.Builder;

public sealed class RedisRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _connectionSource;

    internal RedisRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _connectionSource = source;
    }

    /// <summary>Replace IConnectionMultiplexer with test connection.</summary>
    public RedisRigBuilder ReplaceMultiplexer()
    {
        _services.RemoveService<IConnectionMultiplexer>();
        _services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(_connectionSource.ConnectionString));
        return this;
    }

    /// <summary>Replace a custom Redis wrapper with a factory delegate.</summary>
    public RedisRigBuilder ReplaceClient<TClient>(
        Func<IConnectionMultiplexer, TClient> factory) where TClient : class
    {
        _services.RemoveService<TClient>();
        _services.AddSingleton(sp =>
        {
            var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return factory(multiplexer);
        });
        return this;
    }
}

public static class RedisRigBuilderExtensions
{
    public static RigBuilder UseRedis(
        this RigBuilder builder,
        IRigConnectionSource connectionSource,
        Action<RedisRigBuilder>? configure = null)
    {
        var redisBuilder = new RedisRigBuilder(builder.Services, connectionSource);
        redisBuilder.ReplaceMultiplexer(); // always replace base multiplexer
        configure?.Invoke(redisBuilder);
        return builder;
    }
}
```

### 4.3 ServiceBus Builder

```csharp
namespace Rig.TUnit.ServiceBus.Builder;

public sealed class ServiceBusRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _connectionSource;

    internal ServiceBusRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _connectionSource = source;
    }

    /// <summary>Replace the base ServiceBusClient registration.</summary>
    public ServiceBusRigBuilder ReplaceClient()
    {
        _services.RemoveService<ServiceBusClient>();
        _services.AddSingleton(
            _ => new ServiceBusClient(_connectionSource.ConnectionString));
        return this;
    }

    /// <summary>
    /// Replace a custom ServiceBus wrapper class.
    /// The factory receives the container's connection string.
    /// </summary>
    public ServiceBusRigBuilder ReplaceClient<TClient>(
        Func<string, TClient> factory) where TClient : class
    {
        _services.RemoveService<TClient>();
        _services.AddSingleton(_ => factory(_connectionSource.ConnectionString));
        return this;
    }

    /// <summary>
    /// Replace a custom ServiceBus wrapper using an options-style factory.
    /// Receives both connection string and service provider for complex construction.
    /// </summary>
    public ServiceBusRigBuilder ReplaceClient<TClient>(
        Func<string, IServiceProvider, TClient> factory) where TClient : class
    {
        _services.RemoveService<TClient>();
        _services.AddSingleton(sp => factory(_connectionSource.ConnectionString, sp));
        return this;
    }
}

public static class ServiceBusRigBuilderExtensions
{
    public static RigBuilder UseServiceBus(
        this RigBuilder builder,
        IRigConnectionSource connectionSource,
        Action<ServiceBusRigBuilder> configure)
    {
        var sbBuilder = new ServiceBusRigBuilder(builder.Services, connectionSource);
        configure(sbBuilder);
        return builder;
    }
}
```

### 4.4 Grpc Builder

```csharp
namespace Rig.TUnit.Grpc.Builder;

public sealed class GrpcRigBuilder<TProgram> where TProgram : class
{
    private readonly IServiceCollection _services;
    private readonly WebApplicationFactory<TProgram> _factory;

    internal GrpcRigBuilder(IServiceCollection services, WebApplicationFactory<TProgram> factory)
    {
        _services = services;
        _factory = factory;
    }

    /// <summary>Replace a gRPC client to route through the in-memory test server.</summary>
    public GrpcRigBuilder<TProgram> ReplaceClient<TClient>()
        where TClient : ClientBase<TClient>
    {
        _services.RemoveService<TClient>();
        var channel = _factory.CreateGrpcChannel();
        var client = (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
        _services.AddSingleton(client);
        return this;
    }
}

public static class GrpcRigBuilderExtensions
{
    public static RigBuilder UseGrpc<TProgram>(
        this RigBuilder builder,
        WebApplicationFactory<TProgram> factory,
        Action<GrpcRigBuilder<TProgram>> configure) where TProgram : class
    {
        var grpcBuilder = new GrpcRigBuilder<TProgram>(builder.Services, factory);
        configure(grpcBuilder);
        return builder;
    }
}
```

---

## 5. Rig.TUnit.Mediator — New Package

### 5.1 Why Replace MediatR

MediatR v12+ is **commercially licensed** (requires paid license for commercial use).
martinothamar/Mediator is:
- **MIT licensed** (free forever)
- **Source-generator based** (faster, AOT-compatible, compile-time safety)
- **API-similar** to MediatR (easy migration)
- **ValueTask-based** (fewer allocations)

### 5.2 Package Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
    <PackageReference Include="Mediator.Abstractions" Version="3.0.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**Note**: Only `Mediator.Abstractions` is needed (interfaces only). The `Mediator.SourceGenerator` package is installed by the **consumer's test project**, not by Rig.TUnit.Mediator. The source generator must run in the outermost project that calls `AddMediator()`.

### 5.3 HandlerHelper (Extracted)

```csharp
namespace Rig.TUnit.Mediator.Helpers;

using global::Mediator;

/// <summary>
/// Dispatches Mediator requests within isolated DI scopes.
/// Simulates per-request lifetime for handler testing.
/// </summary>
public sealed class HandlerHelper(IServiceScopeFactory scopeFactory)
{
    /// <summary>Send a request through the Mediator pipeline in an isolated scope.</summary>
    public async ValueTask<TResult> Send<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, cancellationToken);
    }

    /// <summary>Send a command through the Mediator pipeline in an isolated scope.</summary>
    public async ValueTask<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>Send a query through the Mediator pipeline in an isolated scope.</summary>
    public async ValueTask<TResult> Send<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(query, cancellationToken);
    }

    /// <summary>Publish a notification to all handlers in an isolated scope.</summary>
    public async ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(notification, cancellationToken);
    }
}
```

### 5.4 Key Differences from Current HandlerHelper

| Aspect | Current (MediatR) | New (Mediator) |
|--------|-------------|---------------|
| Namespace | `Rig.TUnit.Grpc.Helpers` | `Rig.TUnit.Mediator.Helpers` |
| Package | Rig.TUnit.Grpc | Rig.TUnit.Mediator |
| Return type | `Task<TResult>` | `ValueTask<TResult>` |
| Supports | `IRequest<T>` only | `IRequest<T>`, `ICommand<T>`, `IQuery<T>`, `INotification` |
| License | MediatR commercial | Mediator MIT (free) |

### 5.5 Migration Path for Rig.TUnit.Grpc

`Rig.TUnit.Grpc` gains a project reference to `Rig.TUnit.Mediator`. The old `Rig.TUnit.Grpc.Helpers.HandlerHelper` is deleted — `HandlerHelper` lives only in `Rig.TUnit.Mediator.Helpers`.

---

## 6. Rig.TUnit.WebAPI — New Package

### 6.1 Purpose

Test HTTP/REST APIs without gRPC dependency. Uses `WebApplicationFactory` and `HttpClient`.

### 6.2 Package Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Rig.TUnit.Core\Rig.TUnit.Core.csproj" />
    <ProjectReference Include="..\Rig.TUnit.Mediator\Rig.TUnit.Mediator.csproj" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="TUnit.AspNetCore" Version="1.34.5" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.6" />
  </ItemGroup>
</Project>
```

### 6.3 HttpClientHelper

```csharp
namespace Rig.TUnit.WebAPI.Helpers;

/// <summary>
/// Creates typed HttpClient instances routed through the in-memory test server.
/// </summary>
public sealed class HttpClientHelper<TProgram> : IAsyncDisposable where TProgram : class
{
    private readonly WebApplicationFactory<TProgram> _factory;
    private HttpClient? _client;

    public HttpClientHelper(WebApplicationFactory<TProgram> factory)
    {
        _factory = factory;
    }

    /// <summary>Get or create the test HttpClient.</summary>
    public HttpClient Client => _client ??= _factory.CreateClient();

    /// <summary>Create a client with custom configuration.</summary>
    public HttpClient CreateClient(Action<WebApplicationFactoryClientOptions>? configure = null)
    {
        var options = new WebApplicationFactoryClientOptions();
        configure?.Invoke(options);
        return _factory.CreateClient(options);
    }

    /// <summary>Send a GET request and deserialize the JSON response.</summary>
    public async Task<TResponse?> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
    }

    /// <summary>Send a POST request with JSON body and deserialize the response.</summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync(requestUri, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
    }

    /// <summary>Send a PUT request with JSON body.</summary>
    public async Task<HttpResponseMessage> PutAsync<TRequest>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        return await Client.PutAsJsonAsync(requestUri, body, cancellationToken);
    }

    /// <summary>Send a DELETE request.</summary>
    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await Client.DeleteAsync(requestUri, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
```

### 6.4 WebAPI Extensions

```csharp
namespace Rig.TUnit.WebAPI.Extensions;

public static class WebApiFactoryExtensions
{
    /// <summary>
    /// Configure the test WebApplicationFactory with test services and optional configuration.
    /// </summary>
    public static WebApplicationFactory<TProgram> WithTestServices<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection> configureServices,
        Dictionary<string, string?>? configuration = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            if (configuration is not null)
            {
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(configuration));
            }
            builder.ConfigureServices(configureServices);
        });
    }
}
```

### 6.5 WebAPI Fluent Builder

```csharp
namespace Rig.TUnit.WebAPI.Builder;

public sealed class WebApiRigBuilder<TProgram> where TProgram : class
{
    private readonly IServiceCollection _services;
    private readonly WebApplicationFactory<TProgram> _factory;

    internal WebApiRigBuilder(IServiceCollection services, WebApplicationFactory<TProgram> factory)
    {
        _services = services;
        _factory = factory;
    }

    /// <summary>Register the HttpClientHelper for test HTTP calls.</summary>
    public WebApiRigBuilder<TProgram> AddHttpClientHelper()
    {
        _services.AddSingleton(new HttpClientHelper<TProgram>(_factory));
        return this;
    }

    /// <summary>Register the HandlerHelper for Mediator dispatch testing.</summary>
    public WebApiRigBuilder<TProgram> AddHandlerHelper()
    {
        _services.AddSingleton(sp =>
            new HandlerHelper(sp.GetRequiredService<IServiceScopeFactory>()));
        return this;
    }
}

public static class WebApiRigBuilderExtensions
{
    public static RigBuilder UseWebApi<TProgram>(
        this RigBuilder builder,
        WebApplicationFactory<TProgram> factory,
        Action<WebApiRigBuilder<TProgram>> configure) where TProgram : class
    {
        var webApiBuilder = new WebApiRigBuilder<TProgram>(builder.Services, factory);
        configure(webApiBuilder);
        return builder;
    }
}
```

---

## 7. Core Utilities

### 7.1 WaitHelper — Generic Polling Utility

Extracted from ListenerHelper's internal polling pattern. Reusable across all packages.

```csharp
namespace Rig.TUnit.Core.Helpers;

/// <summary>
/// Generic async polling utility. Waits for a condition to become true.
/// </summary>
public static class WaitHelper
{
    /// <summary>
    /// Polls a condition until it returns true or the timeout is exceeded.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollingInterval">Time between polls. Defaults to 250ms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when timeout is exceeded.</exception>
    public static async Task WaitForAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (!condition())
        {
            if (cts.Token.IsCancellationRequested)
                throw new TimeoutException(
                    $"Condition not met within {timeout.TotalSeconds:F1}s timeout.");
            await Task.Delay(interval, cts.Token);
        }
    }

    /// <summary>
    /// Polls an async condition until it returns true or the timeout is exceeded.
    /// </summary>
    public static async Task WaitForAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (!await condition())
        {
            if (cts.Token.IsCancellationRequested)
                throw new TimeoutException(
                    $"Condition not met within {timeout.TotalSeconds:F1}s timeout.");
            await Task.Delay(interval, cts.Token);
        }
    }

    /// <summary>
    /// Polls until a value is returned (non-null) or the timeout is exceeded.
    /// </summary>
    public static async Task<T> WaitForResultAsync<T>(
        Func<Task<T?>> producer,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (true)
        {
            var result = await producer();
            if (result is not null) return result;

            if (cts.Token.IsCancellationRequested)
                throw new TimeoutException(
                    $"Result not produced within {timeout.TotalSeconds:F1}s timeout.");
            await Task.Delay(interval, cts.Token);
        }
    }
}
```

### 7.2 TestConfigurationBuilder

```csharp
namespace Rig.TUnit.Core.Configuration;

/// <summary>
/// Builds an IConfiguration instance from in-memory key-value pairs.
/// Useful for tests that need configuration without appsettings files.
/// </summary>
public sealed class TestConfigurationBuilder
{
    private readonly Dictionary<string, string?> _values = new();

    /// <summary>Set a configuration key to a value.</summary>
    public TestConfigurationBuilder Set(string key, string value)
    {
        _values[key] = value;
        return this;
    }

    /// <summary>Set a connection string (shorthand for ConnectionStrings:{name}).</summary>
    public TestConfigurationBuilder SetConnectionString(string name, string value)
    {
        _values[$"ConnectionStrings:{name}"] = value;
        return this;
    }

    /// <summary>Set an entire section from a dictionary.</summary>
    public TestConfigurationBuilder SetSection(string sectionName, Dictionary<string, string> values)
    {
        foreach (var (key, value) in values)
            _values[$"{sectionName}:{key}"] = value;
        return this;
    }

    /// <summary>Build the IConfiguration instance.</summary>
    public IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(_values!)
            .Build();
    }

    /// <summary>Build and bind to a strongly-typed options class.</summary>
    public TOptions BuildOptions<TOptions>(string sectionName) where TOptions : class, new()
    {
        var config = Build();
        var options = new TOptions();
        config.GetSection(sectionName).Bind(options);
        return options;
    }

    /// <summary>Static shorthand for simple cases.</summary>
    public static IConfiguration Create(Action<TestConfigurationBuilder> configure)
    {
        var builder = new TestConfigurationBuilder();
        configure(builder);
        return builder.Build();
    }
}
```

### 7.3 RigFixtureBase — IAsyncLifetime Base Class

```csharp
namespace Rig.TUnit.Core.Fixtures;

/// <summary>
/// Base class for fixtures that manage async resources.
/// Implements TUnit's IAsyncInitializer + IAsyncDisposable lifecycle.
/// </summary>
public abstract class RigFixtureBase : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource
{
    public abstract string ConnectionString { get; }

    /// <summary>Called by TUnit before first test. Override to initialize resources.</summary>
    public abstract Task InitializeAsync();

    /// <summary>Called by TUnit after last test. Override to clean up resources.</summary>
    public abstract ValueTask DisposeAsync();
}
```

### 7.4 CompositeFixture — Fixture Composition

```csharp
namespace Rig.TUnit.Core.Fixtures;

/// <summary>
/// Composes multiple fixtures into one. Initializes in parallel, disposes in reverse order.
/// </summary>
public sealed class CompositeFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly IReadOnlyList<object> _fixtures;

    public CompositeFixture(params object[] fixtures)
    {
        _fixtures = fixtures;
    }

    /// <summary>Get a fixture of a specific type from the composition.</summary>
    public T Get<T>() where T : class
        => _fixtures.OfType<T>().FirstOrDefault()
           ?? throw new InvalidOperationException(
               $"No fixture of type {typeof(T).Name} in this composition.");

    /// <summary>Initialize all fixtures that implement IAsyncInitializer, in parallel.</summary>
    public async Task InitializeAsync()
    {
        var tasks = _fixtures
            .OfType<IAsyncInitializer>()
            .Select(f => f.InitializeAsync());
        await Task.WhenAll(tasks);
    }

    /// <summary>Dispose all fixtures in reverse order (LIFO).</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var fixture in _fixtures.Reverse())
        {
            if (fixture is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
        }
    }
}
```

---

## 8. Enhanced Existing Components

### 8.1 DbContextHelper — Seed Support

Add to existing `DbContextHelper<TContext>`:

```csharp
/// <summary>
/// Seed the database with test data in an isolated scope.
/// The scope is disposed after seeding, ensuring a clean change tracker.
/// </summary>
public async Task SeedAsync(Func<TContext, Task> seedAction)
{
    await using var scope = _scopeFactory.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<TContext>();
    await seedAction(context);
    await context.SaveChangesAsync();
}

/// <summary>Seed with synchronous action (for simple data setup).</summary>
public async Task SeedAsync(Action<TContext> seedAction)
{
    await using var scope = _scopeFactory.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<TContext>();
    seedAction(context);
    await context.SaveChangesAsync();
}
```

### 8.2 ServiceBusFixture — Custom Config Path

The current `ServiceBusFixture` hardcodes `"TestInfrastructure/service-bus-config.json"`. Make it configurable:

```csharp
public sealed class ServiceBusFixture : RigFixtureBase
{
    private ServiceBusContainer? _container;

    /// <summary>Path to the Service Bus emulator config file.</summary>
    public string ConfigFilePath { get; set; } = "TestInfrastructure/service-bus-config.json";

    public override string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("ServiceBusFixture not initialized.");

    public override async Task InitializeAsync()
    {
        _container = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true)
            .WithResourceMapping(ConfigFilePath, "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .Build();
        await _container.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
```

### 8.3 ListenerHelper — Use WaitHelper

Refactor to use the shared `WaitHelper` instead of inline polling:

```csharp
public async Task WaitForMessagesAsync(
    int expectedCount,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
{
    var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
    await WaitHelper.WaitForAsync(
        () => Messages.Count >= expectedCount,
        effectiveTimeout,
        pollingInterval: TimeSpan.FromMilliseconds(250),
        cancellationToken);
}
```

---

## 9. Complete Fluent API Usage Examples

### Example 1: Full Stack with Containers (CI/CD)

```csharp
[ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
[ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
[ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
public class OrderIntegrationTests
{
    [Test]
    public async Task CreateOrder_FullPipeline(
        SqlServerFixture sql, RedisFixture redis, ServiceBusFixture sb)
    {
        var services = new ServiceCollection();
        services.AddRigTUnit(rig => rig
            .UseSqlServer(sql, s => s
                .ReplaceDbContext<OrderDbContext>()
                .ReplaceDbContext<AuditDbContext>())
            .UseRedis(redis)
            .UseServiceBus(sb, s => s
                .ReplaceClient()
                .ReplaceClient<CompetitionServiceBus>(conn =>
                    new CompetitionServiceBus(new ServiceBusOptions
                    {
                        CompetitionServiceBus = conn
                    })))
        );
        // ... test logic
    }
}
```

### Example 2: External Services (Local Dev, No Docker)

```csharp
public class OrderLocalTests
{
    [Test]
    public async Task CreateOrder_LocalServices()
    {
        var config = TestConfigurationBuilder.Create(c => c
            .SetConnectionString("OrderDb", "Server=localhost;Database=orders_test;...")
            .SetConnectionString("Redis", "localhost:6379")
            .SetConnectionString("ServiceBus", "Endpoint=sb://localhost;..."));

        var services = new ServiceCollection();
        services.AddRigTUnit(rig => rig
            .UseSqlServer(RigConnect.FromConfig(config, "ConnectionStrings:OrderDb"),
                sql => sql.ReplaceDbContext<OrderDbContext>())
            .UseRedis(RigConnect.FromConfig(config, "ConnectionStrings:Redis"))
            .UseServiceBus(RigConnect.FromConfig(config, "ConnectionStrings:ServiceBus"),
                sb => sb.ReplaceClient())
        );
    }
}
```

### Example 3: Smart Auto Mode (Container in CI, Config Locally)

```csharp
[ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
public class OrderSmartTests
{
    private static readonly IConfiguration _localConfig = TestConfigurationBuilder.Create(c => c
        .SetConnectionString("OrderDb", "Server=localhost;Database=orders_dev;..."));

    [Test]
    public async Task CreateOrder_AutoMode(SqlServerFixture sql)
    {
        var services = new ServiceCollection();
        services.AddRigTUnit(rig => rig
            .ForceContainersInCi()
            .UseSqlServer(
                RigConnect.Auto(sql, _localConfig, "ConnectionStrings:OrderDb"),
                s => s.ReplaceDbContext<OrderDbContext>())
        );
        // In CI: uses sql fixture (container)
        // Locally: uses localhost connection from config
    }
}
```

### Example 4: WebAPI Testing (No gRPC)

```csharp
public class OrderApiTests
{
    [Test]
    public async Task GetOrders_ReturnsOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        await using var http = new HttpClientHelper<Program>(factory);

        var orders = await http.GetAsync<List<OrderDto>>("/api/v1/orders");

        await Assert.That(orders).IsNotNull();
        await Assert.That(orders!.Count).IsGreaterThan(0);
    }
}
```

### Example 5: Mediator Handler Testing (No gRPC)

```csharp
public class CreateOrderHandlerTests
{
    [Test]
    public async Task Handle_ValidRequest_ReturnsOrderId()
    {
        var services = new ServiceCollection();
        services.AddMediator(); // martinothamar/Mediator source-generated
        // ... register handler dependencies

        var provider = services.BuildServiceProvider();
        var handler = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await handler.Send(new CreateOrderCommand("test-order"));

        await Assert.That(result).IsNotNull();
    }
}
```

### Example 6: Composite Fixture

```csharp
public class FullStackFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly CompositeFixture _composite;

    public FullStackFixture()
    {
        _composite = new CompositeFixture(
            new SqlServerFixture(),
            new RedisFixture(),
            new ServiceBusFixture());
    }

    public SqlServerFixture Sql => _composite.Get<SqlServerFixture>();
    public RedisFixture Redis => _composite.Get<RedisFixture>();
    public ServiceBusFixture ServiceBus => _composite.Get<ServiceBusFixture>();

    public Task InitializeAsync() => _composite.InitializeAsync();
    public ValueTask DisposeAsync() => _composite.DisposeAsync();
}
```

---

## 10. NuGet Dependencies per Package

| Package | Dependencies |
|---------|-------------|
| **Rig.TUnit.Core** | TUnit.Core 1.34.5, Bogus 35.6.1, M.E.DI.Abstractions 10.0.0, M.E.Configuration.Abstractions 10.0.0 |
| **Rig.TUnit.Mediator** | → Core, Mediator.Abstractions 3.0.2 |
| **Rig.TUnit.Grpc** | → Core, → Mediator, TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6, Grpc.* 2.71.0, Calzolari 9.0.0, Serilog 4.2.0 |
| **Rig.TUnit.WebAPI** | → Core, → Mediator, TUnit.AspNetCore 1.34.5, M.AspNetCore.Mvc.Testing 10.0.6 |
| **Rig.TUnit.SqlServer** | → Core, Testcontainers.MsSql 4.6.0, EF Core SqlServer 10.0.0, EF Core InMemory 10.0.0 |
| **Rig.TUnit.Redis** | → Core, Testcontainers.Redis 4.6.0, StackExchange.Redis 2.8.16 |
| **Rig.TUnit.ServiceBus** | → Core, Testcontainers.ServiceBus 4.6.0, Azure.Messaging.ServiceBus 7.18.2, Newtonsoft.Json 13.0.3 |
| **Rig.TUnit** | → All above (meta-package) |

---

## 11. Implementation Order

### Phase 1: Core Infrastructure
1. Add `IRigConnectionSource` interface
2. Add connection source implementations (Config, Options, Value, Auto)
3. Add `RigConnect` static factory
4. Add `RigBuilder` class + `AddRigTUnit` entry point
5. Add `WaitHelper`
6. Add `TestConfigurationBuilder`
7. Add `RigFixtureBase` abstract class
8. Add `CompositeFixture`
9. Make existing fixtures implement `IRigConnectionSource`

### Phase 2: Mediator Package
1. Create `Rig.TUnit.Mediator` project
2. Implement `HandlerHelper` using martinothamar/Mediator interfaces
3. Delete `Rig.TUnit.Grpc.Helpers.HandlerHelper`
4. Update `Rig.TUnit.Grpc` to reference `Rig.TUnit.Mediator`

### Phase 3: WebAPI Package
1. Create `Rig.TUnit.WebAPI` project
2. Implement `HttpClientHelper<TProgram>`
3. Implement `WebApiFactoryExtensions`
4. Implement `WebApiRigBuilder<TProgram>` fluent extensions

### Phase 4: Package-Specific Builders
1. `SqlServerRigBuilder` + extensions
2. `RedisRigBuilder` + extensions
3. `ServiceBusRigBuilder` + extensions
4. `GrpcRigBuilder<TProgram>` + extensions

### Phase 5: Enhancements
1. `DbContextHelper.SeedAsync()`
2. `ServiceBusFixture` custom config path
3. Refactor `ListenerHelper` to use `WaitHelper`

### Phase 6: Tests + Verification
- Unit tests for all builders, connection sources, and utilities
- Integration tests for builder + container scenarios
- Benchmarks for new components

---

## 12. Migration — Old APIs Removed

### Old Extension Methods → Fluent Builder

The old standalone extension methods are **removed** and replaced by the fluent builder. No duplicates.

| Removed | Replaced By |
|---------|------------|
| `services.UseSqlServerContainerIsolated<TContext>(fixture)` | `builder.UseSqlServer(source, sql => sql.ReplaceDbContext<TContext>())` |
| `services.UseInMemoryDatabase<TContext>()` | (kept as-is — no container dependency, still useful standalone) |
| `services.UseRedisContainer(fixture)` | `builder.UseRedis(source)` |
| `services.UseServiceBusContainer(fixture)` | `builder.UseServiceBus(source, sb => sb.ReplaceClient())` |
| `services.ReplaceGrpcClient<TClient, TProgram>(fixture, factory)` | `builder.UseGrpc<TProgram>(factory, grpc => grpc.ReplaceClient<TClient>())` |

### MediatR → Mediator

MediatR is fully removed and replaced by martinothamar/Mediator:
- `using MediatR;` → `using Mediator;`
- `Task<T>` handler returns → `ValueTask<T>` handler returns
- `IRequest<T>` stays the same name (different namespace)
- `Rig.TUnit.Grpc.Helpers.HandlerHelper` → removed, lives only in `Rig.TUnit.Mediator.Helpers.HandlerHelper`
- MediatR NuGet package removed from Grpc csproj

### Old Extension Files Removed

| Removed File | Logic Moved To |
|-------------|---------------|
| `SqlServer/Extensions/SqlServerContainerExtensions.cs` | `SqlServer/Builder/SqlServerRigBuilder.cs` |
| `Redis/Extensions/RedisContainerExtensions.cs` | `Redis/Builder/RedisRigBuilder.cs` |
| `ServiceBus/Extensions/ServiceBusContainerExtensions.cs` | `ServiceBus/Builder/ServiceBusRigBuilder.cs` |
| `Grpc/Extensions/GrpcServiceReplacementExtensions.cs` | `Grpc/Builder/GrpcRigBuilder.cs` |
| `Grpc/Helpers/HandlerHelper.cs` | `Mediator/Helpers/HandlerHelper.cs` |
