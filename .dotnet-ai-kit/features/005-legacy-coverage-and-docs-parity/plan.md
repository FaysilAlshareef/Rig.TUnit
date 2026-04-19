# Implementation Plan: Legacy Coverage & Docs Parity

**Feature ID**: 005-legacy-coverage-and-docs-parity
**Generated**: 2026-04-19 | **Revised**: 2026-04-19 (post-`/analyze` task-ID sync)
**Mode**: Generic (single-repo .NET 10 test-infrastructure library; `.dotnet-ai-kit/config.yml` confirms `repos.*: null`)
**Complexity**: Complex (7 phases, 56 FRs, 19 SCs, 8 user stories, ~35 new test projects, 63 README rewrites, 4 new CI jobs)
**Source spec**: [spec.md](spec.md) — 4 clarifications resolved (C-001..C-004), 0 markers remaining
**Task IDs**: aligned with [tasks.md](tasks.md) as the authoritative source of record. The analyze pass (`analysis.md`) reconciled earlier drift between draft plan IDs (T100..T155) and tasks.md IDs (T120..T178).

---

## Constitution Check

`.dotnet-ai-kit/memory/constitution.md` — **NOT PRESENT**. Gate skipped with warning. Run `/dotnet-ai-kit:learn` to generate one. In the meantime, `.claude/rules/*.md` + Feature 004 plan + [handoff.md](../004-provider-consistency-remediation/handoff.md) are the operative rulebook.

**Implied invariants (from `.claude/rules/*` + 004 carry-forward):**

- **Detect-first** — every change begins by reading the state Feature 004 merged (`tests/Rig.TUnit.Architecture.Tests/Rules/*` skip lists, `Shared*Fixture.cs` pattern across 22 test projects, `ci.yml` at 10 jobs, `Directory.Packages.props` with `Testcontainers.* 4.11` already pinned).
- **Pattern fidelity** — use Feature 004's proven per-family cadence (RED → GREEN). The canonical provider shape is `Rig.TUnit.Databases.Sql.SqlServer` (full: Fixture + Options + Builder + Extensions + Helpers + README). Every Phase 4 remediation target copies that shape.
- **Architecture-agnostic** — class-library ecosystem. No Clean Arch / VSA / microservice constraints inside the library itself.
- **TDD non-negotiable — tightened for 005**: RED commit MUST fail (`red-commit-verification`), GREEN commit MUST follow, NO `SkipUntilFixed` / `Skip` / permanent `NotInParallel` markers may be introduced (FR-004). Every `SkipUntilFixed` marker inherited from 004 is retired by its closing phase (FR-005).

---

## Executive Summary

Close every gap left by Feature 004: (1) fix the `master`-CI Postgres flake via per-test ephemeral DBs and audit every `Shared*Fixture.cs` occurrence; (2) enforce the FR-035 / FR-036 coverage gate in CI via the MTP-native `--coverage --coverage-output-format cobertura` path; (3) fill every missing test category on ~23 pre-004 providers (21 benchmarks, multiple Unit / Integration / Contract gaps) until `TestCompletenessTests` has zero skip entries; (4) complete the canonical `Options/ + Builder/` layout on ~20 pre-004 providers until `ProviderCompletenessTests` enforces uniformly; (5) extract inline test infrastructure into per-project `TestInfrastructure/` folders until `TestFileOrganizationTests` enforces uniformly; (6) rewrite all 63 src READMEs against a 14-section canonical template + ship root governance files + tighten `ReadmeCompletenessTests` from `> 100 chars` to a `Markdig`-parsed structural gate; (7) harden CI with dedicated `architecture-tests`, `benchmark-regression`, and `commit-discipline-gate` jobs.

**Delivery discipline — strictly test-first with zero skip escape hatches.**

- RED commit → GREEN commit per task (`test(005): TNNN — RED` then `feat(005): TNNN — GREEN`), enforced at PR gate by a new `commit-discipline-gate` CI job (FR-001, FR-002).
- `red-commit-verification` CI step checks out every RED commit and runs `dotnet test` — exit code MUST be non-zero (FR-003).
- **NO retries on CI matrix jobs.** Red is red. Every genuine flake gets root-caused (C-001 resolved: no retries).
- **NO new `SkipUntilFixed` / `Skip` / permanent `NotInParallel` markers** may be introduced anywhere in 005 (FR-004).
- **NO partial category fill-in.** Either all four canonical categories land together for a provider, or the task does not land (FR-006).
- Every task that retires a `SkipUntilFixed` marker lands in the same GREEN commit as the marker removal (FR-005, FR-036, FR-043, FR-053, FR-069).

**Phase order is a hard dependency chain.** No phase starts until the previous is green.

**Branch strategy** (spec Overview):

- **Phase 1** → `fix/005-phase-1-ci-stabilisation` (hotfix, short-lived).
- **Phases 2–5 + 7** → `feat/005-a-legacy-coverage-and-tests`.
- **Phase 6** → `feat/005-b-docs-parity` (parallel, file-level independent from 005-a).
- Both 005-a and 005-b merge to `master` (order-independent) before Feature 005 is closed.

---

## Target Architecture (post-005)

### Package topology

