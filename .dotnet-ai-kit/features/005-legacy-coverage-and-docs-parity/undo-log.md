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
