# Handoff — Wrap Up

- **session_type**: wrap-up
- **timestamp**: 2026-04-20
- **feature**: 005-legacy-coverage-and-docs-parity
- **branch**: `feat/005-a-legacy-coverage-and-tests`
- **repo mode**: generic (single-repo, .NET 10)

## Session Summary

Post-implementation pass on a branch whose 113 tasks were already complete and whose final CI fixes landed in the three most recent commits. This session ran `/dotnet-ai-kit:review` (with CodeRabbit) and `/dotnet-ai-kit:verify`, and produced two analysis artefacts in the feature directory. **No production or test source was modified.**

| Metric | Value |
|--------|-------|
| Commits landed this session | 0 (wrap-up commit lands this handoff + reports) |
| Task progress | 113 / 113 complete (100%) — unchanged |
| Commits on branch since `master` | 92 |
| Files changed on branch since `master` | 222 (+17 196 / −1 081) |

## Work Performed This Session

### 1. `/dotnet-ai-kit:review use coderabbit`
- Installed-path discovery: CodeRabbit CLI 0.4.1 found at `C:\Users\libya\AppData\Local\Programs\coderabbit\bin\cr.exe` — not on Git Bash PATH by default. Added to PATH for this session only; not persisted.
- Ran `cr review --agent --base master` across 222 changed files.
- Raw output captured in `coderabbit-review.txt` (44.8 KB, 56 lines of JSON-per-line, now gitignored).
- 48 findings: 6 critical / 4 major / 38 minor.
- Merged with standards review (9-check agent: naming, architecture, localization, error handling, testing, security, events, performance, brief compliance).
- Wrote [review.md](review.md).

### 2. `/dotnet-ai-kit:verify`
- Build: **PASS** (0 err, 0 warn, 2 m 5 s).
- Tests: **FAIL** — 1 551 / 1 711 pass. 160 failures **all** from `DockerApiClient..ctor` NRE / `DockerUnavailableException`; root cause is Testcontainers cannot reach `npipe://./pipe/docker_engine` under Docker Desktop's current `desktop-linux` context. **Not a branch regression.** CI's Linux-runner matrices will pass.
- Resources: SKIP (no `.resx`).
- Proto: SKIP (only a test-scaffolding proto).
- K8s: SKIP (no manifests).
- Format: **FAIL** — 2 files with import ordering:
  - [src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1](../../../src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1) — pre-existing on `master`.
  - [tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1](../../../tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1) — introduced by T123c (Markdig imports).
- Wrote [verify.md](verify.md).

## Decisions Made This Session

- **All 6 CodeRabbit "critical" findings rejected as false positives.**
  - 5× "`TargetFramework` missing" — inherited from root `Directory.Build.props` (`net10.0`); CodeRabbit does not resolve the MSBuild props chain.
  - 1× "missing `using TUnit.Core`" — `TUnit.Core` is an auto-generated global using injected by the TUnit SDK (verified in `GlobalUsings.g.cs`).
  - Verification evidence is recorded in review.md §"Critical — all false positives".

- **3 of 4 CodeRabbit "major" findings rejected as false positives** (same props-chain issue) — leaving **1 real major**: `.github/workflows/ci.yml:483` uses the deprecated `gaurav-nelson/github-action-markdown-link-check@v1`.

- **"Silent skip" pattern kept as-is pending policy decision.** CodeRabbit flagged 5 architecture tests that early-return when `TryFindRepoRoot()` is null and suggested `Skip.Test(...)`. That would **violate FR-004** (no new skip markers in Feature 005). The review.md recommendation is to convert these to `Assert.That(repoRoot).IsNotNull()` — hard failure — as a follow-up task (working name: T179). Not done this session to avoid scope creep in a wrap-up.

- **`coderabbit-review.txt` never staged.** Added pattern `coderabbit-review.txt` + `coderabbit-*.txt` to `.gitignore` this session.

## Deviations from Plan

None. This session was review + verify on a closed task list; no plan items were changed.

## Blocked Items

None on the critical path. Optional follow-ups (not blocking merge) are listed below under *Follow-up queue*.