```
Rig.TUnit.Core, .Mediator, .Grpc, .WebAPI, .Http
  [UNCHANGED public surface; gain Integration + Contract + Benchmark test projects]

Rig.TUnit.Ci, .Concurrency, .HealthChecks, .Parallelism, .Resilience
  [UNCHANGED public surface; gain missing Unit / Contract / Benchmark test projects]

Rig.TUnit.Caching.Memory, .Caching.Redis, .Databases.Sql.Sqlite, .Databases.Sql.SqlServer, .Databases.NoSql.Redis
  [UNCHANGED public surface; gain Unit + Benchmark test projects]

Rig.TUnit.Observability.Logging, .Observability.Seq, .Observability.Tracing
  [UNCHANGED public surface; gain Unit + Benchmark test projects]

Rig.TUnit.Microservices.Contracts, .Microservices.Saga, .Microservices.Inbox, .Microservices.Outbox, .Microservices.Snapshots
  [UNCHANGED public surface; gain Unit + Benchmark test projects]

Pre-004 providers missing Options/ or Builder/ (Phase 4):
  Messaging.Kafka, Messaging.Nats, Messaging.RabbitMq, Messaging.Sqs,
  Storage.MinIO, Storage.AzureBlob, Storage.S3, Storage.FileSystem,
  Security.Jwt, Security.OAuth, Security.Mtls, Security.Policies,
  Caching.Memory, Caching.Hybrid, Caching.Fusion,
  Databases.NoSql.* (the ones without Options/Builder),
  Observability.Metrics
  → gain Options/ + Builder/ + Use{Provider} extension + family-specific helpers

Deleted orphans (Phase 1):
  src/Rig.TUnit.ServiceBus/              (pre-rename, only bin/obj/)
  tests/Rig.TUnit.ServiceBus.Tests.Integration/
  tests/Rig.TUnit.SqlServer.Tests.Integration/

Architecture tests (tests/Rig.TUnit.Architecture.Tests/Rules/):
  TestCompletenessTests       → skip list emptied by Phase 3 end
  ProviderCompletenessTests   → skip list emptied by Phase 4 end
  TestFileOrganizationTests   → skip list emptied by Phase 5 end
  ReadmeCompletenessTests     → skip list emptied + parser upgraded to Markdig by Phase 6d end
  CodeOrganizationTests, CoverageRuleTests, DependencyDirectionTests, ForbiddenApiTests
    [UNCHANGED; verify no skips already]

Root governance (Phase 6a):
  LICENSE (MIT), CONTRIBUTING.md, SECURITY.md, CHANGELOG.md, README.md (rewritten)

docs/ (Phase 6a + 6b):
  docs/templates/PROVIDER_README_TEMPLATE.md
  docs/QUALITY-BAR.md
  docs/adr/ADR-001..008.md
  docs/glossary.md
  docs/troubleshooting.md
  docs/performance-tuning.md
  docs/migration-001-to-004.md

All 63 src/Rig.TUnit.{X}/README.md rewritten against 14-section template (Phase 6c).

CI workflow (.github/workflows/ci.yml):
  existing 10 jobs gain --coverage flag + HTML report artefact upload
  new jobs: coverage-summary, architecture-tests, benchmark-regression,
            commit-discipline-gate, snippet-extraction (path-filtered per C-004),
            markdown-link-check

Baselines:
  benchmarks/coverage-baseline-005.json   (written at Phase 2 close)
  benchmarks/baseline-005.json            (written at Phase 3 close)
```

### Canonical provider layout (no change from 004)

```
src/Rig.TUnit.{Family}.{Provider}/
├── {Provider}.csproj
├── README.md                              ← 14-section canonical (Phase 6c)
├── Fixtures/{Provider}Fixture.cs          : {Family}FixtureBase
├── Options/{Provider}FixtureOptions.cs    ← SectionName + [Required] + ValidateOnStart
├── Builder/{Provider}RigBuilder.cs        : {Family}RigBuilder<{Provider}RigBuilder>
├── Builder/{Provider}RigBuilderExtensions.cs  ← Use{Provider}
├── Extensions/                            (SQL only — EF provider wire-up)
└── Helpers/                               (family-specific)
```

### Canonical test-project layout (per provider, Phase 3 completes the set)

```
tests/Rig.TUnit.{X}.Tests.Unit/             ← FixtureOptions + RigBuilder exercise (FR-038)
tests/Rig.TUnit.{X}.Tests.Integration/      ← end-to-end container / in-process flow
tests/Rig.TUnit.{X}.Tests.Contract/         ← {Family}RigContract inherited
tests/Rig.TUnit.Benchmarks/{X}*Benchmarks.cs  ← one class contributed to shared benchmark project
```

N/A matrix for bases and analysers (edge cases):
- `Rig.TUnit.Observability.Logging.Analyzers` — analyser only; ships Unit tests only, Integration/Contract/Benchmark marked `// N/A: Roslyn analyzer`.
- `Rig.TUnit.All`, `Rig.TUnit.Microservices`, `Rig.TUnit` (meta) — README-only per Phase 6; no four-category obligation (edge case in spec).
- Family bases (`Rig.TUnit.Caching`, `Rig.TUnit.Messaging` base, etc.) — Contract test exists per family; no provider-specific Integration/Benchmark obligation.

---

## Phase Plan

Seven phases mapping 1:1 to the spec's user stories US2–US8 (US1 is the cross-cutting TDD discipline enforced throughout).

### Phase 1 — CI stabilisation (1 day, branch `fix/005-phase-1-ci-stabilisation`)

**Goal:** master CI goes green; shared-fixture anti-pattern is machine-visible and partially converted.

**Task ordering (strict):**

