# Proposed Feature 005 — Legacy Coverage & Docs Parity

**Working title:** `feat/005-legacy-coverage-and-docs-parity`
**Draft date:** 2026-04-19
**Status:** proposal — not yet a formal SDD feature
**Source documents:** [CI-Postgres-Flake-RCA.md](CI-Postgres-Flake-RCA.md), [Test-Coverage-Gap-Matrix.md](Test-Coverage-Gap-Matrix.md), [Project-Organization-Audit.md](Project-Organization-Audit.md), [Documentation-Audit.md](Documentation-Audit.md).

## Mission

Close every gap left by Feature 004, turn every architecture-test `SkipUntilFixed` into a real gate, enforce the coverage gate in CI, and produce OSS-ready documentation.

## Non-goals

- No new providers or families (the ecosystem is frozen post-004)
- No rename of public APIs (breaking changes are deferred)
- No refactor of green tests — changes drive from failing gates only
- No deployment or release — this is an internal quality pass

## Delivery mode

**Strict TDD** — same discipline as 004 (FR-024, FR-030, FR-031). Every fix lands as RED commit → GREEN commit → optional REFACTOR commit, with architecture-test rules tightened progressively.

## Proposed phases

### Phase 1 — Immediate CI stabilisation (1 day)

**Goal:** unblock master CI and prevent similar flakes.

- T001: Fix `UsePostgresFluentTests.UsePostgres_DbContext_PerformsInsertSelectRoundTrip` — use per-test ephemeral database via `PostgresDbContextHelper` (preferred option A from RCA)
- T002: Audit every `Shared{X}Fixture.cs` across 20+ test projects; for each, confirm parallel-safe isolation or add `[NotInParallel]` + tracking task
- T003: Delete 3 empty orphan folders (`src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/`)
- T004: Upload TUnit HTML report as CI artefact on failure
- T005: Add retry-on-flake to CI matrix jobs (3 attempts max, flag the run for review)

**Exit gate:** 10 consecutive green CI runs on `feat/005-*`.

### Phase 2 — Coverage gate enforcement (1–2 days)

**Goal:** make FR-035/036 real.

- T010: Add `--coverage --coverage-output-format cobertura` to every integration matrix job
- T011: Add cobertura merge step using `ReportGenerator` or equivalent
- T012: Upload merged cobertura as CI artefact
- T013: Add threshold gate: ≥ 90% line / ≥ 85% branch per package — fail the job below threshold
- T014: Publish baseline coverage report to `benchmarks/coverage-baseline-005.json`
- T015: Document the gate in `CONTRIBUTING.md`

**Exit gate:** coverage report published; every package that currently ships meets the threshold OR has a tracked gap (next phase).

### Phase 3 — Test-category fill-in (5–7 days)

**Goal:** every project from [Test-Coverage-Gap-Matrix.md](Test-Coverage-Gap-Matrix.md) satisfies FR-030.

Per-project cadence: RED commit with the missing test(s) failing → GREEN commit with the test(s) passing. Architecture rule `TestCompletenessTests` removes `SkipUntilFixed` for that project in the GREEN commit.

- **P0 — Foundation (Phase 3a):**
  - T020: `Rig.TUnit.Core` — add Integration + Contract
  - T021: `Rig.TUnit.Mediator` — add Integration + Contract + Benchmark
  - T022: `Rig.TUnit.Grpc` — add Integration + Contract + Benchmark
  - T023: `Rig.TUnit.WebAPI` — add Integration + Contract + Benchmark
  - T024: `Rig.TUnit.Http` — add Integration + Contract + Benchmark

- **P1 — Platform utilities (Phase 3b):**
  - T025: `Rig.TUnit.Ci` — add Integration + Benchmark
  - T026: `Rig.TUnit.Concurrency` — add Unit + Contract + Benchmark
  - T027: `Rig.TUnit.HealthChecks` — add Unit + Benchmark
  - T028: `Rig.TUnit.Parallelism` — add Unit + Benchmark
  - T029: `Rig.TUnit.Resilience` — add Unit + Benchmark

- **P1 — Legacy providers (Phase 3c):**
  - T030: `Rig.TUnit.Caching.Memory` — add Unit + Benchmark
  - T031: `Rig.TUnit.Caching.Redis` — add Unit + Benchmark
  - T032: `Rig.TUnit.Databases.Sql.Sqlite` — add Unit + Benchmark
  - T033: `Rig.TUnit.Databases.Sql.SqlServer` — add Benchmark
  - T034: `Rig.TUnit.Databases.NoSql.Redis` — add Unit + Benchmark

- **P1 — Observability leaves (Phase 3d):**
  - T035: `Rig.TUnit.Observability.Logging` — add Unit + Benchmark
  - T036: `Rig.TUnit.Observability.Seq` — add Unit + Benchmark
  - T037: `Rig.TUnit.Observability.Tracing` — add Unit + Benchmark

