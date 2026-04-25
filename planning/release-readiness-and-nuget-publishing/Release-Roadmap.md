# Release Readiness — Roadmap

**Branch**: `chore/release-readiness-nuget-0.1.0-beta.1`
**Total effort**: ~7 working hours across 7 phases.
**Delivery mode**: each phase is one PR onto `planning`, squash-merged after CI green.

---

## Phase A — Repository settings & community files

**Goal**: a stranger landing on the repo sees a complete OSS project: description, topics,
contribution path, code of conduct, issue forms, PR template, sponsorship link.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| A1 | Set repo description, homepage, topics; enable Discussions; disable Wiki | `gh repo edit` (no file) | 5 min |
| A2 | Enable secret scanning + push protection + Dependabot alerts | `gh api` calls | 5 min |
| A3 | Add `CODEOWNERS` (`* @FaysilAlshareef`) | `.github/CODEOWNERS` | 5 min |
| A4 | Add Contributor Covenant 2.1 | `CODE_OF_CONDUCT.md` | 5 min |
| A5 | Add PR template — checklist mirroring `CONTRIBUTING.md` | `.github/PULL_REQUEST_TEMPLATE.md` | 15 min |
| A6 | Issue forms: bug / feature / provider request / docs | `.github/ISSUE_TEMPLATE/*.yml` + `config.yml` | 30 min |
| A7 | Discussion templates: Q&A / Show & Tell / Ideas | `.github/DISCUSSION_TEMPLATE/*.yml` | 15 min |
| A8 | `FUNDING.yml` (GitHub Sponsors only — leave others empty) | `.github/FUNDING.yml` | 5 min |
| A9 | Apply full label set (28 labels) | `gh label create` script | 15 min |
| A10 | `dependabot.yml` — weekly NuGet (grouped) + GitHub Actions | `.github/dependabot.yml` | 10 min |
| A11 | `stale.yml` — close issues idle 90 d, PRs idle 30 d | `.github/workflows/stale.yml` | 10 min |

**Phase A exit gate**: `gh repo view` shows description + 8+ topics; `.github/` populated; first
Dependabot run scheduled.

**Effort total**: ~2 h.

---

## Phase B — Branch protection ruleset

**Goal**: `master` cannot be merged into without owner approval + green required checks.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| B1 | Apply repository ruleset on `refs/heads/master` (see [Branch-Protection-Ruleset.md](Branch-Protection-Ruleset.md)) | `gh api` POST | 10 min |
| B2 | Apply tag protection on `refs/tags/v*` (only owner can push tags) | `gh api` POST | 5 min |
| B3 | Verify required status checks reference the **exact** workflow job names emitted post-CI-refactor | manual: open a draft PR, confirm checks list | 10 min |
| B4 | Document required-checks list in `CONTRIBUTING.md` | `CONTRIBUTING.md` | 5 min |

**Phase B exit gate**: a test PR with one failing required check cannot be merged; force-push to
`master` is rejected; pushing tag `v0.0.0-test` from a non-admin token is rejected.

**Effort total**: ~30 min.

**Order**: Apply **after** Phase E so the required-status-check names match the refactored CI.

---

## Phase C — Packaging metadata sweep

**Goal**: every `src/**/*.csproj` packs into a deterministic, fully-described, Source-Linked
NuGet package.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| C1 | Extend root `Directory.Build.props` with `Authors`, `RepositoryUrl`, `PackageProjectUrl`, `PackageLicenseExpression`, `PackageReadmeFile`, `PackageIcon`, `PackageTags`, deterministic build flags, `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`, `EmbedUntrackedSources=true`, `IsPackable=false` (default off) | `Directory.Build.props` | 30 min |
| C2 | Add `src/Directory.Build.props` overriding `IsPackable=true`; pulls in shared NuGet README + icon via `<None Include … Pack=true />` | `src/Directory.Build.props` (new) | 15 min |
| C3 | Drop NuGet README + icon under a known path | `docs/nuget/README.md`, `docs/nuget/icon.png` (256×256 PNG) | 20 min |
| C4 | Add MinVer + `Microsoft.SourceLink.GitHub` central versions | `Directory.Packages.props` | 10 min |
| C5 | Per-project `<Description>` audit — add a one-liner to every src csproj that lacks one (see [NuGet-Package-Metadata-Audit.md](NuGet-Package-Metadata-Audit.md)) | `src/**/*.csproj` (~50 files) | 90 min |
| C6 | Mark test/benchmark projects `<IsPackable>false</IsPackable>` explicitly under `tests/Directory.Build.props` (defence-in-depth) | `tests/Directory.Build.props` (new) | 10 min |
| C7 | Local pack rehearsal — `dotnet pack Rig.TUnit.slnx -c Release -o ./artifacts` produces ≥ 60 `.nupkg` and ≥ 60 `.snupkg`, zero `NU5*` warnings | local | 20 min |
| C8 | Inspect a pack manually — extract `Rig.TUnit.0.0.0-alpha.0.{height}.{sha}.nupkg`, verify nuspec contains description, repo URL, license, README, icon, source link metadata | local | 15 min |

