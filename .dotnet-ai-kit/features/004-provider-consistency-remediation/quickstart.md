# Quickstart: Adding a Provider to Rig.TUnit

**Feature ID**: 004-provider-consistency-remediation
**Audience**: contributors adding a new `Rig.TUnit.{Family}.{Provider}` package (or remediating one during Feature 004).
**Time budget**: ~1 hour from empty folder to passing contract suite.

This is the hands-on companion to [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md). The canonical template it references is `src/Rig.TUnit/Contributing-ProviderTemplate.md` (created in Phase 1).

---

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` → `10.x`)
- Docker running (for Testcontainers-backed providers)
- Visual Studio 2022 17.12+, Rider 2024.3+, or VS Code with C# Dev Kit
- Working copy on branch `feat/provider-consistency-remediation`

---

## The TDD rhythm (non-negotiable)

Every change lands in one of three commit shapes:

```
test(004): T{NNN} — RED for {Type}         # failing test first
feat(004): T{NNN} — GREEN implement {Type}  # minimum code to pass
refactor(004): T{NNN} — {description}       # optional, tests stay green
```

Reviewers verify by running:
```bash
git log --oneline feat/provider-consistency-remediation --grep='— RED'
git log --oneline feat/provider-consistency-remediation --grep='— GREEN'
```

A production class with no matching RED commit blocks the PR.

---

## Step-by-step: adding a new provider (example = hypothetical `Rig.TUnit.Databases.NoSql.Example`)

### 1. Create the project folder

```
src/Rig.TUnit.Databases.NoSql.Example/
├── Rig.TUnit.Databases.NoSql.Example.csproj
├── README.md
├── Fixtures/
├── Options/
├── Builder/
└── Helpers/
```

`csproj` contents (no inline package versions — central management is ON):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Rig.TUnit.Databases.NoSql\Rig.TUnit.Databases.NoSql.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Provider-specific runtime NuGet refs — version from Directory.Packages.props -->
  </ItemGroup>
</Project>
```

Register in `Rig.TUnit.slnx` — copy-paste an existing NoSql provider's entry.

### 2. Write the failing contract test (RED)

Create the Tests.Integration project:

```
tests/Rig.TUnit.Databases.NoSql.Example.Tests.Integration/
├── Rig.TUnit.Databases.NoSql.Example.Tests.Integration.csproj
├── ExampleContractTests.cs       // inherits NoSqlRigContract<ExampleFixture>
└── TestInfrastructure/
    └── ExampleTestHarness.cs
```

`ExampleContractTests.cs`:

```csharp
using Rig.TUnit.Databases.NoSql.Example.Fixtures;
using Rig.TUnit.Databases.NoSql.Tests.Contract;

namespace Rig.TUnit.Databases.NoSql.Example.Tests.Integration;

public sealed class ExampleContractTests : NoSqlRigContract<ExampleFixture>
{
    // inherits 13+ tests from the contract suite
}
```

Run `dotnet test tests/Rig.TUnit.Databases.NoSql.Example.Tests.Integration/` — **MUST FAIL** (classes referenced don't exist yet). That's the RED commit.

### 3. Write the Options class (GREEN step 1)

```csharp
using System.ComponentModel.DataAnnotations;
namespace Rig.TUnit.Databases.NoSql.Example.Options;

public sealed class ExampleFixtureOptions
{
    public const string SectionName = "RigTUnit:Example";

    [Required]
    public string ImageTag { get; init; } = "example:latest";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;
}
```

### 4. Write the Fixture (GREEN step 2)

```csharp
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Rig.TUnit.Databases.NoSql.Example.Options;

namespace Rig.TUnit.Databases.NoSql.Example.Fixtures;

public sealed class ExampleFixture : DocumentFixtureBase
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
        ?? throw new InvalidOperationException("InitializeAsync must run first");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new ExampleBuilder()
            .WithImage(_options.ImageTag)
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null) { await _container.DisposeAsync().ConfigureAwait(false); _container = null; }
    }
}
```

### 5. Write the RigBuilder + Extensions (GREEN step 3)

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.Example.Builder;

public sealed class ExampleRigBuilder : NoSqlRigBuilder<ExampleRigBuilder>
{
    public ExampleRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }
    // provider-specific fluent methods here, each returning this
}
```

```csharp
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.Example.Builder;

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

