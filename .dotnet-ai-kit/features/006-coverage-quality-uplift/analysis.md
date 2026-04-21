# Analysis Report: Coverage & Quality Uplift

**Feature**: 006-coverage-quality-uplift | **Mode**: Generic
**Date**: 2026-04-21 | **Findings**: 10 (8 open, 2 resolved)

---

## Summary

- CRITICAL: 0
- HIGH: 5 (all require pre-step project creation during implementation)
- MEDIUM: 3
- LOW: 2 ✅ resolved

No blocking issues that prevent starting implementation — the missing test projects are work that needs to be done (create the projects), not architectural violations.

---

## Findings

### [HIGH] Coverage Gaps — T020 target test project does not exist

**Location**: `tasks.md` T020 / `tests/Rig.TUnit.Caching.Tests.Unit/`

**Details**: T020 specifies adding `CacheAssertTests.cs` and `ClockControlTests.cs` to `tests/Rig.TUnit.Caching.Tests.Unit/`. This directory does not exist on disk. The plan.md and research.md assumption that "each target package already has a `*.Tests.Unit` project" is wrong for this package.

**Suggested Fix**: T020 implementation must first create the `Rig.TUnit.Caching.Tests.Unit` project (`.csproj` + `sln` registration) before adding test files. The `.csproj` must reference `Rig.TUnit.Caching` and TUnit packages matching the other unit test projects.

---

### [HIGH] Coverage Gaps — T022 target test project does not exist

**Location**: `tasks.md` T022 / `tests/Rig.TUnit.Databases.NoSql.Tests.Unit/`

**Details**: T022 specifies adding `JsonDocumentAssertTests.cs` to `tests/Rig.TUnit.Databases.NoSql.Tests.Unit/`. This directory does not exist on disk.

**Suggested Fix**: T022 implementation must create the `Rig.TUnit.Databases.NoSql.Tests.Unit` project before adding test files. Reference `Rig.TUnit.Databases.NoSql` + TUnit packages.

---

### [HIGH] Coverage Gaps — T024 target test project does not exist

**Location**: `tasks.md` T024 / `tests/Rig.TUnit.Messaging.Tests.Unit/`

**Details**: T024 specifies adding `MessagingAssertTests.cs` to `tests/Rig.TUnit.Messaging.Tests.Unit/`. This directory does not exist on disk.

**Suggested Fix**: T024 implementation must create `Rig.TUnit.Messaging.Tests.Unit`. Reference `Rig.TUnit.Messaging` + TUnit packages. All tests use in-memory `List<CapturedMessage<T>>` — no additional dependencies needed.

---

### [HIGH] Coverage Gaps — T025 target test project does not exist

**Location**: `tasks.md` T025 / `tests/Rig.TUnit.Security.Tests.Unit/`

**Details**: T025 specifies adding `SecurityAssertTests.cs` to `tests/Rig.TUnit.Security.Tests.Unit/`. This directory does not exist on disk.

**Suggested Fix**: T025 implementation must create `Rig.TUnit.Security.Tests.Unit`. Reference `Rig.TUnit.Security` + `NSubstitute` + TUnit packages.

---

### [HIGH] Coverage Gaps — T026 target test project does not exist

**Location**: `tasks.md` T026 / `tests/Rig.TUnit.Storage.Tests.Unit/`

**Details**: T026 specifies adding `BlobAssertTests.cs` and `BlobValueObjectTests.cs` to `tests/Rig.TUnit.Storage.Tests.Unit/`. This directory does not exist on disk.

**Suggested Fix**: T026 implementation must create `Rig.TUnit.Storage.Tests.Unit`. Reference `Rig.TUnit.Storage` + `NSubstitute` + TUnit packages.

---

### [MEDIUM] Naming Consistency — Oracle extensions class name mismatch in plan.md

**Location**: `plan.md` T012 / `src/Rig.TUnit.Databases.Sql.Oracle/Builder/`

