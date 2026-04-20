# CI Artifact Upload + Coverage Proposal

**Date:** 2026-04-19
**Status:** proposal — not yet implemented
**Scope:** [.github/workflows/ci.yml](../../.github/workflows/ci.yml)

## Trigger

Every TUnit test run emits the same hint:

```
Tip: To enable automatic HTML report artifact upload, see
https://tunit.dev/docs/guides/html-report#enabling-automatic-artifact-upload
```

The reports are written but thrown away when the runner is destroyed. When a job fails (like the Postgres race on `9d3369f`), triage currently requires scraping raw logs via `gh run view --log-failed`. Uploading the HTML report + bundling coverage into the same artifact fixes this.

## Goals

1. **Every job uploads its TUnit HTML test report** as a GitHub Actions artifact, with explicit expiration.
2. **Every job also collects Cobertura coverage** and uploads it alongside the HTML report.
3. **One summary job merges every job's cobertura XML** into a single consolidated HTML coverage report for the whole run.
4. Artifacts survive failed jobs (so failing tests are the ones you most want to inspect).
5. Retention is bounded — no unlimited storage bloat.

## Background — TUnit / Microsoft.Testing.Platform specifics

- TUnit runs on Microsoft.Testing.Platform (MTP), **not VSTest**. `coverlet.msbuild /p:CollectCoverage=true` is **not supported** — this was already documented in Feature 004 [spec.md FR-036](../../.dotnet-ai-kit/features/004-provider-consistency-remediation/spec.md).
- Coverage is collected via the MTP-native flag `--coverage --coverage-output-format cobertura`, forwarded through `dotnet test` with a `--` separator. ([docs](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage))
- Output defaults to `<proj>/bin/<cfg>/net10.0/TestResults/*.cobertura.xml` and `*-report.html` — overridable via `--coverage-output` / `--results-directory`.
- `Microsoft.Testing.Extensions.CodeCoverage` is transitive via the `TUnit` package — no extra `PackageVersion` add needed.
- TUnit's native "automatic artifact upload" feature requires exposing `ACTIONS_RUNTIME_TOKEN` to the step. This is GitHub's internal token and its user-surface is undocumented/unstable — **prefer explicit `actions/upload-artifact@v4`** for predictability.

## Proposed change shape (illustrative — apply per-job)

### Per-job step pattern

Replace each matrix job's current single "Run integration" step:

```yaml
- name: Run integration
  run: dotnet test --project tests/Rig.TUnit.<X>.Tests.Integration/Rig.TUnit.<X>.Tests.Integration.csproj --no-build -c Release
```

with two steps — run with coverage, then upload:

```yaml
- name: Run integration
  run: >
    dotnet test
    --project tests/Rig.TUnit.<X>.Tests.Integration/Rig.TUnit.<X>.Tests.Integration.csproj
    --no-build -c Release
    --
    --coverage
    --coverage-output-format cobertura
    --coverage-output coverage.cobertura.xml

- name: Upload test artifacts
  if: always()                               # also upload on failure
  uses: actions/upload-artifact@v4
  with:
    name: test-results-${{ github.job }}-${{ matrix.<matrix-key> }}
    path: |
      tests/Rig.TUnit.<X>.Tests.Integration/bin/Release/net10.0/TestResults/**/*-report.html
      tests/Rig.TUnit.<X>.Tests.Integration/bin/Release/net10.0/TestResults/**/coverage.cobertura.xml
    retention-days: 14
    if-no-files-found: warn
```

### `build-unit-arch` job (pwsh loop)

The PowerShell loop already iterates csproj files. Adjust the inner `dotnet test` call and collect outputs at the end:

```pwsh
foreach ($p in $projects) {
    Write-Host "::group::$($p.Name)"
    dotnet test --project $p.FullName --no-build -c Release `
        -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
    if ($LASTEXITCODE -ne 0) { $failed += $p.Name }
    Write-Host "::endgroup::"
}
```

One upload at the end globs across every project:

```yaml
- name: Upload unit+arch artifacts
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-results-build-unit-arch
    path: |
      tests/**/bin/Release/net10.0/TestResults/**/*-report.html
      tests/**/bin/Release/net10.0/TestResults/**/coverage.cobertura.xml
    retention-days: 14
    if-no-files-found: warn
