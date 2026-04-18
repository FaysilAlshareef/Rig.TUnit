# Analysis Report: Provider Consistency Remediation

**Feature**: 004-provider-consistency-remediation | **Mode**: Generic
**Date**: 2026-04-18 | **Findings**: 7 (initial) + 3 (full-scan) = **10** | **Resolved**: 10 | **Status**: ALL FIXES APPLIED (2026-04-18)

## Summary

- CRITICAL: 0
- HIGH: 2 (RESOLVED) — Postgresql gap (initial) + README count accuracy (full-scan)
- MEDIUM: 5 (RESOLVED) — incl. orphan 003-era dir cleanup (full-scan)
- LOW: 3 (RESOLVED) — incl. Rig.TUnit meta-package ambiguity (full-scan)

## Resolution log (applied 2026-04-18 post-analysis)

| # | Severity | Finding | Resolution |
|---|---|---|---|
| 1 | HIGH | Postgresql remediation omitted | Added Phase 3.0 (T174–T176) in tasks.md; updated spec.md Overview + added US4 scenario 0; added FR coverage via E3.0 in data-model.md; plan.md Phase 3 table gains Postgresql row; planning gap matrix updated to show Postgresql in scope. |
| 2 | MEDIUM | `ParallelIsolationContract` absent from FR-014..FR-017 + T122/T130/T136 | FR-014, FR-015, FR-016, FR-017 now explicitly require `ParallelIsolationContract`; T122 (Cosmos), T130 (AppInsights), T136 (Docker) now instantiate `{Provider}ParallelIsolationTests`. |
| 3 | MEDIUM | Cosmos package ambiguity (`Testcontainers.CosmosDb` vs `GenericContainer`) | T118 now explicitly mandates `Testcontainers.GenericContainer` (NOT `Testcontainers.CosmosDb`) with rationale; research.md §R4 gains a package-choice note; reserved T200 tracks removal of the dead `Testcontainers.CosmosDb 4.6.0` pin. |
| 4 | MEDIUM | Coverage-gate mechanism unspecified | T002 adds `coverlet.collector 6.0.*` + `coverlet.msbuild 6.0.*` pins; T097/T140/T152 now carry concrete `dotnet test /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line ...` commands; research.md §R14 documents the tooling choice. |
| 5 | MEDIUM | Architecture-test redundancy / infrastructure reuse | T005 now instructs to reuse `AssemblyLoader` (already knows every Rig.TUnit.* assembly including the 4 new ones) and to NOT duplicate the fixture-base check from `CodeOrganizationTests.AllFixtures_ExtendFixtureBase`. |
| 6 | LOW | Reserved-range header math off by 6 | Renamed to "Reserved range (T177–T218)" (T174–T176 now used by Postgresql remediation); parallel-opportunities summary updated to "176 numbered + 42 reserved = 218". |
| 7 | LOW | Pomelo pin exact vs wildcard | T002 now bumps `Pomelo.EntityFrameworkCore.MySql` from `9.0.0` to `9.0.*` alongside the Testcontainers family bump. |

### Full-scan findings (2026-04-18 second pass)

| # | Severity | Finding | Resolution |
|---|---|---|---|
| 8 | HIGH | "57 of 59 provider packages lack README" figure — inherited from planning docs — is wildly inaccurate. Actual count: **20 of 32 leaf provider packages lack README**; 12 already ship one (Caching.Memory, Caching.Redis, Databases.NoSql.Redis, Databases.Sql.SqlServer, Databases.Sql.Sqlite, Messaging.ServiceBus, Observability.Logging, Observability.Logging.Analyzers, Observability.Seq, Observability.Tracing, Security.Jwt, Security.OAuth). | Updated spec.md:19, plan.md:195, tasks.md T007 and T155 with the correct count + enumerated the 12 providers already compliant. T007's skip list now reflects 20 providers (not "all except 2"). Total Phase-6 README backlog = 20 existing + 4 new packages (MySql, Oracle, Cosmos, AppInsights) = 24 READMEs. Planning docs' "57 of 59" figure explicitly called out as stale/superseded. |
| 9 | MEDIUM | Three orphan directories from 003 hard-cutover still present: `src/Rig.TUnit.SqlServer/obj/`, `src/Rig.TUnit.ServiceBus/obj/`, `tests/Rig.TUnit.Redis.Tests.Integration/obj/`. They contain only build artefacts, are not in `Rig.TUnit.slnx`, but violate 003's own US2 Scenario 1: "these directories MUST NOT exist". | Added **T003a** in Phase 1 (parallel with T004/T005/T006/T007): `git rm -rf` the three paths right after T003's baseline-test pass. Zero code impact — pure filesystem hygiene. T008 exit gate updated to depend on T003a. |
| 10 | LOW | `src/Rig.TUnit/Rig.TUnit.csproj` is a bare SDK project (ProjectReferences to Core/Mediator/Grpc/WebAPI) with no `<Description>` — unlike `Rig.TUnit.All.csproj` which explicitly declares "Meta-only: zero source .cs files". Ambiguous role — future contributors may drop source here. Also this folder hosts `Contributing-ProviderTemplate.md` (T004). | Added **T004a** in Phase 1: add a `<Description>` + `<GenerateDocumentationFile>false</GenerateDocumentationFile>` + `<!-- Meta-only -->` comment to `Rig.TUnit.csproj` matching the `Rig.TUnit.All.csproj` convention. Documents this package as the default convenience facade bundling Core + Mediator + Grpc + WebAPI (vs. `Rig.TUnit.All` which is the "everything" meta). |

