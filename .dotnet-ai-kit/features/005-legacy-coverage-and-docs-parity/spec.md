# Feature Specification: Rig.TUnit Legacy Coverage & Docs Parity

**Feature ID**: 005-legacy-coverage-and-docs-parity
**Created**: 2026-04-19
**Status**: Draft
**Input**: "Use @planning/post-004-remediation and @.dotnet-ai-kit/features/004-provider-consistency-remediation/review.md as reference to start planning and write spec.md. Use the same 004 TDD workflow and don't allow skipping tests for all cases."

---

## Overview

Feature 004 (provider-consistency-remediation) merged to `master` on 2026-04-18 via PR #3 (merge commit `9d3369f`). The `/review` pass (`review.md`, 2026-04-19) returned PASS with 3 MEDIUM and 14 LOW advisories — production-grade, gates enforced, no critical debt. But a post-merge audit of the repository surfaced **four latent gaps that undermine the FR-030 / FR-031 / FR-035 / FR-036 guarantees** the 004 spec promised:

1. **CI flake (HIGH)** — `UsePostgresFluentTests.UsePostgres_DbContext_PerformsInsertSelectRoundTrip` fails intermittently on `master` CI because `SharedPostgresFixture` hands every test in the project the same connection string; `EnsureCreatedAsync` / `EnsureDeletedAsync` on sibling tests race against it. Not a merge regression — the bug lived on the feat branch for 5 failed runs and one lucky-green run (see [planning/post-004-remediation/CI-Postgres-Flake-RCA.md](../../../planning/post-004-remediation/CI-Postgres-Flake-RCA.md)).
2. **Test-category debt (MEDIUM)** — ~23 pre-004 projects violate FR-030's mandate that every provider ship `Unit + Integration + Contract + Benchmark`. 21 providers have **no BenchmarkDotNet class at all**. The Coverage Gap Matrix enumerates every violator (see [planning/post-004-remediation/Test-Coverage-Gap-Matrix.md](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md)).
3. **Coverage gate unenforced (MEDIUM)** — FR-035 / FR-036 define ≥ 90 % line / ≥ 85 % branch gates, but `.github/workflows/ci.yml` never emits cobertura, never merges reports, never compares against thresholds. The gate lives on paper.
4. **Architecture rules partially enforced (MEDIUM)** — `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`, `TestCompletenessTests` all exist but run with `[Category("SkipUntilFixed")]` markers for in-flight legacy providers. These markers were interim — 004 Phase 6 was meant to retire them but merged before completion.

Supporting issues:

- **Documentation gaps (HIGH for OSS-readiness)** — no `LICENSE`, `CONTRIBUTING.md`, `CHANGELOG.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md` at root. Root README is 22 lines. A quality re-audit (see [Documentation-Audit.md §2.2](../../../planning/post-004-remediation/Documentation-Audit.md)) confirms **all 51 existing READMEs — including the previously-EXCELLENT MySql and Outbox — fall below a 14-section quality bar**. Effective scope: all 63 src READMEs need produced or rewritten plus supporting governance files. `ReadmeCompletenessTests` must tighten from `> 100 chars` to a structural section-presence gate.
- **Stale orphan folders (LOW)** — `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` each contain only `bin/obj/` — pre-rename artefacts.
- **CI artefact opacity (LOW)** — Every test run emits a "tip: enable automatic HTML report artifact upload" hint; reports are discarded at runner destroy. Triage of a flake currently requires `gh run view --log-failed` rather than opening an HTML report.

**Delivery mode: strict TDD — identical discipline to Feature 004 (FR-024, FR-030, FR-031, FR-034), with one intentional tightening: NO test skipping is permitted anywhere in Feature 005.**

