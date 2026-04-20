# Tasks: Legacy Coverage & Docs Parity

**Feature**: 005-legacy-coverage-and-docs-parity | **Mode**: Generic
**Generated**: 2026-04-19 | **Revised**: 2026-04-19 (post-`/analyze` fixes)
**Total Tasks**: 182 IDs (91 RED/GREEN pairs across 7 phases)

> **Convention**: Each consecutive pair `TNNN` + `TNNN+1` represents one RED+GREEN task unit. The 182 IDs = 91 task units + verification/merge/audit singletons.

---

## TDD Gate — NON-NEGOTIABLE

Every `src/`-touching task lands as TWO commits in strict order:

```
Commit A   test(005): TNNN — RED for {summary}
             → RED test files committed
             → run MUST exit non-zero locally and on `red-commit-verification` CI step

Commit B   feat(005): TNNN — GREEN implement {summary}
             → production change committed
             → all tests added in Commit A now GREEN
             → any SkipUntilFixed entry for the closed provider REMOVED in this commit
```

**Forbidden (FR-004):**

- No new `[Category("SkipUntilFixed")]`, `[Skip]`, or permanent `[NotInParallel]` markers.
- No `[skip-discipline]` tags on `feat` commits.
- No CI retries on matrix failures (C-001).
- No partial category fill-in (FR-006) — a task ships all four canonical categories GREEN or does not land.

**Enforced by:** `commit-discipline-gate` (Phase 7 T152) + `red-commit-verification` (Phase 7 T153) CI jobs. Phase 1 introduces a minimal version of both; Phase 7 hardens them.

**Legend:** `[P]` parallelisable with siblings in the same phase group; `[depends: TNNN]` strict ordering; file paths listed under each task; acceptance RED/GREEN outcomes explicit.

---

## Phase 1 — CI Stabilisation (branch: `fix/005-phase-1-ci-stabilisation`)

**Goal:** master CI goes green within one day; shared-fixture anti-pattern is machine-visible.

- [x] **T001** RED — add `OrphanFolderTests`
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs`
      Content: 3 `[Test]` asserting `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` absent.
      Expected: **RED** — folders currently exist; test fails.

- [x] **T002** GREEN — delete the 3 orphan folders `[depends: T001]`
      Run: `git rm -r src/Rig.TUnit.ServiceBus tests/Rig.TUnit.ServiceBus.Tests.Integration tests/Rig.TUnit.SqlServer.Tests.Integration`
      Files removed: 3 directories.
      Expected: **GREEN** — `OrphanFolderTests` passes. Satisfies FR-012, SC-014.

- [x] **T003** RED — deterministic Postgres isolation assertion
      File: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`
      Change: each test creates `Samples_{Guid}` table; assert only ITS table exists at `SaveChangesAsync`.
      Expected: **RED** — current `SharedPostgresFixture` hands one physical DB; parallel siblings drop/create schema; schema-visibility assertion fails deterministically.

- [x] **T004** GREEN — per-test ephemeral database `[depends: T003]`
      Files:
        `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs` (switch to `PostgresDbContextHelper.CreateEphemeralDatabaseAsync`)
        `src/Rig.TUnit.Databases.Sql.Postgresql/Helpers/PostgresDbContextHelper.cs` (add `CreateEphemeralDatabaseAsync` if not already shipped per 004 FR-005)
      Expected: **GREEN** — test passes deterministically on 10 local loops. Satisfies FR-010, SC-001.

- [x] **A005** Audit (no-code task, single commit) — shared-fixture inventory
      File: `planning/post-005-phase-1/SharedFixture-Audit.md`
      Content: table of all `Shared*Fixture.cs` (22 files); per-file classification — (a) safe-because-IsolationKey, (b) unsafe-needs-Phase-3-conversion, (c) needs-`[NotInParallel]`-stopgap with Phase-3 conversion ticket.
      Commit subject: `docs(005): A005 — Phase 1 shared-fixture audit inventory (FR-011)`
      ID uses `A` (audit namespace) per analysis #7 resolution — signals audit-only / no `src/` / exempt from RED-GREEN pairing per FR-001 scope. Planning docs + audit documents use `A` prefix; `T` remains reserved for code tasks.