### 6. Write the family-specific helper (if required)

Check `planning/provider-consistency-remediation/Rig.TUnit-Library-Design.md` §4.x for what the family requires. For NoSql, that's usually a per-test-isolation helper (e.g., `CollectionPerTestHelper`, `KeyspacePerTestHelper`).

### 7. Write the README (> 100 chars)

```markdown
# Rig.TUnit.Databases.NoSql.Example

Testcontainers-backed Example fixture for Rig.TUnit integration tests.

## Quick start

    [Test]
    public async Task Sample()
    {
        using var rig = new RigBuilder()
            .UseExample(ConnectionSource.Container, ex => ex.WithDatabase("Tests"))
            .Build();

        var client = rig.Services.GetRequiredService<IExampleClient>();
        // ... act / assert
    }

See [Contributing-ProviderTemplate](../Rig.TUnit/Contributing-ProviderTemplate.md) for the full provider contract.
```

### 8. Run the contract tests (GREEN)

```bash
dotnet test tests/Rig.TUnit.Databases.NoSql.Example.Tests.Integration/
```

Expected: 13+ contract tests pass, coverage ≥ 90% line / ≥ 85% branch. If not:
- Fix the fixture lifecycle (likely `InitializeAsync` / `DisposeAsync`).
- Verify `IsolationKey` is derived per test (contract's parallel-isolation smoke fails otherwise).
- Verify `ConnectionString` throws before `InitializeAsync`.

### 9. Flip architecture tests from `SkipUntilFixed` to enforced

In `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs`, remove the `[Category("SkipUntilFixed")]` marker on the row for this provider (or remove the provider from the skip list — depending on implementation).

### 10. Refactor (optional)

Extract shared patterns if you have duplicated code in other providers of the same family. Tests stay green. Commit as `refactor(004): T{NNN} — {description}`.

---

## Gotchas

- **Central package management** — never put `<Version>` on a `<PackageReference>` in a `.csproj`. Pin in `Directory.Packages.props`. Central transitive pinning is ON, so mixed versions produce warnings.
- **Fixture parameterless constructor** — TUnit needs it for automatic instantiation. Always provide one that delegates to `new {Provider}FixtureOptions()`.
- **`IsolationKey`** — derived from `ExecutionContext` by the family base. Don't override unless adding a provider-specific suffix.
- **Testcontainers wait strategy** — default `Wait.ForListeningPorts()` works for 80% of containers. For Oracle use the same + 5-min timeout (aspire#12036). For Cosmos use the custom `/_explorer/emulator.pem` probe (see [research.md](research.md) §R4).
- **Windows CI runners** — Cosmos Linux emulator requires Linux containers. Skip (don't fail) on Windows.
- **`*Contract.cs` hygiene** — per C-003, contract base classes can't declare inline helper types. Extract to `TestInfrastructure/ContractHelpers/`.
- **Test file organization** — one top-level class per file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/`. A 355-line test file stays as one class — only move helpers, don't split by method-under-test.

---

## Sanity check — am I done?

- [ ] `dotnet build` clean, zero new warnings
- [ ] `dotnet test tests/Rig.TUnit.Databases.NoSql.Example.Tests.Integration/` green
- [ ] `dotnet test tests/Rig.TUnit.Architecture.Tests/` green (all three rules GREEN for this provider)
- [ ] Coverage ≥ 90% line / ≥ 85% branch for the new package
- [ ] `README.md` > 100 chars
- [ ] Package registered in `Rig.TUnit.slnx`
- [ ] `Rig.TUnit.All/Rig.TUnit.All.csproj` has a `ProjectReference` to the new package (if applicable)
- [ ] Commit log shows RED → GREEN → REFACTOR order

If all green, you're ready for PR review.

---

## Where to go next

- [spec.md](spec.md) — the feature's user stories and success criteria
- [plan.md](plan.md) — phased delivery, dependencies, risks
- [research.md](research.md) — .NET 10 driver decisions, compatibility notes
- [data-model.md](data-model.md) — per-provider inventory tables
- `src/Rig.TUnit/Contributing-ProviderTemplate.md` — canonical copy-paste template (created in Phase 1)
- `planning/provider-consistency-remediation/` — original design + handoff docs
