# Release Workflow Specification

Phase D — `.github/workflows/release.yml` and the NuGet **Trusted Publishing** (OIDC) setup.

---

## 1 · One-time setup on nuget.org

Before the first release can succeed, register Rig.TUnit as a Trusted Publisher.

1. Sign in to <https://www.nuget.org> as the package owner account.
2. Manage Account → **Trusted Publishing** → "Add new".
3. Fill in:

   | Field | Value |
   |---|---|
   | Publisher | GitHub Actions |
   | Repository owner | `FaysilAlshareef` |
   | Repository name | `Rig.TUnit` |
   | Workflow filename | `release.yml` |
   | Environment (optional but **recommended**) | `nuget-org` |
   | Package owner | (the nuget.org account) |
   | Package ID glob | `Rig.TUnit*` |

4. Click **Save**. nuget.org reviews glob patterns; same-day approval is typical.
5. Confirm by visiting Manage Account → Trusted Publishing — the entry must show
   "Status: Active". Publishing fails with `403` if status is "Pending".

---

## 2 · Protected GitHub environment

The `nuget-org` environment gates the publish job behind a manual approval.

```bash
# Create the environment
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org \
  -F wait_timer=0 \
  -F prevent_self_review=false \
  -F deployment_branch_policy[protected_branches]=false \
  -F deployment_branch_policy[custom_branch_policies]=true

# Add reviewer (owner)
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org \
  -F 'reviewers[][type]=User' \
  -F 'reviewers[][id]=YOUR_USER_ID'  # gh api user --jq .id

# Restrict deployments to master and v* tags
gh api -X POST repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org/deployment-branch-policies \
  -f name=master -f type=branch
gh api -X POST repos/FaysilAlshareef/Rig.TUnit/environments/nuget-org/deployment-branch-policies \
  -f name='v*' -f type=tag
```

---

## 3 · `release.yml` full specification

```yaml
name: Release

on:
  push:
    tags: ['v*.*.*', 'v*.*.*-*']
  workflow_dispatch:
    inputs:
      tag:
        description: 'Existing tag to publish (e.g. v0.1.0-beta.1)'
        required: true
      skip_nuget:
        description: 'Skip nuget.org publish (rehearsal mode)'
        required: false
        default: 'false'

permissions:
  contents: write    # GitHub Release creation
  packages: write    # GitHub Packages mirror
  id-token: write    # NuGet Trusted Publishing (OIDC)

concurrency:
  group: release-${{ github.ref }}
  cancel-in-progress: false

jobs:
  pack:
    name: Pack & validate
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.minver.outputs.version }}
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0          # MinVer needs full tag history
          ref: ${{ inputs.tag || github.ref }}
      - uses: ./.github/actions/setup-dotnet-cache
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore Rig.TUnit.slnx
      - name: Build
        run: dotnet build Rig.TUnit.slnx -c Release --no-restore
      - name: Resolve MinVer version
        id: minver
        run: |
          version=$(dotnet minver -t v -d alpha)
          echo "version=$version" >> "$GITHUB_OUTPUT"
          echo "Resolved package version: $version"
      - name: Pack
        run: |
          dotnet pack Rig.TUnit.slnx -c Release --no-build \
            -o ./artifacts \
            -p:Version=${{ steps.minver.outputs.version }}
      - name: Validate package metadata
        shell: bash
        run: |
          set -euo pipefail
          shopt -s nullglob
          missing=0
          for pkg in artifacts/*.nupkg; do
            nuspec=$(unzip -p "$pkg" '*.nuspec' | head -c 65536)
            for tag in description authors projectUrl licenseUrl repository readme icon; do
              if ! grep -qi "<$tag" <<<"$nuspec"; then
                # licenseUrl is deprecated in favour of license expression — accept either
                if [[ "$tag" == "licenseUrl" ]]; then
                  if grep -qi '<license' <<<"$nuspec"; then continue; fi
                fi
                echo "::error::$pkg missing <$tag>"
                missing=$((missing + 1))
              fi
            done
          done
          if [[ $missing -gt 0 ]]; then
            echo "::error::Found $missing metadata violations"
            exit 1
          fi
      - uses: actions/upload-artifact@v4
        with:
          name: nupkgs
          path: artifacts/*.{nupkg,snupkg}
          retention-days: 30
          if-no-files-found: error

  publish-nuget:
    name: Publish to nuget.org
    needs: pack
    runs-on: ubuntu-latest
    environment:
      name: nuget-org
      url: https://www.nuget.org/packages/Rig.TUnit/${{ needs.pack.outputs.version }}
    if: ${{ inputs.skip_nuget != 'true' }}
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nupkgs
          path: ./artifacts
      - uses: NuGet/login@v1
        id: nuget-login
        with:
          user: NuGetTrustedPublisher
      - name: Push to nuget.org
        run: |
          dotnet nuget push "artifacts/*.nupkg" \
            --api-key "${{ steps.nuget-login.outputs.NUGET_API_KEY }}" \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate

  publish-ghpackages:
    name: Mirror to GitHub Packages
    needs: pack
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nupkgs
          path: ./artifacts
      - run: |
          dotnet nuget push "artifacts/*.nupkg" \
            --api-key "${{ secrets.GITHUB_TOKEN }}" \
            --source "https://nuget.pkg.github.com/FaysilAlshareef/index.json" \
            --skip-duplicate

  github-release:
    name: Create GitHub Release
    needs: [pack, publish-nuget, publish-ghpackages]
    if: always() && needs.pack.result == 'success'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with:
          ref: ${{ inputs.tag || github.ref }}
      - uses: actions/download-artifact@v4
        with:
          name: nupkgs
          path: ./artifacts
      - name: Extract release notes from CHANGELOG
        id: extract
        shell: bash
        run: |
          version="${{ needs.pack.outputs.version }}"
          # Extract the section between "## [<version>]" and the next "## [" heading
          awk -v v="$version" '
            $0 ~ "^## \\[" v "\\]" { found=1; next }
            found && /^## \[/ { exit }
            found { print }
          ' CHANGELOG.md > release-body.md
          if [[ ! -s release-body.md ]]; then
            echo "::warning::No CHANGELOG section for $version — using auto-notes"
            echo "_Release notes auto-generated by GitHub_" > release-body.md
          fi
      - uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ inputs.tag || github.ref_name }}
          name: ${{ inputs.tag || github.ref_name }}
          body_path: release-body.md
          generate_release_notes: true
          files: |
            artifacts/*.nupkg
            artifacts/*.snupkg
          fail_on_unmatched_files: true
          prerelease: ${{ contains(needs.pack.outputs.version, '-') }}
```

