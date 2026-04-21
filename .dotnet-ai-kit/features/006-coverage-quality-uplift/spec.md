# Feature Specification: Coverage & Quality Uplift

**Feature ID**: 006-coverage-quality-uplift
**Created**: 2026-04-21
**Status**: Draft
**Branch**: `feat/006-coverage-quality-uplift`
**Input**: "Feature 006 — Coverage & Quality Uplift (from CI scan 2026-04-21, run id 24712477011)"

---

## Problem Statement

The 2026-04-21 CI coverage scan exposed systemic gaps across the Rig.TUnit ecosystem:

- **29 of 40 source packages** fail the ≥ 90 % line-coverage gate; overall is **80.4 % line / 66.4 % branch** against targets of **≥ 90 % / ≥ 85 %**.
- **6 integration-test projects** that exist on disk are never executed in production CI: `Rig.TUnit.Core.Tests.Integration`, `Rig.TUnit.Ci.Tests.Integration`, `Rig.TUnit.Grpc.Tests.Integration`, `Rig.TUnit.Http.Tests.Integration`, `Rig.TUnit.WebAPI.Tests.Integration`, `Rig.TUnit.Mediator.Tests.Integration`.
- `.github/workflows/ci.yml` line 363 has the coverage gate on `continue-on-error: true` with no re-enable task.
- `benchmarks/baseline-005.json` is empty and `InProcessEmitBenchmarkConfig.cs` line 18 targets `CoreRuntime.Core80` while the solution targets `net10.0` — benchmark regression detection is non-functional.
- Root `README.md` is a placeholder.

**Mission**: Raise every failing source package to its coverage gate, close the six missing integration-test gaps, restore functional benchmark regression detection, ship a production-quality root README, and re-enable the coverage gate as a hard block — **without changing public API surface or behaviour**.

---

## User Stories

### User Story 1 — Full integration-test matrix in CI (Priority: P1)

As a maintainer, I need all 6 missing integration-test projects to run in CI so that coverage measurements are accurate and regressions in integration paths are caught automatically.

**Acceptance Scenarios**:
1. **Given** a PR is pushed to `feat/006-coverage-quality-uplift`, **When** the `integration-core` CI job runs, **Then** all of `Core, Ci, Grpc, Http, WebAPI, Mediator` appear in the job matrix with a PASS result.
2. **Given** an integration test fails, **When** CI runs, **Then** the job is marked FAILED (not skipped or continue-on-error), blocking merge.

### User Story 2 — All packages meet coverage gates (Priority: P1)

As a developer, I need every source package to report ≥ 90 % line and ≥ 85 % branch coverage so the quality bar is enforced and regressions are visible.

**Acceptance Scenarios**:
1. **Given** the coverage scan workflow runs on `master`, **When** `summary.csv` is generated, **Then** zero packages appear below the 90 % line gate.
2. **Given** the coverage scan workflow runs on `master`, **When** `summary.csv` is generated, **Then** zero packages appear below the 85 % branch gate.
3. **Given** a PR drops a package below 90 % line, **When** the coverage gate step runs, **Then** the step fails and merge is blocked.

### User Story 3 — Coverage gate is a hard block (Priority: P1)

As a CI pipeline operator, I need the coverage gate to be a non-skippable failure so no regression can silently land on `master`.

**Acceptance Scenarios**:
1. **Given** `.github/workflows/ci.yml`, **When** I inspect the coverage gate step at approximately line 363, **Then** `continue-on-error` is absent or set to `false`.
2. **Given** a deliberately-lowered package PR, **When** CI runs, **Then** the coverage gate step exits non-zero and the PR cannot be merged.

### User Story 4 — Functional benchmark regression detection (Priority: P2)

As a developer, I need benchmark results to target the correct runtime (.NET 10) and have a populated baseline so performance regressions ≥ 20 % are automatically detected and block merge.

**Acceptance Scenarios**:
1. **Given** `InProcessEmitBenchmarkConfig.cs`, **When** I read line 18, **Then** it references `CoreRuntime.Core100` (not `Core80`).
2. **Given** `benchmarks/baseline-006.json`, **When** I inspect it, **Then** it contains ≥ 50 entries with `runtime` fields showing `.NET 10.*`.
3. **Given** a PR that deliberately regresses a benchmark by 25 %, **When** the benchmark CI job runs, **Then** the job fails and the PR is blocked.