- **P1 — Microservices (Phase 3e):**
  - T038: `Rig.TUnit.Microservices.Contracts` — add Benchmark
  - T039: `Rig.TUnit.Microservices.Saga` — add Benchmark
  - T040: `Rig.TUnit.Microservices.Inbox` — add Unit + Benchmark
  - T041: `Rig.TUnit.Microservices.Outbox` — add Unit + Benchmark
  - T042: `Rig.TUnit.Microservices.Snapshots` — add Unit + Benchmark

**Exit gate:** `TestCompletenessTests` has zero `SkipUntilFixed` markers; every leaf provider has all four categories green.

### Phase 4 — Canonical layout completion (2–3 days)

**Goal:** every provider conforms to FR-005's canonical template.

- T050–T070: for each of the ~20 pre-004 providers still missing `Builder/` or `Options/`, add the missing files under TDD — RED commit asserting `ProviderCompletenessTests` passes for that provider, GREEN commit adding the class.
- T080: Remove `SkipUntilFixed` markers from `ProviderCompletenessTests` once every provider passes.

**Exit gate:** `ProviderCompletenessTests` enforces uniformly; no skips.

### Phase 5 — Test-file hygiene sweep (3–4 days)

**Goal:** FR-010/011/012 enforced — tests files contain tests only.

- T090: Create `TestInfrastructure/` in every test project where inline setup exists
- T091: Extract shared fixtures, harnesses, fakers, helpers, custom matchers
- T092: Keep large test classes as single classes — do NOT split by method-under-test
- T093: Apply to known offenders explicitly: `Tracing.Tests.Integration`, `Http.Tests.Unit`, `Resilience.Tests.Integration`, `OAuth.Tests.Integration`, `Outbox.Tests.Integration`, every `*QuirkTests.cs` file
- T094: Remove `SkipUntilFixed` from `TestFileOrganizationTests`

**Exit gate:** `TestFileOrganizationTests` enforces uniformly.

### Phase 6 — Documentation parity (10–14 days — quality-driven, not size-driven)

> **Scope revision (2026-04-19):** original estimate of 3–4 days assumed 12 new + 2 minimal expansions plus a string-length gate. After audit revision (see [Documentation-Audit.md](Documentation-Audit.md) §2.2 / §2.3), **all 63 READMEs must be rewritten against a 14-section canonical template**, the gate must be tightened from `> 100 chars` to a structural section-presence check, and several supporting deliverables (template file, quality rubric, glossary, troubleshooting consolidation) are now mandatory because the new READMEs reference them. Phase 6 is large enough that it likely runs as its own sub-feature (`005-b-docs-parity`) in parallel with phases 3–5, not strictly sequential.

**Goal:** OSS-ready documentation, every README meets the 14-section quality bar, `ReadmeCompletenessTests` enforces structural conformance.

#### Phase 6a — Foundation (1–2 days, gates everything else)

- T100: Add root `LICENSE` (MIT recommended; confirm with repo owner)
- T101: Add root `CONTRIBUTING.md` consolidating TDD rules + linking to `Contributing-ProviderTemplate.md` and the new canonical template
- T102: Add root `SECURITY.md` with disclosure channel
- T103: Author `docs/templates/PROVIDER_README_TEMPLATE.md` — single source of truth for the 14-section canonical README (per [Documentation-Audit.md §3](Documentation-Audit.md))
- T104: Author `docs/QUALITY-BAR.md` — human-reviewer rubric grading each section Pass / Needs-work / Missing with examples
- T105: Update `src/Rig.TUnit/Contributing-ProviderTemplate.md` Section 8 to reference the new canonical template instead of the legacy `> 100 chars` floor

#### Phase 6b — Root README + supporting docs (1–2 days)

- T106: Rewrite root `README.md` against the adapted 14-section template (feature matrix replaces "API surface", ecosystem map replaces "Provider quirks") — per [Documentation-Audit.md §7 P0](Documentation-Audit.md)
- T107: Add `CHANGELOG.md` with 001–004 history and the KurrentDB breaking rename narrative
- T108: Add architecture diagram (Mermaid) showing family graph + 60-provider matrix — embedded in root README, linked from each leaf README's section 13
- T109: Add 8 ADRs under `docs/adr/` (per [Documentation-Audit.md §7 P2](Documentation-Audit.md) — was 6, expanded to include IsolationKey rationale and Redis cache/KV split)
- T110: Add `docs/glossary.md` (mandatory — every term used in any README MUST resolve here)
- T111: Add `docs/troubleshooting.md` (consolidated; leaf READMEs link to provider-specific subsections)
- T112: Add `docs/performance-tuning.md` (when to use which provider for which test scenario)
- T113: Add `docs/migration-001-to-004.md` (notably KurrentDb)