**Details**: `plan.md` T012 refers to the Oracle extensions class as `OracleBuilderExtensions` (non-standard, missing "Rig" infix). The actual file on disk is `OracleRigBuilderExtensions.cs`, which matches the project convention `{Provider}RigBuilderExtensions`. The test method names in `tasks.md` (e.g., `UseOracle_NullRig_ThrowsArgumentNullException`) are correct for the extension method name.

**Suggested Fix**: When implementing T012, use the correct class name `OracleRigBuilderExtensions` in both the test `using` directives and documentation comments. No plan file change needed — this is a plan-vs-actual discrepancy that resolves during implementation.

---

### [MEDIUM] T040 (Benchmark CoreRuntime) — edit reverted; task still pending

**Location**: `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` line 18

**Details**: An edit from `CoreRuntime.Core80` to `CoreRuntime.Core100` was applied to this file earlier in the session, but the file was subsequently reverted to `Core80` (confirmed by current file state). `tasks.md` correctly shows T040 as `[ ]` (not started). No inconsistency in the task list, but implementers should be aware the prior edit did not persist.

**Suggested Fix**: T040 is a pending task — apply the edit and commit with `green(T040):`. No other action needed.

---

### [MEDIUM] Progress Summary table in tasks.md is stale

**Location**: `tasks.md` bottom section "Progress Summary"

**Details**: The Progress Summary table still shows `⬜ Not started` for Phase 1 even though T001 and T002 are marked `[x]` in the task list above. T003 (CI verification) is still pending.

**Suggested Fix**: Update the Progress Summary row for Phase 1 to `🟡 In progress` once T003 is confirmed, then `✅ Done` after Phase 1 PR merges. This is a tracking issue only — no implementation impact.

---

### [LOW — RESOLVED] T015 references `Caching.Fusion.Tests.Integration` — confirmed

**Location**: `tasks.md` T015 / `tests/Rig.TUnit.Caching.Fusion.Tests.Integration/`

**Resolution**: `Rig.TUnit.Caching.Fusion.Tests.Integration` is confirmed in `Rig.TUnit.slnx` (line 133). T015 integration tests may be placed there directly. No action required.

---

### [LOW — RESOLVED] T022 Cosmos emulator — confirmed in CI matrix

**Location**: `tasks.md` T022 / `tests/Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration/`

**Resolution**: `Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration` is confirmed in `Rig.TUnit.slnx` (line 181). The Cosmos integration project exists and is wired into the solution. T022 `ChangeFeedCapture` tests may target this project directly. No action required.

---

## Architecture Consistency: PASS

All planned changes are additive (new test files, new test projects, CI YAML edits, docs). The dependency direction Core ← Family ← Provider ← Test is preserved. No production source files change. No new NuGet packages are introduced. No cross-family project references are introduced.

## Requirement Traceability: PASS (with HIGH findings above)

Every FR (FR-060 … FR-069) maps to at least one task. Every task traces to at least one FR. No orphaned tasks. The 5 missing unit test projects are gaps in the research assumption, not gaps in coverage — the implementation steps can create these projects as part of their task scope.

---

## Next Steps

1. **T020/T022/T024/T025/T026 implementors**: Each task must create its missing unit test project (`.csproj` + `Rig.TUnit.slnx` registration) as the first step before adding test files. Pattern: reference `Rig.TUnit.{Package}` source + `TUnit` + `NSubstitute` packages.
2. **T040**: Re-apply the `Core80` → `Core100` edit (current file is reverted); verify build; commit.
3. **T003**: Monitor [PR #6](https://github.com/FaysilAlshareef/Rig.TUnit/pull/6) CI run; record run ID in PR description.
4. ~~Verify `Caching.Fusion.Tests.Integration` — resolved.~~
5. ~~Confirm Cosmos emulator CI matrix entry — resolved.~~
