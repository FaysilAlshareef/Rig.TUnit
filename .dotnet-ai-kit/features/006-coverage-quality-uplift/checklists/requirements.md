# Requirements Quality Checklist — 006-coverage-quality-uplift

Generated: 2026-04-21

## Specification Quality

- [x] User stories have priorities (P1, P2, P3)
- [x] Each user story is independently testable
- [x] Each user story has ≥ 1 acceptance scenario in Given/When/Then form
- [x] Maximum 3 `[NEEDS CLARIFICATION]` markers — 0 present (all items have reasonable defaults)
- [x] Requirements are testable and verifiable by artefact or CI output
- [x] Key entities identified: source package, integration-test project, coverage gate, benchmark baseline, builder pattern
- [x] Edge cases documented (7 edge cases)
- [x] Success criteria are measurable (9 SC items with specific thresholds)

## Functional Requirements

- [x] FR-060 — line coverage gate threshold specified (≥ 90 %)
- [x] FR-061 — branch coverage gate threshold specified (≥ 85 %)
- [x] FR-062 — CI matrix extension scoped to exact 6 project names
- [x] FR-063 — coverage gate hardening is verifiable by `ci.yml` diff
- [x] FR-064 — baseline-006.json entry count and runtime field verifiable by file inspection
- [x] FR-065 — regression threshold specified (≥ 20 %)
- [x] FR-066 — README section count specified (14); link-checker verifiable by CI job
- [x] FR-067 — TDD commit discipline verifiable by `commit-discipline-gate` job
- [x] FR-068 — Clean Architecture verifiable by `.csproj` diff audit
- [x] FR-069 — API stability verifiable by `PublicApiAnalyzers` / `PublicAPI.Shipped.txt` diff

## Architecture Constraints

- [x] .NET version detected: `net10.0` / C# 14
- [x] No new production source files — all changes in tests, CI, benchmark config, docs
- [x] Dependency direction documented and preserved
- [x] Reference implementations identified for builder, contract, and fixture patterns
- [x] `InternalsVisibleTo` scope restricted to matching `.Tests.Unit` project only
- [x] No cross-family project references
- [x] No library swaps — TUnit / MSTest / Cobertura / BenchmarkDotNet remain unchanged

## TDD Discipline

- [x] RED → GREEN commit discipline specified per task
- [x] Tests-only (pre-existing code) commit convention documented: single `green(T###):` with note
- [x] Commit message prefixes defined: `red(T###):`, `green(T###):`
- [x] `--amend` across RED/GREEN boundary explicitly prohibited
- [x] `--no-verify` explicitly prohibited

## Phase Ordering

- [x] Phase 1 identified as BLOCKING (must merge before Phases 2–4)
- [x] Phases 2, 3, 4 identified as parallel-eligible after Phase 1
- [x] Phases 5, 6 identified as fully independent
- [x] Phase 7 identified as LAST (only after SC-060 and SC-061 GREEN)

## Risks

- [x] All 6 risks documented with likelihood, impact, and mitigation
- [x] High-impact risks have concrete mitigations (`continue-on-error` fallback for unstable integration tests)

## Planning References

- [x] All 11 planning reference files listed with purpose
- [x] Ground-truth data sources identified (`summary.csv`, `merged.cobertura.xml`)
- [x] CI anchor lines documented (lines 294, 363 in `ci.yml`)

## Items Requiring No Clarification

The following were judged to have clear defaults and were NOT marked `[NEEDS CLARIFICATION]`:

| Item | Default Applied |
|------|----------------|
| Regression threshold for benchmarks | 20 % (from feature spec) |
| Minimum baseline-006.json entry count | 50 (from FR-064) |
| README section count | 14 (from README-Rewrite-Plan.md) |
| C# language version | C# 14 (from `net10.0` target) |
| TDD commit prefix format | `red(T###):` / `green(T###):` (from HARD CONSTRAINT) |