**Phase C exit gate**: `dotnet pack` clean; nuspec inspection shows complete metadata; meta-packages
(`Rig.TUnit`, `Rig.TUnit.All`) pack with zero source files but full transitive dependency lists.

**Effort total**: ~3.5 h.

---

## Phase D — Release pipeline

**Goal**: tag push `v*.*.*` runs an approval-gated workflow that publishes to nuget.org via
Trusted Publishing + GitHub Packages mirror + creates a GitHub Release.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| D1 | One-time: register repo as a Trusted Publisher on nuget.org → `Rig.TUnit*` glob, environment `nuget-org` | nuget.org account UI | 15 min |
| D2 | Create protected GitHub environment `nuget-org` with reviewer = `@FaysilAlshareef`, deploy branches limited to `master` + tags `v*` | `gh api` PUT environment | 5 min |
| D3 | Author `release.yml` (see [Release-Workflow-Spec.md](Release-Workflow-Spec.md)) with jobs: `pack` → `publish-nuget` (gated) → `publish-ghpackages` → `github-release` | `.github/workflows/release.yml` | 45 min |
| D4 | Add reusable composite action `setup-dotnet-cache` (used by both ci.yml and release.yml) | `.github/actions/setup-dotnet-cache/action.yml` | 20 min |
| D5 | Smoke-test by pushing tag `v0.0.0-rehearsal.1` (publishes only to GitHub Packages, manual flag in workflow) | local + GitHub | 20 min |
| D6 | Document the release ritual in `docs/RELEASING.md` (tag push, approval, post-release verification) | `docs/RELEASING.md` (new) | 20 min |

**Phase D exit gate**: rehearsal tag publishes to GH Packages; workflow run shows the `nuget-org`
environment paused awaiting approval; cancel the run, delete the tag — confirms the approval gate
holds.

**Effort total**: ~2 h (excluding wall-clock on nuget.org Trusted-Publisher review, typically same-day).

---

## Phase E — CI refactor

**Goal**: leaner CI — NuGet cache, warmup-then-fanout, concurrency cancellation, path-filtered
matrices. Targets ~40% wall-clock reduction.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| E1 | Add concurrency group at workflow scope: `group: ci-${{ github.ref }}, cancel-in-progress: true` | `.github/workflows/ci.yml` | 5 min |
| E2 | Replace per-job `actions/setup-dotnet` block with the new `setup-dotnet-cache` composite (drops 30 s × N jobs) | `ci.yml` | 20 min |
| E3 | Introduce a `warmup` job that runs full `dotnet restore + build -c Release`, uploads `artefacts-warmup` artefact (obj/, bin/) — matrix jobs `download-artifact` and `--no-restore --no-build` | `ci.yml` | 45 min |
| E4 | Per-matrix `dorny/paths-filter` so e.g. `integration-sql` only runs when `src/Rig.TUnit.Databases.Sql.**` or `tests/Rig.TUnit.Databases.Sql.**` change | `ci.yml` | 30 min |
| E5 | Drop duplicate link-checker — keep `lycheeverse/lychee-action`, delete `gaurav-nelson/github-action-markdown-link-check` | `ci.yml` | 5 min |
| E6 | Fold `architecture-tests` into `build-unit-arch` (single runner, single restore) | `ci.yml` | 15 min |
| E7 | Either implement real verification in `red-commit-verification` (checkout RED SHA, run touched arch tests, expect non-zero) **or** delete the no-op job — owner choice. Default: delete and rely on `commit-discipline-gate` | `ci.yml` | 15 min |
| E8 | Path-gate `snippet-extraction` via `paths-filter` (only when `src/**/*.cs` or `src/**/README.md` change) | `ci.yml` | 10 min |
| E9 | Add a new `pack-validate` job: `dotnet pack Rig.TUnit.slnx -c Release -o ./artifacts` + scan `.nupkg` for missing description/license/readme — fails PR if a packable project regresses on metadata | `ci.yml` | 30 min |
| E10 | Update `Required status checks` list (Phase B-3) to match the new job names | repo settings | 5 min |

