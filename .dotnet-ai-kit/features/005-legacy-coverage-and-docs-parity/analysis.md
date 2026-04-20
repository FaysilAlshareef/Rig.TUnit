# Analysis Report: 005-legacy-coverage-and-docs-parity

**Feature**: 005-legacy-coverage-and-docs-parity | **Mode**: generic
**Date**: 2026-04-19 | **Findings**: 14 | **Resolved**: 14 / 14 (2026-04-19)

## Summary

- **CRITICAL**: 1 → **RESOLVED**
- **HIGH**: 4 → **RESOLVED**
- **MEDIUM**: 5 → **RESOLVED**
- **LOW**: 4 → **RESOLVED**

**Verdict**: All 14 findings addressed via edits to `spec.md`, `tasks.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`. Feature ready for `/dotnet-ai-kit:implement`.

## Resolution Log (2026-04-19)

| # | Severity | Status | Resolution |
|---|---|---|---|
| 1 | CRITICAL | ✅ Resolved | Task IDs synced across plan.md / research.md / data-model.md / quickstart.md to match tasks.md (T120+ for Phase 6a, T137+ for Phase 6c, T164+ for Phase 7, etc.). |
| 2 | HIGH | ✅ Resolved | Markdig rewrite moved FORWARD from Phase 6d to Phase 6a (new tasks T123b/T123c/T123d). Phase 6c RED commits now genuinely fail the tightened gate. New FR-077 spec clause documents the ordering. |
| 3 | HIGH | ✅ Resolved | Addressed by #2's reorder — T157 repurposed as residual skip-list guard test; T158 is final cleanup. Both now genuinely fail/pass per FR-003. |
| 4 | HIGH | ✅ Resolved | T016 no longer flips threshold to blocking. New task T069b at Phase 3 close does the flip after all providers reach 90/85. Spec FR-022 reworded. |
| 5 | HIGH | ✅ Resolved | New tasks T104b (RED) + T104c (GREEN) add `NoSkipMarkersTests.cs` enforcement. New spec FR-075 formalises it. Bundled with #6. |
| 6 | MEDIUM | ✅ Resolved | T104b/T104c also add `SharedFixtureGuardTests.cs` with rationale-comment check. New spec FR-076 formalises it. |
| 7 | MEDIUM | ✅ Resolved | T005 renamed to A005 (audit-namespace prefix). Spec FR-001 now explicitly carves out `A`-prefix audit tasks and chore/docs/ci-prefix narrow exemptions. |
| 8 | MEDIUM | ✅ Resolved | T121 GREEN body updated in tasks.md to document merge-order behaviour vs T017 stub. Research.md R15 rewritten to describe the 3-step CONTRIBUTING lifecycle. |
| 9 | MEDIUM | ✅ Resolved | T157's bundled pin+rewrite split into T123b (chore — dependency pin only, GREEN-only) + T123c (RED — test rewrite) + T123d (GREEN — interim skip expansion). |
| 10 | MEDIUM | ✅ Resolved | FR-001/FR-002 scope explicitly extended to `src/**`, `Directory.Packages.props`, `global.json`, `.github/workflows/**`. |
| 11 | MEDIUM | ✅ Resolved | tasks.md header now documents "182 IDs = 91 task units (RED+GREEN pairs) + singletons". |
| 12 | LOW | ✅ Accepted | T100 NoSql scope remains deferred to A005 audit output. Documented in plan.md. |
| 13 | LOW | ✅ Resolved | FR-001 now lists `chore`/`docs`/`ci`-prefix GREEN-only exemptions explicitly for narrow cases (dependency pins, doc-only files, CI-config with its own YAML-assertion tests). T174 qualifies as `docs`-exempt. |
| 14 | — | n/a | Architecture-test naming style (`*JobTests.cs` vs `*Tests.cs`) — deferred as style-only, not correctness; can be normalised during implementation. |

### New / renamed tasks summary