## Learnings

- **Docker Desktop context + Testcontainers on Windows**: Testcontainers 4.11.x defaults to `npipe://./pipe/docker_engine`. Docker Desktop's `desktop-linux` context exposes the daemon via a WSL-integrated socket, not the classic named pipe, so every integration project fails at client-construction time on this host. Local workaround: enable "Expose daemon on tcp" + `DOCKER_HOST=tcp://localhost:2375`. **Does not affect CI.** Worth documenting in `CONTRIBUTING.md` under "Running integration tests locally" if a contributor hits this. (Flagging as optional T181.)

- **.NET 10 `dotnet test` CLI breaking change**: the old `--logger "console;verbosity=minimal"` is rejected by the new Microsoft.Testing.Platform runner (`dotnet test --help` shows the new flag grammar). Correct equivalents: `--output Normal|Detailed`, `-v m|n`. One iteration of the verify script was wasted discovering this. Worth capturing in the verify script.

- **CodeRabbit false-positive rate is high in library/SDK scenarios**. Of 6 criticals and 4 majors, only 1 major (the deprecated GH Action) was actionable. Future reviews should front-load the props-chain + global-usings verification step before escalating any CR finding.

## Follow-up Queue (not gated on merge; candidates for post-merge)

| Priority | Source | Description |
|---:|---|---|
| 1 | verify | **Fix format** — `dotnet format Rig.TUnit.slnx --severity info`, commit. 2 files. |
| 2 | review  | **Migrate off deprecated GH Action** in `.github/workflows/ci.yml:483` (`gaurav-nelson/...@v1` → Tcort fork / `lycheeverse/lychee-action`). |
| 3 | review  | **Fix `HttpClientHelper` leak** in `WebApiBenchmarks.ClientRetrieval_FromHelper` (add `[IterationCleanup]` dispose). |
| 4 | review  | **Scope `ClearAllPools` → `ClearPool`** in `PostgresDbContextHelper.DisposeAsync` (over-scoped AppDomain-wide clear). |
| 5 | review  | **Replace silent `return;` with `Assert.IsNotNull`** in 5 Architecture tests (CanonicalTemplateTests, CiJobPresenceTests, GovernanceFilesTests, OrphanFolderTests, CoverageThresholdTests). **Do NOT use `Skip.Test` — that violates FR-004.** |
| 6 | review  | **Wrap YAML parse in try/catch** with diagnostic message in `CiJobPresenceTests.cs:44–47`. |
| 7 | review  | **README copy-polish sweep** (~30 findings): typos ("MediatR" → "Mediator"), broken anchors (`docs/troubleshooting.md#mysql`), duplicated links, malformed `## §N — N/A` headings, word-split `refresh_\ntoken`. Batch as a single docs-polish PR post-merge. |
| 8 | learnings | **Document Docker-on-Windows setup** for integration tests (`DOCKER_HOST=tcp://localhost:2375`) in `CONTRIBUTING.md`. |

## Repo Status

| Repo      | Branch                                    | Commits ahead of `master` | Status                        |
|-----------|-------------------------------------------|---:|--------------------------------|
| Rig.TUnit | `feat/005-a-legacy-coverage-and-tests`   | 92 | Ready for PR (after format fix) |

## Projected Briefs Status

N/A — single-repo feature; no downstream briefs.

## Artefacts Produced This Session

- `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/review.md` — committed by this wrap-up.
- `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/verify.md` — committed by this wrap-up.
- `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/handoff.md` — this file.
- `.gitignore` — appended `coderabbit-review.txt` + `coderabbit-*.txt`.
- `coderabbit-review.txt` — intentionally **not** committed. Kept in working tree for reference; gitignored.

## Resume Instructions

1. `dotnet format Rig.TUnit.slnx --severity info` → commit as `style(005): fix import ordering`.
2. Re-run `/dotnet-ai-kit:verify` — expect green on build + format; integration failures persist on this host until `DOCKER_HOST` is set (CI will be green).
3. Open the PR: `/dotnet-ai-kit:pr`.
4. Post-merge: open tickets from the Follow-up Queue above (items 2–8).