```

### New summary job — merged coverage

Append after all matrix jobs:

```yaml
coverage-summary:
  name: Coverage summary
  runs-on: ubuntu-latest
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
  if: always()                                  # run even if some matrix cells failed
  steps:
    - uses: actions/checkout@v5
    - uses: actions/setup-dotnet@v5
      with:
        dotnet-version: '10.0.x'
    - name: Download all test artifacts
      uses: actions/download-artifact@v4
      with:
        path: ./artifacts
        pattern: test-results-*
        merge-multiple: true
    - name: Install ReportGenerator
      run: dotnet tool install -g dotnet-reportgenerator-globaltool
    - name: Merge coverage into one HTML report
      run: >
        reportgenerator
        "-reports:./artifacts/**/coverage.cobertura.xml"
        "-targetdir:./coverage-report"
        "-reporttypes:Html;Cobertura;MarkdownSummaryGithub"
    - name: Publish summary to step summary
      if: always()
      run: cat ./coverage-report/SummaryGithub.md >> "$GITHUB_STEP_SUMMARY" || true
    - name: Upload merged coverage report
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: ./coverage-report
        retention-days: 30
        if-no-files-found: warn
```

## Artifact naming scheme

| Job | Artifact name |
|---|---|
| `build-unit-arch` | `test-results-build-unit-arch` |
| `integration-sql` | `test-results-integration-sql-<provider>` |
| `integration-nosql` | `test-results-integration-nosql-<provider>` |
| `integration-caching` | `test-results-integration-caching-<provider>` |
| `integration-messaging` | `test-results-integration-messaging-<provider>` |
| `integration-microservices` | `test-results-integration-microservices-<component>` |
| `integration-security` | `test-results-integration-security-<area>` |
| `integration-observability` | `test-results-integration-observability-<provider>` |
| `integration-storage` | `test-results-integration-storage-<provider>` |
| `integration-core` | `test-results-integration-core-<area>` |
| merged coverage (new) | `coverage-report` |

Each artifact contains both the TUnit HTML report(s) and the cobertura XML for that job.

## Retention policy

| Artifact | Retention |
|---|---|
| Per-job test results (HTML + cobertura) | **14 days** |
| Merged coverage report | **30 days** |

Rationale: 14 days covers standard triage / PR review cycle; 30 days on the merged report makes historical comparison easier. GitHub's org default is 90 — explicit shorter retention saves quota.

## Trade-offs & open questions

| Question | Options | Recommendation |
|---|---|---|
| Use TUnit's built-in `ACTIONS_RUNTIME_TOKEN` auto-upload? | (a) Yes — one less step. (b) No — use explicit `upload-artifact`. | **(b)** — explicit, stable, version-pinned action; matches how every other team documents it |
| Coverage threshold gate? | (a) Off this PR — just collect and publish. (b) Enforce 90/85 now. | **(a)** for this proposal. The gate is a separate concern (Feature 005 Phase 2, FR-035). Collecting first avoids "everything red on day one" |
| Upload on success AND failure? | (a) `if: always()`. (b) Only on failure. | **(a)** — coverage trend needs every successful run |
| Add JUnit XML too (for GitHub PR check annotations via `dorny/test-reporter`)? | (a) Yes — `--report-trx --report-trx-filename test.trx` and feed to test-reporter. (b) No — HTML is enough. | Defer; nice-to-have for a later iteration |
| `ReadWriteSummary` permission needed? | Yes to write `$GITHUB_STEP_SUMMARY` on forked PRs | Add `permissions: contents: read` at workflow level; the `contents: read` default plus the runner-provided `summary` permission is enough for same-repo PRs. For forked PRs GitHub already restricts this, so no extra config needed |

## Risks

- **Disk pressure on runner**: running cobertura collection on 45+ test projects inflates each project's `TestResults/` folder. Ubuntu runners have ~14 GB — well within limits for a single workflow run. No action needed.
- **Artifact quota**: GitHub caps free-tier artifact storage; 14-day retention plus 10 small artifacts per run is well under that cap.
- **MTP flag forwarding**: `dotnet test` passes args after `--` to the MTP runner. If a TUnit update ever stops accepting `--coverage` the build will warn, not silently drop coverage — acceptable failure mode.
- **Coverage on `build-unit-arch`**: if a pwsh-loop iteration fails, `$LASTEXITCODE` is captured but `continue`d; coverage from the failing project still writes to disk and uploads fine.

## Effort estimate

- Per-job edit (10 jobs): ~15 minutes each — 2.5 hours.
- Summary job: 30 minutes.
- Local dry-run on a throw-away branch: 30 minutes.
- Review + iteration: 1 hour.

**Total: ~5 hours.**

## Where this fits in the larger roadmap

This belongs to [Proposed-Feature-005-Roadmap.md](Proposed-Feature-005-Roadmap.md) **Phase 1 (CI stabilisation)** — items T004 (upload HTML report on failure) and **Phase 2 (coverage gate enforcement)** — items T010–T015. This document expands those tasks into concrete YAML.

## Open decisions for the owner

1. Retention: 14 days per-job + 30 days merged — OK?
2. Include TRX/JUnit output now or defer? (Enables PR check annotations via `dorny/test-reporter`.)
3. Enforce coverage threshold in the same change, or a follow-up?
4. Should coverage-summary run on PRs or only on `master` pushes? (Recommend PRs too — most valuable there.)