---

## 4 · Composite action — `.github/actions/setup-dotnet-cache/action.yml`

Used by both `ci.yml` and `release.yml`.

```yaml
name: Setup .NET + cache
description: Sets up the .NET SDK and caches the NuGet HTTP cache + global-packages folder
inputs:
  dotnet-version:
    required: false
    default: '10.0.x'
runs:
  using: composite
  steps:
    - uses: actions/setup-dotnet@v5
      with:
        dotnet-version: ${{ inputs.dotnet-version }}
    - uses: actions/cache@v4
      with:
        path: |
          ~/.nuget/packages
          ~/.nuget/http-cache
        key: nuget-${{ runner.os }}-${{ hashFiles('Directory.Packages.props', '**/*.csproj') }}
        restore-keys: |
          nuget-${{ runner.os }}-
```

---

## 5 · Release ritual (`docs/RELEASING.md` content)

```markdown
# Releasing Rig.TUnit

The release process is **tag-driven**. Pushing a `v*.*.*` tag to `master` triggers
`.github/workflows/release.yml`, which packs every `src/**` project, validates metadata,
pauses for owner approval at the `nuget-org` environment, then publishes to nuget.org +
GitHub Packages and creates a GitHub Release.

## Prerequisites (one-time)

- nuget.org Trusted Publishing entry exists for this repo / `release.yml` /
  environment `nuget-org` / glob `Rig.TUnit*`.
- Owner is configured as the sole reviewer on the `nuget-org` GitHub environment.

## Cutting a release

1. Confirm `master` is green:
   ```bash
   gh run list --branch master --limit 1
   ```
2. Update `CHANGELOG.md`:
   - Rename `[Unreleased]` → `[N.N.N] — YYYY-MM-DD`.
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
5. Watch `release.yml` run. When it pauses at `Publish to nuget.org` (environment
   `nuget-org`), open the run, click **Review deployments**, approve.
6. After the run completes:
   - Verify <https://www.nuget.org/packages/Rig.TUnit/N.N.N> renders correctly.
   - Verify the GitHub Release was created with assets attached.
7. Announce on Discussions (Show and Tell category).

## Pre-release / rehearsal

- For pre-1.0 betas, use semver pre-release suffix: `vN.N.N-beta.M`.
- For a dry-run that publishes only to GitHub Packages, push a `v*` tag and trigger
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
```

---

## 6 · Smoke test plan (Phase D-5)

1. Create branch `chore/release-rehearsal`.
2. Push tag `v0.0.0-rehearsal.1` from local — pre-1.0 prerelease, skipped from nuget.org by
   `skip_nuget=true` via workflow_dispatch.
3. Workflow runs:
   - `pack` succeeds → uploads ~70 nupkgs.
   - `publish-nuget` is skipped (input flag).
   - `publish-ghpackages` succeeds → packages appear under `<https://github.com/FaysilAlshareef?tab=packages>`.
   - `github-release` creates a draft release with assets.
4. Inspect the rehearsal release; delete the tag + draft release + GitHub Packages
   versions when done.

If any step fails, fix in a PR, rebase the rehearsal tag onto the new HEAD, re-run.