#### Phase 6c — Per-project README rewrites (6–9 days, staged per family)

Acceptance criteria per README defined in [Documentation-Audit.md §9](Documentation-Audit.md). Cadence per family: RED commit (template-only README failing the new structural gate) → GREEN commit (sections filled with provider-specific research). All 63 READMEs in scope — including the previously-EXCELLENT MySql and Outbox.

- T120: 12 missing READMEs created (`Rig.TUnit`, `Rig.TUnit.All`, `Rig.TUnit.Ci`, `Rig.TUnit.Core`, `Rig.TUnit.Grpc`, `Rig.TUnit.Mediator`, `Rig.TUnit.Microservices` base, `Rig.TUnit.Microservices.Contracts`, `Rig.TUnit.Microservices.Saga`, `Rig.TUnit.Parallelism`, `Rig.TUnit.Storage` base, `Rig.TUnit.WebAPI`)
- T121: SQL family — rewrite all 6 (base + MySql + Oracle + Postgresql + SqlServer + Sqlite) — capture EF-provider compat matrix, AUTO_INCREMENT/PL-SQL/schema quirks, Pomelo EF10 pin
- T122: NoSQL family — rewrite all 8 (base + Cassandra + Cosmos + Dynamo + ElasticSearch + KurrentDb + Mongo + Redis-as-KV) — RU charges, keyspace-per-test, stream semantics, GSI verification
- T123: Caching family — rewrite all 5 (base + Fusion + Hybrid + Memory + Redis) — capture cache-vs-KV Redis split rationale, backplane semantics, fail-safe / eager-refresh
- T124: Messaging family — rewrite all 6 (base + Kafka + Nats + RabbitMq + ServiceBus + Sqs) — listener/sender lifecycle, W3C traceparent, dead-letter, ordering
- T125: Microservices family — rewrite all 7 (base + Contracts + EventSourcing + Inbox + Outbox + Saga + Snapshots) — exactly-once, CAS contention, snapshot scrubbing
- T126: Security family — rewrite all 5 (base + Jwt + Mtls + OAuth + Policies) — kid rotation, negative builders, JWKS lifecycle
- T127: Observability family — rewrite all 6 (base + AppInsights + Logging + Logging.Analyzers + Metrics + Seq + Tracing) — TagCardinalityGuard, ActivitySource lifecycle, snapshot capture
- T128: Storage family — rewrite all 5 (base + AzureBlob + FileSystem + MinIO + S3) — SAS builders, path sandbox
- T129: Cross-cutting / utilities — rewrite remaining (Concurrency, Docker, HealthChecks, Http, Resilience, Microservices container)

#### Phase 6d — Gate tightening + verification (1 day)

- T140: Implement structural section-parser in `ReadmeCompletenessTests` — assert all 14 headings present (or explicit `## §N — N/A: <rationale>`); assert Options-table rows match `*FixtureOptions.cs` via reflection; assert benchmark-class link resolves; markdown link checker
- T141: Remove all `SkipUntilFixed` markers from `ReadmeCompletenessTests`
- T142: Add markdown link-checker step to CI workflow (broken links in any README fail the build)
- T143: Add a snippet-extraction arch-test step that copy-pastes Quick Start blocks into a throwaway test project and compiles them (catches stale READMEs after API changes)

**Exit gate:** `ReadmeCompletenessTests` enforces 14-section structure uniformly; zero skip markers; root has 5 governance files (LICENSE, CONTRIBUTING, SECURITY, CHANGELOG, README); 8 ADRs published; `docs/templates/`, `docs/adr/`, `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md` all present; OSS-ready.

### Phase 7 — CI hardening (1 day)

**Goal:** turn the full gate set on permanently.

- T120: Add dedicated `architecture-tests` CI job (runs `Rig.TUnit.Architecture.Tests` without skips)
- T121: Add benchmark regression gate (> 20% vs `benchmarks/baseline-004.json` fails)
- T122: Replace PowerShell enumeration in `build-unit-arch` with a single `dotnet test Rig.TUnit.slnx --filter Category!=Integration` call once TUnit supports it (or keep enumeration but move to Bash)
- T123: Add commit-history gate: every `src/` touching commit must be preceded by a RED commit (FR-031/FR-034)
- T124: Document the full gate set in `CONTRIBUTING.md`

**Exit gate:** every rule, coverage, benchmark, and commit-discipline check is enforced on every PR.

## Success criteria (SC)

