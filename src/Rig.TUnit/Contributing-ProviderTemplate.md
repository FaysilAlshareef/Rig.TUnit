# Contributing — Provider Template

This is the canonical file layout every `Rig.TUnit.{Family}.{Provider}` package ships. The three architecture tests under `tests/Rig.TUnit.Architecture.Tests/Rules/` (`ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`) enforce this shape. Copy this template verbatim when adding a new provider — it is the fastest path to a GREEN contract suite.

Examples of "complete" providers to study before writing your own:

| Family | Provider | Proof of canonical shape |
|---|---|---|
| Databases.Sql | [`Rig.TUnit.Databases.Sql.SqlServer`](../Rig.TUnit.Databases.Sql.SqlServer/) | Fixture + Options + Builder + Extensions + README |
| Caching | [`Rig.TUnit.Caching.Redis`](../Rig.TUnit.Caching.Redis/) | same |
| Messaging | [`Rig.TUnit.Messaging.ServiceBus`](../Rig.TUnit.Messaging.ServiceBus/) | same |

---

## File layout

Hypothetical new provider: `Rig.TUnit.{Family}.Example`.

```
src/Rig.TUnit.{Family}.Example/
├── Rig.TUnit.{Family}.Example.csproj        (1 <ProjectReference> to base + N <PackageReference>)
├── README.md                                 (> 100 chars — 30-second quick-start)
├── Fixtures/
│   └── ExampleFixture.cs                     : {Family}FixtureBase
├── Options/
│   └── ExampleFixtureOptions.cs              (public const string SectionName + [Required])
├── Builder/
│   ├── ExampleRigBuilder.cs                  : {Family}RigBuilder<ExampleRigBuilder>   (sealed)
│   └── ExampleRigBuilderExtensions.cs        (public static UseExample(this RigBuilder, ...))
├── Extensions/                               (SQL only — EF provider wire-up; omit for non-SQL)
│   └── ExampleBuilderExtensions.cs
└── Helpers/                                  (family-specific: Listener/Sender, SasBuilder, etc.)
    └── {Family}-specific.cs
```

A sibling test project is always required:

```
tests/Rig.TUnit.{Family}.Example.Tests.Integration/
├── Rig.TUnit.{Family}.Example.Tests.Integration.csproj
├── ExampleContract.cs                        (: {Family}RigContract — inherits the family contract tests)
├── ExampleParallelIsolationTests.cs          (: ParallelIsolationContract<ExampleFixture>)
├── ExampleQuirkTests.cs                      (provider-specific quirks)
└── TestInfrastructure/                       (shared fixtures, fakers, harnesses — extract any helper from *Tests.cs)
    └── SharedExampleFixture.cs
```

---

## 1. `{Provider}.csproj`

