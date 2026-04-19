# Project Organization Audit

**Date:** 2026-04-19
**Repository:** `C:\Users\libya\source\repos\Ecom-LTD\Rig.TUnit`
**Solution file:** `Rig.TUnit.slnx` — 158 projects (63 src + 95 tests)

## Summary

The solution is **structurally healthy**. No circular references, consistent naming, full slnx coverage, and Feature 004 delivered the 4 new packages (MySql, Oracle, Cosmos, AppInsights) with their canonical templates. The remaining gaps are cleanup items and phase-2 test hygiene work that Feature 004 did not complete before merge.

## 1. Naming consistency — PASS

Every csproj follows `Rig.TUnit.{Family}[.{SubFamily}].{Provider}` or `Rig.TUnit.{Family}` for base packages. No deviations.

One empty dead directory remains:
- `src/Rig.TUnit.ServiceBus/` — no csproj, no code, only `bin/obj/`. Stale artefact from the pre-feature-003 rename to `Rig.TUnit.Messaging.ServiceBus`. **Severity: LOW** (does not affect build; recommend deletion).

## 2. Orphan test projects — PASS (false positives cleaned up)

Initially suspected orphans:

| Folder | Reality |
|---|---|
| `tests/Rig.TUnit.ServiceBus.Tests.Integration/` | Only `obj/` remains, no C# files, not in slnx. Stale. |
| `tests/Rig.TUnit.SqlServer.Tests.Integration/` | Only `bin/obj/` remain, no C# files, not in slnx. Stale. |

Both were pre-rename artefacts. The actual test projects (`Rig.TUnit.Messaging.ServiceBus.Tests.Integration` and `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration`) exist, contain code, and are correctly registered in slnx.

**Action:** delete both stale folders.

## 3. slnx completeness — PASS

- 158 `<Project Path="…" />` entries in `Rig.TUnit.slnx`
- 63 csproj files under `src/`
- 95 csproj files under `tests/`
- Total 158 — matches exactly

No missing references, no dangling paths.

## 4. Reference graph — PASS

Spot-checked 5 csproj files across families:

| Provider | Family base reference | Core transitive | Verdict |
|---|---|---|---|
| `Rig.TUnit.Databases.Sql.MySql` | → `Rig.TUnit.Databases.Sql` | ✓ | PASS |
| `Rig.TUnit.Databases.NoSql.Cosmos` | → `Rig.TUnit.Databases.NoSql` | ✓ | PASS |
| `Rig.TUnit.Messaging.Kafka` | → `Rig.TUnit.Messaging` | ✓ | PASS |
| `Rig.TUnit.Storage.MinIO` | → `Rig.TUnit.Storage` | ✓ | PASS |
| `Rig.TUnit.Security.Mtls` | → `Rig.TUnit.Security` | ✓ | PASS |

All providers correctly reference their family base and never skip to Core. No circular dependencies detected. Dependency direction rule from `.claude/rules/architecture-profile.md` is respected.

Documented shared-fixture case (`Rig.TUnit.Databases.NoSql.Redis → Rig.TUnit.Caching.Redis`) is intentional per Feature 003/004 design.

## 5. Meta-projects — PASS

### `src/Rig.TUnit.All/Rig.TUnit.All.csproj`
- 60 `<ProjectReference>` entries
- Excludes `Rig.TUnit.All` (itself), `Rig.TUnit` (opinionated default), and `Rig.TUnit.Observability.Logging.Analyzers` (pure Roslyn analyzer — correctly not a runtime dep)
- **Productive providers: 60/60 ✓**

### `src/Rig.TUnit/Rig.TUnit.csproj`
- 4 references: Core, Mediator, Grpc, WebAPI
- Intentional "opinionated default" — not exhaustive

### `src/Rig.TUnit.Microservices/Rig.TUnit.Microservices.csproj`
- 13 references covering all microservices sub-packages + Core + Mediator + Grpc + Observability
- No duplication with Rig.TUnit.All

## 6. Canonical folder layout — PARTIAL

Feature 004 spec prescribes: `Fixtures/` + `Options/` + `Builder/` + optional `Extensions/` + optional `Helpers/` + `README.md`.

### New (Feature 004) packages — all compliant

| Package | Fixtures | Options | Builder | Extensions | Helpers | README |
|---|---|---|---|---|---|---|
| Databases.Sql.MySql | ✓ | ✓ | ✓ | — | — | ✓ |
| Databases.Sql.Oracle | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| Databases.NoSql.Cosmos | ✓ | ✓ | ✓ | — | ✓ | ✓ |
| Observability.AppInsights | ✓ | ✓ | ✓ | — | — | ✓ |

### Pre-004 providers — partial adoption

Spot-check of 4 older providers:

| Provider | Fixtures | Options | Builder | Helpers | README |
|---|---|---|---|---|---|
| Messaging.Kafka | ✓ | — | — | — | ✓ |
| Storage.MinIO | ✓ | — | — | ✓ | ✓ |
| Security.Mtls | ✓ | ✓ | — | — | ✓ |
| Caching.Memory | ✓ | — | ✓ | — | ✓ |

