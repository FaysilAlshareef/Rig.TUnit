# Quickstart: 002-rig-tunit-fluent-builder-expansion

## Prerequisites

- .NET 10 SDK
- Docker (for integration tests)
- martinothamar/Mediator 3.0.2 (Mediator.Abstractions + Mediator.SourceGenerator)

## Start Implementation

```bash
# Ensure clean baseline
dotnet build Rig.TUnit.slnx
dotnet test --filter "Category!=Integration"
```

## TDD Workflow Per Step

```bash
# 1. Write test (RED)
# Create test file with failing tests

# 2. Verify test fails
dotnet test --filter "FullyQualifiedName~WaitHelperTests"

# 3. Implement (GREEN)
# Write minimal code to make tests pass

# 4. Verify test passes
dotnet test --filter "FullyQualifiedName~WaitHelperTests"

# 5. Refactor (REFACTOR)
# Clean up, then re-run tests
```

## Phase Order

| Phase | Command After Phase | Expected |
|-------|-------------------|----------|
| 0 | `dotnet build` | Zero errors, 56 tests pass |
| 1 | `dotnet test --filter "Category!=Integration"` | 56 + ~30 new unit tests pass |
| 2 | `dotnet test --filter "Category!=Integration"` | Mediator tests pass, MediatR gone |
| 3 | `dotnet test --filter "Category!=Integration"` | WebAPI tests pass |
| 4 | `dotnet test` | All builders work, old extensions gone |
| 5 | `dotnet test` | Enhancements verified |
| 6 | `dotnet build Rig.TUnit.slnx` | 17 projects, zero warnings |

## Key Files to Start With

```
# Phase 1 entry point:
tests/Rig.TUnit.Core.Tests.Unit/Builder/ConnectionSourceTests.cs  ← write first
src/Rig.TUnit.Core/Builder/IRigConnectionSource.cs                ← implement after

# Phase 2 entry point:
tests/Rig.TUnit.Mediator.Tests.Unit/Helpers/HandlerHelperTests.cs ← write first
src/Rig.TUnit.Mediator/Helpers/HandlerHelper.cs                   ← implement after

# Phase 3 entry point:
tests/Rig.TUnit.WebAPI.Tests.Unit/Helpers/HttpClientHelperTests.cs ← write first
src/Rig.TUnit.WebAPI/Helpers/HttpClientHelper.cs                   ← implement after
```

## Common Pitfalls

1. **Mediator.SourceGenerator placement**: Only in test/consumer projects, never in library
2. **await Assert.That(...)**: TUnit assertions MUST be awaited or they silently pass
3. **TreatWarningsAsErrors**: Directory.Build.props enforces this — no suppressed warnings
4. **IRigConnectionSource on fixtures**: Just add interface, don't change inheritance
5. **InMemoryDbExtensions**: Keep this file — it's the only old extension that survives
