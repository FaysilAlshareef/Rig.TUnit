# Rig.TUnit — Fluent Builder Expansion — Session Handoff

## What This Is

Complete handoff for implementing the fluent builder expansion on top of the existing Rig.TUnit library.
Use this + `Rig.TUnit-Library-Design.md` (in this same directory) in a new session to implement the changes.

**Prerequisite**: The base library is fully implemented and all 56 tests pass. This document adds new packages, refactors existing ones, and introduces the fluent builder API.

## .NET Version

- **net10.0** (unchanged)

## Framework

- **TUnit 1.34.5** (all packages aligned)
- **martinothamar/Mediator 3.0.2** (replaces MediatR 12.4.1)
- **Mediator.Abstractions 3.0.2** (interfaces only — source generator lives in consumer)

---

## New Packages To Create

### Rig.TUnit.Mediator

```
src/Rig.TUnit.Mediator/Rig.TUnit.Mediator.csproj
src/Rig.TUnit.Mediator/Helpers/HandlerHelper.cs
```

**csproj dependencies:**
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

**Key design decisions:**
- Only depends on `Mediator.Abstractions` (interfaces), NOT `Mediator.SourceGenerator`
- The source generator package is installed by the **consumer's project** (the one calling `AddMediator()`)
- HandlerHelper supports `IRequest<T>`, `ICommand<T>`, `IQuery<T>`, and `INotification`
- All handler methods return `ValueTask<T>` (not `Task<T>` like MediatR)

### Rig.TUnit.WebAPI

```
src/Rig.TUnit.WebAPI/Rig.TUnit.WebAPI.csproj
src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs
src/Rig.TUnit.WebAPI/Extensions/WebApiFactoryExtensions.cs
src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilder.cs
src/Rig.TUnit.WebAPI/Builder/WebApiRigBuilderExtensions.cs
```

**csproj dependencies:**
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

---

## New Files in Existing Packages

### Rig.TUnit.Core — New Files

```
src/Rig.TUnit.Core/Builder/IRigConnectionSource.cs
src/Rig.TUnit.Core/Builder/RigBuilder.cs
src/Rig.TUnit.Core/Builder/RigBuilderExtensions.cs
src/Rig.TUnit.Core/Builder/RigConnect.cs
src/Rig.TUnit.Core/Builder/ConfigConnectionSource.cs
src/Rig.TUnit.Core/Builder/OptionsConnectionSource.cs
src/Rig.TUnit.Core/Builder/ValueConnectionSource.cs
src/Rig.TUnit.Core/Builder/AutoConnectionSource.cs
src/Rig.TUnit.Core/Helpers/WaitHelper.cs
src/Rig.TUnit.Core/Configuration/TestConfigurationBuilder.cs
src/Rig.TUnit.Core/Fixtures/RigFixtureBase.cs
src/Rig.TUnit.Core/Fixtures/CompositeFixture.cs
```