1. **T001 (RED)** — Add an architecture test `OrphanFolderTests.cs` asserting `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` are absent. Run → fails on master (folders still present).
2. **T002 (GREEN)** — `git rm -r` all three folders; `OrphanFolderTests` now passes. Commit. (FR-012)
3. **T003 (RED)** — In `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`, add a deterministic assertion that schema is NOT shared across tests — e.g., each test creates `Samples_{Guid}` table and asserts only ITS table exists. Run → fails deterministically because `SharedPostgresFixture` hands one physical DB.
4. **T004 (GREEN)** — Switch the test to request a per-test ephemeral database via `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` (or add the helper if it doesn't ship exactly per 004 FR-005). Test passes. Run 10× locally to confirm determinism. (FR-010)
5. **T005 (RED + GREEN bundled as audit task)** — Create `planning/post-005-phase-1/SharedFixture-Audit.md` enumerating every `Shared*Fixture.cs` file under `tests/**/` (current count: 22). For each, classify: (a) safe-because-IsolationKey, (b) unsafe-needs-Phase-3-conversion, (c) needs-immediate-`[NotInParallel]`-stopgap-with-Phase-3-conversion-ticket. No code change in T005 — it produces the Phase 3 work-list.
6. **T006 (RED)** — Add `ArtifactUploadTests.cs` (or shell test) asserting `.github/workflows/ci.yml` contains `actions/upload-artifact@v4` for every job with `if: always()` and `retention-days: 14`. Run → fails (current YAML has none).
7. **T007 (GREEN)** — Update `ci.yml` per [CI-Artifact-And-Coverage-Proposal.md](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md) §Per-job step pattern — add the HTML report upload step after every `dotnet test` invocation. (FR-013)
8. **T008** — Open PR `fix/005-phase-1-ci-stabilisation → master`. PR check: `commit-discipline-gate` (newly introduced, see Phase 7 note below) walks the 7-task commit log and confirms T001/T003/T006 are RED and T002/T004/T007 are GREEN. Run CI 10 times in a row via `gh workflow run ci.yml` scripted loop; all 10 MUST be green. (FR-014, SC-001)

> **Note on `commit-discipline-gate`**: Phase 1 introduces a minimal version of this CI job (just the pattern check). Phase 7 hardens it with per-commit RED verification. Phase 1's minimum is sufficient for the 7-commit Phase 1 PR.

**Exit gate:** 10 consecutive green master CI runs after merge; no new `SkipUntilFixed` markers introduced (FR-004 verified by `grep`); three orphan folders gone; shared-fixture audit document committed.

---

### Phase 2 — Coverage gate enforcement (1–2 days, branch `feat/005-a-legacy-coverage-and-tests`)

**Goal:** FR-035 / FR-036 becomes a real CI gate.

**Task ordering:**

1. **T010 (RED)** — Add `CoverageCollectionTests.cs` (shell / YAML assertion) asserting every integration matrix job in `ci.yml` includes `-- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`. Run → fails (only the existing `dotnet test` invocations lack the flags).
2. **T011 (GREEN)** — Update every integration matrix job per [CI-Artifact-And-Coverage-Proposal.md](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md) §Per-job step pattern — forward `--coverage --coverage-output-format cobertura` to the MTP-native runner. Upload the cobertura XML alongside the HTML report. (FR-020)
3. **T012 (RED)** — Add `CoverageSummaryJobTests.cs` (YAML assertion) asserting `ci.yml` contains a `coverage-summary` job that `needs:` every matrix job, runs on `if: always()`, merges cobertura via `dotnet-reportgenerator-globaltool`, publishes `SummaryGithub.md` to `$GITHUB_STEP_SUMMARY`, uploads `./coverage-report/` with 30-day retention. Run → fails (no such job yet).
4. **T013 (GREEN)** — Add the `coverage-summary` job per the proposal §New summary job. (FR-021)
5. **T014 (RED)** — Add `CoverageThresholdTests.cs` (YAML assertion) asserting the `coverage-summary` job fails when any package falls below line ≥ 90 % / branch ≥ 85 %. Run → fails (no threshold step yet).
6. **T015 (GREEN)** — Add a threshold-check step to the `coverage-summary` job using `reportgenerator` threshold flags or a custom bash script parsing the merged `cobertura.xml`. (FR-022)
7. **T016 (baseline capture)** — Trigger CI; capture the merged report; serialise per-package line / branch to `benchmarks/coverage-baseline-005.json`. (FR-023)
8. **T017 (docs)** — Update `CONTRIBUTING.md` (or stub — Phase 6a writes the real one) with a "Coverage gate" section explaining the MTP-native collection command, the `coverlet.msbuild` incompatibility under MTP, and the 90/85 threshold. (FR-024)

**Exit gate:** `coverage-summary` runs on PRs; threshold step is live but **non-blocking for the first run** (report-only). After `coverage-baseline-005.json` lands, a follow-up commit flips the threshold to blocking. From that commit onward, every PR that degrades coverage fails `coverage-summary`.

---

### Phase 3 — Legacy test-category fill-in (5–7 days, same branch)

**Goal:** every pre-004 provider ships `{ Unit, Integration, Contract, Benchmark }` per [Test-Coverage-Gap-Matrix.md](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md).

**Per-provider cadence (STRICT, FR-006):**

```
Commit N   (RED)    test(005): TNNN — RED for {Provider} — missing {Unit|Integration|Contract|Benchmark}
             new tests/Rig.TUnit.{Provider}.Tests.{Category}/  OR
             new tests/Rig.TUnit.Benchmarks/{Provider}Benchmarks.cs
             minimum failing assertion: Assert.Fail("baseline: not implemented") OR
             a real behaviour assertion that fails against current code
             verified failing locally

Commit N+1 (GREEN)  feat(005): TNNN — GREEN implement {Provider} {Category}
             production wiring that makes the new tests pass
             +  remove {Provider} entry from TestCompletenessTests SkipUntilFixed list (lines 22-53)
             + if coverage drops below 90/85, add FR-035 coverage-lifting tests
               ({Provider}FixtureOptionsTests.cs + {Provider}RigBuilder_ExerciseTests.cs)
             all four canonical test categories now GREEN for this provider
```

**Task sequence (P0 foundation first, then P1 utilities / legacy / observability / microservices):**

| Group | Tasks | Providers | Missing categories |
|---|---|---|---|
| **P0 foundation** | T020–T024 | `Core`, `Mediator`, `Grpc`, `WebAPI`, `Http` | Integration + Contract + Benchmark (Core: Integration + Contract) |
| **P1 utilities** | T025–T029 | `Ci`, `Concurrency`, `HealthChecks`, `Parallelism`, `Resilience` | Unit + Contract + Benchmark varies per provider |
| **P1 legacy** | T030–T034 | `Caching.Memory`, `Caching.Redis`, `Databases.Sql.Sqlite`, `Databases.Sql.SqlServer`, `Databases.NoSql.Redis` | Unit + Benchmark (mostly) |
| **P1 observability** | T035–T037 | `Observability.Logging`, `Observability.Seq`, `Observability.Tracing` | Unit + Benchmark |
| **P1 microservices** | T038–T042 | `Microservices.Contracts`, `Microservices.Saga`, `Microservices.Inbox`, `Microservices.Outbox`, `Microservices.Snapshots` | Benchmark (Contracts/Saga); Unit + Benchmark (Inbox/Outbox/Snapshots) |

Each group is ~2 RED+GREEN commits per provider. Estimated: 23 providers × ~2 commits = ~46 commits in Phase 3. Provider order within a group is arbitrary; across groups, P0 → P1 is strict.

**Shared-fixture conversion (runs as sub-thread during Phase 3):**

- **T043 (RED+GREEN per file)** — For each `Shared*Fixture.cs` classified as "unsafe-needs-conversion" in T005's audit, write a test asserting per-test isolation (e.g., each test creates a keyed artefact; all tests run in parallel; each test's artefact is unique and only visible to that test). RED fails against the shared container; GREEN switches the test to a per-test ephemeral artefact (database / schema / keyspace / prefix) using the provider's existing `Helpers/*PerTestHelper.cs`. Files covered in the same pass: every `tests/**/UsePostgresFluentTests.cs`-equivalent across SQL / NoSql / Messaging / Storage families.