- Every fix lands as RED commit → GREEN commit, with the RED commit's test run confirmed failing locally before the GREEN commit's production change.
- Every `[Category("SkipUntilFixed")]` marker that Feature 005 inherits MUST be retired by the exit gate of the phase that closes it (no marker may survive the feature merge).
- No new `[Category("SkipUntilFixed")]` or `[Skip]` or `[NotInParallel]` marker may be introduced as a shortcut — if a test is flaky or hard, the fix is the isolation model (ephemeral DB / per-test container / `IsolationKey`), not a skip.
- Every task that adds a test category to a legacy provider MUST ship all missing tests in that category — a partial fill-in (e.g., "add Unit only, defer Benchmark") is NOT permitted; the task either lands all four canonical categories GREEN or does not land at all.
- The PR gate enforces this at merge time: a new CI job `commit-discipline-gate` walks `git log --reverse feat/005-*` and fails the PR if any `src/`-touching commit is not preceded by a matching RED test commit (retroactively grandfathered only for Feature 004's one known violation — see FR-034 of the 004 spec).

**Scope discipline:**

- No new providers, families, or public APIs (the ecosystem is frozen post-004).
- No renames of existing public types (breaking changes deferred).
- No refactor of green tests — changes land ONLY on failing gates.
- No deployment; no NuGet publication. This is an internal quality pass.
- No feature flags.

**Observed deltas from planning docs (verified 2026-04-19):**

- Feature 004's `Rig.TUnit.Databases.Sql.Postgresql` ships a `PostgresDbContextHelper` with `CreateEphemeralDatabaseAsync` (or equivalent — verify during implementation). The Postgres flake fix in Phase 1 uses that helper; no new helper required.
- `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs` has explicit `SkipUntilFixed` entries at lines 22-53 enumerating every provider missing at least one of the four canonical test categories. Phase 3 tasks retire those entries one at a time — Red → Green — until the list is empty.
- `Directory.Packages.props` already pins `Testcontainers.*` at `4.11.x` after 004 T002. No package bump required in 005.
- Feature 004 Phase 6 authored `src/Rig.TUnit/Contributing-ProviderTemplate.md` but never created a root `docs/templates/PROVIDER_README_TEMPLATE.md`. Phase 6 of 005 creates it as the single source of truth.
- `.github/workflows/ci.yml` currently has 10 jobs (build-unit-arch + 9 integration matrices). Phase 2 adds a `coverage-summary` job; Phase 7 adds `architecture-tests`, `benchmark-regression`, and `commit-discipline-gate` jobs.

**Branch strategy:**

- **Phase 1** lands via `fix/005-phase-1-ci-stabilisation` — short-lived hotfix branch so master CI goes green immediately.
- **Phases 2–7 test/CI-side** live on `feat/005-a-legacy-coverage-and-tests`.
- **Phase 6 documentation-side** lives on `feat/005-b-docs-parity` — runs in parallel because scope is 10–14 days and file-level conflicts with 005-a are minimal (docs vs tests).

---

## User Stories

### User Story 1 - TDD RED-GREEN Forced, No Skipping (Priority: P1)

As a contributor, I need every production change in this feature to land **test-first with no skip escape hatches** so the library's "trustworthy test infrastructure" promise is itself test-proven, and no legacy-coverage gap can be closed on paper only.

**Acceptance Scenarios:**

1. **Given** a task that adds a test category to an existing provider (e.g., add Benchmark to `Rig.TUnit.Caching.Memory`), **When** a PR is opened, **Then** the `git log` on the feature branch MUST show a `test(005): TNNN — RED` commit immediately followed by a `feat(005): TNNN — GREEN` commit — no squashed-together landings, no GREEN-only commits.
2. **Given** `commit-discipline-gate` CI job, **When** a PR targeting master is created from `feat/005-*`, **Then** the job MUST walk `git log master..HEAD` and fail the PR if any `src/`-touching commit is NOT preceded by a matching RED commit.
3. **Given** a task that retires a `[Category("SkipUntilFixed")]` marker, **When** the task's GREEN commit lands, **Then** ALL tests previously skipped for that provider MUST now pass GREEN — partial retirement is not permitted; either every skipped test for that provider runs and passes or the marker stays in place for a future task.
4. **Given** the exit gate of Feature 005, **When** the merge PR is opened, **Then** `grep -rn "SkipUntilFixed" tests/` MUST return zero matches, `grep -rn "[NotInParallel]" tests/**/UsePostgresFluentTests.cs` MUST NOT return the pre-005 workaround (the isolation model is fixed, not serialised), and every architecture rule under `tests/Rig.TUnit.Architecture.Tests/Rules/` MUST enforce uniformly.
5. **Given** the post-004 green count on master (>=1264 `[Test]` methods per review.md), **When** Feature 005 merges, **Then** the final green count MUST be strictly greater — zero regressions, every 004-era test still passes.
6. **Given** a new test file landing in RED, **When** CI runs the RED commit, **Then** it MUST fail — a PR whose RED commit is secretly green (no-op test, premature assertion) fails the `red-commit-verification` CI step that runs `git log --grep="— RED"` → `git checkout <sha>` → `dotnet test` → expect non-zero exit.

---

### User Story 2 - Phase 1: CI Stabilisation (Priority: P1)

As the library maintainer, I need master CI to go green within one day so PR reviews can trust green-means-green, and the Postgres shared-fixture anti-pattern cannot bleed into future provider work.

**Acceptance Scenarios:**

1. **Given** `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`, **When** Phase 1 closes, **Then** the test MUST use a per-test ephemeral database via `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` (or equivalent per 004 FR-005) — the shared physical database anti-pattern MUST be removed. A RED commit introduces a deterministic failing assertion that schema is NOT shared; the GREEN commit switches to the per-test DB and makes it pass.
2. **Given** every `tests/**/Shared*Fixture.cs` across ~20 test projects (SQL + NoSql + Messaging + Storage families), **When** Phase 1 closes, **Then** each MUST either (a) hand out a per-test isolation key to every test (IsolationKey contract preserved), OR (b) carry a `[NotInParallel]` attribute on the shared-state-mutating tests with a tracking task to convert to per-test isolation in Phase 3. No new `SkipUntilFixed` markers allowed.
3. **Given** `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/`, **When** Phase 1 closes, **Then** all three stale orphan folders MUST be deleted via `git rm -r`. A RED commit adds an arch-test asserting these folders are absent; the GREEN commit deletes them and removes the test marker.
4. **Given** `.github/workflows/ci.yml`, **When** Phase 1 closes, **Then** every job MUST upload its TUnit HTML report as an actions-artefact (`if: always()`, 14-day retention) so a failing test on PR review can be diagnosed from the UI in one click.
5. **Given** CI matrix jobs, **When** a matrix row fails on first attempt, **Then** the CI config MUST NOT retry the job — red is red (per clarification C-001). Any genuine flake (image-pull blip, Docker hiccup, intermittent race) MUST be root-caused and added to the Phase 3 shared-fixture audit rather than masked by retry. Retries are a distributed version of the same skip-as-shortcut anti-pattern that FR-004 forbids.
6. **Given** 10 consecutive CI runs on `feat/005-*`, **When** Phase 1 exit gate is evaluated, **Then** all 10 runs MUST be green — no intermittent failures, no flakes.

---

### User Story 3 - Phase 2: Coverage Gate Enforcement (Priority: P1)

As a library maintainer, I need FR-035 / FR-036 to become a real CI gate so the "≥ 90 % line / ≥ 85 % branch" promise is enforced on every PR, not just documented in the spec.

**Acceptance Scenarios:**

1. **Given** every integration matrix job in `ci.yml`, **When** Phase 2 closes, **Then** each MUST pass `--coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` via the TUnit / MTP-native flag (per FR-036), outputting to `bin/Release/net10.0/TestResults/`.
2. **Given** a new `coverage-summary` CI job, **When** it runs after all matrices complete (`if: always()`), **Then** it MUST download every cobertura artefact, merge them via `dotnet-reportgenerator-globaltool` into `./coverage-report/`, publish `SummaryGithub.md` to `$GITHUB_STEP_SUMMARY`, and upload the merged report (30-day retention).
3. **Given** the merged cobertura report, **When** any package falls below ≥ 90 % line OR ≥ 85 % branch, **Then** the `coverage-summary` job MUST fail with a prioritised violator list. NO exemption list is permitted — a package that cannot meet the threshold ships FR-035 coverage-lifting tests (per 004 FR-035) until it passes.
4. **Given** the initial coverage run, **When** Phase 2 publishes baseline, **Then** `benchmarks/coverage-baseline-005.json` MUST capture per-package numbers so Phase 3 remediation can be measured against it.
5. **Given** `CONTRIBUTING.md`, **When** Phase 2 closes, **Then** it MUST document the coverage gate, the TUnit / MTP-native collection command (`dotnet run -- --coverage --coverage-output-format cobertura`), and the reason coverlet.msbuild does NOT work under MTP (per FR-036).

---

### User Story 4 - Phase 3: Legacy Test-Category Fill-In (Priority: P1)

As a developer relying on the FR-030 four-category promise, I need every pre-004 provider to ship `Unit + Integration + Contract + Benchmark` so I can trust that `dotnet test` on a provider exercises every canonical surface.

Per-provider cadence, STRICT: RED commit adds the missing test file(s) asserting failure (or a deliberate `Assert.Fail("baseline: not implemented")` for a brand-new category) → GREEN commit adds the production wiring so the tests pass → the same GREEN commit removes that provider's `[Category("SkipUntilFixed")]` entry in `TestCompletenessTests`. Partial fill-in is NOT permitted — a task either lands all four canonical categories GREEN for that provider or does not land.

**Acceptance Scenarios (P0 — Foundation modules):**

1. **Given** `Rig.TUnit.Core` (missing Integration, Contract), **When** Phase 3a closes, **Then** it MUST ship `tests/Rig.TUnit.Core.Tests.Integration/` and `tests/Rig.TUnit.Core.Tests.Contract/` projects, `TestCompletenessTests` MUST no longer list `Rig.TUnit.Core` in its skip list, and `dotnet test tests/Rig.TUnit.Core.Tests.*` MUST be GREEN.
2. **Given** `Rig.TUnit.Mediator` (missing Integration, Contract, Benchmark), **When** Phase 3a closes, **Then** all three test projects MUST exist with at least one `[Test]` method each exercising a real Mediator pipeline scenario, `tests/Rig.TUnit.Benchmarks/MediatorPipelineBenchmarks.cs` MUST be present with allocation + throughput measurements, and `TestCompletenessTests` MUST pass for Mediator.
3. **Given** `Rig.TUnit.Grpc`, `Rig.TUnit.WebAPI`, `Rig.TUnit.Http` (each missing Integration, Contract, Benchmark), **When** Phase 3a closes, **Then** each MUST ship the 3 missing categories with ≥ 90 % line / ≥ 85 % branch coverage per package.

**Acceptance Scenarios (P1 — Platform utilities):**

4. **Given** `Rig.TUnit.Ci` (missing Integration, Benchmark), `Rig.TUnit.Concurrency` (missing Unit, Contract, Benchmark), `Rig.TUnit.HealthChecks` (missing Unit, Benchmark), `Rig.TUnit.Parallelism` (missing Unit, Benchmark), `Rig.TUnit.Resilience` (missing Unit, Benchmark), **When** Phase 3b closes, **Then** every missing category MUST be filled and `TestCompletenessTests` MUST have zero skip-list entries remaining for these five.

**Acceptance Scenarios (P1 — Legacy providers):**

5. **Given** `Rig.TUnit.Caching.Memory`, `Rig.TUnit.Caching.Redis`, `Rig.TUnit.Databases.Sql.Sqlite`, `Rig.TUnit.Databases.Sql.SqlServer`, `Rig.TUnit.Databases.NoSql.Redis`, **When** Phase 3c closes, **Then** each MUST ship Unit + Benchmark (Redis variants ship matching Unit coverage on the shared `RedisFixture`), `TestCompletenessTests` is clean for each, and merged coverage ≥ 90 % / 85 % per package.

**Acceptance Scenarios (P1 — Observability leaves):**

6. **Given** `Rig.TUnit.Observability.Logging`, `Rig.TUnit.Observability.Seq`, `Rig.TUnit.Observability.Tracing`, **When** Phase 3d closes, **Then** each MUST ship Unit + Benchmark (Tracing's existing 355-line `TraceAssertTests.cs` remains one class per FR-012; only setup infrastructure moves).

**Acceptance Scenarios (P1 — Microservices):**

7. **Given** `Rig.TUnit.Microservices.Contracts`, `Rig.TUnit.Microservices.Saga`, `Rig.TUnit.Microservices.Inbox`, `Rig.TUnit.Microservices.Outbox`, `Rig.TUnit.Microservices.Snapshots`, **When** Phase 3e closes, **Then** every missing category fills (Benchmark for Contracts / Saga; Unit + Benchmark for Inbox / Outbox / Snapshots).

**Phase 3 exit gate:**

8. **Given** `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs`, **When** Phase 3 ends, **Then** the `SkipUntilFixed` list at lines 22-53 MUST be empty (or the skip-list mechanism entirely removed from the file), and the rule MUST enforce uniformly across all 63 src projects.
9. **Given** the merged cobertura report from Phase 2, **When** Phase 3 exit gate is evaluated, **Then** every non-N/A package MUST report line ≥ 90 % / branch ≥ 85 %. No exemption list.

---

### User Story 5 - Phase 4: Canonical Layout Completion (Priority: P2)

As a developer writing a new provider, I need every pre-004 provider to conform to `Fixtures/ + Options/ + Builder/ + [Extensions/] + [Helpers/] + README.md` so my new provider can be modelled on ANY sibling — not just the 004 four (MySql, Oracle, Cosmos, AppInsights).

Project organisation audit (see [Project-Organization-Audit.md §6](../../../planning/post-004-remediation/Project-Organization-Audit.md)) shows ~20 pre-004 providers still lack `Options/` or `Builder/` folders. Per-provider cadence: RED commit adds the missing folder with a skeleton class asserting `ProviderCompletenessTests` passes for that provider → GREEN commit completes the class and wires it through `Use{Provider}` extension.

**Acceptance Scenarios:**

1. **Given** every `src/Rig.TUnit.{Family}.{Provider}/` directory, **When** Phase 4 closes, **Then** each MUST ship `{Provider}FixtureOptions.cs` (with `public const string SectionName` + `[Required]` + `AddOptions<T>().BindConfiguration(SectionName).ValidateDataAnnotations().ValidateOnStart()`), `{Provider}RigBuilder.cs : {Family}RigBuilder<{Provider}RigBuilder>` (CRTP), and `Builder/{Provider}RigBuilderExtensions.cs` exposing `Use{Provider}(this RigBuilder, Action<{Provider}RigBuilder>?)`.
2. **Given** `ProviderCompletenessTests`, **When** Phase 4 closes, **Then** the skip list MUST be empty and the test MUST enforce uniformly across every provider (~60).
3. **Given** family-specific helper requirements (Messaging = Listener + Sender, Storage = SasBuilder, Observability.Metrics = TagCardinalityGuard — per 004 FR-006 / FR-007 / FR-009), **When** Phase 4 closes, **Then** every leaf MUST carry its family-specific helpers per 004 FR-005.

---

### User Story 6 - Phase 5: Test-File Hygiene Sweep (Priority: P2)

As a test author, I need `tests/**/*Tests.cs` files to contain tests only (no inline ActivitySource setup, Polly pipelines, JWKS factories, outbox envelope builders) so a new contributor can read a `*Tests.cs` and understand the test surface without inline infrastructure bleed.

**Acceptance Scenarios:**

1. **Given** `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/`, `tests/Rig.TUnit.Http.Tests.Unit/`, `tests/Rig.TUnit.Resilience.Tests.Integration/`, `tests/Rig.TUnit.Security.OAuth.Tests.Integration/`, `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/`, **When** Phase 5 closes, **Then** each MUST have a `TestInfrastructure/` subfolder holding extracted shared fixtures, harnesses, fakers, helpers, and custom matchers (per 004 FR-010 / FR-011).
2. **Given** every `*QuirkTests.cs` across the test tree, **When** Phase 5 closes, **Then** inline test entities / fake handlers / shared fixtures MUST be extracted to `TestInfrastructure/`.
3. **Given** `TestFileOrganizationTests`, **When** Phase 5 ends, **Then** every `[Category("SkipUntilFixed")]` marker MUST be removed — the rule enforces uniformly.
4. **Given** a 355-line `TraceAssertTests.cs`, **When** Phase 5 closes, **Then** it MUST remain one class — the rule is about **file role** (tests only), not file length (per 004 FR-012). Test files MUST NOT be split by method-under-test.

---

### User Story 7 - Phase 6: Documentation Parity (All 63 READMEs) (Priority: P2)

As a first-time user evaluating the library, I need every src project to ship a 14-section canonical README — not just a one-line description — so I can decide whether to adopt a provider without opening the fixture source file.

**Phase 6 runs on a parallel branch `feat/005-b-docs-parity` per the branch strategy. Within that branch the sub-phase order is strict: 6a → 6b → 6c → 6d.**

**Acceptance Scenarios (Phase 6a — Foundation):**

1. **Given** the repository root, **When** Phase 6a closes, **Then** `LICENSE` (MIT — see clarification C-002), `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, and a rewritten `README.md` (14-section adapted template — feature matrix replaces "API surface"; ecosystem map replaces "Provider quirks") MUST exist at root.
2. **Given** `docs/templates/PROVIDER_README_TEMPLATE.md` and `docs/QUALITY-BAR.md`, **When** Phase 6a closes, **Then** both MUST exist as the single sources of truth for the 14-section structural gate + reviewer rubric. `src/Rig.TUnit/Contributing-ProviderTemplate.md` Section 8 MUST be updated to reference them.

**Acceptance Scenarios (Phase 6b — Supporting docs):**

3. **Given** `docs/adr/`, `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md`, **When** Phase 6b closes, **Then** all MUST exist. `docs/adr/` MUST contain at least 8 ADRs (Testcontainers-over-Compose, CRTP RigBuilder, Options pattern, TUnit-over-xUnit/NUnit, family-level contracts, IsolationKey, Redis-split, KurrentDb-rename per [Documentation-Audit.md §7 P2](../../../planning/post-004-remediation/Documentation-Audit.md)).

**Acceptance Scenarios (Phase 6c — Per-project README rewrites):**

4. **Given** all 63 src projects, **When** Phase 6c closes, **Then** every `src/Rig.TUnit.{X}/README.md` MUST contain all 14 canonical sections from [Documentation-Audit.md §3.1](../../../planning/post-004-remediation/Documentation-Audit.md) OR a `## §N — N/A: <rationale>` placeholder per §3.2 (abstract base / meta packages only).
5. **Given** Phase 6c per-family cadence, **When** a family's READMEs land (e.g., SQL 6 READMEs = base + MySql + Oracle + Postgresql + SqlServer + Sqlite), **Then** each MUST ship as a RED commit (template-only file failing the tightened gate) → GREEN commit (sections filled with provider-specific research — quirks, version-compat, baseline numbers, troubleshooting).

**Acceptance Scenarios (Phase 6d — Gate tightening + verification):**

6. **Given** `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`, **When** Phase 6d closes, **Then** the rule MUST parse Markdown headings (via `Markdig` — see clarification C-003), assert all 14 headings present (or explicit `## §N — N/A:` line), assert the Options-table rows match the matching `*FixtureOptions.cs` via reflection, assert the benchmark-class link resolves, and a Markdown link-checker step MUST pass on every PR.
7. **Given** `ReadmeCompletenessTests`, **When** Phase 6d ends, **Then** ZERO `SkipUntilFixed` markers remain and the rule enforces on every src README.
8. **Given** a snippet-extraction arch-test step, **When** a PR touches `src/**/*.cs`, `src/**/README.md`, or `docs/templates/PROVIDER_README_TEMPLATE.md`, **Then** the step MUST copy-paste every affected README's `## Quick start` block into a throwaway test project and compile it — stale Quick Starts fail the build. Per C-004, docs-only and CI-only PRs skip the step via `paths:` filter.

---

### User Story 8 - Phase 7: CI Hardening (Priority: P2)

As a CI maintainer, I need every rule, coverage, benchmark, and commit-discipline check enforced on every PR so regressions fail loud, not silently.

**Acceptance Scenarios:**

1. **Given** `.github/workflows/ci.yml`, **When** Phase 7 closes, **Then** it MUST contain a dedicated `architecture-tests` job running `dotnet test tests/Rig.TUnit.Architecture.Tests/ --filter "Category!=SkipUntilFixed"` — and the filter MUST be a no-op because every `SkipUntilFixed` marker is gone by this point.
2. **Given** `tests/Rig.TUnit.Benchmarks/`, **When** Phase 7 closes, **Then** a `benchmark-regression` job MUST run `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks`, parse BenchmarkDotNet's JSON output, and fail the job if any metric regresses > 20 % vs `benchmarks/baseline-005.json` (generated at Phase 3 close-out).
3. **Given** the `build-unit-arch` job's PowerShell loop (per Project-Organization-Audit §10), **When** Phase 7 closes, **Then** it MAY be replaced with a single `dotnet test Rig.TUnit.slnx --filter Category!=Integration` call IF TUnit / MTP support lands; otherwise the pwsh loop stays but moves to Bash for cross-platform consistency.
4. **Given** the `commit-discipline-gate` job (introduced in Phase 1 per US1), **When** Phase 7 closes, **Then** it MUST be hardened: walk `git log master..HEAD`, require every `src/`-touching commit to have an ancestor commit matching `test(005): TNNN — RED` within the same feature-branch session, and fail the PR otherwise. The one retroactive exemption is Feature 004's known violation commit `2b149b2`.
5. **Given** `CONTRIBUTING.md`, **When** Phase 7 closes, **Then** it MUST document the full gate set: coverage threshold + contract suite + benchmark regression + commit-discipline + architecture-tests + test-category-completeness + markdown link-checker.

---

## Requirements

### Functional Requirements

**TDD discipline — reinforced, no skip escape hatches**

- **FR-001**: Every task that touches **production-affecting paths** (`src/**`, `Directory.Packages.props`, `global.json`, `.github/workflows/**`) MUST ship as a `test(005): TNNN — RED` commit immediately followed by a `feat(005): TNNN — GREEN` commit — no squashed landings, no GREEN-only commits. Enforced at PR gate by `commit-discipline-gate` CI job. **Exemptions (GREEN-only allowed)** — `chore(005): …` prefix for dependency-pin-only changes (e.g., T123b adds Markdig pin without a test assertion); `docs(005): …` prefix for documentation-only changes (root governance files, `docs/**`, READMEs, CONTRIBUTING, CHANGELOG, ADRs); `ci(005): …` prefix for CI-config-only changes that carry their own YAML-assertion test elsewhere. **Audit-only tasks** (planning docs, inventories, checklists under `planning/**` or `.dotnet-ai-kit/features/**`) use the `A` task-ID namespace (e.g., A005) and are exempt from RED-GREEN pairing by design.
- **FR-002**: `commit-discipline-gate` CI job MUST walk `git log master..HEAD`, require every commit touching `src/**`, `Directory.Packages.props`, `global.json`, or `.github/workflows/**` (the production-affecting set from FR-001) to have a preceding matching RED commit in the same feature-branch session, and fail the PR otherwise. Allowed prefixes that bypass (per FR-001 exemptions): `chore(005)`, `docs(005)`, `ci(005)` for the narrow cases listed in FR-001. The only retroactive SHA exemption is Feature 004's known violation `2b149b2` (grandfathered per 004 FR-034) — no other SHA may be added without a spec amendment.
- **FR-003**: `red-commit-verification` CI step MUST `git checkout` every RED commit and run `dotnet test` — the exit code MUST be non-zero (confirming the RED test genuinely fails). A secretly-green RED commit fails the PR.
- **FR-004**: NO NEW `[Category("SkipUntilFixed")]`, `[Skip]`, or permanent `[NotInParallel]` marker may be introduced in Feature 005, except inside the 4 legitimate architecture rule files (`TestCompletenessTests`, `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests`) where an existing skip list may be temporarily expanded during phased rollout (e.g., T123d expands `ReadmeCompletenessTests` skip list for Phase 6c rollout). If a test is flaky or hard, the fix is the isolation model (ephemeral DB / per-test container / `IsolationKey`) — not a skip. Enforced at architecture-test level by **`NoSkipMarkersTests` (T104b/T104c)** which walks every `tests/**/*.cs` outside the 4 rule files and fails on any such attribute.
- **FR-005**: Every `[Category("SkipUntilFixed")]` marker inherited from Feature 004 MUST be retired by the exit gate of the phase that closes it. `grep -rn "SkipUntilFixed" tests/` MUST return zero matches at the Feature 005 merge PR.
- **FR-006**: Every task that adds a test category to a legacy provider MUST ship ALL missing canonical tests for that provider in one landing — partial fill-in (e.g., "Unit only, defer Benchmark") is NOT permitted.
- **FR-007**: Final green test count MUST be strictly greater than the post-004 master baseline (1264 `[Test]` methods per `review.md`). No 004-era test may regress.

**Phase 1 — CI stabilisation**

- **FR-010**: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs` MUST switch from `SharedPostgresFixture.GetAsync()` to a per-test ephemeral database via `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` (or equivalent). The physical-database-sharing anti-pattern MUST be removed.
- **FR-011**: Every `tests/**/Shared*Fixture.cs` across ~20 test projects MUST be audited; any that hands every test the same connection string / container / mutable state MUST either (a) switch to per-test isolation via `IsolationKey`, OR (b) carry a tracked task (Phase 3 entry) to convert. No untracked shared-mutable-state fixtures permitted.
- **FR-012**: `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` MUST be deleted via `git rm -r`. An arch test asserting these paths are absent lands in the same commit.
- **FR-013**: Every CI job MUST upload its TUnit HTML report via `actions/upload-artifact@v4` with `if: always()` + 14-day retention + `if-no-files-found: warn`. Artefact name pattern: `test-results-{job-id}-{matrix-key}`.
- **FR-014**: Phase 1 exit gate requires 10 consecutive green CI runs on `feat/005-*` — no intermittent failures, no flakes.

**Phase 2 — Coverage gate**

- **FR-020**: Every integration matrix job in `ci.yml` MUST pass `--coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` to the MTP-native runner (per 004 FR-036).
- **FR-021**: A new `coverage-summary` CI job MUST run after all matrices (`if: always()`), download every cobertura artefact, merge via `dotnet-reportgenerator-globaltool`, publish `SummaryGithub.md` to `$GITHUB_STEP_SUMMARY`, and upload the merged `./coverage-report/` (30-day retention).
- **FR-022**: The merged cobertura MUST cause the `coverage-summary` job to report per-package line + branch metrics and a violator list whenever any package falls below line ≥ 90 % OR branch ≥ 85 %. **Phase 2 lands the threshold step as non-blocking (`continue-on-error: true`)** so the baseline can be captured at empirical 77–87 % without blocking in-flight Phase 3 PRs. **Phase 3 close (task T069b) flips the step to blocking (`continue-on-error: false`)** after every provider has been filled in and reaches the threshold. From T069b onward, any PR that drops a package below 90/85 fails `coverage-summary`. NO exemption list permitted — packages below threshold ship FR-038 coverage-lifting tests until they pass.
- **FR-023**: Phase 2 MUST generate `benchmarks/coverage-baseline-005.json` capturing per-package line + branch numbers at Phase 2 close.
- **FR-024**: `CONTRIBUTING.md` MUST document the coverage gate, the MTP-native collection command, and the `coverlet.msbuild` incompatibility under MTP (per 004 FR-036).

**Phase 3 — Legacy test-category fill-in**

- **FR-030**: Every provider listed in [Test-Coverage-Gap-Matrix.md](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md) MUST ship the missing categories from `{ Unit, Integration, Contract, Benchmark }`. Per-project paths follow the canonical pattern: `tests/Rig.TUnit.{X}.Tests.{Category}/`.
- **FR-031**: Foundation modules (P0) fill first: `Core`, `Mediator`, `Grpc`, `WebAPI`, `Http`. Each MUST reach line ≥ 90 % / branch ≥ 85 %.
- **FR-032**: Platform utilities (P1) fill next: `Ci`, `Concurrency`, `HealthChecks`, `Parallelism`, `Resilience`.
- **FR-033**: Legacy providers (P1) fill next: `Caching.Memory`, `Caching.Redis`, `Databases.Sql.Sqlite`, `Databases.Sql.SqlServer`, `Databases.NoSql.Redis`.
- **FR-034**: Observability leaves (P1) fill next: `Observability.Logging`, `Observability.Seq`, `Observability.Tracing`.
- **FR-035**: Microservices (P1) fill last: `Microservices.Contracts`, `Microservices.Saga`, `Microservices.Inbox`, `Microservices.Outbox`, `Microservices.Snapshots`.
- **FR-036**: `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs` MUST have its `SkipUntilFixed` list at lines 22-53 emptied by Phase 3 end. Every one-line removal lands in the GREEN commit of the task that closes the matching provider.
- **FR-037**: Every Benchmark class under `tests/Rig.TUnit.Benchmarks/` MUST measure allocation + throughput of the provider's public surface (minimum: one `[Benchmark]` method on `Fixture.InitializeAsync` or equivalent cold-path), and contribute to `benchmarks/baseline-005.json`.
- **FR-038**: FR-035 coverage-lifting tests from Feature 004 (`{Provider}FixtureOptionsTests.cs` + `{Provider}RigBuilder_ExerciseTests.cs`) MUST be added for every provider that lands under 90/85 after the basic fill-in — per 004 FR-035's empirical 77–87 % baseline.

**Phase 4 — Canonical layout**

- **FR-040**: Every pre-004 provider missing `Options/` or `Builder/` MUST ship `{Provider}FixtureOptions.cs` (with `SectionName` + `[Required]` + `ValidateOnStart()`) and `{Provider}RigBuilder.cs : {Family}RigBuilder<{Provider}RigBuilder>` (CRTP), per 004 FR-005.
- **FR-041**: Every provider MUST ship `Builder/{Provider}RigBuilderExtensions.cs` exposing `Use{Provider}(this RigBuilder, Action<{Provider}RigBuilder>?)` — per 004 FR-005.
- **FR-042**: Family-specific helpers: Messaging providers ship `{Provider}Listener + {Provider}EventSender` (004 FR-006); Storage ships `{Provider}SasBuilder` / `PathSandboxHelper` (004 FR-007); `Observability.Metrics` ships `TagCardinalityGuard` (004 FR-009).
- **FR-043**: `ProviderCompletenessTests` MUST have its `SkipUntilFixed` list emptied by Phase 4 end and enforce uniformly across every provider.

**Phase 5 — Test-file hygiene**

- **FR-050**: Every test `.cs` file outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` MUST declare exactly one top-level class containing only `[Test]` / `[Before]` / `[After]` methods plus private helpers (004 FR-010).
- **FR-051**: Inline shared fixtures, test entities, fake handlers, builder helpers, and setup constants MUST move to per-project `TestInfrastructure/` subfolders (004 FR-011). Known offenders: `Tracing.Tests.Integration`, `Http.Tests.Unit`, `Resilience.Tests.Integration`, `OAuth.Tests.Integration`, `Outbox.Tests.Integration`, every `*QuirkTests.cs`.
- **FR-052**: Test files MUST NOT be split by method-under-test. A 355-line `TraceAssertTests.cs` staying as one class is acceptable — only setup infrastructure is extracted (004 FR-012).
- **FR-053**: `TestFileOrganizationTests` MUST have every `SkipUntilFixed` marker removed by Phase 5 end and enforce uniformly across the entire test tree.

**Phase 6 — Documentation parity**

- **FR-060**: Root MUST ship `LICENSE` (MIT per C-002), `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, and a rewritten `README.md` against the adapted 14-section template.
- **FR-061**: `docs/templates/PROVIDER_README_TEMPLATE.md` MUST exist as the single source of truth for the 14-section structural gate (per [Documentation-Audit.md §3.1](../../../planning/post-004-remediation/Documentation-Audit.md)).
- **FR-062**: `docs/QUALITY-BAR.md` MUST exist as the human-reviewer rubric grading each section Pass / Needs-work / Missing with examples.
- **FR-063**: `docs/adr/` MUST contain at least 8 ADRs covering (ADR-001 Testcontainers-over-Compose, ADR-002 CRTP RigBuilder, ADR-003 Options-over-IConfiguration, ADR-004 TUnit-over-xUnit, ADR-005 family-level-contracts, ADR-006 IsolationKey, ADR-007 Redis-cache-vs-KV-split, ADR-008 KurrentDb-rename).
- **FR-064**: `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md` MUST all exist.
- **FR-065**: All 63 src project READMEs MUST conform to the 14-section canonical template (or carry explicit `## §N — N/A: <rationale>` placeholders per §3.2 for base / meta packages).
- **FR-066**: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` MUST tighten from `> 100 chars` to a structural section-presence parser using `Markdig` (C-003); assert all 14 headings present OR explicit `N/A:` line; assert Options-table rows match `*FixtureOptions.cs` via reflection; assert benchmark-class link resolves.
- **FR-067**: A Markdown link-checker CI step MUST run on every PR — broken links in any README fail the build.
- **FR-068**: A snippet-extraction arch-test MUST copy-paste every README's `## Quick start` block into a throwaway compilation unit; stale snippets after API changes fail the build. Per C-004, the job runs only on PRs that touch `src/**/*.cs`, `src/**/README.md`, or `docs/templates/PROVIDER_README_TEMPLATE.md` — docs-only and CI-only PRs skip the compile step via a `paths:` filter in `ci.yml`.
- **FR-069**: `ReadmeCompletenessTests` MUST have every `SkipUntilFixed` marker removed by Phase 6d end.

**Phase 7 — CI hardening**

- **FR-070**: `ci.yml` MUST contain a dedicated `architecture-tests` job running `dotnet test tests/Rig.TUnit.Architecture.Tests/` with no `SkipUntilFixed` filter (filter is a no-op because every skip marker is gone by this point).
- **FR-071**: `ci.yml` MUST contain a `benchmark-regression` job that parses BenchmarkDotNet JSON and fails if any metric regresses > 20 % vs `benchmarks/baseline-005.json`.
- **FR-072**: `ci.yml` MUST contain a `commit-discipline-gate` job enforcing RED-before-GREEN ordering on every PR — no `src/`-touching commit without a preceding matching RED commit.
- **FR-073**: The `build-unit-arch` pwsh loop MAY be replaced with a single `dotnet test Rig.TUnit.slnx --filter Category!=Integration` call IF TUnit / MTP support it; otherwise the loop stays but moves to Bash.
- **FR-074**: `CONTRIBUTING.md` MUST document the full gate set: coverage threshold + contract suite + benchmark regression + commit-discipline + architecture-tests + test-category-completeness + markdown link-checker.

**Enforcement gap closures (added 2026-04-19 post-`/analyze`)**

- **FR-075**: `tests/Rig.TUnit.Architecture.Tests/Rules/NoSkipMarkersTests.cs` (landed by T104b/T104c) MUST walk `tests/**/*.cs` outside the 4 legitimate architecture rule files and fail on any `[Category("SkipUntilFixed")]`, `[Skip]`, or `[NotInParallel]` attribute — closing the FR-004 enforcement gap (analysis finding #5).
- **FR-076**: `tests/Rig.TUnit.Architecture.Tests/Rules/SharedFixtureGuardTests.cs` (landed by T104b/T104c) MUST walk every `tests/**/Shared*Fixture.cs` and fail if the file lacks an `// Intentional reuse per 004/005 edge case: <reason>` (or semantically equivalent) rationale comment — closing the SC-013 enforcement gap (analysis finding #6).
- **FR-077**: The Markdig-based structural rewrite of `ReadmeCompletenessTests` (T123c, Phase 6a) MUST land BEFORE Phase 6c family README batches begin, so each Phase 6c RED commit genuinely fails the tightened gate (analysis findings #2 + #3). The interim skip-list expansion in T123d is an explicit rescope of the existing rule's own skip markers inside the legitimate rule file — permitted under FR-004's revised carve-out; final skip-list cleanup happens incrementally per-family during Phase 6c GREEN commits and is residually verified by T157/T158 at Phase 6d.

### Key Entities

- **`SkipUntilFixed` marker**: A `[Category("SkipUntilFixed")]` attribute on architecture-test classes. Phase 1 inherits ~N from Feature 004; every one MUST be retired by the exit gate of the closing phase. None may be introduced.
- **Coverage baseline**: `benchmarks/coverage-baseline-005.json` — per-package line + branch percentages captured at Phase 2 close. Used for Phase 3 before / after comparison.
- **Benchmark baseline**: `benchmarks/baseline-005.json` — BenchmarkDotNet JSON output captured at Phase 3 close-out. Phase 7's `benchmark-regression` job compares PRs against this.
- **Commit-discipline gate**: `.github/workflows/ci.yml` job that walks `git log master..HEAD`, requires every `src/`-touching commit to have a preceding RED test commit in the same feature-branch session, fails the PR otherwise.
- **Test category**: One of `{ Unit, Integration, Contract, Benchmark }` — the four canonical categories mandated by 004 FR-030. A provider is NOT "canonical" until all four are present AND GREEN AND cover ≥ 90 / 85 %.
- **Canonical README section**: One of 14 mandated sections per [Documentation-Audit.md §3.1](../../../planning/post-004-remediation/Documentation-Audit.md). `ReadmeCompletenessTests` (tightened at Phase 6d) enforces structural presence.
- **Orphan folder**: `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` — each contains only `bin/obj/`, pre-rename. Phase 1 deletes.
- **Shared-fixture anti-pattern**: A `Shared{X}Fixture.cs` handing every test the same connection string / container (e.g., `SharedPostgresFixture` on the master flake). Phase 1 audits all ~20 occurrences; Phase 3 converts remaining cases to per-test isolation.

---

## Architecture Scope

**Project mode**: **generic** — single-repo .NET 10 class-library solution (`Rig.TUnit.slnx`). No microservices, no cross-repo briefs to project. Config (`.dotnet-ai-kit/config.yml`) confirms `repos.*: null`.

**Affected directories:**

- `tests/` — **heavily modified**: ~23 pre-004 test projects gain missing categories; ~20 test projects receive new `TestInfrastructure/` subfolders; ~20 `Shared*Fixture.cs` files converted to per-test isolation or tracked.
- `tests/Rig.TUnit.Architecture.Tests/Rules/` — **modified**: `TestCompletenessTests`, `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` all have `SkipUntilFixed` lists emptied. `ReadmeCompletenessTests` gains a `Markdig` dependency for structural parsing.
- `tests/Rig.TUnit.Benchmarks/` — **heavily modified**: ~21 new BenchmarkDotNet classes for providers currently missing benchmarks (per Coverage Gap Matrix §4).
- `tests/` — **created**: ~35 new test projects (Unit + Integration + Contract + Benchmark variants for the gap-matrix violators).
- `src/` — **modified**: ~20 pre-004 providers gain missing `Options/` + `Builder/` folders per FR-040 / FR-041.
- `src/` — **deleted**: `src/Rig.TUnit.ServiceBus/` (orphan).
- `src/` — **created**: no new src projects (ecosystem frozen post-004).
- Root — **created**: `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, rewritten `README.md`.
- `docs/` — **created**: `docs/templates/PROVIDER_README_TEMPLATE.md`, `docs/QUALITY-BAR.md`, `docs/adr/` (8 files), `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md`, merged architecture Mermaid diagram embedded in root README.
- `src/Rig.TUnit/Contributing-ProviderTemplate.md` — **modified**: Section 8 updated to reference new canonical template.
- All 63 `src/Rig.TUnit.{X}/README.md` — **rewritten** against 14-section template.
- `.github/workflows/ci.yml` — **heavily modified**: every job adds coverage flag + HTML report upload; new `coverage-summary`, `architecture-tests`, `benchmark-regression`, `commit-discipline-gate` jobs added.
- `Rig.TUnit.slnx` — **modified**: register new Unit / Contract / Benchmark test projects; deregister deleted orphans.
- `Directory.Packages.props` — **modified**: add `Markdig` pin for README heading parsing.

**Architectural constraints (carry-forward from 003 / 004):**

- Dependency flow: `Rig.TUnit.Core` ← family base ← provider package. No cross-provider references (except the documented `Databases.NoSql.Redis → Caching.Redis` shared-fixture case).
- Every provider's RigBuilder MUST use the CRTP pattern.
- Every fixture MUST expose an `IsolationKey` and pass `ParallelIsolationContract`.
- Every public type MUST have XML docs.
- No renames of public APIs (breaking changes deferred to a hypothetical Feature 006).

---

## Edge Cases

- **Postgres flake on Windows vs Linux CI agents**: the shared-fixture race fires on both; fix (per-test ephemeral DB) works identically on both.
- **Retroactive commit-discipline**: Feature 004's commit `2b149b2` landed src code without a preceding RED — the single known violation, grandfathered via explicit exemption in the `commit-discipline-gate` job script.
- **Tracing.Tests.Integration 355-line `TraceAssertTests.cs`**: remains one class. Only the inline `ActivitySource` + `TracerProvider` factories extract to `TestInfrastructure/TracingTestHarness.cs`.
- **Shared fixtures that are intentionally shared**: `Rig.TUnit.Databases.NoSql.Redis` reuses `Rig.TUnit.Caching.Redis`'s `RedisFixture` — documented in 004 spec as an exception, preserved in 005. The hygiene audit treats this case as "shared-but-safe because IsolationKey separates tests".
- **Coverage for abstract base packages**: `Rig.TUnit.Core`, `Rig.TUnit.Databases` (base), `Rig.TUnit.Messaging` (base), etc. — coverage targets apply if they ship concrete code; N/A markers permitted on Contract / Benchmark columns when the base package has no concrete surface.
- **Observability.Logging.Analyzers is a Roslyn analyzer**: excluded from provider-completeness checks (per 004 edge case); `TestCompletenessTests` treats it specially — `Analyzers` / `Source Generators` projects MUST ship Unit tests only (Integration + Contract + Benchmark N/A).
- **Meta-packages (`Rig.TUnit.All`, `Rig.TUnit.Microservices`, `Rig.TUnit`)**: Phase 3 does NOT force them into the four-category template; they get README-only treatment under Phase 6 with adapted content (per Documentation-Audit §3.2).
- **Cosmos emulator on Windows runners**: existing 004 `[Category("containers")]` Linux-only gate is preserved; Phase 2 coverage collection on Cosmos runs on Linux agents only.
- **Markdig licence compatibility**: Markdig is BSD-2-Clause — compatible with MIT (the chosen project licence per C-002). No licence conflict.
- **Snippet-extraction arch-test cost**: compiling 63 README quick-starts on every PR adds ~2 min to CI. Acceptable; may be debounced to master-push-only via C-004 if deemed excessive.
- **Branch parallelism**: 005-a (tests) and 005-b (docs) run in parallel but BOTH must merge before Feature 005 is "closed". 005-b's Phase 6d gate-tightening (`ReadmeCompletenessTests`) depends on all 005-b README rewrites landing first — strict internal 6a → 6b → 6c → 6d order.

---

## Clarifications

- **C-001** [Phase 1 CI flake retry]: retry-on-flake policy for matrix jobs → **Resolved 2026-04-19 — NO retries**. Red is red. A failing matrix job is a failure, period. Test issues MUST be fixed (per-test isolation, ephemeral DBs, `IsolationKey`) — never masked by retry. Retries would be a distributed version of the same `SkipUntilFixed` anti-pattern that FR-004 forbids. Every genuine flake (image-pull blip, Docker hiccup, container race) gets root-caused and added to the Phase 3 shared-fixture audit. Encoded in US2 AC5.
- **C-002** [Licence choice for `LICENSE`]: MIT, Apache-2.0, or other → **Resolved 2026-04-19 — MIT.** Short, permissive, maximally compatible with downstream consumers, matches the typical .NET OSS baseline (xUnit, NUnit, MediatR, Polly, most Testcontainers modules). Allows commercial and proprietary reuse with attribution. Phase 6a Task T121 writes `LICENSE` with the standard MIT text attributed to `Faysil Alshareef` and year `2026`. Verified compatible with every NuGet dependency currently pinned in `Directory.Packages.props` (notably Markdig — BSD-2-Clause — which remains MIT-compatible per C-003).
- **C-003** [`ReadmeCompletenessTests` parser choice]: `Markdig` vs regex vs hand-rolled → **Resolved 2026-04-19 — Markdig.** READMEs reference `docs/templates/PROVIDER_README_TEMPLATE.md` and embed fenced-code examples showing other READMEs' structure; a regex `^##\s+` pattern false-positives on headings inside fenced code blocks. FR-066 additionally requires Section 6 (Options table) rows to match `*FixtureOptions.cs` via reflection — table + heading + link parsing from a real AST is trivial with `Markdig` but reimplemented badly with regex. Hand-rolled ~150 LoC state-machine + its own tests is maintenance debt for no library gain. Markdig ships via BSD-2-Clause (MIT-compatible), ~200 KB, test-assembly-only — no runtime cost to consumers. Adds one pin to `Directory.Packages.props` in Phase 6d Task T140.
- **C-004** [Phase 6d snippet-extraction gate timing]: every PR vs master-only vs path-filtered → **Resolved 2026-04-19 — path-filtered PR gate.** The snippet-compile step runs on any PR that touches `src/**/*.cs` OR `src/**/README.md` OR `docs/templates/PROVIDER_README_TEMPLATE.md`. Docs-only PRs to unrelated paths (ADRs, troubleshooting, glossary) and CI-only PRs skip the step. Every-PR-always is the right default signal but wastes ~2 min on PRs that can't regress the quick-starts; master-only would let a stale-quickstart PR merge and bounce master red, which is the exact failure mode FR-004 is trying to prevent. Path filter buys the PR-time gate only when the PR can actually break it. Implemented via `paths:` filter in the `.github/workflows/ci.yml` `snippet-extraction` job.

---

## Success Criteria

- **SC-001**: Master CI stays green for 10 consecutive runs after Phase 1 merges (FR-014).
- **SC-002**: `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests` has zero `SkipUntilFixed` entries after Phase 3 (FR-036).
- **SC-003**: `ProviderCompletenessTests` has zero `SkipUntilFixed` entries after Phase 4 (FR-043).
- **SC-004**: `TestFileOrganizationTests` has zero `SkipUntilFixed` entries after Phase 5 (FR-053).
- **SC-005**: `ReadmeCompletenessTests` has zero `SkipUntilFixed` entries after Phase 6d and enforces 14-section structural presence (FR-066, FR-069).
- **SC-006**: Every non-N/A package in the merged cobertura report hits line ≥ 90 % / branch ≥ 85 % after Phase 2; no exemption list exists (FR-022).
- **SC-007**: Every provider has a BenchmarkDotNet class under `tests/Rig.TUnit.Benchmarks/` and an entry in `benchmarks/baseline-005.json` (FR-037).
- **SC-008**: Root has `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, and a rewritten 14-section `README.md` (FR-060).
- **SC-009**: All 63 src READMEs satisfy the 14-section canonical template (or carry explicit `## §N — N/A: <rationale>` for abstract base / meta packages per Documentation-Audit §3.2) (FR-065).
- **SC-010**: `docs/templates/PROVIDER_README_TEMPLATE.md`, `docs/QUALITY-BAR.md`, `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md`, and 8 ADRs under `docs/adr/` all exist (FR-061 through FR-064).
- **SC-011**: `git log master..HEAD` on `feat/005-*` shows a RED → GREEN cadence for every `src/`-touching commit, verifiable by the `commit-discipline-gate` job (FR-001, FR-002, FR-072).
- **SC-012**: `grep -rn "SkipUntilFixed" tests/` returns zero matches at the Feature 005 merge PR (FR-005).
- **SC-013**: `grep -rn "Shared.*Fixture" tests/` returns matches ONLY for intentionally-shared cases (e.g., `Rig.TUnit.Databases.NoSql.Redis` → `Caching.Redis`) with a documented rationale comment — all other Shared*Fixture occurrences converted to per-test isolation (FR-011).
- **SC-014**: Three stale orphan folders deleted: `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` (FR-012).
- **SC-015**: Final green test count > post-004 baseline (1264 `[Test]` methods per `review.md`); zero regressions (FR-007).
- **SC-016**: `commit-discipline-gate` CI job enforces RED-before-GREEN on every PR after Phase 7 (FR-072).
- **SC-017**: `benchmark-regression` CI job compares PR metrics against `benchmarks/baseline-005.json` with a 20 % threshold (FR-071).
- **SC-018**: Every CI job uploads its TUnit HTML report as a 14-day-retention artefact; the merged coverage report ships with 30-day retention (FR-013, FR-021).
- **SC-019**: `CONTRIBUTING.md` documents the full gate set: coverage + contract + benchmark + commit-discipline + architecture-tests + test-category-completeness + markdown link-checker (FR-074).

---

## References

- [`planning/post-004-remediation/README.md`](../../../planning/post-004-remediation/README.md) — folder overview.
- [`planning/post-004-remediation/CI-Postgres-Flake-RCA.md`](../../../planning/post-004-remediation/CI-Postgres-Flake-RCA.md) — Phase 1 root-cause analysis.
- [`planning/post-004-remediation/Test-Coverage-Gap-Matrix.md`](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md) — Phase 3 per-provider violation matrix.
- [`planning/post-004-remediation/Project-Organization-Audit.md`](../../../planning/post-004-remediation/Project-Organization-Audit.md) — Phase 4 canonical-layout gaps.
- [`planning/post-004-remediation/Documentation-Audit.md`](../../../planning/post-004-remediation/Documentation-Audit.md) — Phase 6 quality-bar audit + 14-section template + effort estimate.
- [`planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md`](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md) — Phase 1 + Phase 2 CI YAML proposal.
- [`planning/post-004-remediation/Proposed-Feature-005-Roadmap.md`](../../../planning/post-004-remediation/Proposed-Feature-005-Roadmap.md) — full seven-phase roadmap.
- [`.dotnet-ai-kit/features/004-provider-consistency-remediation/spec.md`](../004-provider-consistency-remediation/spec.md) — 004 spec (TDD patterns FR-024/030/031/034/035/036 carried forward).
- [`.dotnet-ai-kit/features/004-provider-consistency-remediation/review.md`](../004-provider-consistency-remediation/review.md) — 004 standards review (reference for post-merge quality state).
- [`.dotnet-ai-kit/features/004-provider-consistency-remediation/handoff.md`](../004-provider-consistency-remediation/handoff.md) — 004 session wrap-up.
- [`.claude/rules/*.md`](../../../.claude/rules/) — project conventions (coding style, async, configuration, observability, security, testing, tool-calls).
- `src/Rig.TUnit.slnx` — solution file (158 projects: 63 src + 95 tests).
- `Directory.Packages.props` — central version pins.
- [`.github/workflows/ci.yml`](../../../.github/workflows/ci.yml) — current 10-job CI pipeline.

---

## Next

```
/dotnet-ai-kit:clarify    # resolve the 4 [NEEDS CLARIFICATION] markers (C-001 retry policy, C-002 licence, C-003 Markdig, C-004 snippet gate timing)
/dotnet-ai-kit:plan       # generate implementation plan once clarifications accepted
```