### User Story 5 — Production-quality root README (Priority: P2)

As a contributor, I need the root `README.md` to accurately describe the ecosystem, provider pattern, builder API, CI pipeline, and benchmarks so I can onboard and contribute without reading source code.

**Acceptance Scenarios**:
1. **Given** the root `README.md`, **When** I count sections, **Then** all 14 sections defined in `README-Rewrite-Plan.md` are present.
2. **Given** a link-checker CI job, **When** it runs against `README.md`, **Then** zero broken links are reported.
3. **Given** all code snippets in `README.md`, **When** compiled against the latest release, **Then** all snippets compile without error.

### User Story 6 — GitHub Pages benchmark trend chart (Priority: P3)

As a developer, I need a publicly reachable benchmark trend chart so I can monitor performance history without reading CI logs.

**Acceptance Scenarios**:
1. **Given** the `gh-pages` branch, **When** `benchmark-action/github-action-benchmark@v1` runs, **Then** benchmark data is pushed and the chart is accessible via GitHub Pages.
2. **Given** a new commit on `master`, **When** the benchmark job completes, **Then** the trend chart on GitHub Pages is updated.

---

## Requirements

### Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-060 | Every source package MUST report ≥ 90 % line coverage in `coverage-scan-results/summary.csv` |
| FR-061 | Every source package MUST report ≥ 85 % branch coverage in `coverage-scan-results/summary.csv` |
| FR-062 | The `integration-core` CI matrix MUST include `Core, Ci, Grpc, Http, WebAPI, Mediator` (currently missing) |
| FR-063 | The coverage gate step in `ci.yml` MUST NOT have `continue-on-error: true` |
| FR-064 | `benchmarks/baseline-006.json` MUST contain ≥ 50 entries with real .NET 10 mean values |
| FR-065 | The benchmark regression CI job MUST fail (block merge) on any ≥ 20 % regression |
| FR-066 | Root `README.md` MUST contain all 14 sections per `README-Rewrite-Plan.md`; link-checker MUST pass |
| FR-067 | Every behaviour task MUST have paired `red(T###):` + `green(T###):` commits (TDD discipline) |
| FR-068 | Clean Architecture layering MUST be preserved — no new cross-family project references |
| FR-069 | `PublicAPI.Shipped.txt` MUST be unchanged for all affected packages (no public API surface changes) |

### Non-Functional Requirements

- Solution targets `net10.0`; only C# 14 / .NET 10 features may be used.
- No new NuGet packages, test frameworks, or build systems introduced.
- No public API surface changes. No runtime behaviour changes.
- No refactoring of unrelated code while adding coverage tests.
- No folder structure reorganisation.
- All new tests follow `{Method}_{Scenario}_{ExpectedResult}` naming with Arrange-Act-Assert layout.

### Key Entities

- **Source package** — a `src/Rig.TUnit.*` project that ships as a NuGet package; 40 total, 29 below gate.
- **Integration-test project** — a `tests/Rig.TUnit.*.Tests.Integration` project executed against a real container or service; 6 currently absent from CI matrix.
- **Coverage gate** — the CI step in `.github/workflows/ci.yml` (~line 363) that enforces ≥ 90 % line / ≥ 85 % branch thresholds.
- **Benchmark baseline** — `benchmarks/baseline-006.json`, the BenchmarkDotNet JSON artefact used as the reference for regression detection.
- **Builder pattern** — `{Provider}RigBuilder` / `{Provider}RigBuilderExtensions` classes that wire connection sources; the primary uncovered surface in Pattern-A packages.

---

## Architecture Scope (Generic Mode)

**Affected layers** (all changes are additive — no layer contracts change):

| Layer | Changes |
|-------|---------|
| `tests/Rig.TUnit.*.Tests.Unit` | New test files for builder, assertion, and helper classes (Patterns A, B, C) |
| `tests/Rig.TUnit.*.Tests.Integration` | Existing integration projects wired into CI matrix; additional test cases for uncovered paths |
| `tests/Rig.TUnit.*.Tests.Contract` | Extended contract scenarios for `Messaging.Tests.Contract` (T039e) |
| `tests/Rig.TUnit.Benchmarks` | Runtime constant fix (`Core80` → `Core100`); new baseline JSON |
| `.github/workflows/ci.yml` | Matrix extension (T001), gate annotation (T002), gate hardening (T090), benchmark action (T042–T043), link-checker job (T065) |
| `README.md` (root) | Complete rewrite per 14-section plan (T060–T065) |