Minimum contents — stay inside Central Package Management (no `<Version>` attributes on `<PackageReference>`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="TUnit.Core" />
    <PackageReference Include="Testcontainers.{Module}" />         <!-- if container-backed -->
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Rig.TUnit.{Family}\Rig.TUnit.{Family}.csproj" />
  </ItemGroup>
</Project>
```

Register the project path in `Rig.TUnit.slnx`, add it to `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` seed list, and add a `<ProjectReference>` to [`src/Rig.TUnit.All/Rig.TUnit.All.csproj`](../Rig.TUnit.All/Rig.TUnit.All.csproj) so the meta-package transitively covers the new provider.

---

## 2. `Options/ExampleFixtureOptions.cs`

The architecture test asserts presence of:
- `public const string SectionName` at the top of the class (used by `AddOptions<T>().BindConfiguration(SectionName)`)
- `sealed` modifier
- `[Required]` / `[Range(...)]` on mandatory properties
- Sensible defaults wherever the container has a default

```csharp
using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.{Family}.Example.Options;

public sealed class ExampleFixtureOptions
{
    public const string SectionName = "RigTUnit:Example";

    [Required]
    public string ImageTag { get; init; } = "latest";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;
}
```

Registration example (callers do this in their DI pipeline):

```csharp
services.AddOptions<ExampleFixtureOptions>()
    .BindConfiguration(ExampleFixtureOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## 3. `Fixtures/ExampleFixture.cs`

Must derive from the correct family base (`CodeOrganizationTests.AllFixtures_ExtendFixtureBase` enforces this):

| Family | Base class |
|---|---|
| Databases.Sql | `SqlFixtureBase` |
| Databases.NoSql | `DocumentFixtureBase` |
| Messaging | `MessagingFixtureBase` |
| Caching | `CacheFixtureBase` |
| Storage | `StorageFixtureBase` |
| Security | `SecurityFixtureBase` |
| Observability | `TelemetryFixtureBase` |

```csharp
using Microsoft.Extensions.Options;
using Rig.TUnit.{Family}.Fixtures;
using Rig.TUnit.{Family}.Example.Options;
using Testcontainers.{Module};

namespace Rig.TUnit.{Family}.Example.Fixtures;

public sealed class ExampleFixture : {Family}FixtureBase
{
    private readonly ExampleFixtureOptions _options;
    private ExampleContainer? _container;

    public ExampleFixture() : this(new ExampleFixtureOptions()) { }
    public ExampleFixture(IOptions<ExampleFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }
    public ExampleFixture(ExampleFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        // Testcontainers 4.11+: pass the image to the ctor — parameterless ctor is [Obsolete].
        _container = new ExampleBuilder($"example-image:{_options.ImageTag}").Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
```

---

## 4. `Builder/ExampleRigBuilder.cs`

CRTP pattern — the generic type argument is the provider's own RigBuilder. Must be `sealed` (leaf providers) and inherit the family's `{Family}RigBuilder<TSelf>` base. Mirrors `SqlServerRigBuilder` in shape.

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.{Family}.Builder;

namespace Rig.TUnit.{Family}.Example.Builder;

public sealed class ExampleRigBuilder : {Family}RigBuilder<ExampleRigBuilder>
{
    public ExampleRigBuilder(RigBuilder root, IRigConnectionSource source)
        : base(root, source)
    {
    }

    // Override family-specific hooks here — e.g. SQL providers override UseProvider(DbContextOptionsBuilder, string).
}
```

---

## 5. `Builder/ExampleRigBuilderExtensions.cs`

Single public entry point `Use{Provider}`:

```csharp
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.{Family}.Example.Builder;

public static class ExampleRigBuilderExtensions
{
    public static RigBuilder UseExample(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<ExampleRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ExampleRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
```

`ProviderCompletenessTests` looks for exactly this shape — `public static RigBuilder Use{Provider}(...)` on a `static` class under `{Provider}.Builder`.

---

## 6. `Extensions/ExampleBuilderExtensions.cs` (SQL only)

SQL providers additionally wrap EF Core so callers can write `options.UseExampleInMemory(...)` for quickstart scenarios. Non-SQL providers skip this folder entirely. Look at [`SqliteBuilderExtensions`](../Rig.TUnit.Databases.Sql.Sqlite/Extensions/SqliteBuilderExtensions.cs) for a minimal example.

---

## 7. `Helpers/*` — family-specific

| Family | Expected helpers |
|---|---|
| Databases.NoSql (Mongo / Cassandra / Dynamo / Elastic / KurrentDb / Cosmos) | Collection-per-test, keyspace-per-test, GSI verifier, index-refresh helper, DSL assert, stream/projection assert, RU-charge capture, partition-distribution checker — per family §4.4 of the library design |
| Messaging (Kafka / RabbitMq / Nats / Sqs) | `{Provider}Listener : ListenerBase` + `{Provider}EventSender : EventSenderBase` |
| Storage (AzureBlob / S3 / MinIO) | `{Provider}SasBuilder`; FileSystem gets `PathSandboxHelper` in lieu of SAS |
| Observability (Metrics) | `TagCardinalityGuard` (default N=100) |
| Caching (Fusion) | fail-safe helper + eager-refresh helper |

All helpers MUST be `sealed` or `static` (enforced by `CodeOrganizationTests.PublicStaticHelpers_AreSealed`).

---

## 8. `README.md` (> 100 chars — enforced by `ReadmeCompletenessTests`)

Minimum template — `dotnet add` + a runnable `[Test]` snippet:

```markdown
# Rig.TUnit.{Family}.Example

One-line description.

## Install

dotnet add package Rig.TUnit.{Family}.Example

## Example

[Test] public async Task Sample()
{
    using var fx = new ExampleFixture();
    await fx.InitializeAsync();

    // Use fx.ConnectionString / fx.Client / etc.
}

## Dependencies

Rig.TUnit.{Family}, Testcontainers.{Module}, ...
```

---

## 9. Tests — contract inheritance + isolation smoke + quirks

The contract suite (`{Family}RigContract`) lives in `tests/Rig.TUnit.{Family}.Tests.Contract/` and has an abstract `CreateRigAsync` method. Your Integration project inherits it:

```csharp
[InheritsTests]
public sealed class ExampleContract : {Family}RigContract
{
    protected override async ValueTask<I{Family}Rig> CreateRigAsync(CancellationToken ct)
        => await SharedExampleFixture.GetAsync();
}
```

Plus the parallel-isolation smoke (required for every provider, per FR-013..FR-017 + Architecture Scope):

```csharp
public sealed class ExampleParallelIsolationTests : ParallelIsolationContract<ExampleFixture>
{
    // the contract does the heavy lifting — 20 parallel fixtures, distinct IsolationKey, zero cross-talk
}
```

Provider-specific quirks go in `{Provider}QuirkTests.cs` (one file, one class). Examples — RU-charge for Cosmos, AUTO_INCREMENT for MySql, PL/SQL timing for Oracle.

---

## 10. Test-file organisation rules (enforced by `TestFileOrganizationTests`)

Every `.cs` file under `tests/**/` outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`, `obj/`, `bin/` MUST declare exactly one top-level class. Inline shared fixtures, test entities, fake handlers, envelope builders, key factories — all of these belong in `TestInfrastructure/`. The rule applies uniformly to `*Contract.cs` files as well (see clarification C-003).

A 355-line `TraceAssertTests.cs` staying as one class is acceptable — test files are NOT split by method-under-test. Only setup infrastructure is extracted.

---

## 11. Coverage + CI gates

Per-package merge gate (identical to feature 003):

- Line coverage ≥ 90%
- Branch coverage ≥ 85%
- Contract suite 100% GREEN
- `ParallelIsolationContract<{Provider}Fixture>` GREEN

Coverage is measured by `coverlet.msbuild` — the `Directory.Packages.props` pin is already in place. Command:

```
dotnet test /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum
dotnet test /p:CollectCoverage=true /p:Threshold=85 /p:ThresholdType=branch /p:ThresholdStat=minimum
```

---

## 12. Checklist before opening a PR

- [ ] `src/Rig.TUnit.{Family}.{Provider}/{Provider}.csproj` present + registered in `Rig.TUnit.slnx` + `Rig.TUnit.All.csproj`
- [ ] `Fixtures/{Provider}Fixture.cs` inherits `{Family}FixtureBase`
- [ ] `Options/{Provider}FixtureOptions.cs` with `public const string SectionName`
- [ ] `Builder/{Provider}RigBuilder.cs` CRTP + sealed
- [ ] `Builder/{Provider}RigBuilderExtensions.cs` exposes `Use{Provider}`
- [ ] Family-specific `Helpers/*` present (sealed or static)
- [ ] `README.md` > 100 chars with runnable quick-start
- [ ] `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` inherits `{Family}RigContract` + `ParallelIsolationContract` + adds `*QuirkTests.cs`
- [ ] `TestInfrastructure/` used for any shared setup types — no inline infrastructure in `*Tests.cs`
- [ ] `ProviderCompletenessTests` GREEN for the new provider (skip list entry removed)
- [ ] Coverage thresholds met
- [ ] `AssemblyLoader.cs` seed updated if the provider's name is new

When all 12 boxes tick, the PR is ready for review.
