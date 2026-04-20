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