**No new production source files** — all changes are in test projects, CI config, benchmark config, or documentation.

**Dependency direction preserved**:
- Core (domain) ← Family base ← Provider ← Test projects
- Test projects reference their own provider package only
- No cross-family test references

**Reference implementations to follow**:
- Builder tests: `src/Rig.TUnit.Databases.Sql.Postgresql/Builder/PostgresRigBuilder.cs` (100 % covered)
- Contract tests: `Rig.TUnit.Caching.Tests.Contract`, `Rig.TUnit.Databases.Sql.Tests.Contract` (both 100 %)
- Fixture pattern: `Rig.TUnit.WebAPI` (100 %)

---

## Phase Plan

### Phase 1 — CI Foundation (BLOCKING)
Must merge before Phases 2–4.

| Task | Description | Exit Gate |
|------|-------------|-----------|
| T001 | Extend `integration-core` matrix to include `Core, Ci, Grpc, Http, WebAPI, Mediator` | All 6 projects appear with PASS in CI log |
| T002 | Annotate `continue-on-error: true` at `ci.yml:363` with re-enable reference | Annotation present in merged `ci.yml` |
| T003 | Verify all 6 newly-added integration projects PASS; record run id in PR | Run id in PR description |

### Phase 2 — Pattern A: Builder API Coverage (parallel after Phase 1)

| Task | Package | Current → Target |
|------|---------|-----------------|
| T010 | `Databases.Sql.SqlServer` | 51.4 % → ≥ 90 % |
| T011 | `Databases.Sql.MySql` | 72.9 % → ≥ 90 % |
| T012 | `Databases.Sql.Oracle` | 62.5 % → ≥ 90 % |
| T013 | `Databases.Sql.Sqlite` | 74.3 % → ≥ 90 % |
| T014 | `Databases.NoSql.Redis` | 23.5 % → ≥ 90 % |
| T015 | `Caching.Redis` | 38.0 % → ≥ 90 % |
| T016 | `Caching.Memory` | 63.1 % → ≥ 90 % |

### Phase 3 — Pattern B: Base-Family Assertion Coverage (parallel after Phase 1)

| Task | Package | Current → Target |
|------|---------|-----------------|
| T020 | `Caching` | 18.0 % → ≥ 90 % |
| T021 | `Databases` | 46.9 % → ≥ 90 % |
| T022 | `Databases.NoSql` | 12.5 % → ≥ 90 % |
| T023 | `Databases.Sql` | 43.5 % → ≥ 90 % |
| T024 | `Messaging` | 30.9 % → ≥ 90 % |
| T025 | `Security` | 25.9 % → ≥ 90 % |
| T026 | `Storage` | 16.6 % → ≥ 90 % |

### Phase 4 — Pattern C: Targeted Helper Coverage (parallel after Phase 1)

| Task | Package | Current → Target |
|------|---------|-----------------|
| T030 | `Grpc` | 40.4 % → ≥ 90 % |
| T031 | `Observability.Seq` | 25.5 % → ≥ 90 % |
| T032 | `Microservices.Contracts` | 35.0 % → ≥ 90 % — use `NSubstitute` + temp-dir files (not WireMock.Net, see C-001) |
| T033 | `Messaging.ServiceBus` | 59.7 % → ≥ 90 % |
| T034 | `Http` | 85.1 % → ≥ 90 % |
| T035 | `HealthChecks` | 83.7 % → ≥ 90 % |
| T036 | `Resilience` | 81.7 % → ≥ 90 % |
| T037 | `Microservices.Saga` | 77.8 % → ≥ 90 % |
| T038 | `Microservices.Outbox` | 82.7 % → ≥ 90 % |
| T039 | `Observability.AppInsights` | 71.7 % → ≥ 90 % |
| T039b | `Microservices.EventSourcing` | 88.7 % → ≥ 90 % |
| T039c | `Security.Jwt` | 87.6 % → ≥ 90 % |
| T039d | `Security.Policies` | 88.8 % → ≥ 90 % |
| T039e | `Messaging.Tests.Contract` | 78.4 % → ≥ 90 % |

### Phase 5 — Benchmark Remediation (independent)

