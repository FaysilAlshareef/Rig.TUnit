# CI Refactor Plan

Phase E — concrete edits to `.github/workflows/ci.yml`. Targets ~40 % wall-clock reduction
on a typical PR.

---

## 1 · Goals

| Today | Target |
|---|---|
| ~30 min wall-clock for a full PR | ~18 min |
| Every matrix cell does `dotnet restore Rig.TUnit.slnx` (full graph) | One warmup restore + matrix cells download artefact and `--no-restore --no-build` |
| Re-runs of in-progress runs accumulate | Concurrency group cancels older runs |
| Every PR runs every provider matrix even for docs-only changes | `paths-filter` skips matrices whose source/tests didn't change |
| Two link-checkers (markdown-link-check + lychee) | Lychee only |
| `architecture-tests` is a separate runner with its own restore | Folded into `build-unit-arch` |
| `red-commit-verification` only emits notices (no real check) | Deleted (commit-discipline-gate already covers RED→GREEN pairing) |

---

## 2 · Top-of-file additions

```yaml
name: CI

on:
  push:
    branches: [master, main]
  pull_request:
    branches: [master, main]

# NEW — cancel in-flight runs for the same ref when a new push lands.
concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read
  pull-requests: read
  checks: write
```

---

## 3 · New `paths-filter` job at the top

Drives `if:` conditions on every matrix.

```yaml
jobs:
  changes:
    name: Detect changed paths
    runs-on: ubuntu-latest
    outputs:
      src:           ${{ steps.filter.outputs.src }}
      docs:          ${{ steps.filter.outputs.docs }}
      sql:           ${{ steps.filter.outputs.sql }}
      nosql:         ${{ steps.filter.outputs.nosql }}
      messaging:     ${{ steps.filter.outputs.messaging }}
      caching:       ${{ steps.filter.outputs.caching }}
      microservices: ${{ steps.filter.outputs.microservices }}
      security:      ${{ steps.filter.outputs.security }}
      observability: ${{ steps.filter.outputs.observability }}
      storage:       ${{ steps.filter.outputs.storage }}
      core:          ${{ steps.filter.outputs.core }}
      ci:            ${{ steps.filter.outputs.ci }}
    steps:
      - uses: actions/checkout@v5
      - uses: dorny/paths-filter@v3
        id: filter
        with:
          filters: |
            src:           [ 'src/**', 'tests/**', 'Directory.Build.props', 'Directory.Packages.props', 'src/Directory.Build.props', 'tests/Directory.Build.props' ]
            docs:          [ '**/*.md', 'docs/**', 'planning/**', '.dotnet-ai-kit/**' ]
            sql:           [ 'src/Rig.TUnit.Databases.Sql*/**', 'tests/Rig.TUnit.Databases.Sql*/**' ]
            nosql:         [ 'src/Rig.TUnit.Databases.NoSql*/**', 'tests/Rig.TUnit.Databases.NoSql*/**' ]
            messaging:     [ 'src/Rig.TUnit.Messaging*/**', 'tests/Rig.TUnit.Messaging*/**' ]
            caching:       [ 'src/Rig.TUnit.Caching*/**', 'tests/Rig.TUnit.Caching*/**' ]
            microservices: [ 'src/Rig.TUnit.Microservices*/**', 'tests/Rig.TUnit.Microservices*/**' ]
            security:      [ 'src/Rig.TUnit.Security*/**', 'tests/Rig.TUnit.Security*/**' ]
            observability: [ 'src/Rig.TUnit.Observability*/**', 'tests/Rig.TUnit.Observability*/**' ]
            storage:       [ 'src/Rig.TUnit.Storage*/**', 'tests/Rig.TUnit.Storage*/**' ]
            core:          [ 'src/Rig.TUnit/**', 'src/Rig.TUnit.Core*/**', 'src/Rig.TUnit.Concurrency/**', 'src/Rig.TUnit.Docker/**', 'src/Rig.TUnit.HealthChecks/**', 'src/Rig.TUnit.Parallelism/**', 'src/Rig.TUnit.Resilience/**', 'src/Rig.TUnit.Ci/**', 'src/Rig.TUnit.Grpc/**', 'src/Rig.TUnit.Http/**', 'src/Rig.TUnit.WebAPI/**', 'src/Rig.TUnit.Mediator/**', 'tests/Rig.TUnit.*.Tests.*/**' ]
            ci:            [ '.github/workflows/**', '.github/actions/**' ]
```

Then every matrix gates on its own slice:

```yaml
  integration-sql:
    needs: [changes, warmup]
    if: needs.changes.outputs.sql == 'true' || needs.changes.outputs.ci == 'true' || needs.changes.outputs.src == 'true'
    # ...
```

(The `src` fallback ensures `src/Rig.TUnit.Core/**` changes still trigger every matrix —
core touches everything.)

---

## 4 · `warmup` job — single restore + build

```yaml
  warmup:
    name: Warmup (restore + build)
    needs: changes
    if: needs.changes.outputs.src == 'true' || needs.changes.outputs.ci == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: ./.github/actions/setup-dotnet-cache
      - name: Restore
        run: dotnet restore Rig.TUnit.slnx
      - name: Build (Release)
        run: dotnet build Rig.TUnit.slnx -c Release --no-restore
      - name: Pack obj/+bin/ artefact
        run: |
          tar --use-compress-program=zstdmt -cf warmup.tar.zst \
            $(find . -type d \( -name obj -o -name bin \) -not -path '*/node_modules/*')
      - uses: actions/upload-artifact@v4
        with:
          name: warmup-build
          path: warmup.tar.zst
          retention-days: 1
          if-no-files-found: error
```

Matrix jobs consume it:

```yaml
  integration-sql:
    needs: [changes, warmup]
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        provider: [SqlServer, Sqlite, Postgresql, MySql, Oracle]
    steps:
      - uses: actions/checkout@v5
      - uses: ./.github/actions/setup-dotnet-cache
      - uses: actions/download-artifact@v4
        with: { name: warmup-build }
      - name: Unpack warmup
        run: tar --use-compress-program=zstdmt -xf warmup.tar.zst
      - name: Pull provider image (warm cache)
        if: matrix.provider == 'MySql' || matrix.provider == 'Oracle'
        run: |
          case "${{ matrix.provider }}" in
            MySql)  docker pull mysql:8.4 ;;
            Oracle) docker pull gvenzl/oracle-free:23.5-slim-faststart ;;
          esac
      - name: Run integration
        run: |
          dotnet test --project tests/Rig.TUnit.Databases.Sql.${{ matrix.provider }}.Tests.Integration/Rig.TUnit.Databases.Sql.${{ matrix.provider }}.Tests.Integration.csproj \
            --no-build -c Release \
            -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ github.job }}-${{ matrix.provider }}
          path: tests/**/bin/Release/net10.0/TestResults/**
          retention-days: 14
          if-no-files-found: warn
```

Same shape applies to `integration-nosql`, `integration-caching`, `integration-messaging`,
`integration-microservices`, `integration-security`, `integration-observability`,
`integration-storage`, `integration-core`.

---

## 5 · `build-unit-arch` — fold in architecture tests

```yaml
  build-unit-arch:
    name: Build + Unit + Arch
    needs: [changes, warmup]
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v5
      - uses: ./.github/actions/setup-dotnet-cache
      - uses: actions/download-artifact@v4
        with: { name: warmup-build }
      - name: Unpack warmup
        shell: pwsh
        run: tar --use-compress-program=zstdmt -xf warmup.tar.zst
      - name: Test (unit + arch + contract)
        shell: pwsh
        run: |
          $projects = Get-ChildItem tests -Recurse -Filter "*.csproj" |
              Where-Object { $_.Name -notmatch "Integration|Benchmarks|Tests\.Contract" }
          $failed = @()
          foreach ($p in $projects) {
              Write-Host "::group::$($p.Name)"
              dotnet test --project $p.FullName --no-build -c Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
              if ($LASTEXITCODE -ne 0) { $failed += $p.Name }
              Write-Host "::endgroup::"
          }
          if ($failed.Count -gt 0) {
              Write-Error "Failed projects: $($failed -join ', ')"
              exit 1
          }
      - name: Architecture tests
        run: dotnet test --project tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj --no-build -c Release
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ github.job }}
          path: tests/**/bin/Release/net10.0/TestResults/**
          retention-days: 14
          if-no-files-found: warn
```

Delete the standalone `architecture-tests` job.

---

## 6 · `pack-validate` job (new — Phase E-9)