**Benchmark authoring rule (FR-037):**

- Each Benchmark class MUST measure at least `Fixture.InitializeAsync` cold-path allocation + throughput AND one representative public-surface operation (e.g., `CacheFixture.SetAsync` / `StorageFixture.PutAsync`). Allocation measurements come from `[MemoryDiagnoser]`. Numbers are captured at Phase 3 close in `benchmarks/baseline-005.json`.

**Coverage-lifting tests rule (FR-038):**

- If post-fill-in merged coverage for a provider lands below 90/85, add `{Provider}FixtureOptionsTests.cs` + `{Provider}RigBuilder_ExerciseTests.cs` per 004 FR-035 empirical precedent (Mongo/Postgres hit 77–87 % without them). Written as a RED+GREEN pair that lands in the same PR as the main fill-in.

**Exit gate (SC-002, SC-006, SC-007):**

- `TestCompletenessTests` skip list at lines 22-53 is emptied; the test enforces uniformly on every src project.
- Merged cobertura reports line ≥ 90 % / branch ≥ 85 % for every non-N/A package.
- `tests/Rig.TUnit.Benchmarks/` contains at least one `*Benchmarks.cs` for every non-N/A provider.
- `benchmarks/baseline-005.json` ships with the close-out commit.
- `grep -rn "Shared.*Fixture" tests/` returns ONLY documented safe cases (e.g., `NoSql.Redis` reuses `Caching.Redis`'s `RedisFixture`) with rationale comments.

---

### Phase 4 — Canonical layout completion (2–3 days, same branch)

**Goal:** ~20 pre-004 providers conform to `Fixtures/ + Options/ + Builder/ + [Extensions/] + [Helpers/]`.

**Per-provider cadence (FR-040 / FR-041 / FR-042):**

```
Commit N   (RED)    test(005): TNNN — RED for {Provider} — canonical shape
             assertion in ProviderCompletenessTests-flavoured unit test
             asserts {Provider}FixtureOptions/{Provider}RigBuilder/Use{Provider} exist
             verified failing because files absent

Commit N+1 (GREEN)  feat(005): TNNN — GREEN implement {Provider} canonical shape
             Options/{Provider}FixtureOptions.cs (SectionName + [Required] + defaults)
             Builder/{Provider}RigBuilder.cs (CRTP over {Family}RigBuilder<TSelf>)
             Builder/{Provider}RigBuilderExtensions.cs (Use{Provider})
             family-specific helpers (Listener/Sender for Messaging,
               SasBuilder for Storage, TagCardinalityGuard for Metrics)
             remove {Provider} from ProviderCompletenessTests SkipUntilFixed list
```

**Task scope (expected, pending T005's Phase-1 audit for final list):**

| Family | Providers needing Options/ | Providers needing Builder/ | Helpers |
|---|---|---|---|
| Messaging | Kafka, Nats?, Sqs, RabbitMq | Kafka, Nats, Sqs, RabbitMq | `{Provider}Listener + {Provider}EventSender` (FR-042) |
| Storage | MinIO, FileSystem | AzureBlob, S3, MinIO, FileSystem | `{Provider}SasBuilder` / `PathSandboxHelper` (FR-042) |
| Security | — (Mtls has Options) | Jwt, OAuth, Mtls, Policies | existing types stay as helpers (FR-042) |
| Caching | Memory, Fusion, Hybrid | Memory, Fusion, Hybrid | fail-safe + eager-refresh (Fusion) |
| NoSql | leaf-by-leaf per audit | leaf-by-leaf per audit | per-family helpers per 003 §4.4 |
| Observability | Metrics | Metrics | `TagCardinalityGuard` (FR-042) |

**Approach: batch by family.** One PR per family keeps review cadence manageable and aligns with the 005-roadmap's per-family merge recommendation (§Open question 2, resolved: per-family PRs).

**Exit gate (SC-003):**

- `ProviderCompletenessTests` skip list is empty; the rule enforces uniformly across every provider.
- Every provider exposes `Use{Provider}(this RigBuilder, Action<{Provider}RigBuilder>?)` on the public fluent surface.

---

### Phase 5 — Test-file hygiene sweep (3–4 days, same branch)

**Goal:** every `tests/**/*.cs` outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` declares exactly one top-level class containing only `[Test]`/`[Before]`/`[After]` methods.

**Per-project cadence (FR-050 / FR-051 / FR-052):**

```
Commit N   (RED)    test(005): TNNN — RED for {TestProject} — extract TestInfrastructure
             new TestInfrastructure/{Project}TestHarness.cs contains the extracted types
             the original *Tests.cs now references the harness via using statement
             compilation fails (harness doesn't exist yet, or references broken) ← this is RED

Commit N+1 (GREEN)  feat(005): TNNN — GREEN complete harness extraction
             harness populated with the extracted setup types (fixtures, fakers, harnesses)
             *Tests.cs file now contains only [Test] methods + private helpers
             TestFileOrganizationTests (with provider's SkipUntilFixed entry removed) passes
```

**Known offenders (from spec US6 + Project-Organization-Audit §7):**

| Test project | Extract targets | New harness file |
|---|---|---|
| `Rig.TUnit.Observability.Tracing.Tests.Integration` | `ActivitySource` + `TracerProvider` factories | `TestInfrastructure/TracingTestHarness.cs` |
| `Rig.TUnit.Http.Tests.Unit` | custom matchers + response-builder helpers | `TestInfrastructure/HttpMockTestHarness.cs` |
| `Rig.TUnit.Resilience.Tests.Integration` | Polly pipeline builders | `TestInfrastructure/ResiliencePipelines.cs` |
| `Rig.TUnit.Security.OAuth.Tests.Integration` | JWKS + RSA key factories | `TestInfrastructure/OAuthTestHarness.cs` |
| `Rig.TUnit.Microservices.Outbox.Tests.Integration` | `OutboxMessage` seed builders + envelope fakers + custom store stubs | `TestInfrastructure/OutboxTestData.cs` |
| every `*QuirkTests.cs` across the test tree | inline test entities + fake handlers + shared fixtures | per-project `TestInfrastructure/` |
| every `*Contract.cs` with inline helper types (C-003 from 004 carry-forward) | helpers | `TestInfrastructure/ContractHelpers/` |

**Explicit do-not (FR-052):**

- 355-line `TraceAssertTests.cs` stays one class — only setup infrastructure moves.
- NO test files split by method-under-test.

**Exit gate (SC-004):**

- `TestFileOrganizationTests` skip list is empty; the rule enforces uniformly across every test project.

---

### Phase 6 — Documentation parity (10–14 days, parallel branch `feat/005-b-docs-parity`)

**Goal:** OSS-ready governance + 14-section canonical README for all 63 src projects + supporting docs + hardened arch-test gate.

**Strict internal order: 6a → 6b → 6c → 6d.** 6d's gate-tightening depends on every 6c README landing first.

#### Phase 6a — Foundation + Markdig gate (1–2 days)

> **Analyze-revision**: the Markdig rewrite (originally drafted as Phase 6d T140) was pulled FORWARD into Phase 6a as **T123b/T123c/T123d** so each Phase 6c family RED commit genuinely fails the tightened gate. Phase 6d becomes residual cleanup only.

**Per-file cadence (FR-060 / FR-061 / FR-062):**

```
T120 (RED)   test(005): T120 — RED governance files present
              arch test asserts LICENSE + CONTRIBUTING.md + SECURITY.md +
              CHANGELOG.md + README.md present at repo root
              fails (only README.md exists today)

T121 (GREEN) feat(005): T121 — GREEN add governance files
              LICENSE (MIT — C-002, attributed to "Faysil Alshareef", year 2026)
              CONTRIBUTING.md (TDD rules + coverage command + skip-forbidden + links)
              SECURITY.md (disclosure email + SLA)
              CHANGELOG.md (001–004 history + KurrentDb breaking rename narrative)
              README.md rewritten against adapted 14-section template
```

```
T122 (RED)   test(005): T122 — RED canonical template present
              arch test asserts docs/templates/PROVIDER_README_TEMPLATE.md and
              docs/QUALITY-BAR.md exist
              fails

T123 (GREEN) feat(005): T123 — GREEN author canonical template
              docs/templates/PROVIDER_README_TEMPLATE.md (14 sections, placeholders)
              docs/QUALITY-BAR.md (reviewer rubric Pass/Needs-work/Missing)
              update src/Rig.TUnit/Contributing-ProviderTemplate.md §8 to reference the template
```

```
T123b (chore) chore(005): T123b — add Markdig pin for README structural parser (C-003)
               Directory.Packages.props adds `<PackageVersion Include="Markdig" Version="0.38.*" />`
               Rig.TUnit.Architecture.Tests.csproj adds `<PackageReference Include="Markdig" />`
               GREEN-only (FR-001 chore exemption); verify `dotnet build` still passes

T123c (RED)  test(005): T123c — RED tighten ReadmeCompletenessTests to Markdig structural gate
               rewrite to parse headings via Markdig, assert all 14 sections present
               (or `## §N — N/A: <rationale>` for base/meta), Options-table reflects
               `*FixtureOptions.cs`, benchmark-link resolves.
               RED because current READMEs (pre-6c) do not satisfy the tightened gate.

T123d (GREEN) feat(005): T123d — GREEN expand skip list for Phase 6c rollout (FR-066)
               expand SkipUntilFixed markers to cover every not-yet-populated provider
               (rescope of existing skip list inside the legitimate rule file — permitted under FR-004)
               Phase 6c family GREEN commits will each trim the skip entries for their family.
```

#### Phase 6b — Supporting docs (1–2 days)

- **T124 (RED) + T125 (GREEN)** — add architecture Mermaid diagram (family graph + 60-provider matrix) as `docs/architecture-diagram.md` and embed from root `README.md`. Link from every leaf README's section 13.
- **T126 (RED) + T127 (GREEN)** — `docs/adr/` with 8 ADRs:
  - ADR-001 Why Testcontainers over Docker Compose as primary
  - ADR-002 Why CRTP `RigBuilder<TSelf>` pattern
  - ADR-003 Why Options pattern with `SectionName` (vs `IConfiguration` injection)
  - ADR-004 Why TUnit / Microsoft.Testing.Platform over xUnit / NUnit / MSTest
  - ADR-005 Why family-level contract tests over per-provider contract files
  - ADR-006 Why `IsolationKey` over static state for parallel safety
  - ADR-007 Why explicit `UseRedisCache` / `UseRedisKv` instead of bare `UseRedis`
  - ADR-008 KurrentDb rename (Feature 004 Phase 1) — breaking change rationale
- **T128 (RED) + T129 (GREEN)** — `docs/glossary.md`
- **T130 (RED) + T131 (GREEN)** — `docs/troubleshooting.md`
- **T132 (RED) + T133 (GREEN)** — `docs/performance-tuning.md`
- **T134 (RED) + T135 (GREEN)** — `docs/migration-001-to-004.md`
- **T136 (GREEN)** — `docs/third-party-notices.md` (GREEN-only — `docs` exemption per FR-001)

#### Phase 6c — Per-project README rewrites (6–9 days, per-family batching)

**Per-family cadence (FR-065) — revised per analyze #2/#3:**

```
T1NN (RED)      test(005): T1NN — RED family {X} READMEs template-only
                 each leaf README rewritten with 14 section headings + placeholder
                 content (## Quick start contains `// TODO: runnable snippet`)
                 now GENUINELY RED because T123c's Markdig gate is live — placeholder
                 Options tables fail reflection match; // TODO Quick starts fail Section 5 content check
T1NN+1 (GREEN)  feat(005): T1NN+1 — GREEN family {X} READMEs populated
                 each placeholder replaced with provider-specific content: Purpose,
                 When NOT to use, Install + version compat matrix, Quick start (runnable),
                 Configuration (Options table via reflection + SectionName + appsettings),
                 API surface (every public type one-liner), Fluent wiring, Provider quirks,
                 Troubleshooting, Testing contracts, Performance (benchmark link + numbers),
                 Dependencies, Spec/versioning/contributing
                 ALSO trims this family's entries from ReadmeCompletenessTests skip list
```

Families to batch, one PR each (10 PRs):

- **T137/T138** — 12 missing READMEs (base / meta packages): `Rig.TUnit`, `Rig.TUnit.All`, `Rig.TUnit.Ci`, `Rig.TUnit.Core`, `Rig.TUnit.Grpc`, `Rig.TUnit.Mediator`, `Rig.TUnit.Microservices` base, `Rig.TUnit.Microservices.Contracts`, `Rig.TUnit.Microservices.Saga`, `Rig.TUnit.Parallelism`, `Rig.TUnit.Storage` base, `Rig.TUnit.WebAPI`.
- **T139/T140** — SQL family (6 READMEs): base + MySql + Oracle + Postgresql + SqlServer + Sqlite — EF-provider compat matrix, AUTO_INCREMENT / PL-SQL / schema quirks, Pomelo EF10 pin narrative.
- **T141/T142** — NoSQL family (8 READMEs): base + Cassandra + Cosmos + Dynamo + ElasticSearch + KurrentDb + Mongo + Redis-as-KV.
- **T143/T144** — Caching family (5): base + Fusion + Hybrid + Memory + Redis.
- **T145/T146** — Messaging family (6): base + Kafka + Nats + RabbitMq + ServiceBus + Sqs.
- **T147/T148** — Microservices family (7): base + Contracts + EventSourcing + Inbox + Outbox + Saga + Snapshots.
- **T149/T150** — Security family (5): base + Jwt + Mtls + OAuth + Policies.
- **T151/T152** — Observability family (7): base + AppInsights + Logging + Logging.Analyzers + Metrics + Seq + Tracing.
- **T153/T154** — Storage family (5): base + AzureBlob + FileSystem + MinIO + S3.
- **T155/T156** — Cross-cutting (~7): `Concurrency`, `Docker`, `HealthChecks`, `Http`, `Resilience`, `Microservices` meta where not covered by T137/T138.

#### Phase 6d — Residual cleanup + link / snippet gates (1 day)

- **T157 (RED)** — guard-test asserting the `ReadmeCompletenessTests` skip list is empty. Fails if any Phase 6c family GREEN forgot to trim its entries. (FR-069)
- **T158 (GREEN)** — trim the last residual skip markers. (FR-066, FR-069)
- **T159 (RED) + T160 (GREEN)** — add `markdown-link-check` CI step (`gaurav-nelson/github-action-markdown-link-check`). Broken links anywhere in `**/*.md` fail the build. (FR-067)
- **T161 (RED) + T162 (GREEN)** — add `snippet-extraction` CI job that extracts every README's `## Quick start` code fence into a throwaway `.cs` file under a scratch test project and runs `dotnet build`. Path filter per C-004: runs only on PRs touching `src/**/*.cs`, `src/**/README.md`, or `docs/templates/PROVIDER_README_TEMPLATE.md`. (FR-068)

**Exit gate (SC-005, SC-008, SC-009, SC-010):**

- `ReadmeCompletenessTests` enforces 14-section structural gate; zero skip markers.
- Every markdown file's links resolve.
- All 63 src READMEs pass.
- Root has `LICENSE + CONTRIBUTING + SECURITY + CHANGELOG + README`.
- `docs/templates/`, `docs/QUALITY-BAR.md`, `docs/adr/` (8 ADRs), `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md` all present.

---

### Phase 7 — CI hardening (1 day, back on `feat/005-a-legacy-coverage-and-tests`)

**Goal:** every rule, coverage, benchmark, and commit-discipline check is enforced on every PR.

1. **T164 (RED) + T165 (GREEN)** — Add dedicated `architecture-tests` CI job: `dotnet test tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj -c Release`. No filter — every skip marker is gone by this point. (FR-070)
2. **T166 (RED) + T167 (GREEN)** — Add `benchmark-regression` CI job: `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks/Rig.TUnit.Benchmarks.csproj -- --artifacts ./benchmark-results`. Parse BenchmarkDotNet JSON (`*-report-full.json`); compare against `benchmarks/baseline-005.json`; fail if any metric regresses > 20 %. Runs on PRs that touch `src/**/*.cs`, `tests/Rig.TUnit.Benchmarks/**`, or `Directory.Packages.props`. (FR-071)
3. **T168 (RED) + T169 (GREEN)** — Harden `commit-discipline-gate` CI job introduced minimally in Phase 1: walk `git log master..HEAD --pretty=format:"%H %s"`; for every commit with subject matching `feat(005): T\d+ — GREEN`, assert the immediately-preceding commit on the same branch matches `test(005): T\d+ — RED` with the same task ID. Scope per revised FR-001: production-affecting paths (`src/**`, `Directory.Packages.props`, `global.json`, `.github/workflows/**`). Allowed prefixes `chore(005)`, `docs(005)`, `ci(005)` bypass for their respective narrow cases. Retroactive SHA exemption: hardcoded `EXEMPT_SHAS=("2b149b2")` and nothing else. (FR-002, FR-072, SC-011)
4. **T170 (RED) + T171 (GREEN)** — Add `red-commit-verification` CI step to `commit-discipline-gate`: for each RED commit identified, `git checkout <sha>` in a worktree and run `dotnet test --filter "Category!=Integration&Category!=Benchmark"` on the projects touched by that commit; assert exit code ≠ 0 (confirming RED genuinely failed). A secretly-green RED fails the PR. (FR-003)
5. **T172 (RED) + T173 (GREEN)** (optional) — Replace `build-unit-arch` pwsh loop with `dotnet test Rig.TUnit.slnx --filter Category!=Integration` IF TUnit / MTP now supports `--filter` on MTP runner (verify during implementation; if unsupported, keep the pwsh loop but port to Bash for cross-platform consistency). (FR-073)
6. **T174 (GREEN only)** — Update `CONTRIBUTING.md` with the full gate set: coverage threshold + contract suite + benchmark regression + commit-discipline + architecture-tests + test-category-completeness + markdown link-checker + snippet-extraction. (FR-074, SC-019) — GREEN-only per revised FR-001 `docs` exemption.

**Exit gate (SC-016, SC-017):**

- CI has `architecture-tests + benchmark-regression + commit-discipline-gate + red-commit-verification + coverage-summary + snippet-extraction + markdown-link-check` jobs, all non-bypassable on PR.
- `benchmarks/baseline-005.json` is the reference.

---

## Verification Matrix — FR → Phase → Task

| FR | Phase | Task(s) | SC |
|---|---|---|---|
| FR-001 Commit cadence | 1–7 | All `T` tasks (non-`A`, non-`chore`/`docs`/`ci` exempted) | SC-011 |
| FR-002 Commit-discipline-gate | 1 / 7 | T008 (minimal) / T168–T169 (hardened) | SC-011, SC-016 |
| FR-003 red-commit-verification | 7 | T170–T171 | SC-011 |
| FR-004 No new skip markers | 4 (enforcement) | T104b/T104c `NoSkipMarkersTests` | SC-012 |
| FR-005 Retire inherited skips | 3/4/5/6c/6d | T021+ / T071+ / T106+ / Phase 6c family GREENs / T158 | SC-002, SC-003, SC-004, SC-005 |
| FR-006 No partial fill-in | 3 | All Phase 3 pairs | SC-002 |
| FR-007 No regressions | all | Implicit CI pass | SC-015 |
| FR-010 Postgres flake fix | 1 | T003–T004 | SC-001 |
| FR-011 Shared-fixture audit | 1 / 3 / 4 | A005 / T066–T067 / T104b-T104c SharedFixtureGuardTests | SC-013 |
| FR-012 Orphan deletion | 1 | T001–T002 | SC-014 |
| FR-013 HTML report upload | 1 | T006–T007 | SC-018 |
| FR-014 10 green runs | 1 | T008 | SC-001 |
| FR-020..FR-021 Coverage collection | 2 | T010–T015 | SC-006, SC-018 |
| FR-022 Threshold block | 2 (collect) / 3 close (flip) | T014–T015 (non-blocking) / **T069b (flip to blocking)** | SC-006 |
| FR-023 Coverage baseline | 2 | T016 | SC-006 |
| FR-024 CONTRIBUTING coverage | 2 / 6a / 7 | T017 (stub) / T121 (full) / T174 (full gate set) | SC-019 |
| FR-030..FR-038 Test-category fill-in | 3 | T020–T069b | SC-002, SC-006, SC-007 |
| FR-040..FR-043 Canonical layout | 4 | T070–T103 | SC-003 |
| FR-050..FR-053 Test hygiene | 5 | T105–T118 | SC-004 |
| FR-060..FR-065 Documentation | 6a–6c | T120–T156 | SC-008, SC-009, SC-010 |
| FR-066 Markdig parser | 6a / 6d | **T123b/T123c/T123d (Phase 6a)** / T157–T158 (residual) | SC-005 |
| FR-067 Markdown link check | 6d | T159–T160 | SC-005 |
| FR-068 Snippet extraction | 6d | T161–T162 | SC-005 |
| FR-069 Empty Readme skip | 6c / 6d | Phase 6c family GREENs (per-family trim) / T157–T158 (residual) | SC-005 |
| FR-070..FR-074 CI hardening | 7 | T164–T174 | SC-016, SC-017, SC-019 |
| FR-075 NoSkipMarkersTests | 4 | T104b/T104c | SC-012 |
| FR-076 SharedFixtureGuardTests | 4 | T104b/T104c | SC-013 |
| FR-077 Markdig rewrite ordering | 6a | T123b/T123c/T123d lands before 6c | SC-005 |

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Coverage gate fails on existing packages once blocking | HIGH | HIGH | Phase 2 publishes non-blocking baseline first; flip to blocking only after every package passes. FR-035 coverage-lifting tests close the gap before enforcement. |
| Phase 3 Unit tests introduce new flakes | MEDIUM | MEDIUM | Every new Integration test uses per-test isolation; no new `Shared*Fixture` occurrences allowed (FR-011 enforced by grep). |
| Benchmark regression gate fires on .NET 10 GC noise | MEDIUM | LOW | 20 % threshold is generous; runs medians of 2 executions (BenchmarkDotNet default); gate-introduction PR runs 3× to smoke stability. |
| `commit-discipline-gate` blocks legitimate bundled work | MEDIUM | MEDIUM | Allow explicit `chore(005):` / `docs(005):` / `ci(005):` subject prefixes (FR-001 exemption) for governance / dependency-pin / CI-config-only changes (e.g., T123b Markdig pin, T174 CONTRIBUTING extension). No `[skip-discipline]` free-form tags. Scope cross-referenced in `/review`. |
| `Markdig` transitive dependency clashes | LOW | LOW | Verify licensing (BSD-2-Clause) + transitive version; it's already pulled by `dotnet-format` toolchain. Add to `Directory.Packages.props` with explicit version pin. |
| Phase 6 scope-creep on glossary / troubleshooting / tuning | MEDIUM | MEDIUM | Phase 6c READMEs reference specific glossary terms; T128/T129's glossary MUST cover every referenced term — rule the PR scope to "stop adding READMEs until glossary entries exist". |
| Retroactive `2b149b2` exemption generalises into "add another exemption" | HIGH | MEDIUM | `commit-discipline-gate` hardcodes ONLY `2b149b2` — any proposed exemption requires a spec amendment, not a config change. |
| Phase 6b `markdown-link-check` flakes on 3P URLs | MEDIUM | LOW | Configure the action with a retry count + explicit allow-list for known flaky domains (kurrent.io, etc.). |
| Parallel 005-a / 005-b merge conflicts | LOW | LOW | File-level independent. `ReadmeCompletenessTests` is the only file both branches touch — 005-a should avoid editing it until 005-b merges 6d. |

---

## Dependencies & Ordering

```
Phase 1 (hotfix branch)  ─────┐
                               ├──► master gate green ──► Phase 2 opens
                               │
Phase 2 (feat/005-a)           │
   ├ T010..T015 coverage plumbing
   ├ T016        baseline capture ─── required for Phase 3 GREEN commits
   └ T017        CONTRIBUTING stub
                                  │
Phase 3 (feat/005-a)              │
   ├ T020..T024  P0 foundation   ─── MUST complete before P1
   ├ T025..T029  P1 utilities
   ├ T030..T034  P1 legacy
   ├ T035..T037  P1 observability
   ├ T038..T042  P1 microservices
   └ T043        shared-fixture conversions (interleaved)
                                      │
Phase 4 (feat/005-a)                  │
   └ T044..T049 per-family canonical-shape PRs
                                         │
Phase 5 (feat/005-a)                     │
   └ T050..T056 per-project TestInfrastructure extraction
                                            │
Phase 6 (feat/005-b, PARALLEL from Phase 2 onward)
   ├ 6a T120..T123     foundation + canonical template
   │   + T123b/c/d    Markdig dep + rewrite + interim skip list (moved fwd per analyze #2/#3)
   ├ 6b T124..T136     supporting docs + ADRs + third-party-notices
   ├ 6c T137..T156     per-family README batches (each GREEN trims its family's skip entries)
   └ 6d T157..T162     residual cleanup + markdown-link-check + snippet-extraction
                                               │
Phase 7 (feat/005-a, after 5)
   └ T164..T174       CI hardening (arch-tests, benchmark-regression,
                      hardened commit-discipline, red-commit-verification,
                      optional pwsh→Bash, full CONTRIBUTING)
                                               │
                                       Feature close — merge 005-a, merge 005-b,
                                       delete feat branches, tag v005
```

---

## Next Steps

1. `/dotnet-ai-kit:tasks` — generate the per-task tasks.md with ordered task IDs, RED/GREEN commit templates, and file lists.
2. `/dotnet-ai-kit:analyze` — cross-check plan vs spec vs tasks for consistency before any code lands.
3. Create feature branches:
   - `fix/005-phase-1-ci-stabilisation` (from `master`)
   - `feat/005-a-legacy-coverage-and-tests` (from `master`, after Phase 1 merges)
   - `feat/005-b-docs-parity` (from `master`, can branch any time after Phase 1)
4. Start Phase 1.

---

## References

- [spec.md](spec.md) — authoritative requirements (53 FR, 19 SC, 4 clarifications resolved)
- [`.dotnet-ai-kit/features/004-provider-consistency-remediation/plan.md`](../004-provider-consistency-remediation/plan.md) — precedent for phased RED→GREEN cadence
- [`.dotnet-ai-kit/features/004-provider-consistency-remediation/review.md`](../004-provider-consistency-remediation/review.md) — post-004 quality baseline (1264 `[Test]` methods)
- [planning/post-004-remediation/*](../../../planning/post-004-remediation/) — research inputs (six documents)
- [.github/workflows/ci.yml](../../../.github/workflows/ci.yml) — current 10-job pipeline
- [Directory.Packages.props](../../../Directory.Packages.props) — central pins (Markdig to be added in T123b `chore` commit)
- [.claude/rules/*.md](../../../.claude/rules/) — project conventions (TDD, async, configuration, testing, tool-calls)