All 10 findings resolved. Task count raised from 176 → 178 numbered. Spec / plan / tasks / data-model / research / gap-matrix now internally consistent and consistent with the actual 2026-04-18 codebase state.

## Passes run

- ✓ Pass 1: Architecture consistency (library-ecosystem variant)
- ✓ Pass 2: Naming consistency (spec ↔ plan ↔ tasks ↔ data-model)
- ✓ Pass 3: Coverage gaps (FR ↔ tasks traceability)
- ✓ Pass 4: Concurrency (fixture isolation across providers)
- — Passes 5–11 skipped (microservice-only)

---

## Findings

### [HIGH] Coverage Gap: Postgresql remediation omitted from spec and tasks

**Location**: spec.md:14 (Overview) vs plan.md:45 + planning/…/Rig.TUnit-Library-Design.md §4.1

**Details**:
The library-design gap matrix (§4.1) and plan.md's topology diagram both state **"Postgresql gets BuilderExtensions"** — meaning `PostgresBuilderExtensions.cs` (EF quickstart) is a remediation target. The provider-gap-matrix document (evidence snapshot) shows Postgresql with `BuilderExt: —` and `EF Ext: —`.

But **spec.md:14 (Overview) lists Postgresql among packages that "ship full Fixture + Options + Builder + Extensions + Helpers + README"** — contradicting both the plan and the source-of-truth gap matrix. tasks.md has no T{NNN} for Postgresql at all; the only Postgresql mention is in T005's non-skip list (where it's treated as complete).

**Impact**: `ProviderCompletenessTests` may pass for Postgresql (the file-based rule likely only checks required types), but the `UsePostgresInMemory`-style EF quickstart the design doc calls for will never ship. Phase 3 of this feature would close as "done" with a known gap.

**Suggested Fix**:
1. Correct spec.md:14 to remove Postgresql from the "complete" list.
2. Add a Phase 3g sub-section in tasks.md with ~2 tasks:
   - `T{new} RED→GREEN PostgresqlRigBuilderExtensions (UsePostgresInMemory fluent shortcut)`
   - `T{new} Add README for Rig.TUnit.Databases.Sql.Postgresql; confirm ProviderCompletenessTests GREEN`
3. Update `Rig.TUnit-Provider-Gap-Matrix.md` to mark Postgresql row intentionally covered.

---

### [MEDIUM] Inconsistency: `ParallelIsolationContract` coverage for new Phase-4 packages

**Location**: tasks.md:T122 (Cosmos), T130 (AppInsights), T136 (Docker) vs spec.md:279 (Architecture Scope) + plan.md:165 + FR-013

**Details**:
- spec Architecture Scope (line 279): "Every fixture MUST expose an `IsolationKey` derived from `ExecutionContext` and pass `ParallelIsolationContract`."
- plan.md:165: "Each new package lands with the full canonical layout plus an `*.Tests.Integration` project inheriting the family contract + `ParallelIsolationContract` + provider-specific quirk tests."
- FR-013 (MySql) explicitly requires `ParallelIsolationContract` ✓
- FR-014 (Oracle), FR-015 (Cosmos), FR-016 (AppInsights), FR-017 (Docker) **do not explicitly mention** `ParallelIsolationContract`.
- tasks.md T106 (MySql), T114 (Oracle) mention it explicitly ✓
- tasks.md T122 (Cosmos), T130 (AppInsights), T136 (Docker) **do not mention it**.

**Impact**: A scrupulous implementer following FR-015/016/017 literally may skip the parallel-isolation smoke for Cosmos/AppInsights/Docker. Production-Phase-6 gate (T165) won't catch this because the session-handoff checklist doesn't spell it out either.

