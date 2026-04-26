# Release Readiness & NuGet Publishing

**Status**: Planning
**Proposed branch**: `chore/release-readiness-nuget-0.1.0-beta.1`
**Target release**: `v0.1.0-beta.1` (first public NuGet drop)
**Owner**: @FaysilAlshareef
**Decisions locked**:
- Versioning — **MinVer** (tag-driven)
- Initial version — **0.1.0-beta.1**
- NuGet publishing — **Trusted Publishing via OIDC** (no API-key secret)
- Meta-packages — **Keep both** `Rig.TUnit` and `Rig.TUnit.All`
- Discussions — **Enable now**
- Required reviewers — **1 (@FaysilAlshareef)**
- Default branch — **`master`** (no rename)

---

## Mission

Take the repository from "internal pre-release" to "open-source, NuGet-publishable, security-hardened" in one coordinated landing. After this work:

1. Anyone landing on the GitHub repo sees a clear description, topics, code of conduct, contribution path, issue/PR templates, security policy, and labels.
2. `master` is protected — only PRs with owner approval + green required-status-checks + signed commits can merge.
3. Every production project (`src/**`) packs into a deterministic, Source-Linked NuGet package with full metadata (description, tags, repo URL, README, icon, symbols).
4. Tag push `v*.*.*` triggers a release pipeline that validates, packs, waits for owner approval, then publishes to **nuget.org via Trusted Publishing** + GitHub Packages mirror + creates a GitHub Release with auto-generated notes.
5. CI is leaner — NuGet cache, single-warmup restore, concurrency cancellation, path-filtered matrices — targeting ~40% wall-clock reduction.
6. Dependabot, CodeQL, secret-scanning push protection, and a stale-issue bot all run on a schedule.

---

## Non-goals

- **No API changes.** This feature is purely build/publish/governance. The public surface area at `0.1.0-beta.1` is whatever is currently on `master`.
- **No `master` → `main` rename.** Locked per decision; revisit at `1.0`.
- **No multi-org / multi-maintainer setup.** Single owner today; scale later.
- **No SBOM / Sigstore / package signing.** Future feature once the release flow is stable.
- **No `master`-branch rewriting.** All work lands as additive PRs to `planning` then squashes to `master`.

---

## Document index

| File | Purpose |
|---|---|
| [Release-Roadmap.md](Release-Roadmap.md) | The phased task list (Phases A–G), per-phase exit gates, files touched, effort estimates |
| [Release-Notes-v0.1.0-beta.1.md](Release-Notes-v0.1.0-beta.1.md) | The body of the GitHub Release — what shipped, install commands, known limits, links |
| [CHANGELOG-Update.md](CHANGELOG-Update.md) | The exact `[0.1.0-beta.1]` section to insert into `CHANGELOG.md` when the tag is cut |
| [Branch-Protection-Ruleset.md](Branch-Protection-Ruleset.md) | JSON payload + `gh api` invocation for the `master` ruleset; tag protection for `v*` |
| [NuGet-Package-Metadata-Audit.md](NuGet-Package-Metadata-Audit.md) | Per-csproj audit table: which projects need `<Description>`, current state, target one-liner |
| [Repository-Settings.md](Repository-Settings.md) | All `gh repo edit` / `gh api` settings calls (description, topics, labels, security flags) |
| [Release-Workflow-Spec.md](Release-Workflow-Spec.md) | Full `release.yml` spec, secrets/environments, Trusted-Publishing setup runbook |
| [CI-Refactor-Plan.md](CI-Refactor-Plan.md) | Concrete edits to `ci.yml`: NuGet cache, warmup job, path filters, composite action |

---

## Out-of-scope follow-ups (track separately)

- **Sigstore-signed packages** — wait until `1.0`, depends on nuget.org Trusted Publishing being stable for several releases.
- **Per-package README sync** — every leaf provider already has its own `README.md`; future feature wires `<PackageReadmeFile>` to that local file instead of the shared root README.
- **Snapshot / API-baseline diff** — `<EnablePackageValidation>` + `<PackageValidationBaselineVersion>` flips on at `0.2.0` once `0.1.0` ships.
- **Renaming `master` → `main`** — defer to `1.0`.
- **Co-maintainer onboarding** — defer until first external contributor lands a non-trivial PR.

---

## Exit criteria for this feature

- [ ] `gh repo view` shows non-empty description, ≥ 8 topics, discussions enabled
- [ ] `gh api repos/.../branches/master/protection` returns 200 with required reviews + status checks
- [ ] `master` cannot be force-pushed or deleted
- [ ] `dotnet pack Rig.TUnit.slnx -c Release` produces ≥ 60 `.nupkg` + matching `.snupkg` with no `NU5*` warnings
- [ ] `release.yml` dry-run on a `v0.1.0-beta.1-rc1` tag publishes to GitHub Packages (rehearsal); rerun with real tag publishes to nuget.org
- [ ] `Rig.TUnit` package page on nuget.org shows the README, icon, repo link, MIT license, Source Link verified
- [ ] Dependabot opens its first weekly PR within 7 days of merge
- [ ] CodeQL "C# code scanning" badge is green on the README

---

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Trusted Publishing OIDC misconfigured → first tag publish fails | Rehearse on a throwaway pre-tag (`v0.0.0-rehearsal.1`) against GitHub Packages first; only flip nuget.org publish on once rehearsal is green |
| Branch-protection rules block the very PR that introduces the rules | Apply the ruleset **after** the metadata sweep PR merges; create an "admin override" allow-list for the owner during initial rollout, remove at `0.1.0` GA |
| `EnablePackageValidation=true` + no baseline = false positives | Set `<DisablePackageBaselineValidation>true</DisablePackageBaselineValidation>` for `0.1.0-beta.1`; introduce baseline at `0.1.0` GA |
| MinVer fails because tag history is shallow | `actions/checkout@v5` with `fetch-depth: 0` in the release job; verified locally via `dotnet minver` before tag push |
| Meta-package `Rig.TUnit.All` produces a 50-MB nupkg with redundant deps | Mark `<DevelopmentDependency>false</DevelopmentDependency>`; meta-packages contain zero source; transitive references resolve via NuGet at consumer build time, not by re-bundling |
| `dotnet format --verify-no-changes` regresses after metadata edits | Run `dotnet format` once after the csproj sweep; commit the diff in the same PR |
| First-time contributors hit the commit-discipline gate (RED→GREEN) on a one-line typo fix | Add a `docs:` / `chore:` exemption path in `commit-discipline-gate` that already exists; document in `CONTRIBUTING.md` |
