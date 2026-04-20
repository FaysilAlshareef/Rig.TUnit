# Undo Log: 005-legacy-coverage-and-docs-parity

All file creations and modifications performed by `/dotnet-ai-kit:implement`.
Ordered chronologically; used by `--resume` to find the last failed task.

---

## T001 — RED: add OrphanFolderTests
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs`

**RED evidence**: ran `dotnet test --treenode-filter /*/*/OrphanFolderTests/*` locally — 3 tests failed because stale `bin/` / `obj/` artefacts leftover on the dev machine kept the orphan folders physically present.

**Note on RED fidelity**: on a fresh CI clone, the orphan folders do NOT exist (they were removed from the tree by `feat(003)` — commit `529451d`). The RED commit therefore fails locally but would pass on a clean checkout. T001 is a regression guard — not a discovery of current tree violation. Phase 7's `red-commit-verification` (T170/T171) will need to exempt this SHA or treat pre-hardening commits per the exemption list documented in T169.

## T002 — GREEN: confirm orphan folders absent
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- removed (locally, not in git): leftover `bin/` / `obj/` artefacts at:
  - `src/Rig.TUnit.ServiceBus/` (bin, obj)
  - `tests/Rig.TUnit.ServiceBus.Tests.Integration/` (obj)
  - `tests/Rig.TUnit.SqlServer.Tests.Integration/` (bin, obj)
- created: `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/undo-log.md`

**GREEN evidence**: `dotnet test --treenode-filter /*/*/OrphanFolderTests/*` → `Passed: 3`.

**Note**: no `git rm` was required — those paths held no tracked files at `HEAD`. `feat(003)` (529451d) performed the logical deletion. T002's commit exists to (a) establish the undo-log file and (b) document the stale-bin/obj cleanup for audit.

Satisfies FR-012, SC-014.

## T003 — RED: deterministic Postgres schema-visibility assertion
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK (compile verified; runtime verification deferred to CI — Docker Desktop Linux engine is down on this workstation)

- modified: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`
  - added `UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable` assertion that queries
    `information_schema.tables` for foreign `samples_*` entries.
  - tightened `SampleEntity` to use `private set` + factory ctor per architecture rule.

**RED evidence**: cannot produce locally — no Docker engine. Commit builds; CI Postgres matrix run will observe the deterministic failure.

## T004 — GREEN: per-test ephemeral Postgres database
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK (compile verified; runtime verification deferred to CI)

- created: `src/Rig.TUnit.Databases.Sql.Postgresql/Helpers/PostgresDbContextHelper.cs`
  - `CreateEphemeralDatabaseAsync(string, CancellationToken) -> EphemeralDatabase`
  - `EphemeralDatabase` disposes via `pg_terminate_backend` + `DROP DATABASE`.
- modified: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration/UsePostgresFluentTests.cs`
  - all 3 tests now acquire their own ephemeral DB from the shared container.

**GREEN evidence**: compile-clean (`dotnet build` → 0 errors). CI Postgres job validates schema-isolation under parallel load; first run will also serve as the 10-green requirement for T008.

Satisfies FR-010, SC-001.

## A005 — Shared-fixture audit inventory (GREEN-only, audit namespace)
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- created: `planning/post-005-phase-1/SharedFixture-Audit.md`

20 fixtures inventoried — 7 safe (IsolationKey-based consumers), 12 unsafe (Phase 3 T066 conversion), 1 stopgap (Kafka listener subset). No RED/GREEN partner per analysis #7 (A-prefix audit-namespace tasks exempt from FR-001).

Satisfies FR-011, SC-013.

## T006 — RED: ArtifactUploadTests (YamlDotNet-based YAML assertion)
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK (RED confirmed locally)

- modified: `Directory.Packages.props` — added `YamlDotNet 16.3.0` pin (MIT)
- modified: `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` — added YamlDotNet reference
- created: `tests/Rig.TUnit.Architecture.Tests/Rules/ArtifactUploadTests.cs`

**RED evidence**: `dotnet test --treenode-filter /*/*/ArtifactUploadTests/*` → `Failed: 1`. Offender list enumerated 10 jobs — every one missing `actions/upload-artifact@v4`.

## T007 — GREEN: add upload-artifact step to every CI job (+ Phase-1 commit-discipline-gate)
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- modified: `.github/workflows/ci.yml`
  - 10 jobs gain `Upload test artifacts` step (actions/upload-artifact@v4, `if: always()`, `retention-days: 14`, matrix-aware `name`, glob `path: tests/**/bin/Release/net10.0/TestResults/**`).
  - new `commit-discipline-gate` job runs on `pull_request` — walks commits, asserts every GREEN subject has a matching RED predecessor.
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ArtifactUploadTests.cs` — added `commit-discipline-gate` to `ExemptJobs` (meta-job, no test output).

**GREEN evidence**: full architecture test suite → `total: 23, failed: 0, succeeded: 23`.

Satisfies FR-013, SC-018 (paired with T006). Partial FR-002 (Phase-1 minimal subject-pair check; full hardening lands T168–T171).

## T010-T011 — coverage flag assertion + implementation
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageCollectionTests.cs`
- modified: `.github/workflows/ci.yml` — all 9 integration-* `dotnet test` steps now pass `-- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`

Satisfies FR-020.

## T012-T013 — coverage-summary job assertion + implementation
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageSummaryJobTests.cs`
- modified: `.github/workflows/ci.yml` — new `coverage-summary` job (download artefacts, ReportGenerator Html+Cobertura+MarkdownSummaryGithub, $GITHUB_STEP_SUMMARY, 30-day upload)
- modified: `tests/Rig.TUnit.Architecture.Tests/Rules/ArtifactUploadTests.cs` — added `coverage-summary` to `ExemptJobs` (intentional 30-day retention).

Satisfies FR-021, SC-018.

## T014-T015 — threshold step assertion + implementation (non-blocking)
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK

- created: `tests/Rig.TUnit.Architecture.Tests/Rules/CoverageThresholdTests.cs`
- modified: `.github/workflows/ci.yml` — `coverage-summary` gains a Python threshold step (`line-rate ≥ 0.90`, `branch-rate ≥ 0.85`) with `continue-on-error: true`.

Partial FR-022 (non-blocking). T069b at Phase 3 close flips `continue-on-error` to `false`.

## T016 — coverage baseline schema (user-handoff for first CI run data)
**Timestamp**: 2026-04-20
**Repo**: primary
**Status**: OK (schema stub; awaits CI artefact download)

- created: `benchmarks/coverage-baseline-005.json`

**Handoff**: after T013/T015 run on CI, download the `coverage-report` artefact, extract per-package `line-rate` / `branch-rate` from `Cobertura.xml`, and write them into `providers: { <name>: { line_rate, branch_rate } }`.

Satisfies FR-023.