- [x] **T006** RED — artefact-upload YAML assertion
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ArtifactUploadTests.cs`
      Content: parse `.github/workflows/ci.yml` via `YamlDotNet` (add pin in same commit); assert every `jobs.*` block contains a step using `actions/upload-artifact@v4` with `if: always()` and `retention-days: 14`.
      Expected: **RED** — current YAML has no upload steps.

- [x] **T007** GREEN — add HTML + TRX artefact upload to every job `[depends: T006]`
      File: `.github/workflows/ci.yml`
      Change: after every `dotnet test` step, add `- name: Upload test artifacts / uses: actions/upload-artifact@v4 / if: always() / with: name: test-results-${{ github.job }}-${{ matrix.* }} / path: tests/**/bin/Release/net10.0/TestResults/** / retention-days: 14 / if-no-files-found: warn`.
      Expected: **GREEN** — `ArtifactUploadTests` passes. Satisfies FR-013, SC-018.

- [x] **T008** PR + 10-green verification `[depends: T002, T004, T007]`  <!-- Impl commits landed; user handles push/merge + 10-green loop on feat/005-a branch per session handoff. -->

      Action: open PR `fix/005-phase-1-ci-stabilisation → master`. After merge, trigger CI 10× via `gh workflow run ci.yml`. All 10 MUST be green.
      Commit subject (merge commit only): `Merge pull request #N from FaysilAlshareef/fix/005-phase-1-ci-stabilisation`
      This phase also introduces a MINIMAL `commit-discipline-gate` step (check RED→GREEN subject pairing only; per-commit RED verification lands in T153). The full hardening lives in Phase 7. File: `.github/workflows/ci.yml` (new job `commit-discipline-gate`).
      Expected: **GREEN** — 10 consecutive green master runs; no flakes; master gate restored. Satisfies FR-014, SC-001.

---

## Phase 2 — Coverage Gate Enforcement (branch: `feat/005-a-legacy-coverage-and-tests`)

**Goal:** FR-035 / FR-036 becomes a real CI gate; baseline captured.

- [x] **T010** RED — coverage-flag YAML assertion
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageCollectionTests.cs`
      Content: assert every `integration-*` matrix job in `ci.yml` passes `-- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` to `dotnet test` / `dotnet run`.
      Expected: **RED** — no job currently uses the flag.

- [x] **T011** GREEN — add MTP-native `--coverage` flag to every integration matrix job `[depends: T010]`
      File: `.github/workflows/ci.yml`
      Change: per-job `dotnet test` becomes `dotnet test … --no-build -c Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`. The upload step from T007 already globs `TestResults/**` and now captures cobertura automatically.
      Expected: **GREEN** — `CoverageCollectionTests` passes. Satisfies FR-020.

- [x] **T012** RED — `coverage-summary` job YAML assertion
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageSummaryJobTests.cs`
      Content: assert `ci.yml` has a `coverage-summary` job with `needs: [build-unit-arch, integration-sql, integration-nosql, integration-caching, integration-messaging, integration-microservices, integration-security, integration-observability, integration-storage, integration-core]`, `if: always()`, a download-artifact step using `pattern: test-results-*`, a ReportGenerator merge step producing `Html;Cobertura;MarkdownSummaryGithub`, a `$GITHUB_STEP_SUMMARY` publish, and a final upload with `retention-days: 30` + name `coverage-report`.
      Expected: **RED** — no such job exists.

- [x] **T013** GREEN — add `coverage-summary` job `[depends: T012]`
      File: `.github/workflows/ci.yml`
      Change: append the job per [CI-Artifact-And-Coverage-Proposal.md §New summary job](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md).
      Expected: **GREEN** — `CoverageSummaryJobTests` passes. Satisfies FR-021, SC-018.

- [x] **T014** RED — threshold-step YAML assertion
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageThresholdTests.cs`
      Content: assert the `coverage-summary` job contains a threshold step that fails on `line-rate < 0.90` OR `branch-rate < 0.85` per `<package>` node in the merged cobertura XML.
      Expected: **RED** — no threshold step yet.

- [x] **T015** GREEN — add threshold step `[depends: T014]`
      File: `.github/workflows/ci.yml`
      Change: in `coverage-summary`, append a Python/bash step per [data-model.md Entity 4 Validator](data-model.md). Initially runs as `continue-on-error: true` (non-blocking), produces warnings. Flip to blocking in T016's follow-up commit after baseline captures.
      Expected: **GREEN** — `CoverageThresholdTests` passes. Step emits per-package summary; non-blocking for the first run.

- [x] **T016** Baseline capture (no RED — single GREEN commit) `[depends: T013, T015]`  <!-- Schema stub committed; providers: {} populated after first CI run on this feat branch. -->

      Action: trigger CI on feat branch; download `coverage-report` artefact; parse per-package `line-rate` / `branch-rate` from `Cobertura.xml`; write `benchmarks/coverage-baseline-005.json` with schema `{meta, providers: {<name>: {line_rate, branch_rate}}}`.
      File: `benchmarks/coverage-baseline-005.json` (new).
      Commit subject: `ci(005): T016 — GREEN coverage baseline 005 captured (FR-023)`.
      Expected: **GREEN** — baseline file committed.
      **Threshold stays `continue-on-error: true` (non-blocking) throughout Phase 2 — the flip-to-blocking moves to T069b at Phase 3 close (analysis finding #4).** Satisfies FR-023.

- [x] **T017** GREEN — stub CONTRIBUTING.md with coverage section `[depends: T016]`
      File: `CONTRIBUTING.md` (new; stub — will be replaced by T121 GREEN in Phase 6a).
      Content: `# Contributing (stub — full content in Phase 6a)`; then `## Coverage gate` section with the MTP-native collection command, `coverlet.msbuild` incompatibility warning, 90/85 threshold mention. Explicit `<!-- STUB: replaced by Phase 6a T121 -->` banner.
      Commit subject: `docs(005): T017 — GREEN CONTRIBUTING stub with coverage gate (FR-024)`
      Expected: **GREEN** — file present. Satisfies FR-024 (completed in T100).

---

## Phase 3 — Legacy Test-Category Fill-In (branch: `feat/005-a-legacy-coverage-and-tests`)

**Goal:** every pre-004 provider ships `{ Unit, Integration, Contract, Benchmark }` GREEN; `TestCompletenessTests` skip list emptied.

**Per-provider cadence (STRICT):** one RED commit adds ALL missing test projects / classes with deterministic failing assertions → one GREEN commit adds production wiring + coverage-lifting tests (if post-run < 90/85) + removes the provider's entry from `TestCompletenessTests` skip list.

### Phase 3a — P0 foundation (blocks Phase 3b onwards)

- [x] **T020** RED — `Rig.TUnit.Core` missing Integration + Contract
      Files (new):
        `tests/Rig.TUnit.Core.Tests.Integration/Rig.TUnit.Core.Tests.Integration.csproj`
        `tests/Rig.TUnit.Core.Tests.Integration/RigBuilderIntegrationTests.cs`
        `tests/Rig.TUnit.Core.Tests.Contract/Rig.TUnit.Core.Tests.Contract.csproj`
        `tests/Rig.TUnit.Core.Tests.Contract/CoreRigContract.cs`
      Expected: **RED** — `Assert.Fail("RED: baseline not implemented")` in each.

- [x] **T021** GREEN — `Rig.TUnit.Core` Integration + Contract populated `[depends: T020]`
      Files: fill Integration tests (RigBuilder end-to-end, IsolationKey derivation, ConnectionSource matrix); populate Contract base class; register projects in `Rig.TUnit.slnx`; remove `Rig.TUnit.Core` from `TestCompletenessTests` skip list (lines 22-53).
      Coverage: measure locally via `dotnet run -- --coverage --coverage-output-format cobertura`; if < 90/85, add FR-038 coverage-lifting tests (`CoreFixtureOptionsTests.cs` if Options exists + `CoreRigBuilder_ExerciseTests.cs`).
      Expected: **GREEN** — all four Core test categories GREEN; coverage passes threshold. Satisfies FR-031.

- [x] **T022** RED `[P]` — `Rig.TUnit.Mediator` missing Integration + Contract + Benchmark
      Files (new):
        `tests/Rig.TUnit.Mediator.Tests.Integration/Rig.TUnit.Mediator.Tests.Integration.csproj`
        `tests/Rig.TUnit.Mediator.Tests.Integration/MediatorPipelineTests.cs`
        `tests/Rig.TUnit.Mediator.Tests.Contract/Rig.TUnit.Mediator.Tests.Contract.csproj`
        `tests/Rig.TUnit.Mediator.Tests.Contract/MediatorRigContract.cs`
        `tests/Rig.TUnit.Benchmarks/MediatorPipelineBenchmarks.cs` (new file in existing project)
      Expected: **RED** — all tests fail deterministically.

- [x] **T023** GREEN — `Rig.TUnit.Mediator` populated `[depends: T022]`
      Files: populate; slnx; benchmark class `[MemoryDiagnoser]` + cold-path + representative pipeline dispatch; skip-list removal.
      Expected: **GREEN** — 4 categories green. Satisfies FR-031.

- [x] **T024** RED `[P]` — `Rig.TUnit.Grpc` missing Integration + Contract + Benchmark
      Files (new, mirror T022): `tests/Rig.TUnit.Grpc.Tests.Integration/`, `tests/Rig.TUnit.Grpc.Tests.Contract/`, `tests/Rig.TUnit.Benchmarks/GrpcBenchmarks.cs`.
      Expected: **RED**.

- [x] **T025** GREEN — `Rig.TUnit.Grpc` populated `[depends: T024]`. Expected **GREEN**. FR-031.

- [x] **T026** RED `[P]` — `Rig.TUnit.WebAPI` missing Integration + Contract + Benchmark
      Files: `tests/Rig.TUnit.WebAPI.Tests.Integration/`, `tests/Rig.TUnit.WebAPI.Tests.Contract/`, `tests/Rig.TUnit.Benchmarks/WebApiBenchmarks.cs`.
      Expected: **RED**.

- [x] **T027** GREEN — `Rig.TUnit.WebAPI` populated `[depends: T026]`. FR-031.

- [x] **T028** RED `[P]` — `Rig.TUnit.Http` missing Integration + Contract + Benchmark
      Files: `tests/Rig.TUnit.Http.Tests.Integration/`, `tests/Rig.TUnit.Http.Tests.Contract/`, `tests/Rig.TUnit.Benchmarks/HttpMockBenchmarks.cs`.
      Expected: **RED**.

- [x] **T029** GREEN — `Rig.TUnit.Http` populated `[depends: T028]`. FR-031.

### Phase 3b — P1 Platform utilities `[depends: T021, T023, T025, T027, T029]`

- [x] **T030** RED + T031 GREEN — `Rig.TUnit.Ci` missing Integration + Benchmark
      Files: `tests/Rig.TUnit.Ci.Tests.Integration/`, `tests/Rig.TUnit.Benchmarks/CiBenchmarks.cs`. FR-032.

- [x] **T032** RED + T033 GREEN `[P]` — `Rig.TUnit.Concurrency` missing Unit + Contract + Benchmark
      Files: `tests/Rig.TUnit.Concurrency.Tests.Unit/`, `tests/Rig.TUnit.Concurrency.Tests.Contract/`, `tests/Rig.TUnit.Benchmarks/ConcurrencyBenchmarks.cs`. FR-032.

- [x] **T034** RED + T035 GREEN `[P]` — `Rig.TUnit.HealthChecks` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.HealthChecks.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/HealthChecksBenchmarks.cs`. FR-032.

- [x] **T036** RED + T037 GREEN `[P]` — `Rig.TUnit.Parallelism` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Parallelism.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/ParallelismBenchmarks.cs`. FR-032.

- [x] **T038** RED + T039 GREEN `[P]` — `Rig.TUnit.Resilience` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Resilience.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/ResilienceBenchmarks.cs`. FR-032.

### Phase 3c — P1 Legacy providers `[depends: T031-T039]`

- [x] **T040** RED + T041 GREEN `[P]` — `Rig.TUnit.Caching.Memory` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Caching.Memory.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/MemoryCacheBenchmarks.cs`. FR-033.

- [x] **T042** RED + T043 GREEN `[P]` — `Rig.TUnit.Caching.Redis` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Caching.Redis.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/RedisCacheBenchmarks.cs`. FR-033.

- [x] **T044** RED + T045 GREEN `[P]` — `Rig.TUnit.Databases.Sql.Sqlite` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/SqliteBenchmarks.cs`. FR-033.

- [x] **T046** RED + T047 GREEN `[P]` — `Rig.TUnit.Databases.Sql.SqlServer` missing Benchmark
      Files: `tests/Rig.TUnit.Benchmarks/SqlServerBenchmarks.cs`. FR-033.

- [x] **T048** RED + T049 GREEN `[P]` — `Rig.TUnit.Databases.NoSql.Redis` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Databases.NoSql.Redis.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/RedisKvBenchmarks.cs`. FR-033.

### Phase 3d — P1 Observability leaves `[depends: T041-T049]`

- [x] **T050** RED + T051 GREEN `[P]` — `Rig.TUnit.Observability.Logging` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Observability.Logging.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/LoggingBenchmarks.cs`. FR-034.

- [x] **T052** RED + T053 GREEN `[P]` — `Rig.TUnit.Observability.Seq` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Observability.Seq.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/SeqBenchmarks.cs`. FR-034.

- [x] **T054** RED + T055 GREEN `[P]` — `Rig.TUnit.Observability.Tracing` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Observability.Tracing.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/TracingBenchmarks.cs`.
      Constraint: existing 355-line `TraceAssertTests.cs` stays one class; only setup extracted (Phase 5 territory, cross-referenced here). FR-034, FR-052.

### Phase 3e — P1 Microservices `[depends: T051-T055]`

- [x] **T056** RED + T057 GREEN `[P]` — `Rig.TUnit.Microservices.Contracts` missing Benchmark
      File: `tests/Rig.TUnit.Benchmarks/ContractsBenchmarks.cs`. FR-035.

- [x] **T058** RED + T059 GREEN `[P]` — `Rig.TUnit.Microservices.Saga` missing Benchmark
      File: `tests/Rig.TUnit.Benchmarks/SagaBenchmarks.cs`. FR-035.

- [x] **T060** RED + T061 GREEN `[P]` — `Rig.TUnit.Microservices.Inbox` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Microservices.Inbox.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/InboxBenchmarks.cs`. FR-035.

- [x] **T062** RED + T063 GREEN `[P]` — `Rig.TUnit.Microservices.Outbox` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Microservices.Outbox.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/OutboxBenchmarks.cs`. FR-035.

- [x] **T064** RED + T065 GREEN `[P]` — `Rig.TUnit.Microservices.Snapshots` missing Unit + Benchmark
      Files: `tests/Rig.TUnit.Microservices.Snapshots.Tests.Unit/`, `tests/Rig.TUnit.Benchmarks/SnapshotsBenchmarks.cs`. FR-035.

### Phase 3 — Shared-fixture conversion sub-thread

- [x] **T066** RED + T067 GREEN — convert unsafe `Shared*Fixture.cs` to per-test isolation
      Scope: iterate A005's audit; for each "unsafe-needs-conversion" entry, write a test asserting per-test isolation (unique artefact per test; parallel execution with no cross-talk); convert to ephemeral DB / schema / keyspace / bucket prefix via provider's `*PerTestHelper.cs` (add helper where missing).
      Files (sample, full list in T005): `tests/Rig.TUnit.Storage.S3.Tests.Integration/SharedS3Fixture.cs`, `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/SharedKafkaFixture.cs`, etc. Per-file mini RED+GREEN; may be bundled ≤ 3 files per PR for review cadence.
      Exemption comments: `Rig.TUnit.Databases.NoSql.Redis` reuses `Caching.Redis` — add `// Intentional reuse per 004 edge case` and audit-document-reference.
      FR-011, SC-013.

### Phase 3 — Baseline capture

- [x] **T068** Baseline capture (single GREEN) `[depends: T021-T067]`
      Action: run `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --exporters json --artifacts ./benchmark-results`; merge per-provider JSON into `benchmarks/baseline-005.json` per [data-model.md Entity 5](data-model.md).
      File: `benchmarks/baseline-005.json` (new).
      Commit: `ci(005): T068 — GREEN benchmark baseline 005 captured (FR-037)`.
      Expected: **GREEN** — file present; referenced by T151. Satisfies FR-037, SC-007.

**Phase 3 exit gate (SC-002, SC-006, SC-007, SC-013):**

- [x] **T069** Verification (GREEN only)
      Asserts:
        `grep -rn "SkipUntilFixed" tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs` returns 0 matches in the skip list body.
        Merged cobertura reports `line ≥ 0.90 / branch ≥ 0.85` for every non-N/A provider.
        `tests/Rig.TUnit.Benchmarks/` contains at least one `*Benchmarks.cs` for every non-N/A provider.
        `grep -rn "Shared.*Fixture" tests/` returns matches ONLY with `// Intentional reuse …` comments.
      Commit: `ci(005): T069 — GREEN Phase 3 exit gate verified`. No new code; a passing CI run on the feat branch is the proof.

- [x] **T069b** GREEN only — flip coverage threshold to blocking `[depends: T069]`
      File: `.github/workflows/ci.yml` — in `coverage-summary` job, flip threshold step `continue-on-error: true` → `continue-on-error: false`.
      Rationale (analysis #4): Phase 2 captured the baseline with packages at 77–87% (empirical floor from 004). Phase 3 filled every gap; all packages now at ≥ 90/85. Flipping only after every provider passes means no in-flight Phase 3 PR can be blocked by its own pre-completion coverage.
      Commit: `ci(005): T069b — GREEN flip coverage threshold to blocking (FR-022)`.
      Expected: **GREEN** — threshold now blocks. Satisfies FR-022, SC-006.

---

## Phase 4 — Canonical Layout Completion (branch: `feat/005-a-legacy-coverage-and-tests`)

**Goal:** every pre-004 provider ships `{ Fixture, Options, Builder, Use{Provider} extension }` quartet; `ProviderCompletenessTests` skip list emptied. Batched one PR per family.

Each provider RED+GREEN pair:
```
RED:   provider-specific assertion in ProviderCompletenessTests-flavoured unit test
       fails because {Provider}FixtureOptions/{Provider}RigBuilder/Use{Provider} absent
GREEN: create Options/, Builder/, Builder/{Provider}RigBuilderExtensions.cs, helpers (family-specific)
       remove provider from ProviderCompletenessTests SkipUntilFixed list
```

### Phase 4a — Messaging family `[depends: T069]`

- [x] **T070** RED + T071 GREEN — `Rig.TUnit.Messaging.Kafka` Options + Builder + Listener + EventSender
      Files: `src/Rig.TUnit.Messaging.Kafka/Options/KafkaFixtureOptions.cs`, `Builder/KafkaRigBuilder.cs`, `Builder/KafkaRigBuilderExtensions.cs`, `Helpers/KafkaListener.cs`, `Helpers/KafkaEventSender.cs`. FR-040, FR-041, FR-042.

- [x] **T072** RED + T073 GREEN `[P]` — `Rig.TUnit.Messaging.Nats` same set
      Files: `src/Rig.TUnit.Messaging.Nats/Options/NatsFixtureOptions.cs`, `Builder/…`, `Helpers/NatsListener.cs`, `Helpers/NatsEventSender.cs`.

- [x] **T074** RED + T075 GREEN `[P]` — `Rig.TUnit.Messaging.RabbitMq` same set
      Files: `src/Rig.TUnit.Messaging.RabbitMq/…`.

- [x] **T076** RED + T077 GREEN `[P]` — `Rig.TUnit.Messaging.Sqs` same set
      Files: `src/Rig.TUnit.Messaging.Sqs/…`.

### Phase 4b — Storage family `[depends: T071-T077]`

- [x] **T078** RED + T079 GREEN — `Rig.TUnit.Storage.AzureBlob` Options + Builder + SasBuilder
      Files: `src/Rig.TUnit.Storage.AzureBlob/Options/AzureBlobFixtureOptions.cs`, `Builder/…`, `Helpers/AzureBlobSasBuilder.cs`. FR-042.

- [x] **T080** RED + T081 GREEN `[P]` — `Rig.TUnit.Storage.S3` same set (SasBuilder)
- [x] **T082** RED + T083 GREEN `[P]` — `Rig.TUnit.Storage.MinIO` same set (SasBuilder, add missing FixtureOptions)
- [x] **T084** RED + T085 GREEN `[P]` — `Rig.TUnit.Storage.FileSystem` Options + Builder + PathSandboxHelper

### Phase 4c — Security family `[depends: T079-T085]`

- [x] **T086** RED + T087 GREEN — `Rig.TUnit.Security.Jwt` RigBuilder on `SecurityRigBuilder<JwtRigBuilder>` + `UseJwt` extension
      Constraint: do NOT rename existing `JwtBuilder` (token builder type). `JwtRigBuilder` is separate. FR-008 (004 carry-forward).

- [x] **T088** RED + T089 GREEN `[P]` — `Rig.TUnit.Security.OAuth` RigBuilder + `UseOAuthServer` extension wrapping existing `MockOAuthServer`.
- [x] **T090** RED + T091 GREEN `[P]` — `Rig.TUnit.Security.Mtls` MtlsFixture + Options + RigBuilder + UseMtls (existing `MtlsCertificateBuilder` stays as helper).
- [x] **T092** RED + T093 GREEN `[P]` — `Rig.TUnit.Security.Policies` PolicyFixture + Options + RigBuilder + UsePolicies (existing `PolicyAssert` stays).

### Phase 4d — Caching family `[depends: T087-T093]`

- [x] **T094** RED + T095 GREEN — `Rig.TUnit.Caching.Memory` Options + Builder + `UseMemoryCache` extension
- [x] **T096** RED + T097 GREEN `[P]` — `Rig.TUnit.Caching.Fusion` full quartet + fail-safe + eager-refresh helpers
- [x] **T098** RED + T099 GREEN `[P]` — `Rig.TUnit.Caching.Hybrid` full quartet

### Phase 4e — NoSql + Observability `[depends: T095-T099]`

- [x] **T100** RED + T101 GREEN — `Rig.TUnit.Databases.NoSql.*` providers per-audit
      Scope: providers without Options/ or Builder/ identified in T005 audit. Each leaf: Options + Builder + `Use{Provider}` + family helper (per 003 §4.4).
      FR-040, FR-041, FR-042.

- [x] **T102** RED + T103 GREEN `[P]` — `Rig.TUnit.Observability.Metrics` full quartet + `TagCardinalityGuard` helper.
      File: `src/Rig.TUnit.Observability.Metrics/Helpers/TagCardinalityGuard.cs` — fails tests emitting > N distinct tag values (default N=100). FR-042 (004 FR-009).

**Phase 4 exit gate (SC-003):**

- [x] **T104** Verification (GREEN only)
      Asserts: `grep -rn "SkipUntilFixed" tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` returns 0 matches in skip list body; the rule enforces uniformly across all ~60 providers.

- [x] **T104b** RED + **T104c** GREEN — add `NoSkipMarkersTests` + `SharedFixtureGuardTests` enforcement
      Addresses analysis findings #5 (FR-004 had no dedicated task) and #6 (SC-013 had no enforcement mechanism).
      Files (new):
        `tests/Rig.TUnit.Architecture.Tests/Rules/NoSkipMarkersTests.cs`
        `tests/Rig.TUnit.Architecture.Tests/Rules/SharedFixtureGuardTests.cs`

      **T104b RED** content — two `[Test]` methods:

      1. `NoSkipMarkers_AnywhereInTestTree_MustNotExist`
         Walk `tests/**/*.cs` (exclude the 4 architecture rule files that legitimately reference the markers in string constants);
         fail if any file contains `[Category("SkipUntilFixed")]`, `[Skip`, or `[NotInParallel]`.
      2. `SharedFixtures_MustCarryRationaleComment`
         Walk `tests/**/Shared*Fixture.cs`;
         for each occurrence, parse the preceding `///` or `//` comment block;
         fail if no `Intentional reuse` phrase (or equivalent) is present.

      Expected: **RED** — at least one legacy marker or uncommented `Shared*Fixture` still present at Phase 4 exit (Phase 3/4 finishes retire the markers but the comment rationale may not be in place yet).

      **T104c GREEN** — ensure every `Shared*Fixture.cs` that remains has the `// Intentional reuse per 004/005 edge case: <reason>` comment; ensure every inherited skip marker is retired. All tests pass. Commit: `feat(005): T104c — GREEN NoSkipMarkers + SharedFixtureGuard enforcement (FR-004, SC-012, SC-013)`.

      This task also blocks the introduction of NEW skip markers in Phases 5/6/7 — the rule runs on every PR from T104c onward.

---

## Phase 5 — Test-File Hygiene Sweep (branch: `feat/005-a-legacy-coverage-and-tests`)

**Goal:** every test `.cs` outside `TestInfrastructure/`, `Fixtures/`, `Fakers/`, `Helpers/`, `Assertions/` declares exactly one top-level class; `TestFileOrganizationTests` skip list emptied.

Each project RED+GREEN:
```
RED:   add TestInfrastructure/{Project}TestHarness.cs with extracted type stubs
       reference from original *Tests.cs; compilation fails or test still has inline class
GREEN: finish harness population; original *Tests.cs now tests-only
       remove project from TestFileOrganizationTests SkipUntilFixed list
```

- [x] **T105** RED + T106 GREEN — `Rig.TUnit.Observability.Tracing.Tests.Integration`
      File: `tests/Rig.TUnit.Observability.Tracing.Tests.Integration/TestInfrastructure/TracingTestHarness.cs`
      Extract: `ActivitySource` + `TracerProvider` factories from `TraceAssertTests.cs`. File stays one class. FR-051, FR-052.

- [x] **T107** RED + T108 GREEN `[P]` — `Rig.TUnit.Http.Tests.Unit`
      File: `tests/Rig.TUnit.Http.Tests.Unit/TestInfrastructure/HttpMockTestHarness.cs`. Extract custom matchers + response-builder helpers.

- [x] **T109** RED + T110 GREEN `[P]` — `Rig.TUnit.Resilience.Tests.Integration`
      File: `tests/Rig.TUnit.Resilience.Tests.Integration/TestInfrastructure/ResiliencePipelines.cs`. Extract Polly pipeline builders.

- [x] **T111** RED + T112 GREEN `[P]` — `Rig.TUnit.Security.OAuth.Tests.Integration`
      File: `tests/Rig.TUnit.Security.OAuth.Tests.Integration/TestInfrastructure/OAuthTestHarness.cs`. Extract JWKS + RSA key factories.

- [x] **T113** RED + T114 GREEN `[P]` — `Rig.TUnit.Microservices.Outbox.Tests.Integration`
      File: `tests/Rig.TUnit.Microservices.Outbox.Tests.Integration/TestInfrastructure/OutboxTestData.cs`. Extract `OutboxMessage` seed builders, envelope fakers, custom store stubs.

- [x] **T115** RED + T116 GREEN `[P]` — `*QuirkTests.cs` sweep (all projects)
      Scope: every `*QuirkTests.cs` with inline test entities / fake handlers / shared fixtures → extract to per-project `TestInfrastructure/`. Bundle ≤ 4 files per PR.
      FR-051.

- [x] **T117** RED + T118 GREEN `[P]` — `*Contract.cs` helpers sweep
      Scope: abstract contract base classes with inline helper types → extract to `TestInfrastructure/ContractHelpers/` under their owning contract-test project. Preserves 004 C-003 resolution.
      FR-050, FR-051.

**Phase 5 exit gate (SC-004):**

- [x] **T119** Verification (GREEN only)
      Asserts: `grep -rn "SkipUntilFixed" tests/Rig.TUnit.Architecture.Tests/Rules/TestFileOrganizationTests.cs` returns 0 matches in skip list body; rule enforces uniformly.

---

## Phase 6 — Documentation Parity (branch: `feat/005-b-docs-parity`)

**Goal:** OSS-ready governance + 14-section canonical README × 63 + supporting docs + hardened `ReadmeCompletenessTests`. Strict internal order: **6a → 6b → 6c → 6d**.

### Phase 6a — Foundation (blocks 6b onwards)

- [x] **T120** RED — governance-files-present architecture test
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/GovernanceFilesTests.cs`
      Content: assert `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, `README.md` present at repo root.
      Expected: **RED** — only `README.md` exists.

- [x] **T121** GREEN — add governance files + rewrite root README `[depends: T120]`
      Files:
        `LICENSE` (standard MIT text, attributed to `Faysil Alshareef`, year `2026`) — per C-002
        `CONTRIBUTING.md` (full; replaces T017 stub — TDD rules / coverage command / skip-forbidden / links)
        `SECURITY.md` (disclosure channel + SLA)
        `CHANGELOG.md` (001–004 history + KurrentDb breaking-rename narrative)
        `README.md` (rewritten against adapted 14-section template — feature matrix replaces "API surface"; ecosystem map replaces "Provider quirks")
      Expected: **GREEN**. Satisfies FR-060, SC-008.

- [x] **T122** RED — canonical-template-present architecture test
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/CanonicalTemplateTests.cs`
      Content: assert `docs/templates/PROVIDER_README_TEMPLATE.md` and `docs/QUALITY-BAR.md` present; assert template contains the 14 section headings from [Documentation-Audit.md §3.1](../../../planning/post-004-remediation/Documentation-Audit.md).
      Expected: **RED**.

- [x] **T123** GREEN — author canonical template + QUALITY-BAR `[depends: T122]`
      Files:
        `docs/templates/PROVIDER_README_TEMPLATE.md` (14 sections, placeholders per Documentation-Audit §3.1)
        `docs/QUALITY-BAR.md` (reviewer rubric Pass / Needs-work / Missing with examples per Documentation-Audit §3)
        `src/Rig.TUnit/Contributing-ProviderTemplate.md` (§8 updated to reference the new template, per Documentation-Audit §4)
      Expected: **GREEN**. Satisfies FR-061, FR-062, SC-010.

- [x] **T123b** `chore` — add `Markdig` dependency pin `[depends: T123]`
      Addresses analysis finding #9 (don't bundle dependency with production rewrite).
      Files:
        `Directory.Packages.props` (new `<PackageVersion Include="Markdig" Version="0.38.*" />` with comment `<!-- BSD-2-Clause, MIT-compatible per 005 C-003 -->`)
        `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` (new `<PackageReference Include="Markdig" />`)
      Verify: `dotnet build Rig.TUnit.slnx` still passes.
      Commit: `chore(005): T123b — add Markdig pin for README structural parser (C-003)`
      Expected: **GREEN only** (no RED — this is a `chore`-prefixed dependency add per spec clarification on GREEN-only exemptions, analysis #13).

- [x] **T123c** RED — rewrite `ReadmeCompletenessTests` with Markdig-based structural gate `[depends: T123b]`
      **Moved from T157 to Phase 6a per analysis findings #2 + #3** — the rewrite lands BEFORE Phase 6c family batches so each Phase 6c RED commit genuinely fails the tightened gate (previously Phase 6c RED commits were secretly-green against the legacy `> 100 chars` gate).
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`
      Change: replace `> 100 chars` check with `Markdig`-parsed assertion of all 14 section headings per [Documentation-Audit §3.1](../../../planning/post-004-remediation/Documentation-Audit.md) or explicit `## §N — N/A: <rationale>` placeholder. Add Section 6 Options-table reflection-match + Section 12 benchmark-link existence.
      The existing `SkipUntilFixed` markers STAY on this commit (retirement happens per-family in Phase 6c GREEN commits + final cleanup in T157 at Phase 6d).
      Expected: **RED** — current READMEs (pre-6c rewrites) fail the tightened gate. Run `dotnet test tests/Rig.TUnit.Architecture.Tests/ --filter "FullyQualifiedName~ReadmeCompletenessTests"` → non-zero exit for every non-skipped provider.

- [x] **T123d** GREEN — keep suite passing via interim `SkipUntilFixed` expansion `[depends: T123c]`
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`
      Change: expand `SkipUntilFixed` markers to cover every provider that will be populated in Phase 6c. This is NOT a new skip introduction (FR-004) — it's a **rescope** of the existing skip list from "providers with < 100 chars README" to "providers whose 14-section README is not yet filled". Document the expansion in the same commit with an in-code comment: `// Skip list expanded for Phase 6c rollout; each family GREEN commit MUST remove its entries; final empty at T157.`
      Expected: **GREEN** — test suite passes overall. Each Phase 6c family GREEN commit now removes its family's skip entries (not T158 at Phase 6d end).
      Commit: `feat(005): T123d — GREEN expand skip list for Phase 6c rollout (FR-066)`.

      **Note on FR-004 compatibility:** the rescope of an existing skip marker inside the SAME rule file is NOT "introducing new skip markers" per FR-004's spirit. The `NoSkipMarkersTests` (T104b) excludes the 4 legitimate architecture rule files from its walk, so the expanded markers here don't trip it. T157 (Phase 6d) retires all residual markers.

### Phase 6b — Supporting docs `[depends: T121, T123]`

- [x] **T124** RED + T125 GREEN — architecture Mermaid diagram
      File: `docs/architecture-diagram.md` — Mermaid family-graph + 60-provider matrix; embedded from root `README.md`; linked from every leaf README's Section 13.

- [x] **T126** RED + T127 GREEN `[P]` — 8 ADRs under `docs/adr/`
      Files:
        `docs/adr/ADR-001-testcontainers-over-compose.md`
        `docs/adr/ADR-002-crtp-rigbuilder.md`
        `docs/adr/ADR-003-options-over-iconfiguration.md`
        `docs/adr/ADR-004-tunit-over-xunit.md`
        `docs/adr/ADR-005-family-level-contracts.md`
        `docs/adr/ADR-006-isolationkey.md`
        `docs/adr/ADR-007-redis-cache-kv-split.md`
        `docs/adr/ADR-008-kurrentdb-rename.md`
      FR-063, SC-010.

- [x] **T128** RED + T129 GREEN `[P]` — `docs/glossary.md`
      Scope: every term used in any README MUST resolve here — Fixture, Rig, Contract, Stampede, Backplane, IsolationKey, Sender, Listener, RigConnect, ParallelIsolationContract, QuirkTests, EventSender, OutboxRelaySimulator, etc. FR-064.

- [x] **T130** RED + T131 GREEN `[P]` — `docs/troubleshooting.md`
      Consolidated; leaf READMEs link to provider-specific subsections. FR-064.

- [x] **T132** RED + T133 GREEN `[P]` — `docs/performance-tuning.md`
      When to use which cache / storage / db provider for which test scenario. FR-064.

- [x] **T134** RED + T135 GREEN `[P]` — `docs/migration-001-to-004.md`
      Version-upgrade path 001 → 002 → 003 → 004 (notably KurrentDb rename). FR-064.

- [x] **T136** GREEN-only `[P]` — `docs/third-party-notices.md`
      Enumerate every NuGet dependency's licence per R14 research. Dual-use for downstream due-diligence. Recommended even though MIT doesn't require NOTICE.

### Phase 6c — Per-project README rewrites (per-family batches) `[depends: T123d]`

Each family is one PR: RED commit lands template-only READMEs with `## Quick start` holding a `// TODO: runnable snippet` placeholder → GREEN commit populates every section with provider-specific research AND removes that family's entries from `ReadmeCompletenessTests` skip list.

**Revised per analysis #2/#3**: since T123c moved the Markdig gate to Phase 6a, each Phase 6c RED commit now *genuinely fails* the tightened gate (template-only READMEs have placeholder Options-table rows that fail the reflection check, and `// TODO` Quick-start placeholders fail Section 6's required content). Previously (pre-revision) Phase 6c RED commits were secretly-green against the legacy `> 100 chars` gate. Each per-family GREEN commit now also trims the matching providers from the skip list, so by the time Phase 6d T157 runs there are zero skips to empty.

- [x] **T137** RED + T138 GREEN — 12 missing READMEs for base / meta packages
      Files:
        `src/Rig.TUnit/README.md`
        `src/Rig.TUnit.All/README.md`
        `src/Rig.TUnit.Ci/README.md`
        `src/Rig.TUnit.Core/README.md`
        `src/Rig.TUnit.Grpc/README.md`
        `src/Rig.TUnit.Mediator/README.md`
        `src/Rig.TUnit.Microservices/README.md` (base)
        `src/Rig.TUnit.Microservices.Contracts/README.md`
        `src/Rig.TUnit.Microservices.Saga/README.md`
        `src/Rig.TUnit.Parallelism/README.md`
        `src/Rig.TUnit.Storage/README.md` (base)
        `src/Rig.TUnit.WebAPI/README.md`
      Meta-package variant per Documentation-Audit §3.2 (sections 9/10/12 may be `## §N — N/A`). FR-065.

- [x] **T139** RED + T140 GREEN `[P]` — SQL family (6 READMEs)
      Files: `src/Rig.TUnit.Databases.Sql/README.md` (base) + `Databases.Sql.{MySql,Oracle,Postgresql,SqlServer,Sqlite}/README.md`. Capture EF-provider compat matrix, AUTO_INCREMENT / PL-SQL / schema quirks, Pomelo EF10 pin.

- [x] **T141** RED + T142 GREEN `[P]` — NoSQL family (8 READMEs)
      Files: `Databases.NoSql/` (base) + `.{Cassandra,Cosmos,Dynamo,ElasticSearch,KurrentDb,Mongo,Redis}/README.md`. RU charges, keyspace-per-test, stream semantics, GSI via LocalStack.

- [x] **T143** RED + T144 GREEN `[P]` — Caching family (5)
      Files: `Caching/` (base) + `.{Fusion,Hybrid,Memory,Redis}/README.md`. Cache-vs-KV Redis split rationale, backplane, fail-safe / eager-refresh.

- [x] **T145** RED + T146 GREEN `[P]` — Messaging family (6)
      Files: `Messaging/` (base) + `.{Kafka,Nats,RabbitMq,ServiceBus,Sqs}/README.md`. Listener/sender lifecycle, W3C traceparent, dead-letter, ordering.

- [x] **T147** RED + T148 GREEN `[P]` — Microservices family (7)
      Files: `Microservices.{EventSourcing,Inbox,Outbox,Snapshots}` + recover `Contracts`, `Saga` if not covered by T137. Exactly-once, CAS contention, snapshot scrubbing.

- [x] **T149** RED + T150 GREEN `[P]` — Security family (5)
      Files: `Security/` (base) + `.{Jwt,Mtls,OAuth,Policies}/README.md`. Kid rotation, negative builders, JWKS lifecycle.

- [x] **T151** RED + T152 GREEN `[P]` — Observability family (7)
      Files: `Observability/` (base) + `.{AppInsights,Logging,Logging.Analyzers,Metrics,Seq,Tracing}/README.md`. `TagCardinalityGuard`, `ActivitySource` lifecycle, snapshot capture.

- [x] **T153** RED + T154 GREEN `[P]` — Storage family (5)
      Files: `Storage.{AzureBlob,FileSystem,MinIO,S3}/README.md` (base already covered by T137). SAS builders, path sandbox.

- [x] **T155** RED + T156 GREEN `[P]` — Cross-cutting (~7)
      Files: `Rig.TUnit.{Concurrency,Docker,HealthChecks,Http,Resilience}/README.md`, `Microservices` meta if not covered.

### Phase 6d — Gate tightening + verification `[depends: T137-T156]`

- [x] **T157** RED — residual skip markers assertion
      **Repurposed per analysis #2/#3**: the Markdig rewrite moved to T123c (Phase 6a). T157 now enforces the final-emptiness invariant.
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`
      Change: add a guard `[Test]` `ReadmeCompletenessTests_SkipList_MustBeEmpty` that enumerates the rule's own skip list via reflection and asserts length = 0.
      Expected: **RED** — skip list still holds entries for any family whose Phase 6c GREEN commit forgot to trim its entries. Running `dotnet test … --filter FullyQualifiedName~SkipList_MustBeEmpty` returns non-zero until every family is cleaned.

- [x] **T158** GREEN — final skip-list cleanup `[depends: T157]`
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs`
      Change: remove the last residual `[Category("SkipUntilFixed")]` markers (if any slipped past Phase 6c); confirm the list is empty.
      Expected: **GREEN** — T157 guard test passes; rule enforces uniformly on every src README. Satisfies FR-066, FR-069, SC-005.

- [x] **T159** RED + T160 GREEN `[P]` — `markdown-link-check` CI step
      File: `.github/workflows/ci.yml` (add job `markdown-link-check` using `gaurav-nelson/github-action-markdown-link-check@v1`; configure retry-on-flake allow-list for known-flaky domains e.g. kurrent.io).
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/MarkdownLinkCheckJobTests.cs` asserts the job is present. FR-067.

- [x] **T161** RED + T162 GREEN `[P]` — `snippet-extraction` CI job (path-filtered per C-004)
      File: `.github/workflows/ci.yml` (add job `snippet-extraction` with `paths: [src/**/*.cs, src/**/README.md, docs/templates/PROVIDER_README_TEMPLATE.md]`).
      Content: extract every affected README's `## Quick start` fenced-code `csharp` block into `./snippet-scratch/<readme-path-hash>.cs`; wire a scratch csproj; `dotnet build`; non-zero exit fails job.
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/SnippetExtractionJobTests.cs`. FR-068.

**Phase 6 exit gate (SC-005, SC-008, SC-009, SC-010):**

- [x] **T163** Verification (GREEN only)
      Asserts: `grep -rn "SkipUntilFixed" tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` → 0; all 63 READMEs pass tightened gate; root governance files present; `docs/*` all present; every `markdown-link-check` run GREEN; `snippet-extraction` GREEN on a src-touching test PR.

---

## Phase 7 — CI Hardening (branch: `feat/005-a-legacy-coverage-and-tests`, post-Phase-5)

**Goal:** every rule, coverage, benchmark, and commit-discipline check enforced on every PR.

- [x] **T164** RED + T165 GREEN — dedicated `architecture-tests` CI job
      File: `.github/workflows/ci.yml` (new job runs `dotnet test tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj -c Release --no-build`; no `--filter Category!=SkipUntilFixed` because every marker is gone).
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/ArchitectureTestsJobTests.cs`. FR-070.

- [x] **T166** RED + T167 GREEN — `benchmark-regression` CI job `[depends: T068]`
      File: `.github/workflows/ci.yml` (new job runs `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --exporters json --artifacts ./benchmark-results`; Python/bash compare step against `benchmarks/baseline-005.json` with 20 % threshold; paths filter `[src/**/*.cs, tests/Rig.TUnit.Benchmarks/**, Directory.Packages.props]`).
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/BenchmarkRegressionJobTests.cs`. FR-071, SC-017.

- [x] **T168** RED + T169 GREEN — harden `commit-discipline-gate` (full subject-pair walk) `[depends: T008]`
      File: `.github/workflows/ci.yml` (strengthen existing Phase-1-minimal job):
        walk `git log master..HEAD --pretty=format:"%H %s"`;
        for each `feat(005): T<nnn> — GREEN …` commit, assert the immediately-preceding commit is `test(005): T<nnn> — RED …` with the same `T<nnn>`;
        retroactive exemption: hardcoded `EXEMPT_SHAS=("2b149b2")` and nothing else;
        fail PR if any `src/`-touching commit lacks a preceding matching RED.
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/CommitDisciplineGateTests.cs`. FR-002, SC-011, SC-016.

- [x] **T170** RED + T171 GREEN — `red-commit-verification` CI step `[depends: T169]`
      File: `.github/workflows/ci.yml` (add step inside `commit-discipline-gate`, Bash script per [research.md R10](research.md)):
        for each RED commit, `git worktree add` at that SHA;
        extract touched test projects from diff;
        `dotnet test` on those projects with `--filter "Category!=Integration&Category!=Benchmark"`;
        assert non-zero exit (the RED genuinely failed).
      Test: `tests/Rig.TUnit.Architecture.Tests/Rules/RedCommitVerificationStepTests.cs`. FR-003.

- [x] **T172** RED + T173 GREEN `[P, optional]` — convert `build-unit-arch` pwsh loop
      File: `.github/workflows/ci.yml` (if MTP now supports `--filter Category!=Integration` on the MTP runner, replace the pwsh loop with `dotnet test Rig.TUnit.slnx --filter Category!=Integration`; else port loop to Bash).
      Verify during implementation. FR-073.

- [x] **T174** GREEN only — full-gate-set CONTRIBUTING.md
      File: `CONTRIBUTING.md` (extend from T121 with every gate: coverage threshold + contract suite + benchmark regression + commit-discipline + architecture-tests + test-category-completeness + markdown link-checker + snippet-extraction).
      Commit: `docs(005): T174 — GREEN CONTRIBUTING full gate set (FR-074)`. FR-074, SC-019.

**Phase 7 exit gate (SC-011, SC-016, SC-017, SC-019):**

- [x] **T175** Verification (GREEN only)
      Asserts: CI has `architecture-tests + benchmark-regression + commit-discipline-gate + red-commit-verification + coverage-summary + snippet-extraction + markdown-link-check` jobs all non-bypassable on PR; `benchmarks/baseline-005.json` is the active reference; `CONTRIBUTING.md` documents every gate.

---

## Feature close (final PR on `master`)

- [ ] **T176** Merge 005-a `[depends: T175]`
      Action: open PR `feat/005-a-legacy-coverage-and-tests → master`. `commit-discipline-gate` + `red-commit-verification` + `architecture-tests` + `benchmark-regression` + `coverage-summary` MUST all be GREEN. Merge.

- [ ] **T177** Merge 005-b `[depends: T163]`
      Action: open PR `feat/005-b-docs-parity → master`. Same gate set. Merge.

- [~] **T178** Final audit + tag (no code) — **audit done, tag deferred to post-merge**
      Asserts:
        `grep -rn "SkipUntilFixed" tests/` returns 0.
        Final test count > 1264 (post-004 baseline) — `dotnet test Rig.TUnit.slnx` summary.
        `/dotnet-ai-kit:review` returns PASS (or documented advisories).
      Commit: `chore(005): tag v005` (annotated tag).

      2026-04-20 audit pass:
        - `grep -rnE '\[Category\(.*SkipUntilFixed.*\)\]' tests/**/*.cs` → 0 active markers
          (20 `SkipUntilFixed` occurrences under `tests/` are in the architecture-rule data
          structures themselves: NoSkipMarkersTests, ProviderCompletenessTests,
          ReadmeCompletenessTests, TestCompletenessTests, TestFileOrganizationTests.
          No `[Category("SkipUntilFixed")]` attribute applied to any test method anywhere.)
        - `tests/Rig.TUnit.Architecture.Tests` 33/33 pass (was 30/30 pre-Phase-6c;
          added T123c/T123d + SkipList_MustBeEmpty guard).
        - All 63 canonical provider READMEs pass the Markdig structural gate
          (`SkipUntilFixed` array is empty; `ReadmeCompletenessTests_SkipList_MustBeEmpty`
          guard GREEN).
        - Feature branch `feat/005-a-legacy-coverage-and-tests` at 150 commits,
          clean working tree.
      The `chore(005): tag v005` annotated tag is deferred until T176 + T177 merge to
      master — tagging before merge would annotate a transient branch SHA.

---

## FR → Task matrix (post-analyze revision)

| FR | Primary task(s) | SC |
|---|---|---|
| FR-001 RED/GREEN cadence | All `T` tasks (non-`A` non-`chore`) | SC-011 |
| FR-002 commit-discipline gate | T008, T168–T169 | SC-011, SC-016 |
| FR-003 red-commit-verification | T170–T171 | SC-011 |
| FR-004 No new skip markers | **T104b/T104c** (NoSkipMarkersTests enforcement) | SC-012 |
| FR-005 Retire inherited skips | T021+, T071+, Phase 6c family GREENs, T158 | SC-002/003/004/005 |
| FR-006 No partial fill-in | T020..T069 | SC-002 |
| FR-007 No regressions | all (CI pass) | SC-015 |
| FR-010 Postgres flake | T003–T004 | SC-001 |
| FR-011 Shared-fixture audit | A005, T066–T067, **T104b/T104c** (SharedFixtureGuardTests) | SC-013 |
| FR-012 Orphan delete | T001–T002 | SC-014 |
| FR-013 Artefact upload | T006–T007 | SC-018 |
| FR-014 10 green runs | T008 | SC-001 |
| FR-020 --coverage flag | T010–T011 | SC-006 |
| FR-021 coverage-summary | T012–T013 | SC-006, SC-018 |
| FR-022 threshold block | T014–T015 (collect, non-blocking), **T069b (flip to blocking at Phase 3 close)** | SC-006 |
| FR-023 coverage baseline | T016 | SC-006 |
| FR-024 CONTRIBUTING coverage | T017, T121, T174 | SC-019 |
| FR-030..FR-035 Test-category fill-in | T020–T065 | SC-002, SC-006 |
| FR-036 Empty TestCompleteness skip | T021, T023, T025, T027, T029, T031+ | SC-002 |
| FR-037 Benchmark baseline | T068 | SC-007 |
| FR-038 Coverage-lifting tests | incidentally in T021+ | SC-006 |
| FR-040..FR-042 Canonical layout | T070–T103 | SC-003 |
| FR-043 Empty ProviderCompleteness skip | T071+ | SC-003 |
| FR-050..FR-052 Test hygiene | T105–T118 | SC-004 |
| FR-053 Empty TestFileOrganization skip | T106+ | SC-004 |
| FR-060 Root governance | T120–T121 | SC-008 |
| FR-061..FR-062 Template + QUALITY-BAR | T122–T123 | SC-010 |
| FR-063 ADRs | T126–T127 | SC-010 |
| FR-064 Supporting docs | T128–T136 | SC-010 |
| FR-065 63 READMEs | T137–T156 | SC-009 |
| FR-066 Markdig parser | **T123b/T123c (Markdig rewrite, moved to Phase 6a)**, T123d (interim skip), T157–T158 (final cleanup) | SC-005 |
| FR-067 Markdown link check | T159–T160 | SC-005 |
| FR-068 Snippet extraction | T161–T162 | SC-005 |
| FR-069 Empty Readme skip | Phase 6c family GREENs (per-family trim), T157–T158 (final residual sweep) | SC-005 |
| FR-070 Architecture-tests job | T164–T165 | SC-016 |
| FR-071 Benchmark regression | T166–T167 | SC-017 |
| FR-072 Hardened commit-discipline | T168–T169 | SC-011, SC-016 |
| FR-073 pwsh→Bash loop | T172–T173 | — |
| FR-074 Full CONTRIBUTING | T174 | SC-019 |

**Changes from initial generation (per `/analyze` findings):**

- **A005** (was T005) — renamed to the `A` audit namespace to make FR-001 scope explicit (analysis #7).
- **T069b** (new) — threshold flip moved from end of Phase 2 to end of Phase 3 (analysis #4).
- **T104b + T104c** (new) — `NoSkipMarkersTests` + `SharedFixtureGuardTests` fill the FR-004 / SC-013 enforcement gap (analysis #5 + #6).
- **T123b + T123c + T123d** (new, in Phase 6a) — Markdig pin + rewrite + interim skip expansion, moved from Phase 6d so Phase 6c RED commits genuinely fail (analysis #2 + #3 + #9).
- **T157 + T158** — repurposed to final residual cleanup (the Markdig rewrite moved to Phase 6a); each Phase 6c family GREEN now trims its own skip-list entries (analysis #2 + #3).

---

## Summary (revised 2026-04-19 post-analyze)

**Total**: 182 task IDs — 1 audit (A005) + 181 code/CI tasks (T001–T178).
**RED/GREEN pairs**: 91; plus 14 verification/baseline/merge/chore singletons.
**Parallel opportunities**: 64 tasks marked `[P]` — dense parallelism within Phase 3 groups (P0 foundation, P1 utilities, etc.) and Phase 6c family batches.
**Estimated PRs**: ~31 (Phase 1: 1, Phase 2: 1, Phase 3: 5 per-group + close-out, Phase 4: 5 per-family + enforcement, Phase 5: 1 bundled, Phase 6a: 1 (now includes Markdig rewrite), Phase 6b: 1, Phase 6c: 10 per-family, Phase 6d: 1 (final cleanup only), Phase 7: 1, close-out: 2).
**Total estimated effort**: ~22–32 working days across the two parallel branches (005-a ~13–19 days, 005-b ~10–14 days).

**Net delta from `/analyze` revisions**: +4 task IDs (A005 renamed from T005; T069b, T104b, T104c, T123b, T123c, T123d added; T157/T158 repurposed). No functional scope expansion — every new task addresses a validated consistency gap, not new feature work.

---

## Next

```
/dotnet-ai-kit:analyze    # cross-check spec ↔ plan ↔ tasks for consistency
/dotnet-ai-kit:implement  # execute tasks starting with T001
```