- **SC-001:** CI on `master` is green 10 consecutive runs after Phase 1 lands
- **SC-002:** `TestCompletenessTests` has zero skip markers after Phase 3
- **SC-003:** `ProviderCompletenessTests` has zero skip markers after Phase 4
- **SC-004:** `TestFileOrganizationTests` has zero skip markers after Phase 5
- **SC-005:** `ReadmeCompletenessTests` has zero skip markers after Phase 6
- **SC-006:** Every provider reports line ≥ 90% / branch ≥ 85% via the CI coverage gate
- **SC-007:** Every provider has a BenchmarkDotNet class and a baseline entry in `benchmarks/baseline-005.json`
- **SC-008:** Root has `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, and a rewritten `README.md` against the 14-section template
- **SC-009:** All 63 src projects have a `README.md` that satisfies the 14-section canonical template (per [Documentation-Audit.md §3](Documentation-Audit.md)) — structural conformance, not character-count
- **SC-009a:** `ReadmeCompletenessTests` is tightened from `> 100 chars` to a structural section-presence parser; zero `SkipUntilFixed` markers; markdown link checker passes
- **SC-009b:** `docs/templates/PROVIDER_README_TEMPLATE.md`, `docs/QUALITY-BAR.md`, `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md`, and 8 ADRs under `docs/adr/` all exist
- **SC-010:** Commit history on the feature branch shows RED → GREEN cadence for every src-touching commit (FR-031 / FR-034)
- **SC-011:** Three stale orphan folders deleted
- **SC-012:** Final green test count > current 004-era baseline; zero regressions

## Effort estimate

| Phase | Effort |
|---|---|
| 1 — CI stabilisation | 1 day |
| 2 — Coverage gate | 1–2 days |
| 3 — Test-category fill-in | 5–7 days |
| 4 — Canonical layout | 2–3 days |
| 5 — Test-file hygiene | 3–4 days |
| 6 — Documentation (quality-driven, all 63 READMEs) | **10–14 days** |
| 7 — CI hardening | 1 day |
| **Total** | **23–32 days** |

> Phase 6 was originally 3–4 days when the scope was "12 new + 2 expansions + size gate." After scope revision to "all 63 rewritten against 14-section template + structural gate + supporting docs" the effort grew to 10–14 days. See [Documentation-Audit.md §8](Documentation-Audit.md) for the line-item breakdown.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Coverage gate fails on existing packages once enforced | Phase 2 publishes baseline before threshold is applied; short-term exemption list |
| Test-category fill-in introduces new flakes | Every new Integration test uses per-test isolation (no `Shared{X}Fixture` anti-pattern) |
| Benchmark regression gate blocks merges on .NET 10 GC noise | Allow 2× runs; compare medians; > 20% threshold is generous |
| LICENSE choice needs legal review | Tag repo owner on the PR before merge |
| Retro commit-history gate fails on existing mixed commits | Apply only to `feat/005-*` forward; 004 commits grandfathered |

## Order of execution

1. Create `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/` via `/dotnet-ai-kit:specify`
2. Clarify any unresolved questions via `/dotnet-ai-kit:clarify`
3. Generate plan via `/dotnet-ai-kit:plan`
4. Generate tasks via `/dotnet-ai-kit:tasks`
5. Execute phases 1–7 in order (Phase 1 MUST land first to stabilise master CI)

## Branch strategy

Phase 1 lands as a hotfix via its own short-lived branch `fix/005-phase-1-ci-stabilisation` — so master becomes green immediately even if later phases take weeks.

Two long-lived feature branches off `master` after Phase 1 lands, run in parallel because Phase 6 is now large enough to justify its own owner / reviewer cadence:

- `feat/005-a-legacy-coverage-and-tests` — Phases 2, 3, 4, 5, 7 (test-quality + CI hardening)
- `feat/005-b-docs-parity` — Phase 6 (all 63 READMEs + supporting docs + gate tightening)

Phase 6's gate-tightening (T140) requires every README to be rewritten first, so within the docs branch the order is strictly 6a → 6b → 6c → 6d.

The two branches are independent at the file level (docs vs tests) so merge conflicts are minimal. They merge to `master` in either order.

## Open questions

1. Should the `LICENSE` be MIT, Apache-2.0, or another choice? (Owner decision.)
2. Should Phase 3 / 4 / 5 land as one giant PR or staged PRs per family? (Recommend per-family PRs for review cadence.)
3. Should the benchmark gate's 20% threshold from 004 carry forward or tighten? (Recommend keep 20% for now; tighten after one release.)
4. ~~Do we need a separate `docs-only` branch for Phase 6, or bundle with Phase 5?~~ **Resolved 2026-04-19**: separate branch `feat/005-b-docs-parity` runs in parallel — Phase 6 is now 10–14 days, too large to bundle.
5. Should `ReadmeCompletenessTests` parse Markdown with `Markdig` (transitive dep) or use a regex-based heading scan? (Recommend `Markdig` for robustness; the dep is already pulled in by `dotnet-format` toolchain.)
6. Should the snippet-extraction gate (T143) run on every PR or only on `master` pushes? (Recommend PR — catches stale README quick-starts at review time.)