**Suggested Fix**:
1. Amend FR-014 through FR-017 to include `ParallelIsolationContract`.
2. Amend T122 to add `CosmosParallelIsolationTests : ParallelIsolationContract<CosmosFixture>` alongside the contract + quirk tests.
3. Amend T130 to add `AppInsightsParallelIsolationTests`.
4. Amend T136 to add `DockerParallelIsolationTests` (running a tiny repeatable container like `alpine:3` 20× in parallel).

Single-point fix in tasks.md is enough — spec FR-014..FR-017 inherit the universal constraint from Architecture Scope.

---

### [MEDIUM] Ambiguity: Cosmos fixture — `Testcontainers.CosmosDb` package vs `GenericContainer`

**Location**: `Directory.Packages.props:26` (already pinned `Testcontainers.CosmosDb 4.6.0`) vs plan.md Phase 4c + T118

**Details**:
`Testcontainers.CosmosDb` is pinned at 4.6.0 (scheduled to bump to 4.11 under C-001) but targets the legacy Windows emulator image. Feature 004 uses the Linux emulator (`mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`) with a custom wait probe at `/_explorer/emulator.pem`.

The `Testcontainers.CosmosDb` module's public API (`CosmosDbBuilder` / `CosmosDbContainer`) hard-codes the Windows image path and the default wait strategy — unusable for vNext Linux. Implementation will likely use `Testcontainers.GenericContainer` (from the base `Testcontainers` package) instead.

Neither plan.md nor tasks.md explicitly calls out which. An implementer may try `Testcontainers.CosmosDb` first, hit the image mismatch, and thrash.

**Impact**: ~30 min of implementation friction at T118.

**Suggested Fix**:
1. Add a single sentence to T118 (or research.md §R4): "Use `Testcontainers.GenericContainer` (from the base `Testcontainers` package) — NOT `Testcontainers.CosmosDb`, which targets the Windows emulator only."
2. Consider removing `Testcontainers.CosmosDb 4.6.0` from `Directory.Packages.props` if no production code references it post-004 — it's dead weight in the transitive graph.

---

### [MEDIUM] Coverage-gate mechanism not specified in tasks

**Location**: tasks.md T097, T140, T152 (coverage-gate verification tasks)

**Details**:
Three tasks declare "Verify coverage gate: line ≥ 90% + branch ≥ 85% per modified package" but none names the tool or the command. The 003 feature used coverlet + ReportGenerator (referenced in `tests/Rig.TUnit.Architecture.Tests/coverage-whitelist.txt`, confirmed present during research). The implementer may or may not know this.

**Impact**: First coverage check may stall while the implementer figures out the invocation.

**Suggested Fix**:
Annotate T097/T140/T152 with the concrete command, e.g.:
`dotnet test --collect:"XPlat Code Coverage" /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=total`
Or simply reference the existing 003 CI workflow snippet that runs coverage.

---

### [MEDIUM] `CoverageRuleTests` already exists — new rules should integrate with existing harness

**Location**: `tests/Rig.TUnit.Architecture.Tests/Rules/` (existing: `CoverageRuleTests.cs`, `CodeOrganizationTests.cs`, `DependencyDirectionTests.cs`, `ForbiddenApiTests.cs`) vs tasks.md T005–T007 (new rules)

**Details**:
Four architecture-rule files already exist. Tasks T005–T007 add three new files (`ProviderCompletenessTests.cs`, `TestFileOrganizationTests.cs`, `ReadmeCompletenessTests.cs`) but do not specify whether:
- They should share the existing `AssemblyLoader` infrastructure (`tests/Rig.TUnit.Architecture.Tests/Infrastructure/`) — yes, per research.md §R7.
- The existing `CodeOrganizationTests.AllFixtures_ExtendFixtureBase` overlaps with the new `ProviderCompletenessTests` fixture-base check — potential redundancy.

**Impact**: Low; new rules will work stand-alone. Redundancy is cosmetic but reviewers may question why two rules test fixture-base inheritance.

**Suggested Fix**: Add a one-line note in T005 — "Reuse `AssemblyLoader` helper from `tests/Rig.TUnit.Architecture.Tests/Infrastructure/`. Defer fixture-base check to the existing `CodeOrganizationTests.AllFixtures_ExtendFixtureBase`; `ProviderCompletenessTests` only enforces the presence of the four canonical types."

---

### [LOW] Reserved-range math inconsistency

**Location**: tasks.md (final "Reserved range" section + "Parallel opportunities summary")

