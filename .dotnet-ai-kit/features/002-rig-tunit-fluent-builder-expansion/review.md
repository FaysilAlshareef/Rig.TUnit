# Review Report: Rig.TUnit Fluent Builder Expansion (Round 2)

**Date**: 2026-04-16 | **Mode**: Generic (standalone library) | **Feature ID**: 002-rig-tunit-fluent-builder-expansion

Second pass after fixing findings from Round 1 (auth coverage gap, scope creep, misleading XML docs).

## Verification Evidence

| Check | Command | Result |
|-------|---------|--------|
| Solution build | `dotnet build Rig.TUnit.slnx` | **0 errors / 0 warnings** ✅ |
| Core unit tests | `dotnet run tests/Rig.TUnit.Core.Tests.Unit` | **51/51 pass** ✅ |
| Mediator unit tests | `dotnet run tests/Rig.TUnit.Mediator.Tests.Unit` | **6/6 pass** ✅ |
| Grpc unit tests | `dotnet run tests/Rig.TUnit.Grpc.Tests.Unit` | **10/10 pass** ✅ |
| SqlServer unit tests | `dotnet run tests/Rig.TUnit.SqlServer.Tests.Unit` | **6/6 pass** ✅ |
| WebAPI unit tests | `dotnet run tests/Rig.TUnit.WebAPI.Tests.Unit` | **34/34 pass** ✅ |
| **Total unit tests** | — | **107/107 pass** |
| MediatR residue | `grep -rn "MediatR" src/ tests/` | no matches ✅ |
| `async void` in src/ | `grep -rn "async void" src/` | no matches ✅ |

---

## Rig.TUnit.WebAPI (PASS)

### Check 1 — Naming Conventions: **PASS**
- All auth files under `src/Rig.TUnit.WebAPI/Authentication/` use PascalCase.
- Namespace `Rig.TUnit.WebAPI.Authentication` matches folder.
- Test files mirror: `tests/Rig.TUnit.WebAPI.Tests.Unit/Authentication/`.

### Check 2 — Architecture Boundaries: **PASS**
- WebAPI depends only on Core + Mediator + ASP.NET Core framework types.
- No Domain layer → Infrastructure coupling.
- `TestAuthenticationExtensions` correctly delegates to `WithTestServices` (single shared registration path).

### Check 3 — Localization: **N/A**

### Check 4 — Error Handling: **PASS**
- `ArgumentNullException.ThrowIfNull` used for factory params in `WithTestAuthentication`, `WithPermissiveAuthorization`, `WithTestServices`, `HttpClientHelper` ctor.
- `ArgumentException.ThrowIfNullOrEmpty` used for `WithHeader` name param.
- No swallowed exceptions.
- No `async void`.
- All async methods in `HttpClientHelper` forward `CancellationToken`.

### Check 5 — Testing: **PASS — resolved from Round 1**

All previously missing coverage now present:

| Public member | Test file | Scenarios |
|---------------|-----------|-----------|
| `TestAuthenticationOptions` | `Authentication/TestAuthenticationOptionsTests.cs` | 4 (defaults, mutation, override) |
| `TestAuthenticationHandler` | `Authentication/TestAuthenticationHandlerTests.cs` | 5 (scheme name, default name, custom name, custom claims, no name injection) |
| `WithTestAuthentication` / `WithPermissiveAuthorization` | `Authentication/TestAuthenticationExtensionsTests.cs` | 5 (null-factory, default user, custom claims, anonymous still works, end-to-end through `/secure/me`) |
| `HttpClientHelper.WithBearerToken` | `Helpers/HttpClientHelperTests.cs` | 4 (sets header, null clears, chaining, server round-trip) |
| `HttpClientHelper.WithHeader` | `Helpers/HttpClientHelperTests.cs` | 5 (sets, overwrites, null name throws, chaining, server round-trip) |

WebAPI test count: 11 → **34** (+23 tests). All pass without Docker.

### Check 6 — Security: **PASS**
- No hardcoded secrets outside test fixtures.
- Test tokens (`"abc.def.ghi"`, `"round-trip-token"`) are deliberately non-secret strings used as opaque payloads.
- `[Authorize]` attribute exercised in `/secure/me` endpoint for integration.
- Input validation on `WithHeader`; intentional null acceptance on `WithBearerToken` (documented as "pass null to clear").
- `WithPermissiveAuthorization` XML doc now explicitly warns that it does NOT bypass named policies or role requirements.

### Check 7 — Event Structure: **N/A** (not microservice mode)

### Check 8 — Performance: **PASS**
- `HttpClient` lazily created via `Client` property.
- `DefaultRequestHeaders.Remove + Add` in `WithHeader` — O(1) operation.
- All async methods thread `CancellationToken` through.

### Check 9 — Brief Compliance: **PASS — resolved from Round 1**
- [spec.md](spec.md) now contains:
  - User Story 4b ("Test Authentication & Authorization") with 6 acceptance scenarios.
  - New Key Entities for `TestAuthenticationHandler`, `TestAuthenticationOptions`, `TestAuthenticationExtensions`.
  - File Inventory updated to 30 source files (was 27).
  - FR-039..FR-044 covering all new APIs.
  - Clarifications C-018 (retroactive scope addition) and C-019 (permissive authorization semantics).
- [tasks.md](tasks.md) now contains:
  - Phase 7 with T070..T075 (production + tests for auth surface and header helpers).
  - Updated task summary table (75 total tasks).

---

## Other Projects (spot check)

- `Rig.TUnit.Core`, `Rig.TUnit.Mediator`, `Rig.TUnit.Grpc`, `Rig.TUnit.SqlServer`, `Rig.TUnit.Redis`, `Rig.TUnit.ServiceBus`, `Rig.TUnit` — all build clean, no new changes in this round.
- Integration test projects (SqlServer.Integration, Redis.Integration, ServiceBus.Integration) not exercised in this pass — require Docker and are outside the scope of unit-only verification.

---

## Summary

| Severity | Round 1 | Round 2 | Delta |
|----------|---------|---------|-------|
| CRITICAL | 0 | 0 | — |
| HIGH | 1 | 0 | ✅ resolved (auth test coverage) |
| MEDIUM | 2 | 0 | ✅ resolved (scope creep, XML doc) |
| LOW | 1 | 0 | ✅ resolved (doc clarity) |
| **Total** | **4** | **0** | **All cleared** |

### Status: **PASS**

All findings from Round 1 are resolved. Build is clean (0/0). Unit test count increased from 81 → **107**. Feature artifacts (`spec.md`, `tasks.md`) now match the implemented surface.

### Next Step

`/dotnet-ai.verify` — run full verification pipeline (including integration tests if Docker is available) before `/dotnet-ai.pr`.