- `A005` (renamed from `T005`) — shared-fixture audit, audit-namespace prefix
- `T069b` (new) — coverage threshold flip to blocking at Phase 3 close
- `T104b` / `T104c` (new) — RED/GREEN pair adding `NoSkipMarkersTests` + `SharedFixtureGuardTests`
- `T123b` (new) — `chore` adding Markdig dependency pin
- `T123c` (new) — RED rewriting `ReadmeCompletenessTests` with Markdig (moved forward from `T157`)
- `T123d` (new) — GREEN expanding skip list for Phase 6c rollout
- `T157` (repurposed) — residual skip-list guard test
- `T158` (repurposed) — final residual cleanup

### New / revised FRs in spec.md

- `FR-001` — scope extended to production-affecting paths; exemption prefixes explicit
- `FR-002` — scope aligned to FR-001
- `FR-004` — carve-out for legitimate rule-file skip-list rescoping
- `FR-022` — threshold non-blocking through Phase 2; blocks from T069b onward
- `FR-075` (new) — `NoSkipMarkersTests` enforcement
- `FR-076` (new) — `SharedFixtureGuardTests` enforcement
- `FR-077` (new) — Markdig rewrite ordering

**Total spec growth**: 53 → 56 FRs; 19 SCs unchanged (existing SCs now have concrete tasks pointing to their enforcement).

---

*The original findings listing below is preserved for audit trail.*

---

## Findings

### [CRITICAL] Pass 2 Naming: Task-ID collision between plan.md and tasks.md

**Location**: [plan.md §Phase 6a T100/T101](plan.md) vs [tasks.md §Phase 4e T100/T101](tasks.md) and [§Phase 6a T120/T121](tasks.md).

**Details**: plan.md uses `T100` for "RED governance files present" (Phase 6a) and `T101` for "GREEN author canonical template" (Phase 6a). tasks.md reassigned these IDs — tasks.md `T100/T101` is the Phase 4e NoSql provider canonical-layout task, and Phase 6a governance lives at `T120/T121` instead. The plan's task-ID references (e.g., "T100 writes LICENSE" in research.md R14, "Phase 6a T100" in quickstart.md §9) will resolve to the wrong task when a reviewer cross-reads.

Concrete collision sites in already-written artefacts:

- [plan.md §Phase 6a](plan.md) references `T100`..`T107`, `T120`..`T129`, `T140`..`T143` (all wrong vs tasks.md).
- [research.md R14](research.md): "Phase 6a T100 GREEN" — wrong, should be T121.
- [research.md R11](research.md): references `T153` for red-commit-verification — tasks.md uses `T170/T171`.
- [data-model.md Entity 3](data-model.md): references `Phase 7 T152` for commit-discipline-gate — tasks.md uses `T168/T169`.
- [quickstart.md §9](quickstart.md): references "T100 writes LICENSE" — wrong.

**Suggested Fix**: Because tasks.md is the authoritative source of task IDs and its numbering is the one `commit-discipline-gate` will parse from commit subjects, update `plan.md`, `research.md`, `data-model.md`, and `quickstart.md` to match tasks.md IDs. Concrete edit list (do NOT edit in this analysis pass — schedule the edits for a `docs(005): sync task IDs across planning artefacts` pre-implementation chore):

| plan/research/data-model/quickstart ID | tasks.md ID |
|---|---|
| T100 (Phase 6a governance) | T120/T121 |
| T101 (canonical template) | T122/T123 |
| T102 (Mermaid) | T124/T125 |
| T103 (8 ADRs) | T126/T127 |
| T104 (glossary) | T128/T129 |
| T105 (troubleshooting) | T130/T131 |
| T106 (performance-tuning) | T132/T133 |
| T107 (migration) | T134/T135 |
| T120..T129 (family batches) | T137..T156 |
| T140 (Markdig rewrite) | T157 |
| T141 (empty Readme skip list) | T158 |
| T142 (markdown-link-check) | T159/T160 |
| T143 (snippet-extraction) | T161/T162 |
| T150 (architecture-tests job) | T164/T165 |
| T151 (benchmark-regression) | T166/T167 |
| T152 (hardened commit-discipline) | T168/T169 |
| T153 (red-commit-verification) | T170/T171 |
| T154 (pwsh → Bash) | T172/T173 |
| T155 (full CONTRIBUTING) | T174 |