**Details**:
- Summary says "Total: 173 numbered + 45 reserved = 218 slots"
- Reserved-range header says "(180–218)" → 218 − 180 + 1 = 39 slots
- Gap: T174–T179 (6 slots) is neither numbered nor labeled reserved

**Impact**: None functionally; a reviewer may notice the off-by-one.

**Suggested Fix**: Rename header to "Reserved range (T174–T218)" — 45 slots — matching the summary.

---

### [LOW] Pomelo pin specificity vs design doc wildcard

**Location**: `Directory.Packages.props:44` (`Pomelo.EntityFrameworkCore.MySql 9.0.0`) vs planning/…/Rig.TUnit-Library-Design.md §6.1 ("Pin to `9.0.*`") and spec FR-008 / US8 scenario 1

**Details**:
The design doc and spec both say "pin to `9.0.*`" (wildcard). The props file has the exact `9.0.0`. Practically identical today (9.0.0 is the only 9.0.x release), but the wildcard anticipates 9.0.x servicing updates.

**Impact**: Zero today. If Pomelo ships `9.0.1` with a bugfix, the exact pin requires a manual bump while wildcard would auto-consume.

**Suggested Fix**: Bump `Pomelo.EntityFrameworkCore.MySql` pin to `9.0.*` when T002 lands (include in the same props-file edit).

---

## Coverage matrix (FR ↔ Task)

All 26 FRs trace to at least one task. No orphan tasks detected.

| FR | Tasks |
|---|---|
| FR-001 | T005 |
| FR-002 | T006, T019 (enforcement flip) |
| FR-003 | T007, T156 (enforcement flip) |
| FR-004 | T005, T006, T007, T019, T156 |
| FR-005 | T022–T095 (Phase 3 + coverage at T096) |
| FR-006 | T043, T046, T050, T054 |
| FR-007 | T066, T069, T073, T077 |
| FR-008 | T079–T089 |
| FR-009 | T094 |
| FR-010 | T019 (rule enforcement) |
| FR-011 | T011–T018 |
| FR-012 | Plan Phase 2 narrative (no task — policy statement) |
| FR-013 | T100–T107 |
| FR-014 | T108–T115 |
| FR-015 | T116–T123 |
| FR-016 | T124–T131 |
| FR-017 | T132–T137 |
| FR-018 | T142–T145 |
| FR-019 | T146–T148 |
| FR-020 | T149–T151 |
| FR-021 | T100, T108, T116, T124, T136 (slnx registrations) |
| FR-022 | T138 |
| FR-023 | T160–T162 |
| FR-024 | TDD-cadence header + T171 verification |
| FR-025 | T097, T140, T152, T164 |
| FR-026 | T003, T020, T139, T152, T164 |

Clarifications traced:
- **C-001** (Testcontainers bump) → T002
- **C-002** (file-based PactBrokerClientStub) → T149
- **C-003** (TestFileOrganizationTests applies to `*Contract.cs`) → T016 (extraction) + T019 (rule enforcement)

---

## Naming consistency spot checks

| Concept | spec | plan | data-model | tasks | Match |
|---|---|---|---|---|---|
| `MtlsFixture` | ✓ | ✓ | ✓ | T084 | ✓ |
| `PolicyFixture` | ✓ | ✓ | ✓ | T088 | ✓ |
| `CosmosFixture` | ✓ | ✓ | ✓ | T118 | ✓ |
| `AppInsightsAssert` | ✓ | ✓ | ✓ | T129 | ✓ |
| `PactBrokerClientStub` | ✓ | ✓ | ✓ | T149 | ✓ |
| `RuChargeCapture` | ✓ | ✓ | ✓ | T120 | ✓ |
| `TagCardinalityGuard` | ✓ | ✓ | ✓ | T094 | ✓ |
| `SecurityRigBuilder<TSelf>` | (already in src) | ✓ | ✓ | referenced by T079, T081, T085, T089 | ✓ |

No naming drift detected.

---

## Recommendation (historical — prior to fix-up)

One HIGH finding (Postgresql coverage gap) warranted a spec+tasks touch-up before `/dotnet-ai-kit:implement` began — otherwise Phase 3 would close without addressing a documented gap matrix cell.

The 4 MEDIUM findings were implementation-quality issues: worth fixing but not blockers. The 2 LOW findings were cosmetic.

**All 7 findings resolved in the same session (2026-04-18). See "Resolution log" above.**

---

## Final status

Spec / plan / tasks / data-model / research / planning gap-matrix are internally consistent. Feature is ready to implement.

## Next

```
/dotnet-ai-kit:implement   # begin execution at T001
```