| Task | Description |
|------|-------------|
| T040 | Fix `CoreRuntime.Core80` → `CoreRuntime.Core100` in `InProcessEmitBenchmarkConfig.cs:18` |
| T041 | Run full benchmark suite; populate `benchmarks/baseline-006.json` (≥ 50 entries) |
| T042 | Update CI regression step to reference `baseline-006.json`; remove `|| echo "::warning::..."` guard |
| T043 | Add `benchmark-action/github-action-benchmark@v1`; create `gh-pages` branch; enable GitHub Pages |

### Phase 6 — Root README Rewrite (independent)

| Task | Sections |
|------|----------|
| T060 | 1–4: headline+badges, what-is, families, quick-start |
| T061 | 5–7: builder API, isolation, provider catalogue |
| T062 | 8–11: running tests, benchmarks, CI, TDD |
| T063 | 12–14: contributing, architecture diagram, license |
| T064 | Review pass: snippets compile, NuGet IDs exist, file refs resolve, mermaid renders |
| T065 | Add link-checker CI job; merge |

### Phase 7 — Gate Hardening (LAST — after SC-060 and SC-061 GREEN)

| Task | Description |
|------|-------------|
| T090 | Remove `continue-on-error: true` from coverage gate at `ci.yml:363` |
| T091 | Open deliberate-regression PR; verify CI blocks merge; close and record evidence |

---

## Hard Constraints

### TDD Discipline
- Every behaviour task: one `red(T###):` commit (tests only, failing) + one `green(T###):` commit (production fix or test-only if code already existed).
- Tests against already-existing untested code: single `green(T###):` commit with body note `Tests only — production code already existed at {file:line}`.
- NEVER squash RED + GREEN. NEVER `--amend` across boundary. NEVER `--no-verify`.

### Clean Architecture
- Dependency direction: Core ← Family base ← Provider ← Test. Never reverse.
- No circular references between projects/namespaces.
- `Rig.TUnit.Core` MUST NOT gain provider SDK references.
- Provider packages MUST NOT depend on sibling providers.
- Base packages expose abstractions only.

### Existing Project Respect
- .NET target: `net10.0` / C# 14 only.
- Naming: `{Provider}RigBuilder`, `{Provider}RigBuilderExtensions`, `{Entity}Assert`, `{Entity}AssertionException`.
- Test project naming: `Rig.TUnit.{Package}.Tests.Unit`, `.Tests.Integration`, `.Tests.Contract`.
- No new packages, frameworks, or build systems.
- No refactoring of unrelated code.

---

## Edge Cases

- `Grpc` / `Http` integration tests may have latent failures — add with temporary `continue-on-error: true` in T001 and create a follow-up fix task before T090.
- `ChangeFeedCapture<TDocument>` (Cosmos) requires the Cosmos emulator CI job — verify it is already in the matrix before writing T022 tests.
- `Azure Service Bus emulator` may be flaky — add `[Retry(3)]` to affected tests; escalate to `FlakyQuarantine` if persistent.
- `benchmark-action/github-action-benchmark@v1` requires the `gh-pages` branch to exist — create it before T043.
- `InternalsVisibleTo` must target only the matching `*.Tests.Unit` project — never cross-family.
- If Pattern-A builder tests reveal API bugs, treat as a true RED → GREEN pair and record the behavioural change in the PR description.
- `CoreRuntime.Core100` constant: **confirmed available in BDN 0.14.0** (the installed version) per `Benchmark-Remediation-Plan.md:34`. No package upgrade needed for T040.

---

## Success Criteria