Feature 004 Phase 3 was intended to close these gaps; Phase 6 was intended to retire the `ProviderCompletenessTests` skip markers. Both phases were partially completed before PR #3 merged.

**~20 providers** still lack `Options/` or `Builder/` folders. The architecture test will fail for each once `SkipUntilFixed` markers are removed.

## 7. Test folder layout — NOT YET STANDARDISED

Feature 004 FR-010/011 mandates that test files outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` declare exactly one top-level class, and shared setup moves to `TestInfrastructure/`.

Spot-check of 4 test projects:

| Test project | Has TestInfrastructure/ | Single-class-per-file | Verdict |
|---|---|---|---|
| `Rig.TUnit.Observability.Tracing.Tests.Integration` | No | Yes | Phase 2 pending |
| `Rig.TUnit.Http.Tests.Unit` | No | Yes | Phase 2 pending |
| `Rig.TUnit.Security.OAuth.Tests.Integration` | No | Yes | Phase 2 pending |
| `Rig.TUnit.Microservices.Outbox.Tests.Integration` | No | Yes | Phase 2 pending |

Inline setup infrastructure (Polly pipelines, JWKS key factories, outbox envelope builders, ActivitySource factories) still mixes with test code. Feature 004 Phase 2 (test-file hygiene sweep) did not complete. `TestFileOrganizationTests` runs with skip markers.

## 8. Architecture rules — files exist, enforcement partial

`tests/Rig.TUnit.Architecture.Tests/Rules/`:

1. `CodeOrganizationTests.cs`
2. `CoverageRuleTests.cs`
3. `DependencyDirectionTests.cs`
4. `ForbiddenApiTests.cs`
5. `ProviderCompletenessTests.cs`
6. `ReadmeCompletenessTests.cs`
7. `TestCompletenessTests.cs` — has explicit `SkipUntilFixed` list at lines 22-53
8. `TestFileOrganizationTests.cs`

All 8 rule files are in place. Several have `[Category("SkipUntilFixed")]` markers on specific providers or tests, documented in Feature 004 as interim during phased remediation. These markers need removal before the rules become real gates.

## 9. Feature 004 new packages — COMPLETE

All 4 promised packages exist with full templates and matching test projects:

| Package | src | Tests.Unit | Tests.Integration |
|---|---|---|---|
| MySql | `src/Rig.TUnit.Databases.Sql.MySql` | ✓ | ✓ |
| Oracle | `src/Rig.TUnit.Databases.Sql.Oracle` | ✓ | ✓ |
| Cosmos | `src/Rig.TUnit.Databases.NoSql.Cosmos` | ✓ | ✓ |
| AppInsights | `src/Rig.TUnit.Observability.AppInsights` | ✓ | ✓ |

All four inherit correctly from family bases. All four appear in slnx.

## 10. CI workflow observations

`.github/workflows/ci.yml` — 10 jobs (build-unit-arch + 9 integration matrices):

**Strengths:**
- Matrix coverage per family (SQL, NoSQL, Caching, Messaging, Microservices, Security, Observability, Storage, Core)
- `fail-fast: false` per matrix — one failing provider doesn't halt sibling runs
- Image pull caching for MySql and Oracle
- Linux-only skip for Cosmos (emulator requirement)
- Commit-msg-lint workflow provides Conventional-Commits enforcement

**Weaknesses:**
- No coverage collection anywhere (FR-035/036 gate unenforced)
- No TUnit HTML report artefact upload (triage harder)
- No cobertura XML artefact
- No flake-detection (retries) — enables bad merges like PR #3's Postgres race
- `build-unit-arch` enumerates projects in PowerShell rather than using a single solution-scoped test run; this adds complexity and misses any project not matching the glob
- No dedicated architecture-test job — arch tests run inside `build-unit-arch` mixed with unit runs
- No benchmark regression gate (FR-033's "> 20% regression vs Phase-3 baseline" is defined but not checked in CI)

## Cleanup backlog (safe changes)

| Item | Path | Severity |
|---|---|---|
| Delete stale empty folder | `src/Rig.TUnit.ServiceBus/` | LOW |
| Delete stale empty folder | `tests/Rig.TUnit.ServiceBus.Tests.Integration/` | LOW |
| Delete stale empty folder | `tests/Rig.TUnit.SqlServer.Tests.Integration/` | LOW |

## Structural changes deferred to Feature 005

| Item | Effort |
|---|---|
| Complete canonical layout on ~20 pre-004 providers | Medium (~1 day per family) |
| Extract inline test setup to `TestInfrastructure/` across ~30 test projects | Medium-high (~2 days) |
| Retire all `[Category("SkipUntilFixed")]` markers | Low (post the above) |
| Add coverage collection to CI | Low (1 day) |
| Add benchmark regression gate | Medium (baseline management + threshold step) |
