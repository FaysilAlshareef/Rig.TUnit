# ADR-004: TUnit over xUnit as the test-framework substrate

**Status**: Accepted
**Date**: 2026-01 (Feature 001)
**Context**: The library chose between xUnit, NUnit, MSTest, and TUnit for its own
test projects + as the recommended framework for consumers.

## Decision

TUnit (Microsoft.Testing.Platform–native) is the canonical substrate.

## Rationale

1. **MTP-native** — TUnit targets Microsoft.Testing.Platform directly, so
   `dotnet test --coverage` (MTP collector) works out of the box without coverlet.
2. **Parallelism by default** — TUnit runs test classes in parallel without
   `[Collection]` attributes, matching our per-test-isolation architecture.
3. **Async-first assertions** — `await Assert.That(x).IsEqualTo(y)` — no
   sync-over-async footguns.
4. **Data-driven tests** via `[InheritsTests]` enable per-provider contract inheritance
   without repeating `[Theory]` + `[MemberData]` plumbing.

## Consequences

- **MTP-only ecosystem** — `dotnet test --filter Category=X` syntax varies from xUnit;
  provider docs show the TUnit-correct form.
- **coverlet.msbuild is incompatible** with MTP and is forbidden in new code (see
  CONTRIBUTING.md).
- Consumers already invested in xUnit can keep using xUnit — Rig.TUnit fixtures +
  helpers work with any test framework (they're just disposables + factories). Only
  the *contract suites* require TUnit.