| SC | Criterion |
|----|-----------|
| SC-060 | `summary.csv` shows 0 packages below 90 % line gate |
| SC-061 | `summary.csv` shows 0 packages below 85 % branch gate |
| SC-062 | All 6 previously-missing integration projects have GREEN runs in CI |
| SC-063 | Coverage gate blocks a deliberate-regression PR |
| SC-064 | `baseline-006.json` ≥ 50 entries, all `runtime` fields = `.NET 10.*` |
| SC-065 | GitHub Pages benchmark dashboard is publicly accessible |
| SC-066 | README link-checker job is GREEN on `master` |
| SC-067 | Every task PR shows `red(T###):` + `green(T###):` pair (or single `green` with tests-only note) |
| SC-068 | Project-reference audit shows zero new cross-family edges |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Grpc` / `Http` integration tests have latent failures | Medium | High | Temporary `continue-on-error: true` in T001; fix before T090 |
| Azure Service Bus emulator flaky in CI | Medium | Medium | `[Retry(3)]`; `FlakyQuarantine` enricher if persistent |
| Pattern-A builder tests expose API bugs | Low | High | True RED → GREEN; record as behavioural change in PR |
| `ChangeFeedCapture` requires Cosmos emulator | Low | Low | Verify Cosmos job in CI matrix before T022 |
| `benchmark-action` requires `gh-pages` branch | Low | Low | Create branch before T043 |
| Test additions reach private API | Medium | Medium | `InternalsVisibleTo` only for matching `*.Tests.Unit` project |

---

## Ordering Rules

1. **Phase 1 MUST merge before Phases 2–4 begin** (accurate CI measurement).
2. **Phases 2, 3, 4 MAY run as parallel PRs** after Phase 1 merges.
3. **Phases 5 and 6 are independent** — may run in parallel with Phases 2–4.
4. **Phase 7 MUST be last** — only after SC-060 and SC-061 are demonstrably GREEN on `master`.

---

## Acceptance (Definition of Done)

DONE when ALL of the following hold on `master` simultaneously:

1. All 10 functional requirements (FR-060 … FR-069) verified.
2. All 9 success criteria (SC-060 … SC-068) verified.
3. Deliberate-regression PR (SC-063) was opened, blocked by CI, and closed with evidence in feature wrap-up.
4. `commit-discipline-gate` has run GREEN on every task PR.
5. Final coverage scan (re-run of `ci/coverage-scan` workflow) produces `summary.csv` with every package ≥ 90 % line / ≥ 85 % branch.

---

## Planning References

| File | Purpose |
|------|---------|
| `planning/post-005-coverage-quality-uplift/README.md` | Feature index |
| `planning/post-005-coverage-quality-uplift/Real-Coverage-Gap-Matrix.md` | Per-package gap table; root-cause patterns A/B/C |
| `planning/post-005-coverage-quality-uplift/CI-Pipeline-Gap-Audit.md` | 4 CI gaps with file+line references |
| `planning/post-005-coverage-quality-uplift/Benchmark-Remediation-Plan.md` | 3 benchmark defects with fixes |
| `planning/post-005-coverage-quality-uplift/README-Rewrite-Plan.md` | 14-section README template |
| `planning/post-005-coverage-quality-uplift/Feature-006-Roadmap.md` | Phased delivery plan |
| `coverage-scan-results/summary.csv` | Raw scan data (ground truth backlog) |
| `coverage-scan-results/merged.cobertura.xml` | Merged Cobertura report |
| `.github/workflows/ci.yml` | CI pipeline (lines 294, 363 are key anchors) |
| `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs` | Runtime defect at line 18 |
| `benchmarks/baseline-005.json` | Empty baseline (superseded by baseline-006.json) |

---

*Effort estimate: 35 tasks across 7 phases, ~72 engineering hours.*

---

## Clarifications

- **C-001** [Edge Cases / Dependency]: T032 referenced `WireMock.Net` as the test approach for `Microservices.Contracts` coverage, but `WireMock.Net` is not in `Directory.Packages.props` and adding it violates the "no new NuGet packages" constraint. **Resolution**: `PactBrokerClientStub` is testable via temp filesystem files (`Path.GetTempPath()` + `File.WriteAllText`); `ProviderVerificationHarness` is testable via inline `Func<ContractInteraction, Task<(int, string?)>>` lambdas; `ProviderVerificationReport` is a record testable by direct construction. T032 uses only `NSubstitute` (already in project at v5.3.0) and standard `System.IO`. No new packages required.

- **C-002** [Edge Cases / Tooling]: T040 edge case said "verify `CoreRuntime.Core100` exists before committing" without stating the outcome of that verification. **Resolution**: Confirmed available in BDN 0.14.0 (the version pinned in `Directory.Packages.props`) per `planning/post-005-coverage-quality-uplift/Benchmark-Remediation-Plan.md:34`. T040 requires no package upgrade — change the constant directly and verify with `dotnet build`.

- **C-003** [Edge Cases / Operations]: T043 says "enable GitHub Pages in repo settings" without specifying whether this is scripted or manual. **Resolution**: This is a one-time manual step performed by the repository owner via the GitHub repository settings UI or `gh` CLI (`gh repo edit`). It is NOT scripted in CI. Implementer documents in the PR that Pages was enabled and records the public URL.