**New dependency required:**
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Memory" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="10.0.0" />
```

### Rig.TUnit.SqlServer — New Files

```
src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilder.cs
src/Rig.TUnit.SqlServer/Builder/SqlServerRigBuilderExtensions.cs
```

### Rig.TUnit.Redis — New Files

```
src/Rig.TUnit.Redis/Builder/RedisRigBuilder.cs
src/Rig.TUnit.Redis/Builder/RedisRigBuilderExtensions.cs
```

### Rig.TUnit.ServiceBus — New Files

```
src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilder.cs
src/Rig.TUnit.ServiceBus/Builder/ServiceBusRigBuilderExtensions.cs
```

### Rig.TUnit.Grpc — New Files

```
src/Rig.TUnit.Grpc/Builder/GrpcRigBuilder.cs
src/Rig.TUnit.Grpc/Builder/GrpcRigBuilderExtensions.cs
```

---

## Modified Existing Files

### Rig.TUnit.Core

| File | Change |
|------|--------|
| `Rig.TUnit.Core.csproj` | Add Configuration, Options package references |

### Rig.TUnit.SqlServer

| File | Change |
|------|--------|
| `Fixtures/SqlServerFixture.cs` | Implement `IRigConnectionSource` |
| `Helpers/DbContextHelper.cs` | Add `SeedAsync()` overloads |
| `Extensions/SqlServerContainerExtensions.cs` | **DELETE** — logic moved to `Builder/SqlServerRigBuilder.cs` |

### Rig.TUnit.Redis

| File | Change |
|------|--------|
| `Fixtures/RedisFixture.cs` | Implement `IRigConnectionSource` |
| `Extensions/RedisContainerExtensions.cs` | **DELETE** — logic moved to `Builder/RedisRigBuilder.cs` |

### Rig.TUnit.ServiceBus

| File | Change |
|------|--------|
| `Fixtures/ServiceBusFixture.cs` | Implement `IRigConnectionSource`, make `ConfigFilePath` settable |
| `Helpers/ListenerHelper.cs` | Refactor to use `WaitHelper` internally |
| `Extensions/ServiceBusContainerExtensions.cs` | **DELETE** — logic moved to `Builder/ServiceBusRigBuilder.cs` |

### Rig.TUnit.Grpc

| File | Change |
|------|--------|
| `Rig.TUnit.Grpc.csproj` | Add ProjectReference to Rig.TUnit.Mediator, remove MediatR package |
| `Helpers/HandlerHelper.cs` | **DELETE** — moved to `Rig.TUnit.Mediator.Helpers.HandlerHelper` |
| `Extensions/GrpcServiceReplacementExtensions.cs` | **DELETE** — logic moved to `Builder/GrpcRigBuilder.cs` |

### Meta-Package

| File | Change |
|------|--------|
| `src/Rig.TUnit/Rig.TUnit.csproj` | Add references to Mediator and WebAPI projects |

### Solution

| File | Change |
|------|--------|
| `Rig.TUnit.slnx` | Add Mediator and WebAPI projects + their test projects |

---

## New Test Projects

### Rig.TUnit.Mediator.Tests.Unit

```
tests/Rig.TUnit.Mediator.Tests.Unit/Rig.TUnit.Mediator.Tests.Unit.csproj
tests/Rig.TUnit.Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestRequest.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestRequestHandler.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestCommand.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestCommandHandler.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestQuery.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestQueryHandler.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestNotification.cs
tests/Rig.TUnit.Mediator.Tests.Unit/TestInfrastructure/TestNotificationHandler.cs
```

**csproj dependencies:**
```xml
<PackageReference Include="TUnit" Version="1.34.5" />
<PackageReference Include="Mediator.SourceGenerator" Version="3.0.2">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
<PackageReference Include="Mediator.Abstractions" Version="3.0.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
<ProjectReference Include="..\..\src\Rig.TUnit.Mediator\Rig.TUnit.Mediator.csproj" />
```

**CRITICAL**: The test project MUST have `Mediator.SourceGenerator` because it's the outermost project that calls `AddMediator()`. The library (`Rig.TUnit.Mediator`) only has `Mediator.Abstractions`.

### Rig.TUnit.WebAPI.Tests.Unit

```
tests/Rig.TUnit.WebAPI.Tests.Unit/Rig.TUnit.WebAPI.Tests.Unit.csproj
tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs
tests/Rig.TUnit.WebAPI.Tests.Unit/Extensions/WebApiFactoryExtensionsTests.cs
tests/Rig.TUnit.WebAPI.Tests.Unit/Builder/WebApiRigBuilderTests.cs
tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestProgram.cs
tests/Rig.TUnit.WebAPI.Tests.Unit/TestInfrastructure/TestEndpoints.cs
```

**csproj dependencies:**
```xml
<PackageReference Include="TUnit" Version="1.34.5" />
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
<ProjectReference Include="..\..\src\Rig.TUnit.WebAPI\Rig.TUnit.WebAPI.csproj" />
```

### Rig.TUnit.Core.Tests.Unit — New Test Files

```
tests/Rig.TUnit.Core.Tests.Unit/Builder/RigBuilderTests.cs
tests/Rig.TUnit.Core.Tests.Unit/Builder/RigConnectTests.cs
tests/Rig.TUnit.Core.Tests.Unit/Builder/ConnectionSourceTests.cs
tests/Rig.TUnit.Core.Tests.Unit/Helpers/WaitHelperTests.cs
tests/Rig.TUnit.Core.Tests.Unit/Configuration/TestConfigurationBuilderTests.cs
tests/Rig.TUnit.Core.Tests.Unit/Fixtures/CompositeFixtureTests.cs
```

**New dependencies needed in existing Core.Tests.Unit csproj:**
```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Memory" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="10.0.0" />
```

### Existing Integration Test Updates

| Test Project | Changes |
|-------------|---------|
| `Rig.TUnit.SqlServer.Tests.Unit` | Add `DbContextHelperSeedTests.cs` |
| `Rig.TUnit.SqlServer.Tests.Integration` | Add `Builder/SqlServerRigBuilderTests.cs` |
| `Rig.TUnit.Redis.Tests.Integration` | Add `Builder/RedisRigBuilderTests.cs` |
| `Rig.TUnit.ServiceBus.Tests.Integration` | Add `Builder/ServiceBusRigBuilderTests.cs` |

---

## Key Design Decisions

### D1: Mediator.Abstractions Only in Library

The `Rig.TUnit.Mediator` package references `Mediator.Abstractions` (interfaces), NOT `Mediator.SourceGenerator`. The source generator must run in the **outermost project** (the one calling `AddMediator()`). For tests, that's the test project. For consumers, that's their app. This prevents multiple source generator instances in the build.

### D2: Fluent Builders Replace Old Extensions

Old standalone extension methods (`UseSqlServerContainerIsolated`, `UseRedisContainer`, `UseServiceBusContainer`, `ReplaceGrpcClient`) are **removed**. Their logic moves into the fluent builders. No duplicate APIs. `InMemoryDbExtensions` is kept (no container dependency, still useful standalone).

### D3: IRigConnectionSource on Existing Fixtures

Existing fixtures (`SqlServerFixture`, `RedisFixture`, `ServiceBusFixture`) gain `IRigConnectionSource` implementation. They already have `ConnectionString` property — just add the interface marker. This is a binary-compatible change.

### D4: Internal Connection Source Classes

`ConfigConnectionSource`, `OptionsConnectionSource`, `ValueConnectionSource`, `AutoConnectionSource` are all `internal sealed`. Users interact through `RigConnect` static factory only.

### D5: WaitHelper Replaces Inline Polling

`ListenerHelper.WaitForMessagesAsync` is refactored to delegate to `WaitHelper.WaitForAsync`. Same behavior, shared implementation. The `WaitHelper` is also available to consumers for their own polling needs.

### D6: HandlerHelper Moved, Not Duplicated

The existing `Rig.TUnit.Grpc.Helpers.HandlerHelper` is **deleted**. It moves to `Rig.TUnit.Mediator.Helpers.HandlerHelper` using martinothamar/Mediator instead of MediatR. No wrapper, no deprecation — clean removal.

### D7: WebAPI Depends on Mediator

`Rig.TUnit.WebAPI` references `Rig.TUnit.Mediator` so users testing REST APIs with MediatR/Mediator handlers get `HandlerHelper` without pulling in gRPC packages.

### D8: Core Gains Configuration Dependencies

`Rig.TUnit.Core` adds `Microsoft.Extensions.Configuration.*` and `Microsoft.Extensions.Options` dependencies for `TestConfigurationBuilder`, `RigConnect.FromConfig`, and `RigConnect.FromOptions`. These are lightweight Microsoft abstractions with no runtime cost if unused.

---

## Implementation Sequence

```
Phase 1: Core Infrastructure (no breaking changes)
  1. IRigConnectionSource + implementations
  2. RigConnect static factory
  3. RigBuilder + AddRigTUnit entry point
  4. WaitHelper
  5. TestConfigurationBuilder
  6. RigFixtureBase
  7. CompositeFixture
  8. Update Core.csproj with new dependencies
  9. Existing fixtures: add IRigConnectionSource interface
  10. Unit tests for all above

