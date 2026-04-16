# Data Model: 002-rig-tunit-fluent-builder-expansion

## Type Hierarchy

### Public Interfaces

```
IRigConnectionSource
├── string ConnectionString { get; }
```

### Public Abstract Classes

```
RigFixtureBase (abstract)
├── implements: IAsyncInitializer, IAsyncDisposable, IRigConnectionSource
├── abstract string ConnectionString { get; }
├── abstract Task InitializeAsync()
├── abstract ValueTask DisposeAsync()
```

### Public Static Classes

```
RigConnect
├── FromContainer(IRigConnectionSource) → IRigConnectionSource
├── FromConfig(IConfiguration, string) → IRigConnectionSource
├── FromOptions<T>(IOptions<T>, Func<T,string>) → IRigConnectionSource
├── FromValue(string) → IRigConnectionSource
├── Auto(IRigConnectionSource, IConfiguration, string) → IRigConnectionSource

WaitHelper
├── WaitForAsync(Func<bool>, TimeSpan, TimeSpan?, CancellationToken) → Task
├── WaitForAsync(Func<Task<bool>>, TimeSpan, TimeSpan?, CancellationToken) → Task
├── WaitForResultAsync<T>(Func<Task<T?>>, TimeSpan, TimeSpan?, CancellationToken) → Task<T>
```

### Public Sealed Classes

```
RigBuilder
├── internal ctor(IServiceCollection)
├── ForceContainersInCi() → RigBuilder
├── public Services → IServiceCollection  [public for cross-assembly extension methods]
├── internal IsForceContainersInCi → bool  [metadata flag, does NOT override AutoConnectionSource]

TestConfigurationBuilder
├── Set(string, string) → TestConfigurationBuilder
├── SetConnectionString(string, string) → TestConfigurationBuilder
├── SetSection(string, Dictionary<string,string>) → TestConfigurationBuilder
├── Build() → IConfiguration
├── BuildOptions<T>(string) → T
├── static Create(Action<TestConfigurationBuilder>) → IConfiguration

CompositeFixture
├── implements: IAsyncInitializer, IAsyncDisposable
├── ctor(params object[])
├── Get<T>() → T
├── InitializeAsync() → Task  [parallel]
├── DisposeAsync() → ValueTask  [LIFO]

HandlerHelper (Rig.TUnit.Mediator)
├── ctor(IServiceScopeFactory)
├── Send<T>(IRequest<T>, CancellationToken?) → ValueTask<T>
├── Send<T>(ICommand<T>, CancellationToken?) → ValueTask<T>
├── Send<T>(IQuery<T>, CancellationToken?) → ValueTask<T>
├── Publish<T>(T, CancellationToken?) → ValueTask  [where T : INotification]

HttpClientHelper<TProgram> (Rig.TUnit.WebAPI)
├── implements: IAsyncDisposable
├── ctor(WebApplicationFactory<TProgram>)
├── Client → HttpClient  [lazy]
├── CreateClient(Action<Options>?) → HttpClient
├── GetAsync<T>(string, CancellationToken?) → Task<T?>
├── PostAsync<TReq,TRes>(string, TReq, CancellationToken?) → Task<TRes?>
├── PutAsync<TReq>(string, TReq, CancellationToken?) → Task<HttpResponseMessage>
├── DeleteAsync(string, CancellationToken?) → Task<HttpResponseMessage>

SqlServerRigBuilder
├── internal ctor(IServiceCollection, IRigConnectionSource)
├── ReplaceDbContext<T>() → SqlServerRigBuilder
├── ReplaceDbContext<T>(Func<string,string>) → SqlServerRigBuilder

RedisRigBuilder
├── internal ctor(IServiceCollection, IRigConnectionSource)
├── ReplaceMultiplexer() → RedisRigBuilder
├── ReplaceClient<T>(Func<IConnectionMultiplexer,T>) → RedisRigBuilder

ServiceBusRigBuilder
├── internal ctor(IServiceCollection, IRigConnectionSource)
├── ReplaceClient() → ServiceBusRigBuilder
├── ReplaceClient<T>(Func<string,T>) → ServiceBusRigBuilder
├── ReplaceClient<T>(Func<string,IServiceProvider,T>) → ServiceBusRigBuilder

GrpcRigBuilder<TProgram>
├── internal ctor(IServiceCollection, WebApplicationFactory<TProgram>)
├── ReplaceClient<TClient>() → GrpcRigBuilder<TProgram>

WebApiRigBuilder<TProgram>
├── internal ctor(IServiceCollection, WebApplicationFactory<TProgram>)
├── AddHttpClientHelper() → WebApiRigBuilder<TProgram>
├── AddHandlerHelper() → WebApiRigBuilder<TProgram>
```

### Internal Sealed Classes

```
ConfigConnectionSource
├── implements: IRigConnectionSource
├── ctor(IConfiguration, string key)
├── ConnectionString → throws InvalidOperationException if key missing

OptionsConnectionSource<T>
├── implements: IRigConnectionSource
├── ctor(IOptions<T>, Func<T,string>)
├── ConnectionString → throws InvalidOperationException if selector returns null

ValueConnectionSource
├── implements: IRigConnectionSource
├── ctor(string) → throws ArgumentNullException if null
├── ConnectionString → string (eager, set in ctor)

AutoConnectionSource
├── implements: IRigConnectionSource
├── ctor(IRigConnectionSource fixture, IConfiguration, string configKey)
├── ConnectionString → fixture if CI, config if present, fixture if not
├── uses: EnvironmentDetection.IsRunningInCiCd() (same assembly, Rig.TUnit.Core)
```

### Modified Existing Types

```
SqlServerFixture : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource  [+interface]
RedisFixture : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource  [+interface]
ServiceBusFixture : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource  [+interface]

DbContextHelper<TContext>  [+methods]
├── +SeedAsync(Func<TContext, Task>) → Task
├── +SeedAsync(Action<TContext>) → Task
```

## Extension Method Map

| Extension Class | On Type | Methods |
|----------------|---------|---------|
| `RigBuilderExtensions` | `IServiceCollection` | `AddRigTUnit(Action<RigBuilder>)` |
| `SqlServerRigBuilderExtensions` | `RigBuilder` | `UseSqlServer(source, Action<SqlServerRigBuilder>)`, `UseSqlServer<T>(source)` |
| `RedisRigBuilderExtensions` | `RigBuilder` | `UseRedis(source, Action<RedisRigBuilder>?)` |
| `ServiceBusRigBuilderExtensions` | `RigBuilder` | `UseServiceBus(source, Action<ServiceBusRigBuilder>)` |
| `GrpcRigBuilderExtensions` | `RigBuilder` | `UseGrpc<TProgram>(factory, Action<GrpcRigBuilder<TProgram>>)` |
| `WebApiRigBuilderExtensions` | `RigBuilder` | `UseWebApi<TProgram>(factory, Action<WebApiRigBuilder<TProgram>>)` |
| `WebApiFactoryExtensions` | `WebApplicationFactory<T>` | `WithTestServices(Action<IServiceCollection>, Dictionary?)` |
