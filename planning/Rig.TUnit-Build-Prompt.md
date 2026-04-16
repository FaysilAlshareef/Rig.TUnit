# Build Prompt — Rig.TUnit

Copy everything below this line and paste it into a new Claude Code session.

---

## Task

Build the `Rig.TUnit` library from scratch. This is a standalone .NET testing infrastructure library built on TUnit (not xUnit). It provides container fixtures, generic helpers, and service replacement extensions for integration testing gRPC microservices.

Read both design documents before writing any code:
- `Rig.TUnit-Session-Handoff.md` — exact files, dependencies, versions, and design decisions
- `Rig.TUnit-Library-Design.md` — full API design with complete code for every class

## What to build

6 projects, 1 solution, targeting **net10.0**:

| Project | What it contains |
|---------|-----------------|
| `Rig.TUnit.Core` | `CustomConstructorFaker<T>`, `ServiceRemovalExtensions`, `EnvironmentDetection` |
| `Rig.TUnit.Grpc` | `GrpcClientHelper<TClient>`, `HandlerHelper`, `MetadataHelper`, `WebApplicationFactoryExtensions`, `GrpcServiceReplacementExtensions` |
| `Rig.TUnit.SqlServer` | `SqlServerFixture`, `DbContextHelper<TContext>`, `InMemoryDbExtensions`, `SqlServerContainerExtensions` |
| `Rig.TUnit.Redis` | `RedisFixture`, `RedisContainerExtensions` |
| `Rig.TUnit.ServiceBus` | `ServiceBusFixture`, `ListenerHelper`, `ServiceBusEventSender`, `ServiceBusContainerExtensions` |
| `Rig.TUnit` | Meta-package referencing all above |

## Critical constraints

1. **TUnit 1.33.0+** — NOT xUnit. No `Microsoft.NET.Test.Sdk`, no `coverlet.collector`
2. **net10.0** — all projects target .NET 10
3. **Generic types only** — no references to `Program`, `ApplicationDbContext`, or any service-specific types. `DbContextHelper<TContext>`, `GrpcClientHelper<TClient>`, etc.
4. **Solution file**: `Rig.TUnit.slnx` (XML-based slnx format)
5. **Namespaces match folder structure**: `Rig.TUnit.Core.Fakers`, `Rig.TUnit.Grpc.Helpers`, `Rig.TUnit.SqlServer.Fixtures`, etc.
6. **NuGet versions are exact** — use the versions specified in the handoff doc, do not change them
7. **Container fixtures implement `IAsyncInitializer` + `IAsyncDisposable`** — no abstract base classes, no factory pattern, no manager pattern
8. **Serilog `WriteTo.Console()`** — not `Serilog.Sinks.XUnit` (doesn't work with TUnit)

## Implementation order

1. Create solution file `Rig.TUnit.slnx`
2. Create `Rig.TUnit.Core` — csproj + all classes
3. Create `Rig.TUnit.Grpc` — csproj + all classes
4. Create `Rig.TUnit.SqlServer` — csproj + all classes
5. Create `Rig.TUnit.Redis` — csproj + all classes
6. Create `Rig.TUnit.ServiceBus` — csproj + all classes
7. Create `Rig.TUnit` meta-package — csproj only
8. Run `dotnet build` to verify everything compiles

## Do NOT

- Do NOT add test projects (those come later)
- Do NOT add README or docs files
- Do NOT add NuGet packaging config yet
- Do NOT add CI/CD workflows yet
- Do NOT create abstract base classes for fixtures — keep them simple sealed classes
- Do NOT add classes not listed in the handoff doc (like FakeServiceBusPublisher, FakeGrpcServiceBase, AssertionExtensions, CacheHelper, MessageAssertBase — those are mentioned in the design but NOT in the handoff file list)
- Do NOT deviate from the exact file paths in the handoff doc

## Verification

After creating all files, run `dotnet build Rig.TUnit.slnx` and fix any compilation errors until it builds clean.