Phase 2: Rig.TUnit.Mediator (new package)
  1. Create project + csproj
  2. HandlerHelper with Mediator interfaces
  3. Test project with source generator
  4. Unit tests (Request, Command, Query, Notification)
  5. Update Grpc: reference Mediator, delete old HandlerHelper, remove MediatR

Phase 3: Rig.TUnit.WebAPI (new package)
  1. Create project + csproj
  2. HttpClientHelper<TProgram>
  3. WebApiFactoryExtensions
  4. WebApiRigBuilder + extensions
  5. Test project with TestProgram + TestEndpoints
  6. Unit tests

Phase 4: Package Builders (fluent extensions)
  1. SqlServerRigBuilder + extensions
  2. RedisRigBuilder + extensions
  3. ServiceBusRigBuilder + extensions
  4. GrpcRigBuilder + extensions
  5. Integration tests for each builder

Phase 5: Enhancements
  1. DbContextHelper.SeedAsync()
  2. ServiceBusFixture custom config path
  3. ListenerHelper refactor to use WaitHelper
  4. Tests for enhancements

Phase 6: Solution + Verification
  1. Update Rig.TUnit.slnx with all new projects
  2. Update meta-package references
  3. dotnet build — zero errors, zero warnings
  4. dotnet test (unit tests) — all pass
  5. dotnet test (integration tests) — all pass with Docker
  6. Benchmarks updated for new components
```

---

## Final Solution Structure

```
Rig.TUnit.slnx
├── src/
│   ├── Rig.TUnit.Core/              (existing + new Builder/, Helpers/, Configuration/, Fixtures/)
│   ├── Rig.TUnit.Mediator/          [NEW]
│   ├── Rig.TUnit.Grpc/              (existing + new Builder/, modified Helpers/)
│   ├── Rig.TUnit.WebAPI/            [NEW]
│   ├── Rig.TUnit.SqlServer/         (existing + new Builder/, modified Helpers/)
│   ├── Rig.TUnit.Redis/             (existing + new Builder/)
│   ├── Rig.TUnit.ServiceBus/        (existing + new Builder/, modified Fixtures/, Helpers/)
│   └── Rig.TUnit/                   (meta-package, updated refs)
├── tests/
│   ├── Rig.TUnit.Core.Tests.Unit/          (existing + new test files)
│   ├── Rig.TUnit.Mediator.Tests.Unit/      [NEW]
│   ├── Rig.TUnit.Grpc.Tests.Unit/          (existing)
│   ├── Rig.TUnit.WebAPI.Tests.Unit/        [NEW]
│   ├── Rig.TUnit.SqlServer.Tests.Unit/     (existing + new)
│   ├── Rig.TUnit.SqlServer.Tests.Integration/ (existing + new)
│   ├── Rig.TUnit.Redis.Tests.Integration/  (existing + new)
│   ├── Rig.TUnit.ServiceBus.Tests.Integration/ (existing + new)
│   └── Rig.TUnit.Benchmarks/               (existing + new)
```

**Total**: 8 source projects + 9 test projects = 17 projects
