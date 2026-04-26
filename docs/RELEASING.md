# Releasing Rig.TUnit

The release process is **tag-driven**. Pushing a `v*.*.*` tag to `master` triggers
[`.github/workflows/release.yml`](../.github/workflows/release.yml), which packs every
`src/**` project, validates metadata, pauses for owner approval at the `nuget-org`
environment, then publishes to nuget.org + GitHub Packages and creates a GitHub Release.

---

## One-time setup

Before the first release can succeed:

### 1. Register Trusted Publishing on nuget.org

1. Sign in to <https://www.nuget.org> as the package owner account.
2. Manage Account → **Trusted Publishing** → "Add new".
3. Fill in:

   | Field | Value |
   |---|---|
   | Publisher | GitHub Actions |
   | Repository owner | `FaysilAlshareef` |
   | Repository name | `Rig.TUnit` |
   | Workflow filename | `release.yml` |
   | Environment | `nuget-org` |
   | Package owner | (your nuget.org account) |
   | Package ID glob | `Rig.TUnit*` |

4. Submit. nuget.org reviews glob patterns; same-day approval is typical.
5. Confirm Status = "Active" before pushing the first tag.

### 2. Create the protected GitHub environment

```bash
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org \
  -F wait_timer=0 \
  -F prevent_self_review=false \
  -F deployment_branch_policy[protected_branches]=false \
  -F deployment_branch_policy[custom_branch_policies]=true

USER_ID=$(gh api user --jq .id)
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org \
  -F "reviewers[][type]=User" \
  -F "reviewers[][id]=$USER_ID"

gh api -X POST repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org/deployment-branch-policies \
  -f name=master -f type=branch
gh api -X POST repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org/deployment-branch-policies \
  -f name='v*' -f type=tag
```

---

## Cutting a release

1. Confirm `master` is green:
   ```bash
   gh run list --branch master --limit 1
   ```
2. Update `CHANGELOG.md`:
   - Rename `[Unreleased]` → `[N.N.N] - YYYY-MM-DD`.
   - Insert a fresh empty `[Unreleased]` heading above it.
   - Update comparison links at the bottom.
3. Commit:
   ```bash
   git commit -am "release: vN.N.N"
   git push origin master
   ```
4. Wait for CI green on the release commit, then tag:
   ```bash
   git tag -a vN.N.N -m "vN.N.N"
   git push origin vN.N.N
   ```
5. Watch the run at `gh run list --workflow release.yml`. When it pauses at
   "Publish to nuget.org" (environment `nuget-org`), open the run, click
   **Review deployments**, approve.
6. After the run completes, verify:
   - <https://www.nuget.org/packages/Rig.TUnit/N.N.N> renders with README, icon, repo link
   - The GitHub Release was created with `.nupkg` and `.snupkg` assets attached
   - GitHub Packages mirror succeeded
7. Announce on Discussions (Show and Tell category).

## Pre-release / rehearsal

- For pre-1.0 betas, use a semver pre-release suffix: `vN.N.N-beta.M`.
- For a dry-run that publishes only to GitHub Packages, push the tag and trigger
  `release.yml` via `workflow_dispatch` with `skip_nuget=true`.

## If a release fails mid-flight

| Failure point | Recovery |
|---|---|
| `pack` job fails | Investigate, push a fix to `master`, delete and re-create the tag at the new SHA |
| `publish-nuget` fails (e.g. duplicate version) | Bump the patch suffix; rerun |
| Owner approval is rejected | Cancel the run; delete the tag; investigate; retry |
| `github-release` fails after nuget.org publish | The package is live; manually create the GitHub Release using the tag and the artefacts from the run summary |

**Never delete a published nuget.org version.** Use Unlisted on nuget.org if a version
must be hidden, then ship a corrected `+1` patch.

---

## Versioning (MinVer)

Versions are derived from the latest `v*` git tag using [MinVer](https://github.com/adamralph/minver):

- Tag `v0.1.0` → package `0.1.0`
- Tag `v0.1.0-beta.1` → package `0.1.0-beta.1`
- Untagged build (CI on `master` between releases) → `0.1.0-alpha.0.{height}.{sha}`

To preview the next version locally:
```bash
dotnet tool install -g minver-cli
minver -t v -d alpha
```
