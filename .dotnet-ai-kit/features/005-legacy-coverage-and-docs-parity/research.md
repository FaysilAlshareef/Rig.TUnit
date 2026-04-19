# Research: 005-legacy-coverage-and-docs-parity

**Generated**: 2026-04-19
**Scope**: Capture the state facts, tool constraints, and evidence that drive the Phase 1–7 task choices in [plan.md](plan.md). Each finding has a reference to a concrete file, line, or external doc so reviewers can verify.

---

## R1 — Postgres CI flake root cause (Phase 1 driver)

**Finding.** `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs:31` fails intermittently on master CI with `Npgsql.PostgresException 42P01: relation "Samples" does not exist`. Full RCA in [CI-Postgres-Flake-RCA.md](../../../planning/post-004-remediation/CI-Postgres-Flake-RCA.md).

**Mechanism.** `SharedPostgresFixture.GetAsync()` returns one `PostgresFixture` (one physical DB) to every test in the project; TUnit parallelism means `UsePostgres_DbContext_PerformsInsertSelectRoundTrip` can call `EnsureCreatedAsync` on a sibling's just-dropped schema, then fail at `SaveChangesAsync` when the sibling drops it again.

**Pre-existing.** Feat-branch CI history: 5 failures + 1 green + merge. The master-green feat-branch commit was luck, not a fix.

**Fix direction (Option A preferred).** Request a per-test ephemeral database via `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` — shipped per 004 FR-005. Option B (unique schema per test) invasive; Option C (`[NotInParallel]`) halves parallelism AND FR-004 now forbids the marker as a workaround.

**Reference.** [CI-Postgres-Flake-RCA.md §Fix direction](../../../planning/post-004-remediation/CI-Postgres-Flake-RCA.md).

---

## R2 — Shared-fixture pattern prevalence (Phase 1 + 3 audit)

**Finding.** `grep -rn "Shared.*Fixture"` under `tests/` returns 66 matches across 22 test projects. Every SQL / NoSQL / Messaging / Storage integration project ships one.

**Documented-safe exception.** `Rig.TUnit.Databases.NoSql.Redis.Tests.Integration/SharedRedisKvFixture.cs` reuses `Rig.TUnit.Caching.Redis.Tests.Integration/SharedRedisFixture.cs` — intentional per 004 spec edge case. Marker: needs an explicit `// Intentional reuse per 004 edge case` comment or it fails audit.

**Implication.** T005 produces the Phase 3 conversion work-list. Expected outcome: ~18 of 22 Shared*Fixture usages are unsafe and convert to per-test isolation; ~4 are documented-safe and gain a rationale comment.

**Reference.** grep output against `tests/`; 004 spec Edge Cases ("Redis KV reusing Caching.Redis fixture").

---

## R3 — TestCompletenessTests skip list (Phase 3 scope source)

**Finding.** `tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs` lines 22-53 enumerate every provider missing at least one of the four canonical test categories. The file was added in 004 T157a; explicit skip list was the 004 Phase 6 deferral.

**Mapped to [Test-Coverage-Gap-Matrix.md](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md):**

- P0 foundation (5): Core, Mediator, Grpc, WebAPI, Http
- P1 utilities (5): Ci, Concurrency, HealthChecks, Parallelism, Resilience
- P1 legacy providers (5): Caching.Memory, Caching.Redis, Databases.Sql.Sqlite, Databases.Sql.SqlServer, Databases.NoSql.Redis
- P1 observability (3): Observability.Logging, Observability.Seq, Observability.Tracing
- P1 microservices (5): Microservices.Contracts, Microservices.Saga, Microservices.Inbox, Microservices.Outbox, Microservices.Snapshots

**Total**: 23 providers, estimated 46–50 RED+GREEN commit pairs in Phase 3.

**Reference.** [Test-Coverage-Gap-Matrix.md §Projects failing FR-030](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md).

---

## R4 — Benchmark gap (FR-033 / FR-037 scope)

**Finding.** 21 providers lack any BenchmarkDotNet class under `tests/Rig.TUnit.Benchmarks/`:

```
Caching.Memory, Caching.Redis, Ci, Concurrency, Databases.Sql.SqlServer,
Databases.Sql.Sqlite, Databases.NoSql.Redis, HealthChecks, Mediator,
Microservices.Contracts, Microservices.Inbox, Microservices.Outbox,
Microservices.Saga, Microservices.Snapshots, Observability.Logging,
Observability.Logging.Analyzers, Observability.Seq, Observability.Tracing,
Parallelism, Resilience, WebAPI
```