**Phase E exit gate**: a typical full-PR run drops from ~30 min to ~18 min (measured against the
last 5 PRs on `planning`); a docs-only PR runs only `markdown-link-check` + `commit-msg-lint` in
< 2 min.

**Effort total**: ~3 h.

---

## Phase F — Security & hygiene workflows

**Goal**: weekly SAST + auto-merge bot for trivial Dependabot updates.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| F1 | Add `codeql.yml` — language `csharp`, scheduled weekly, on PR | `.github/workflows/codeql.yml` | 15 min |
| F2 | Add `release-drafter.yml` — drafts release notes from merged PR labels (used by `release.yml` body) | `.github/workflows/release-drafter.yml`, `.github/release-drafter.yml` config | 25 min |
| F3 | Optional: `auto-merge.yml` — auto-merges Dependabot patch/minor PRs once required checks pass | `.github/workflows/auto-merge.yml` | 15 min |
| F4 | Verify GitHub Security tab shows: code-scanning alerts (CodeQL), Dependabot alerts, secret-scanning alerts (zero on first run) | manual | 5 min |

**Phase F exit gate**: CodeQL job runs once on merge to `master`, surfaces zero P0 findings;
release-drafter populates draft release notes from labels on the next merge.

**Effort total**: ~1 h.

---

## Phase G — First release `v0.1.0-beta.1`

**Goal**: cut the first public NuGet drop.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| G1 | Land all changes from Phases A–F to `master` via squash merge (one squash per phase = 6 commits) | n/a | n/a |
| G2 | Update `CHANGELOG.md` — move "Unreleased" → "[0.1.0-beta.1] — YYYY-MM-DD" using exact text from [CHANGELOG-Update.md](CHANGELOG-Update.md) | `CHANGELOG.md` | 15 min |
| G3 | Create draft GitHub Release using [Release-Notes-v0.1.0-beta.1.md](Release-Notes-v0.1.0-beta.1.md) as body | GitHub UI | 10 min |
| G4 | Push tag — `git tag -a v0.1.0-beta.1 -m "0.1.0-beta.1" && git push origin v0.1.0-beta.1` | local | 2 min |
| G5 | `release.yml` runs → pack → pause at `nuget-org` environment → owner approves → publishes to nuget.org → mirror to GH Packages → finalize draft release | GitHub UI | 15 min wall-clock |
| G6 | Post-publish verification: `https://www.nuget.org/packages/Rig.TUnit/0.1.0-beta.1` page renders README, icon, repo link, MIT, Source Link green | manual | 10 min |
| G7 | Dogfood: `dotnet new tunit -n Smoke && cd Smoke && dotnet add package Rig.TUnit --prerelease && dotnet add package Rig.TUnit.Databases.Sql.Sqlite --prerelease && dotnet test` | local | 20 min |
| G8 | Announce on Discussions ("Show and Tell") — link the release, the README quick-start, and the issue tracker for feedback | GitHub Discussions | 15 min |
| G9 | Tweet/Bluesky/Mastodon (optional) | external | 10 min |

**Phase G exit gate**: package live on nuget.org, smoke project consumes it and runs a green test
hitting a real Sqlite container, 0 issues opened in first 24 h means launch is clean.

**Effort total**: ~1.5 h.

---

## Cross-phase order of operations

```
A (settings + community)          ─┐
                                   ├──► merge to master ──► E (CI refactor)
C (packaging sweep)               ─┘                        │
                                                            ▼
                                                  B (branch protection w/ correct check names)
                                                            │
                                                            ▼
                                                            D (release.yml + Trusted Publishing)
                                                            │
                                                            ▼
                                                            F (CodeQL + release-drafter)
                                                            │
                                                            ▼
                                                            G (cut v0.1.0-beta.1)
```

A & C can land in either order, both can ship before E. B **must** wait for E (so required-check
names match). D depends on C (packaging works) + B (tag protection on `v*`). F is independent —
land any time after A. G is the gated final step.

---

## Per-phase commit message style

Following the existing convention (`feat(NNN):`, `chore:`, `docs:`):

```
chore(release): Phase A — repo settings & community files
chore(release): Phase B — apply master branch protection
chore(release): Phase C — packaging metadata sweep
chore(release): Phase D — release.yml + Trusted Publishing
chore(release): Phase E — CI refactor (cache, warmup, path filters)
chore(release): Phase F — CodeQL + release-drafter
release: v0.1.0-beta.1
```

The final commit (G2) updating `CHANGELOG.md` is `release: v0.1.0-beta.1` — the tag points at
that commit's SHA.
