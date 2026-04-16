# Requirements Quality Checklist — 001-rig-tunit-library

## User Stories
- [x] All user stories have acceptance scenarios with Given/When/Then
- [x] All user stories have priorities assigned (P1: 5, P2: 2, P3: 2)
- [x] Each user story is independently testable
- [x] Stories cover all 6 source projects in the solution
- [x] Stories cover unit tests (US-7), integration tests (US-8), and benchmarks (US-9)

## Requirements
- [x] All functional requirements are testable (build verification, naming, versioning)
- [x] Requirements reference specific versions and constraints from handoff doc
- [x] No ambiguous "should" language — all use MUST
- [x] Testing requirements (FR-013 to FR-022) specify TDD methodology, coverage, naming, and structure

## Key Entities
- [x] All 10 key entities identified with descriptions
- [x] Generic type parameters documented (`<T>`, `<TClient, TProgram>`, `<TContext>`)
- [x] No service-specific types referenced in source projects

## Architecture
- [x] Package dependency graph documented (source projects)
- [x] Test project dependency graph documented (6 test projects)
- [x] All 12 projects listed with their files (6 source + 6 test)
- [x] Solution file format specified (slnx) — 12 projects total
- [x] NuGet dependencies per package documented in handoff doc
- [x] Test NuGet dependencies documented per test project

## Edge Cases
- [x] Parallel test isolation addressed
- [x] Container startup failure documented
- [x] Missing config file scenario covered
- [x] DbContext scope leak prevention documented
- [x] TUnit assertion pitfall (forgetting `await`) documented
- [x] Serilog sink incompatibility documented

## Constraints
- [x] Explicit DO NOT list with 10 items
- [x] TDD red-green-refactor methodology enforced
- [x] No mocking for container-based tests
- [x] Test infrastructure types scoped to individual test projects
- [x] Edge case test coverage required (empty collections, nulls, timeouts, disposal)
- [x] No abstract base classes for fixtures
- [x] No packaging/CI/CD in scope

## Success Criteria
- [x] 16 measurable success criteria defined across 4 categories
- [x] Primary criterion is `dotnet build` clean compilation (SC-001)
- [x] Unit test pass criterion (SC-008) — no Docker dependency
- [x] Integration test pass criterion (SC-011) — real containers
- [x] Benchmark execution criterion (SC-015) — BenchmarkDotNet summary
- [x] Coverage criterion (SC-009) — every public type/method has tests
- [x] File path and namespace matching verifiable (SC-004, SC-007)

## Clarifications
- [x] 6 clarifications resolved (C-001 to C-006)
- [x] Handoff document path resolved (C-001)
- [x] Generic TProgram parameter for FR-003 compliance (C-002)
- [x] File count corrected to 25 (C-003)
- [x] MetadataHelper claims format specified (C-004)
- [x] GrpcServiceReplacementExtensions purpose clarified (C-005)
- [x] TDD scope change documented (C-006)