**Implication.** Each needs one `*Benchmarks.cs` contributed to the single shared `Rig.TUnit.Benchmarks` project. Minimum per FR-037: `[MemoryDiagnoser]` + cold-path `InitializeAsync` + one representative operation.

**Reference.** [Test-Coverage-Gap-Matrix.md §Benchmark gap](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md).

---

## R5 — Coverage collection mechanism (Phase 2 constraint)

**Finding.** TUnit runs under Microsoft.Testing.Platform (MTP) per [global.json](../../../global.json):

```json
"test": { "runner": "Microsoft.Testing.Platform" }
```

**Consequence.** `coverlet.msbuild /p:CollectCoverage=true` is **not supported** under MTP. This was already acknowledged in 004 FR-036. The MTP-native flag is the only working path:

```bash
dotnet run --no-build -c Release --project tests/<proj>/ \
  -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

Output lands in `<proj>/bin/Release/net10.0/TestResults/`.

**Insurance pins.** `coverlet.collector` + `coverlet.msbuild` remain pinned in `Directory.Packages.props` as transitive-graph insurance but are NOT the primary collection path.

**Reference.** [global.json](../../../global.json); [CI-Artifact-And-Coverage-Proposal.md §Background](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md); [004 FR-036](../004-provider-consistency-remediation/spec.md).

---

## R6 — Coverage empirical floor on the 4-test template

**Finding.** Feature 004 measurement on Mongo + Postgres (2026-04-18) showed the basic Unit + Integration + Contract + Benchmark template lands at 77–87 % line coverage — **below the 90 % gate**.

**Cause (004 empirical).** Init-only autoprops on `*FixtureOptions` don't register as line-covered when exercised only through DI binding (coverlet measurement quirk). RigBuilder constructor-only code is dead-covered by integration tests.

**Fix (004 FR-035, carried forward to 005 FR-038).** Every provider that lands below 90/85 after basic fill-in adds two Unit tests:

1. `{Provider}FixtureOptionsTests.cs` — constructs Options with defaults + every property overridden.
2. `{Provider}RigBuilder_ExerciseTests.cs` — drives `Use{Provider}` (SQL) or the `ConnectionString` getter (others) through a minimal test double.

**Implication.** Phase 3 tasks that miss the gate add these as a RED+GREEN pair inside the same PR.

**Reference.** [004 FR-035](../004-provider-consistency-remediation/spec.md).

---

## R7 — CI workflow current shape (Phase 1 + 2 + 7 integration points)

**Finding.** `.github/workflows/ci.yml` (233 lines) has 10 jobs:

1. `build-unit-arch` (windows-latest, pwsh loop enumerating `tests/**/*.csproj` excluding Integration/Benchmarks/Contract)
2–10. `integration-{sql,nosql,caching,messaging,microservices,security,observability,storage,core}` (ubuntu-latest, matrix over providers per family)

**Observations for plan:**

- No `actions/upload-artifact@v4` anywhere → Phase 1 T007 adds HTML upload.
- No `--coverage` flag anywhere → Phase 2 T011 adds MTP-native collection.
- No `coverage-summary` job → Phase 2 T013 adds it.
- No `architecture-tests` dedicated job (arch tests run inside `build-unit-arch`'s pwsh loop mixed with unit runs) → Phase 7 T164/T165 adds it.
- No `benchmark-regression` → Phase 7 T166/T167 adds it.
- No `commit-discipline-gate` → Phase 1 T008 (minimal) + Phase 7 T168/T169 (hardened) + T170/T171 (red-commit-verification) add it.
- `fail-fast: false` matrix → preserved (one green provider doesn't hide a sibling flake).

**Reference.** [.github/workflows/ci.yml](../../../.github/workflows/ci.yml).

---

## R8 — Markdig for README parser (C-003 resolution)

**Finding.** CommonMark-compliant Markdown parsing is non-negotiable for the 14-section gate because READMEs reference `docs/templates/PROVIDER_README_TEMPLATE.md` and embed fenced-code examples that show other READMEs' structure (`## Quick start`, `## Purpose & value`). Regex `^##\s+` false-positives on those.

**Markdig facts.**