```yaml
  pack-validate:
    name: Pack & validate
    needs: [changes, warmup]
    if: needs.changes.outputs.src == 'true' || needs.changes.outputs.ci == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with: { fetch-depth: 0 }   # MinVer
      - uses: ./.github/actions/setup-dotnet-cache
      - uses: actions/download-artifact@v4
        with: { name: warmup-build }
      - name: Unpack warmup
        run: tar --use-compress-program=zstdmt -xf warmup.tar.zst
      - name: Pack
        run: dotnet pack Rig.TUnit.slnx -c Release --no-build -o ./artifacts
      - name: Validate metadata
        shell: bash
        run: |
          set -euo pipefail
          shopt -s nullglob
          missing=0
          for pkg in artifacts/*.nupkg; do
            nuspec=$(unzip -p "$pkg" '*.nuspec')
            for tag in description authors projectUrl repository readme icon license; do
              if ! grep -qi "<$tag" <<<"$nuspec"; then
                echo "::error::$pkg missing <$tag>"
                missing=$((missing + 1))
              fi
            done
          done
          [[ $missing -eq 0 ]] || exit 1
      - uses: actions/upload-artifact@v4
        with:
          name: pack-artefacts
          path: artifacts/*.{nupkg,snupkg}
          retention-days: 7
          if-no-files-found: error
```

---

## 7 · Coverage summary — fan-in unchanged shape, simpler dependencies

```yaml
  coverage-summary:
    name: Coverage summary
    runs-on: ubuntu-latest
    if: always()
    needs:
      - build-unit-arch
      - integration-sql
      - integration-nosql
      - integration-caching
      - integration-messaging
      - integration-microservices
      - integration-security
      - integration-observability
      - integration-storage
      - integration-core
    # body unchanged from current ci.yml — Python threshold script kept verbatim
```

---

## 8 · Removals

Delete these jobs from `ci.yml`:

- `red-commit-verification` (no-op — only emits notices).
- `markdown-link-check` (the gaurav-nelson one). Keep `linkcheck` (lychee).
- The standalone `architecture-tests` job (now folded into `build-unit-arch`).

---

## 9 · `linkcheck` — keep, generalise

Was scoped to `README.md` only; broaden to all docs.

```yaml
  linkcheck:
    name: Link checker
    runs-on: ubuntu-latest
    needs: changes
    if: needs.changes.outputs.docs == 'true' || needs.changes.outputs.ci == 'true'
    steps:
      - uses: actions/checkout@v5
      - uses: lycheeverse/lychee-action@v2
        with:
          args: |
            --verbose --no-progress
            --exclude 'https://www\.nuget\.org/packages/Rig\.TUnit'
            --exclude 'https://github\.com/FaysilAlshareef/Rig\.TUnit/(actions|releases|discussions|issues)'
            'README.md' 'CHANGELOG.md' 'CONTRIBUTING.md' 'SECURITY.md'
            'docs/**/*.md' 'src/**/README.md' 'planning/**/*.md'
          fail: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## 10 · `commit-discipline-gate` — keep as-is

The Phase 1 minimal pairing check is fine. No changes needed.

---

## 11 · `snippet-extraction` — path-gate

```yaml
  snippet-extraction:
    name: Snippet extraction
    needs: changes
    if: needs.changes.outputs.src == 'true' && github.event_name == 'pull_request'
    # body unchanged
```

---

## 12 · Final job graph after refactor

```
changes ─┬─► warmup ─┬─► build-unit-arch (Win) ──┐
         │           ├─► integration-sql ────────┤
         │           ├─► integration-nosql ──────┤
         │           ├─► integration-caching ────┤
         │           ├─► integration-messaging ──┤
         │           ├─► integration-microservices ┤
         │           ├─► integration-security ───┤
         │           ├─► integration-observability ┤
         │           ├─► integration-storage ────┤
         │           ├─► integration-core ───────┤
         │           ├─► pack-validate           │
         │           └─► snippet-extraction      │
         ├─► linkcheck                           │
         └─► commit-discipline-gate              │
                                                 ▼
                                          coverage-summary
```

`commit-msg-lint.yml` (separate workflow) is untouched.
`benchmark.yml` is untouched.
`codeql.yml`, `release.yml`, `release-drafter.yml`, `stale.yml` are added under their own
files, not interleaved with `ci.yml`.

---

## 13 · Verification post-refactor

1. Open a docs-only PR — only `linkcheck` + `commit-msg-lint` + `commit-discipline-gate`
   should run. Total time < 2 min.
2. Open a SQL-only PR — `warmup` + `integration-sql` + `build-unit-arch` + `pack-validate`
   + `coverage-summary` should run; nosql/messaging/caching/etc skipped.
3. Open a `src/Rig.TUnit.Core` PR — every matrix runs (core fallback in `paths-filter`).
4. Push two commits in quick succession to a PR branch — confirm the older run was
   cancelled by the concurrency group.
5. Inspect CI billing: ubuntu-runner minutes/month should drop ~30–40 %.