---

### [HIGH] Pass 3 Coverage: Phase 6c RED commits are secretly-green (FR-003 violation)

**Location**: [tasks.md T137/T139/T141/.../T155](tasks.md) Phase 6c family batches.

**Details**: Each Phase 6c family task has the same shape — RED commit lands template-only READMEs with 14 section headings + `// TODO: runnable snippet` placeholders; GREEN commit populates them. Problem: the RED commit is tested against the *current* `ReadmeCompletenessTests`, which uses the legacy `> 100 chars` gate. 14 section headings with placeholder content easily exceeds 100 chars — so **every Phase 6c RED commit passes CI**. That means `red-commit-verification` (Phase 7 T170/T171) would fail all 10 Phase 6c PRs retroactively once it lands.

The tasks.md text even acknowledges this: "Verify RED against the (future, Phase 6d) structural gate — encoded as branch-local skip". That's a euphemism for "the RED isn't actually RED". Violates FR-003.

**Suggested Fix**: Move the Markdig rewrite (currently T157, Phase 6d) to **before** Phase 6c — rename as `T122b` or `T123b` landing in Phase 6a right after the canonical template. Keep it RED-only against the template-only family READMEs (they WILL fail the 14-section reflected-Options check). Then Phase 6c family RED commits genuinely fail the tightened gate, GREEN commits populate the content and pass. `T158` (empty skip list) stays at Phase 6d end because you still need every skip marker retired in one pass after 6c finishes.

Alternative: keep T157 at Phase 6d but change Phase 6c RED definition — the RED commit could add a deliberate assertion that a known-missing section (e.g., a new fixture file `tests/Rig.TUnit.Architecture.Tests/Fixtures/BrokenReadme.md`) fails, which lands with each family PR and is removed in the GREEN commit. More mechanical; breaks the natural "write README, fail, fix it" flow.

Recommended: the reorder approach — move the Markdig gate-tightening earlier.

---

### [HIGH] Pass 3 Coverage: T157 RED may be secretly-green if Phase 6c is complete

**Location**: [tasks.md T157](tasks.md).

**Details**: tasks.md T157 description: "RED — if 6c is complete every README passes; if not, the failing ones surface." If all 63 READMEs are fully populated at this point, the rewritten `ReadmeCompletenessTests` passes on commit — not RED. Violates FR-003 `red-commit-verification`.

**Suggested Fix**: If Finding #2 is implemented (move Markdig rewrite to Phase 6a), this finding resolves naturally — the test always has failing Phase 6c placeholder READMEs to assert against until Phase 6c completes. Otherwise, T157's RED commit needs a deliberate failure hook: add a fixture file like `tests/Rig.TUnit.Architecture.Tests/Fixtures/Readmes/Incomplete.md` that the test scans and fails on; remove the fixture in T158's GREEN commit. Keep the fixture approach if reordering is too invasive.

---

### [HIGH] Pass 3 Coverage: T016 flips threshold to blocking BEFORE Phase 3 fills gaps

**Location**: [tasks.md T016](tasks.md) "Follow-up commit (SAME PR): flip threshold step `continue-on-error: true` → `continue-on-error: false`".

**Details**: Phase 2 captures the baseline from the CURRENT (post-004) code, where the 77–87 % coverage empirical floor is documented in [research.md R6](research.md) (Mongo/Postgres observed, likely others similar). T016's second commit flips the threshold to blocking **in the same PR**. The next PR (T020 Phase 3 Core Integration) then tries to land, `coverage-summary` fails because Core's coverage drops transiently while new tests are being wired, and Phase 3 cannot land at all.

The fix sequence is encoded upside-down: flip-to-blocking belongs at the END of Phase 3 (after every package reaches the 90/85 threshold), not the end of Phase 2.

**Suggested Fix**: Split T016 into:

- **T016a (Phase 2 close)** — write `benchmarks/coverage-baseline-005.json`; keep threshold step at `continue-on-error: true` (non-blocking, warning only).
- **T016b (moves to Phase 3 close, pair with T069)** — flip `continue-on-error: true → false`. All Phase 3 providers now at or above threshold; the flip cannot fail any already-landed PR.

Update [spec.md FR-022](spec.md) phrasing accordingly: "MUST fail … after Phase 3 close, non-blocking before that".

---

### [HIGH] Pass 3 Coverage: FR-004 (no new skip markers) has no dedicated enforcement task

**Location**: [spec.md FR-004](spec.md); [tasks.md FR → Task matrix](tasks.md) says "audit on every PR" but no task creates that audit.

**Details**: FR-004 forbids introducing NEW `[Category("SkipUntilFixed")]`, `[Skip]`, or permanent `[NotInParallel]` markers. The existing architecture tests don't enforce this — they only check that certain rules don't have specific provider entries in their own skip lists. A developer could land a new `[Skip]` attribute on a brand-new test in a new test project and nothing in CI would catch it.

SC-012 says `grep -rn "SkipUntilFixed" tests/` MUST return zero at merge, but this is a one-time assertion at the merge PR, not a per-PR gate.

**Suggested Fix**: Add a new task — call it `T104b` (nestled between Phase 4 verification T104 and Phase 5 T105) — `RED + GREEN`:

- **RED**: add `tests/Rig.TUnit.Architecture.Tests/Rules/NoSkipMarkersTests.cs` with `[Test]` asserting `grep`-equivalent: walk all `tests/**/*.cs`, fail if any line matches `[Category("SkipUntilFixed")]` / `[Skip]` / `[NotInParallel]`. Run → fails (Phase 4 may still have ONE SkipUntilFixed marker mid-phase depending on ordering).
- **GREEN**: ensure the last SkipUntilFixed is already gone (coordinated with Phase 4's T104 verification); commit.

Alternatively, add this as a CI YAML step in `architecture-tests` job (T164/T165) rather than a `[Test]`, so PR failure message is explicit.

Update FR-004 to name the task ID; update FR→Task matrix accordingly.

---

### [MEDIUM] Pass 3 Coverage: SC-013 (shared-fixture audit) has no CI enforcement task

**Location**: [spec.md SC-013](spec.md); [tasks.md T066–T067](tasks.md) converts fixtures but no task creates the audit CI step.

**Details**: SC-013 requires `grep -rn "Shared.*Fixture" tests/` to return matches ONLY with rationale comments (e.g., the intentional `Databases.NoSql.Redis → Caching.Redis` reuse). T066/T067 converts the unsafe cases but no task enforces the grep-with-comment invariant at PR gate.

**Suggested Fix**: Add the invariant to `NoSkipMarkersTests.cs` (proposed in Finding #5) or as a separate `SharedFixtureGuardTests.cs` — walk every `Shared*Fixture.cs` under `tests/**`, parse preceding comment block, fail if no `// Intentional reuse …` or equivalent rationale comment. Schedule as part of Finding #5's new task OR as a new `T067b` GREEN-only verification task.

---

### [MEDIUM] Pass 3 Coverage: T005 exception to RED/GREEN discipline creates precedent

**Location**: [tasks.md T005](tasks.md) "This task is an exception to the RED/GREEN rule: audit-only with no src/ touch."

**Details**: T005 is labelled audit-only, single commit, no RED pair. This is defensible under FR-001's "Every task touching `src/`" scope — an audit document under `planning/` is not `src/`. But the task file uses the same `T005` ID convention and the exception-labelling implies a policy extension. If future reviewers point to T005 as a precedent to skip RED/GREEN for other "audit-only" work, it weakens FR-001 gradually.

**Suggested Fix**: Rename T005 to `A005` (audit-task namespace) OR add a terse spec clarification: "Non-`src/`, non-`tests/` artefacts (planning docs, checklists) are exempt from RED/GREEN but MUST land in their own commit with a `docs(005):` or `chore(005):` prefix — never bundled with a `test(005)` or `feat(005)` commit." Reinforces the discipline without forcing fake RED tests on audit documents.

---

### [MEDIUM] Pass 3 Coverage: T017 CONTRIBUTING.md stub timing

**Location**: [tasks.md T017](tasks.md) Phase 2 stub; [tasks.md T121](tasks.md) Phase 6a full rewrite; [tasks.md T174](tasks.md) Phase 7 extension.

**Details**: `CONTRIBUTING.md` is written 3 times (stub → full → extended). Each edit is its own commit on a different branch (005-a for T017, 005-b for T121, 005-a for T174). The 005-b T121 rewrite **overwrites** the T017 stub coverage content — the merge order between 005-a and 005-b determines what survives. If 005-a lands first and includes T017's stub, then 005-b's T121 full rewrite merges, T121 must re-incorporate the coverage section or it'll be lost. If 005-b lands first, T121 authors everything including coverage; 005-a's T017 stub is effectively dead. T174 then extends with the full gate set.

**Suggested Fix**: Clarify in tasks.md T121 GREEN body: "This commit MUST incorporate the coverage-gate content from T017 even if 005-a has already merged (stub content survives in a subsection of the full CONTRIBUTING.md)." OR: drop T017 entirely and have Phase 2 write the coverage instructions into a fresh `docs/COVERAGE.md` file; T121 then links to it from the final CONTRIBUTING.md.

---

### [MEDIUM] Pass 1 Architecture: T157 bundles Markdig pin + production rewrite in one RED commit

**Location**: [tasks.md T157](tasks.md) "Same commit bumps: `Directory.Packages.props` adds `Markdig` pin".

**Details**: T157's RED commit adds a dependency AND rewrites a test file in one go. `Directory.Packages.props` changes affect the whole solution's build graph. If the Markdig pin causes any transitive conflict (e.g., with `dotnet-format`'s existing Markdig version), the build breaks everywhere before the test even runs — which would register as "RED" but for the wrong reason (build error, not assertion failure). `red-commit-verification` would see non-zero exit and consider it valid RED, but it's masking an infra break, not validating a real TDD red state.

**Suggested Fix**: Split T157 into:

- **T157a RED** — add `Markdig` pin to `Directory.Packages.props` and `PackageReference` to `Rig.TUnit.Architecture.Tests.csproj`. Verify `dotnet build` still passes. Commit as `chore(005): T157a — add Markdig pin for README structural parser`.
- **T157b RED** — rewrite `ReadmeCompletenessTests` using Markdig. The test assertion genuinely fails against Phase 6c placeholder READMEs (per Finding #2's recommended reorder). Commit as `test(005): T157b — RED structural README gate via Markdig`.
- **T158 GREEN** (unchanged) — empty skip list; test passes.

---

### [MEDIUM] Pass 2 Naming: Phase 6c RED / GREEN pair ID convention uses odd/even

**Location**: [tasks.md T137–T156](tasks.md).

**Details**: Phase 6c family batches alternate RED+GREEN pairs with consecutive task IDs (T137 RED + T138 GREEN; T139 RED + T140 GREEN; etc.). Earlier phases use the pattern `TNNN RED + TNNN+1 GREEN` implicitly but the file header says "Each task represents one unit of work that can be a single RED+GREEN commit pair" (paraphrase). Phase 6c is inconsistent — it breaks the "one task ID = one RED+GREEN pair" convention by consuming two IDs per pair.

This means tasks.md says "178 total tasks" but the count includes both sides of pairs, so the actual "task unit" count is smaller (~89 units).

**Suggested Fix**: Either:

- Keep tasks.md's 178-ID scheme but explicitly document "each pair of consecutive IDs (RED + GREEN) represents one task unit; 178 IDs = 89 task units" in the summary.
- Renumber to use one ID per pair (Phase 6c becomes T137–T146 covering 10 families), aligning with Phase 3 style (which writes "T020 RED ... T021 GREEN — Core" as two lines under one logical task but could be compressed).

Low implementation risk; purely editorial clarity.

---

### [LOW] Pass 1 Architecture: Directory.Packages.props is not `src/` — commit-discipline scope

**Location**: [spec.md FR-002](spec.md) scope "src/-touching"; various tasks touch `Directory.Packages.props` (T157, potentially T006 via `YamlDotNet` add).

**Details**: Commit-discipline-gate checks `src/`-touching commits. `Directory.Packages.props` is at repo root (not `src/`). A commit that bumps a NuGet pin — arguably a production-impact change — would bypass the gate. Not a correctness issue today (all such commits in 005 are either bundled with `test/feat(005)` subjects anyway, or are `chore`-prefixed docs/CI commits).

**Suggested Fix**: Extend commit-discipline-gate's trigger scope in T168/T169 GREEN to match `src/**` OR `Directory.Packages.props` OR `global.json`. Update [spec.md FR-002](spec.md) wording from "src/-touching" to "production-affecting (src/**, Directory.Packages.props, global.json)".

---

### [LOW] Pass 2 Naming: Architecture-test file names include "JobTests" / "StepTests"

**Location**: [tasks.md T010/T012/T014/T159/T161/T164/T166](tasks.md) — `CoverageCollectionTests`, `CoverageSummaryJobTests`, `CoverageThresholdTests`, `MarkdownLinkCheckJobTests`, `SnippetExtractionJobTests`, `ArchitectureTestsJobTests`, `BenchmarkRegressionJobTests`, `CommitDisciplineGateTests`, `RedCommitVerificationStepTests`.

**Details**: Naming varies between `*Tests.cs`, `*JobTests.cs`, `*StepTests.cs`. The existing architecture-rule files (`ProviderCompletenessTests`, `CodeOrganizationTests`, `DependencyDirectionTests`) use bare `*Tests.cs`. Adding `Job`/`Step` suffix breaks the convention.

**Suggested Fix**: Normalise to `*Tests.cs` only — `CoverageSummaryJobTests` → `CoverageSummaryTests`, `MarkdownLinkCheckJobTests` → `MarkdownLinkCheckTests`, etc. Purely cosmetic.

---

### [LOW] Pass 3 Coverage: Phase 4 NoSql count unspecified in T100

**Location**: [tasks.md T100/T101](tasks.md) "Rig.TUnit.Databases.NoSql.* providers per-audit".

**Details**: Task defers provider list to T005's Phase-1 audit. Without the audit the precise provider count for T100's scope is unknown. Estimated effort depends on N providers (could be 0, 4, or 7 depending on which NoSql leaves lack Options/Builder). Tasks.md does not bound the range.

**Suggested Fix**: Either (a) inline the list here once T005 completes — this is a non-code documentation update via `docs(005): T100 — refine NoSql scope post-audit`; or (b) pre-audit the NoSql leaves now as part of this analysis to provide the bound.

`ls src/Rig.TUnit.Databases.NoSql.*/` inspection shows 8 NoSql providers (Cassandra, Cosmos, Dynamo, ElasticSearch, KurrentDb, Mongo, Redis + base). 003/004 history indicates most already ship Options + Builder. Likely T100 touches 0–2 providers; planning estimate is conservative.

---

### [LOW] Pass 3 Coverage: T174 full-CONTRIBUTING labelled GREEN-only

**Location**: [tasks.md T174](tasks.md) "GREEN only — full-gate-set CONTRIBUTING.md".

**Details**: T174 has no matching RED task. CONTRIBUTING.md is docs-only, not `src/`, so strictly speaking it's exempt from FR-001. But T170/T171 introduces `red-commit-verification` which walks every RED commit and checks matching GREEN — the reverse check (every GREEN has a RED) is not specified in spec but may be tempting to add. T174 would fail such a check.

**Suggested Fix**: Leave as-is (docs-only exemption is reasonable per FR-001 scope). Optionally add a spec clarification: "GREEN-only commits are permitted for docs-only / CI-config-only / chore-only changes where no behavioural assertion can meaningfully fail beforehand. Subject prefix MUST be `docs`/`ci`/`chore`, never `feat`." This makes the carve-out explicit.

---

## Cross-Artefact Drift Summary

| Artefact | Drift vs authoritative tasks.md | Severity |
|---|---|---|
| plan.md | Task IDs T100–T155 obsolete; use T120–T178 | CRITICAL (Finding #1) |
| research.md | R10 references `T153` for red-commit-verify; R14 references `T100` for LICENSE | CRITICAL (same finding) |
| data-model.md | Entity 3 references `Phase 7 T152` for commit-discipline; Entity 5 references `T151` for regression | CRITICAL (same finding) |
| quickstart.md | §4 Phase 1 example uses T001/T002 (correct); §7 Phase 6c refers to `T121` (tasks.md has T139/T140) | CRITICAL (same finding) |
| spec.md | No task-ID references (resolved-by-design); only FR+SC numbers | OK |
| checklists/requirements.md | No task-ID references | OK |

---

## Pass Results

| Pass | Status | Findings |
|---|---|---|
| 1. Architecture Consistency | PASS with 2 LOW | #9 T157 bundling, #10 Directory.Packages.props scope |
| 2. Naming Consistency | FAIL — 1 CRITICAL, 2 LOW | #1 ID collision, #10 architecture-test naming, #11 Phase 6c pair convention |
| 3. Coverage Gaps | FAIL — 4 HIGH, 3 MEDIUM, 2 LOW | #2 Phase 6c RED, #3 T157 RED, #4 T016 threshold flip, #5 FR-004 no task; #6 SC-013 no task, #7 T005 precedent, #8 T017 stub; #12 T100 NoSql count, #13 T174 GREEN-only |
| 4. Concurrency | N/A (test-infrastructure library; no shared-entity concurrency concerns) | — |

---

## Recommended Order of Fix

1. **Finding #1 (CRITICAL)** — Task-ID sync. Schedule a pre-implementation chore: `docs(005): sync task IDs across planning artefacts (plan.md, research.md, data-model.md, quickstart.md)`. Authoritative source = tasks.md. ~30 min of mechanical find-replace.
2. **Finding #4 (HIGH)** — T016 threshold flip. Move the `continue-on-error: false` commit out of Phase 2 T016 and into a new task `T069b` at Phase 3 close. Update spec FR-022 wording.
3. **Finding #2 + #3 (HIGH)** — Reorder Markdig rewrite into Phase 6a. Promote T157 to `T123b` (right after canonical template), keep T158 at Phase 6d end. This resolves both Phase 6c RED-not-genuinely-red (Finding #2) and T157-RED-not-genuinely-red (Finding #3) in one move.
4. **Finding #5 + #6 (HIGH / MEDIUM)** — Add `T104b` or add a CI step in architecture-tests job asserting no new skip markers AND no undocumented `Shared*Fixture.cs` occurrences. Address both SCs (SC-012 + SC-013) with one enforcement point.
5. **Findings #7, #8, #9, #10, #11, #12, #13 (MEDIUM/LOW)** — Editorial / clarification updates to spec.md and tasks.md. Can be bundled with #1's sync chore.

---

## Next

```
No CRITICAL-blocking architectural issues remain once Finding #1 is resolved.
Four HIGH findings (TDD-discipline ordering) are tractable by a single reordering pass.
```

Recommended sequence:

1. Address Finding #1 (task-ID sync) — mechanical, 30 min.
2. Address Findings #2, #3, #4 (TDD ordering) by editing spec.md + plan.md + tasks.md with the reorder — ~1 h.
3. Address Finding #5 + #6 (enforcement gap) by adding the new task / CI step — ~30 min spec edit.
4. Bundle Findings #7–#13 editorial fixes into the same PR as step 1.
5. `/dotnet-ai-kit:implement` from T001.

**After Finding #1 is fixed, there are no blocking CRITICAL issues. HIGH findings are TDD-discipline risks that would become real problems during Phase 6 execution — fixing before start is strongly recommended.**