- NuGet package `Markdig` (author: Alexandre Mutel / xoofx).
- Licence: BSD-2-Clause (MIT-compatible).
- Runtime footprint: ~200 KB.
- Transitively pulled by `dotnet-format` analysers in the .NET 10 toolchain (verify during T123b chore implementation).
- AST-based — fenced code blocks, setext headings, tables, link resolution all handled natively.

**Implication for Phase 6a T123b (moved from T140 per analyze #2/#3/#9).** Add a direct `PackageReference Include="Markdig"` to `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` with a fresh `PackageVersion` pin in `Directory.Packages.props`. This lands as a `chore(005):` commit (GREEN-only per FR-001 dependency-pin exemption) so the subsequent T123c RED commit can rewrite `ReadmeCompletenessTests` against the new dependency without mixing pin+rewrite concerns. Licence attribution goes into `docs/third-party-notices.md` (produced by T136).

**Reference.** [C-003 resolution in spec.md](spec.md); https://github.com/xoofx/markdig.

---

## R9 — Documentation-audit quality bar (Phase 6 scope driver)

**Finding.** All 51 existing READMEs fall below the 14-section canonical template; 12 more are missing outright. Total scope: 63 rewrites.

**Section list (FR-065, [Documentation-Audit §3.1](../../../planning/post-004-remediation/Documentation-Audit.md)):**

1. Top-of-file badges (NuGet version, downloads, CI, coverage, licence)
2. `## Purpose & value`
3. `## When NOT to use`
4. `## Install` (+ version-compat matrix, Docker/OS prereqs)
5. `## Quick start` (runnable `[Test]` copy-paste)
6. `## Configuration` (full Options table + SectionName + appsettings + env var binding)
7. `## API surface`
8. `## Fluent wiring` (RigBuilder.Use{Provider}, RigConnect, IsolationKey, CancellationToken, disposal)
9. `## Provider quirks`
10. `## Troubleshooting`
11. `## Testing contracts` (which {Family}RigContract + ParallelIsolationContract inherited)
12. `## Performance` (BenchmarkDotNet class + baseline numbers)
13. `## Dependencies & related packages`
14. `## Spec, versioning, contributing`

**Variant for base / meta packages.** Sections 9, 10, 12 may be `## §N — N/A: <rationale>` per [§3.2](../../../planning/post-004-remediation/Documentation-Audit.md). Other 11 sections mandatory.

**Effort.** 45–90 min per README × 63 = ~47–94 hours of writing (per [Documentation-Audit §8](../../../planning/post-004-remediation/Documentation-Audit.md)). Plus ~3–4 hours for template + QUALITY-BAR + gate tightening + link checker = **~80–110 hours total**, which is the basis for running Phase 6 on its own parallel branch.

**Reference.** [Documentation-Audit.md §3.1, §7, §8](../../../planning/post-004-remediation/Documentation-Audit.md).

---

## R10 — RED commit verification technique (FR-003 implementation)

**Finding.** GitHub Actions lacks a native "checkout-and-test-a-specific-commit" step, but the workflow mechanic is straightforward.

**Proposed T170/T171 shape** (was T153 in the draft plan).

```yaml
- name: Verify RED commits genuinely failed
  run: |
    set -e
    git fetch --all --unshallow || true
    BASE=$(git merge-base origin/master HEAD)
    for sha in $(git rev-list --reverse $BASE..HEAD); do
      subj=$(git log -1 --pretty=%s $sha)
      if [[ "$subj" =~ ^test\(005\):[[:space:]]T[0-9]+[[:space:]]—[[:space:]]RED ]]; then
        echo "::group::Verifying RED $sha :: $subj"
        worktree=$(mktemp -d)
        git worktree add --detach "$worktree" "$sha"
        pushd "$worktree"
        # Touched projects: extract from commit diff, filter to tests/
        touched=$(git diff-tree --no-commit-id --name-only -r "$sha" | grep -E '^tests/' | sed -E 's|/[^/]+$||' | sort -u)
        exit_ok=0
        for proj in $touched; do
          if ! dotnet test "$proj" --no-restore --filter "Category!=Integration&Category!=Benchmark"; then
            exit_ok=1
            break
          fi
        done
        popd
        git worktree remove --force "$worktree"
        if [[ $exit_ok -eq 0 ]]; then
          echo "::error::RED commit $sha did not fail locally"
          exit 1
        fi
        echo "::endgroup::"
      fi
    done
```

**Pitfall.** Some RED tests assert architectural facts (file absence, YAML content) that don't require a full build per project — the `touched` filter covers most cases; genuinely cross-cutting RED commits (e.g., `ReadmeCompletenessTests` rewrite) may need manual escape via `[skip-red-verify]` tag in the commit subject, documented in CONTRIBUTING.md.

**Reference.** Technique adapted from existing GitHub Actions patterns; no external citation required.

---

## R11 — `commit-discipline-gate` exemption policy (FR-002)

**Finding.** Exactly one commit is grandfathered: `2b149b2` (Feature 004 Phase 3.0 Postgres baseline commit that landed src code without a preceding RED, per [004 FR-034](../004-provider-consistency-remediation/spec.md)).

**Implementation.** The `commit-discipline-gate` script hardcodes the SHA prefix as the sole exemption:

```bash
EXEMPT_SHAS=("2b149b2")
for sha in ${commits}; do
  if is_exempt "$sha"; then continue; fi
  # ... verify preceding RED ...
done
```

**Anti-pattern to avoid.** Adding more exemptions. Spec FR-002 explicitly allows only this one SHA. Any future exemption requires a spec amendment, not a script edit.

**Reference.** [spec.md FR-002](spec.md); [004 FR-034](../004-provider-consistency-remediation/spec.md).

---

## R12 — Feature 004 state reference (baseline for 005)

**Finding.** Feature 004 merged at `9d3369f` with 1264 `[Test]` methods, 8 architecture rules present, 3 MEDIUM / 14 LOW advisories per [004 review.md](../004-provider-consistency-remediation/review.md). Verdict PASS.

**Carries forward to 005:**

- Every FR-030 / FR-031 / FR-033 / FR-035 / FR-036 behavior.
- The architecture-rule file list (8 rules, all in `tests/Rig.TUnit.Architecture.Tests/Rules/`).
- The coverage-lifting test pattern (`*FixtureOptionsTests.cs` + `*RigBuilder_ExerciseTests.cs`).
- The per-commit RED → GREEN discipline (tightened in 005 by removing every skip escape hatch).

**Does NOT carry forward (005 changes):**

- Coverage gate as paper-only (005 Phase 2 makes it real CI).
- `[Category("SkipUntilFixed")]` as an acceptable interim marker (005 FR-004 / FR-005 forbid new ones and retire old ones).
- Matrix-job retries (005 C-001: no retries, red is red).

**Reference.** [004 review.md](../004-provider-consistency-remediation/review.md); [004 handoff.md](../004-provider-consistency-remediation/handoff.md).

---

## R13 — Architecture test file inventory

**Finding.** `tests/Rig.TUnit.Architecture.Tests/Rules/` contains 8 files (verified 2026-04-19):

```
CodeOrganizationTests.cs          (87 lines)     — unknown skip status, verify Phase 7
CoverageRuleTests.cs              (170 lines)    — unknown skip status, verify Phase 7
DependencyDirectionTests.cs       (148 lines)    — unknown skip status, verify Phase 7
ForbiddenApiTests.cs              (120 lines)    — unknown skip status, verify Phase 7
ProviderCompletenessTests.cs      (182 lines)    — HAS SkipUntilFixed (Phase 4 retires)
ReadmeCompletenessTests.cs        (150 lines)    — HAS SkipUntilFixed (Phase 6d retires + Markdig rewrite)
TestCompletenessTests.cs          (187 lines)    — HAS SkipUntilFixed lines 22-53 (Phase 3 retires)
TestFileOrganizationTests.cs      (174 lines)    — HAS SkipUntilFixed (Phase 5 retires)
```

Plus 2 new files 005 adds:

```
OrphanFolderTests.cs              (to add Phase 1 T001)
CoverageCollectionTests.cs        (to add Phase 2 T010 — YAML assertion)
CoverageSummaryJobTests.cs        (to add Phase 2 T012 — YAML assertion)
CoverageThresholdTests.cs         (to add Phase 2 T014 — YAML assertion)
ArtifactUploadTests.cs            (to add Phase 1 T006 — YAML assertion)
```

**Implication.** The YAML-assertion test pattern (reading `ci.yml` via `YamlDotNet` or plain regex against file content) is a 005-introduced idiom. Research shows no existing tests do this — so the pattern is new; document it in Phase 6a's CONTRIBUTING.md.

**Reference.** `ls -la tests/Rig.TUnit.Architecture.Tests/Rules/` output.

---

## R14 — Licence compatibility (C-002 MIT resolution)

**Finding.** Every pinned package in `Directory.Packages.props` has an MIT-compatible licence:

- TUnit + TUnit.Assertions + TUnit.Core — MIT
- Testcontainers.* — MIT
- Microsoft.EntityFrameworkCore.* — MIT
- Npgsql.EntityFrameworkCore.PostgreSQL — PostgreSQL Licence (similar to BSD, MIT-compatible)
- Pomelo.EntityFrameworkCore.MySql — MIT
- Oracle.EntityFrameworkCore — Oracle Universal Permissive Licence (compatible for redistribution)
- Mediator.Abstractions + Mediator.SourceGenerator — MIT
- Grpc.AspNetCore — Apache-2.0 (MIT-compatible for combined distribution)
- Microsoft.ApplicationInsights — MIT
- KurrentDB.Client — Apache-2.0 (MIT-compatible)
- Verify.TUnit — MIT
- Markdig (to add in T123b chore) — BSD-2-Clause (MIT-compatible)

**Implication for T121 GREEN** (was T100 in the draft plan). The standard MIT `LICENSE` text at repo root attributed to "Faysil Alshareef" (per [.dotnet-ai-kit/config.yml `company.name`](../../../.dotnet-ai-kit/config.yml)) year 2026 is compatible with all transitive dependencies. No `NOTICE` file required for MIT alone, but Phase 6b ships `docs/third-party-notices.md` (T136) enumerating dependency licences for downstream due diligence.

**Reference.** [Directory.Packages.props](../../../Directory.Packages.props); [config.yml](../../../.dotnet-ai-kit/config.yml).

---

## R15 — Branch parallelism risk (005-a / 005-b conflict surface)

**Finding.** 005-a (tests / CI) and 005-b (docs) touch largely disjoint file sets.

**Overlap:**

- `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — 005-b Phase 6d rewrites this file. 005-a's Phase 3/4/5 tasks may add RED tests that reference it indirectly. **Mitigation**: 005-a MUST NOT touch this file after branching; any change to it is a 005-b concern.
- `tests/Rig.TUnit.Architecture.Tests/Infrastructure/AssemblyLoader.cs` — 005-a may add test projects requiring registration. 005-b doesn't touch this. Low conflict risk.
- `Directory.Packages.props` — 005-a adds no new pins (Testcontainers already at 4.11 per 004). 005-b Phase 6d adds `Markdig`. Single-line conflict possible; resolve by landing Markdig first via its own tiny PR if 005-a and 005-b race the file.
- `CONTRIBUTING.md` — 005-a Phase 2 T017 writes a stub; 005-b Phase 6a T121 writes the full version incorporating the stub's coverage content; 005-a Phase 7 T174 extends with the full gate set. Merge order matters — if 005-a merges first, T121's full rewrite MUST re-incorporate any already-merged stub content.

**Reference.** `git log --name-only` planning; verified by visual inspection of spec Affected Directories.

---

## R16 — Estimated task count and PR cadence

**Finding.** Rolling the per-phase task lists:

| Phase | Tasks (RED+GREEN pairs) | Estimated PRs |
|---|---|---|
| 1 | 8 | 1 |
| 2 | 8 | 1 |
| 3 | ~46 | 5 (one per P0/P1 group) |
| 4 | ~40 | 8 (one per family) |
| 5 | ~10 | 1 |
| 6a | 4 | 1 |
| 6b | 12 | 1 |
| 6c | ~20 (per-family batches) | 10 (one per family) |
| 6d | 8 | 1 |
| 7 | 12 | 1 |
| **Total** | **~168 RED+GREEN commits** | **~30 PRs** |

**Estimate basis.** 004 landed ~172 tasks across 7 phases with a similar PR count (~25). 005 has more test-writing and less new-production-code, so per-task cost is lower but total count is similar.

**Reference.** Aggregated from plan.md phase sections.

---

## References

All findings reference one of:

- [planning/post-004-remediation/*](../../../planning/post-004-remediation/) — research inputs
- [.dotnet-ai-kit/features/004-provider-consistency-remediation/*](../004-provider-consistency-remediation/) — precedent
- Live repository state (`git log`, `grep`, file inspection) at commit `7988fa0`
- External docs: Markdig (github.com/xoofx/markdig), Microsoft Testing Platform coverage guide, BenchmarkDotNet reporting
