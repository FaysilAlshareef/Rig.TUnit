# Quality Checklist — 005-legacy-coverage-and-docs-parity

Verify the spec at `../spec.md` before running `/dotnet-ai-kit:plan`.

## Spec completeness

- [x] Feature ID + created date + status present
- [x] User stories have priorities (P1, P2)
- [x] Each user story has at least one acceptance scenario in Given/When/Then form
- [x] Each user story is independently testable
- [x] Functional requirements numbered FR-NNN (grouped by phase)
- [x] Key entities identified
- [x] Edge cases documented
- [x] Success criteria measurable (numbered SC-NNN)
- [x] Architecture Scope section (generic mode) present
- [x] `[NEEDS CLARIFICATION]` markers ≤ 4 (currently 4: C-001 retry policy, C-002 licence, C-003 Markdig, C-004 snippet gate timing)
- [x] Defaults stated for each clarification marker so planning can proceed if unanswered

## TDD discipline (tightened from 004)

- [x] FR-001 — RED-before-GREEN commit pairing required for every `src/`-touching task
- [x] FR-002 — `commit-discipline-gate` CI job enforces at PR gate
- [x] FR-003 — `red-commit-verification` CI step confirms RED commits actually fail
- [x] FR-004 — No new `SkipUntilFixed` / `Skip` / permanent `NotInParallel` markers permitted
- [x] FR-005 — Every inherited `SkipUntilFixed` marker retired by phase exit
- [x] FR-006 — No partial category fill-in; all four canonical categories land together
- [x] FR-007 — Zero regressions vs post-004 baseline (1264 `[Test]` methods)

## Requirement quality

- [x] Every FR-NNN is verifiable by an automated test (architecture test, contract test, CI gate, or coverage threshold)
- [x] No FR relies on subjective language — each ties to a file path, type name, CI job, or measurable invariant
- [x] Merge-gate thresholds carry forward from 003/004 (90% line / 85% branch / contract 100%)
- [x] No pre-existing test is forecast to regress (FR-007 + SC-015)

## User-story coverage

- [x] US1 TDD RED-GREEN forced, no skipping (tightens 004 FR-031 / FR-034)
- [x] US2 Phase 1 CI stabilisation (Postgres flake + shared-fixture audit + orphan deletion + HTML report upload)
- [x] US3 Phase 2 coverage gate enforcement (MTP-native collection + merged cobertura + 90/85 gate)
- [x] US4 Phase 3 legacy test-category fill-in (P0 foundation → P1 utilities → P1 providers → P1 observability → P1 microservices)
- [x] US5 Phase 4 canonical layout completion (close ~20 pre-004 provider gaps)
- [x] US6 Phase 5 test-file hygiene sweep (extract inline infra to `TestInfrastructure/`)
- [x] US7 Phase 6 documentation parity (root governance + 14-section template + all 63 READMEs + supporting docs + gate tightening)
- [x] US8 Phase 7 CI hardening (architecture-tests + benchmark-regression + commit-discipline gates)

## Reference integrity

- [x] CI-Postgres-Flake-RCA referenced (Phase 1 root cause)
- [x] Test-Coverage-Gap-Matrix referenced (Phase 3 source-of-truth)
- [x] Project-Organization-Audit referenced (Phase 4 scope)
- [x] Documentation-Audit referenced (Phase 6 quality bar + 14-section template)
- [x] CI-Artifact-And-Coverage-Proposal referenced (Phase 1 + Phase 2 YAML shape)
- [x] Proposed-Feature-005-Roadmap referenced (seven-phase contract)
- [x] 004 spec referenced (carries forward FR-024 / FR-030 / FR-031 / FR-034 / FR-035 / FR-036)
- [x] 004 review referenced (post-merge quality state baseline)
- [x] `.claude/rules/*` conventions referenced

## Risks flagged in spec

- [x] CI-flake retry policy flagged as `[NEEDS CLARIFICATION] C-001`
- [x] Licence choice flagged as `[NEEDS CLARIFICATION] C-002`
- [x] `ReadmeCompletenessTests` parser choice flagged as `[NEEDS CLARIFICATION] C-003`
- [x] Snippet-extraction gate timing flagged as `[NEEDS CLARIFICATION] C-004`
- [x] Shared-fixture anti-pattern exposure audited (Edge Cases: ~20 files across SQL / NoSql / Messaging / Storage)
- [x] Retroactive commit-discipline exemption for 004's `2b149b2` documented (FR-002 / Edge Cases)
- [x] Meta-package (Rig.TUnit.All / Microservices) exclusion from four-category template documented (Edge Cases)
- [x] Observability.Logging.Analyzers analyzer-only treatment documented (Edge Cases)
- [x] 355-line `TraceAssertTests.cs` stays one class (Edge Cases, FR-052)
- [x] Markdig licence compatibility with MIT verified (Edge Cases + C-003)
- [x] Phase 6d gate-tightening ordering dependency (6a → 6b → 6c → 6d) documented (US7)
- [x] Parallel branch strategy (005-a tests + 005-b docs) documented (Overview)

## Scope boundaries (explicit non-goals)

- [x] No new providers or families (ecosystem frozen post-004)
- [x] No renames of existing public APIs (breaking changes deferred)
- [x] No refactor of green tests — changes drive from failing gates only
- [x] No NuGet publication in this feature
- [x] No deployment — internal quality pass
- [x] No feature flags
- [x] No new `SkipUntilFixed` / `Skip` markers (FR-004 — this is the tightening over 004)

## Observed-state alignment (verified 2026-04-19)

- [x] Feature 004 merged via PR #3, merge commit `9d3369f` (Overview)
- [x] Postgres flake confirmed pre-existing on feat branch (5 failures, 1 green, merged) — not a merge regression (Overview + CI-Postgres-Flake-RCA reference)
- [x] `Testcontainers.*` already pinned at 4.11.x post-004 — no package bump needed (Overview "Observed deltas")
- [x] `PostgresDbContextHelper` already ships per 004 FR-005 — Phase 1 reuses it (Overview)
- [x] `TestCompletenessTests` skip list lives at lines 22-53 of the file (Overview)
- [x] `ci.yml` has 10 jobs; Phase 2 adds `coverage-summary`; Phase 7 adds 3 more (Overview + Architecture Scope)
- [x] Config confirms generic mode (`repos.*: null`) — no cross-repo briefs (Architecture Scope)

## Success criteria measurability

- [x] SC-001 (10 green CI runs) — machine-measurable via `gh run list --branch feat/005-* --status success`
- [x] SC-002 through SC-005 (zero `SkipUntilFixed`) — grep-verifiable
- [x] SC-006 (coverage threshold) — machine-measurable via merged cobertura
- [x] SC-007 (benchmark baseline) — file-presence-verifiable
- [x] SC-008, SC-009, SC-010 (governance + READMEs + docs) — file-presence-verifiable
- [x] SC-011 (RED → GREEN cadence) — `commit-discipline-gate` enforces
- [x] SC-012, SC-013, SC-014 (grep / directory state) — machine-verifiable
- [x] SC-015 (test count > 1264) — `dotnet test` summary count
- [x] SC-016, SC-017 (CI gates live) — ci.yml job-list verifiable
- [x] SC-018, SC-019 (artefact retention + CONTRIBUTING) — file + YAML inspectable

## Ready for `/dotnet-ai-kit:plan`?

- [x] All checklist items above are `[x]` ticked
- [x] Clarifications have defaults that allow planning to proceed without blocking
- [x] References resolve to existing files in the repository
- [x] No FR conflicts with 004 FR (all carry-forward, tighten, or extend — none override)
