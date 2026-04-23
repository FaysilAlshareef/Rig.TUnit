# CI Pipeline Gap Audit — Feature 006

**Audit date**: 2026-04-21  
**Source file**: `.github/workflows/ci.yml`  
**Auditor**: coverage-scan CI run `24712477011`

---

## Gap 1 — Six integration-test projects never run in production CI

### Current state

The `integration-core` matrix in `ci.yml` (line 294) is:

```yaml
area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]
```

The `build-unit-arch` job (PowerShell) explicitly filters out all `*Integration*` projects with
`-notmatch "Integration|Benchmarks|Contract"`.

Six integration-test projects that exist on disk are therefore **never executed on any CI push or
pull request**:

| Missing project | Test path | Impact |
|----------------|-----------|--------|
| `Rig.TUnit.Core.Tests.Integration` | `tests/Rig.TUnit.Core.Tests.Integration/` | Core builder, `RigConnect`, `IsolationKey` integration paths |
| `Rig.TUnit.Ci.Tests.Integration` | `tests/Rig.TUnit.Ci.Tests.Integration/` | `CoverageDeltaEnforcer`, `FlakyQuarantine`, `TrxEnricher` integration |
| `Rig.TUnit.Grpc.Tests.Integration` | `tests/Rig.TUnit.Grpc.Tests.Integration/` | `GrpcClientHelper`, `EndpointMappingStartupFilter` — both 0 % in scan |
| `Rig.TUnit.Http.Tests.Integration` | `tests/Rig.TUnit.Http.Tests.Integration/` | `CapturedRequest`, `NoopHandler` — both 0 % in scan |
| `Rig.TUnit.WebAPI.Tests.Integration` | `tests/Rig.TUnit.WebAPI.Tests.Integration/` | All pass (100 %) but only because scan branch included them |
| `Rig.TUnit.Mediator.Tests.Integration` | `tests/Rig.TUnit.Mediator.Tests.Integration/` | All pass (100 %) but not in production CI |

### Fix

Extend the `integration-core` matrix in `ci.yml`:

```yaml
# BEFORE
area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]

# AFTER
area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience, Core, Ci, Grpc, Http, WebAPI, Mediator]
```

The step template already uses `${{ matrix.area }}` as a suffix so the existing step bodies require
no changes.

**Task**: T001 (Phase 1)  
**Owner**: CI / DevOps  
**Effort**: < 1 hour  
**Risk**: `Rig.TUnit.Grpc.Tests.Integration` and `Rig.TUnit.Http.Tests.Integration` may have
failing tests once added; coverage work (T010–T020) should be done first or these can be added with
`continue-on-error: true` as a temporary gate.

---

## Gap 2 — Coverage gate disabled with no re-enable date

### Current state

`ci.yml` line 363:

```yaml
- name: Check coverage gate
  continue-on-error: true   # warn-only since 2026-04-20
  run: |
    ...
```

The inline comment records the disable date but no re-enable condition.  Without this gate any PR
can silently regress coverage — the gate never blocks a merge.

### Fix

Remove `continue-on-error: true` **once every failing package reaches its gate**.
Feature 006 Phase 5 (T090) is the gate-hardening task.

As an interim measure (Phase 1, T002), add a comment with the specific feature and task that will
re-enable it:

```yaml
- name: Check coverage gate
  continue-on-error: true   # Disabled 2026-04-20; re-enabled by feat/006 T090
  run: |
    ...
```

**Task**: T002 (Phase 1 — comment), T090 (Phase 5 — remove `continue-on-error`)  
**Owner**: CI  
**Effort**: T002 < 15 min; T090 depends on all coverage tasks completing

---

## Gap 3 — No coverage artifact retention for trend analysis

### Current state

Coverage artefacts (`coverage.cobertura.xml`) are uploaded via `actions/upload-artifact@v4` with
`retention-days: 14`.  There is no consolidated history across runs, no GitHub Pages badge, and no
trend chart.

### Fix (Phase 3, T080)

After the gate is hardened, publish a `ReportGenerator` HTML + badge artifact to GitHub Pages
(or as a PR comment via `actions/github-script`) so coverage history is visible without running the
`ci/coverage-scan` branch manually.

Options (in priority order):

1. **PR comment** — use `marocchino/sticky-pull-request-comment` to post the MarkdownSummaryGithub
   report as a PR comment; zero infrastructure cost.
2. **GitHub Pages** — publish the `HtmlSummary` report to `gh-pages` branch after each `master`
   merge.
3. **Third-party badge** — Codecov / Coveralls free tier; adds an external dependency.

Preferred: option 1 for PRs + option 2 for main-branch history.

**Task**: T080 (Phase 3)

---

## Gap 4 — Benchmark regression job can never block a merge

### Current state

`.github/workflows/ci.yml` benchmark regression step (lines 646–682) uses:

```bash
python3 check_regression.py || echo "::warning::Benchmark regression detected (non-blocking)"
```

The `|| echo` swallows the non-zero exit code.  Combined with `benchmarks/baseline-005.json` being
empty (`"benchmarks": {}`), the regression detector has never had data to compare against and the
job can never fail.

### Fix

See `Benchmark-Remediation-Plan.md` for the full plan.  CI fix (T050) removes the `|| echo`
guard **after** the baseline is populated (T051).

---

## Summary table

| Gap | Severity | Phase | Tasks | Effort |
|-----|----------|-------|-------|--------|
| 6 missing integration-test projects | High | 1 | T001 | < 1 h |
| Coverage gate disabled indefinitely | High | 1 (comment) / 5 (enforce) | T002 / T090 | 15 min / gated |
| No coverage trend/history | Low | 3 | T080 | 2–4 h |
| Benchmark regression never blocks | Medium | 2 | T050–T051 | 2 h |
